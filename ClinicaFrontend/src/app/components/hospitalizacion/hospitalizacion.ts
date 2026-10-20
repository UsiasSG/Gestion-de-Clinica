import { Component, OnInit, computed, inject, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { Observable } from 'rxjs';
import { HospitalizacionService } from '../../services/hospitalizacion.service';
import { CuentaService } from '../../services/cuenta.service';
import { PermisosService } from '../../services/permisos.service';
import { CuentaPacienteComponent } from '../cuenta-paciente/cuenta-paciente';
import {
  CargoHospitalario, CatalogosHospitalizacion, CirugiaHospitalaria, DetalleHospitalizacion,
  HospitalizacionResumen, PacienteHospitalizable
} from '../../models/hospitalizacion.model';

type Vista = 'Solicitada' | 'Activa' | 'Recientes';
type Seccion = 'cargos' | 'cirugias' | 'alta';

@Component({
  selector: 'app-hospitalizacion',
  standalone: true,
  imports: [CommonModule, FormsModule, CuentaPacienteComponent],
  templateUrl: './hospitalizacion.html',
  styleUrl: './hospitalizacion.css'
})
export class HospitalizacionComponent implements OnInit {
  private service = inject(HospitalizacionService);
  private cuentaService = inject(CuentaService);
  permisos = inject(PermisosService);

  lista = signal<HospitalizacionResumen[]>([]);
  catalogos = signal<CatalogosHospitalizacion | null>(null);
  vista = signal<Vista>('Activa');
  idSeleccionado = signal<number | null>(null);
  detalle = signal<DetalleHospitalizacion | null>(null);
  cargandoDetalle = signal(false);
  seccion = signal<Seccion>('cargos');
  procesando = signal(false);
  modalNueva = signal(false);
  pacientes = signal<PacienteHospitalizable[]>([]);

  ahora = '';

  // Formularios
  formNueva = { idPaciente: null as number | null, tipo: 'Vigilancia', motivo: '', idMedicoResponsable: null as number | null };
  busquedaPaciente = '';
  private timerPaciente?: ReturnType<typeof setTimeout>;

  formIngreso = { idHabitacion: null as number | null, fechaIngreso: '' };
  formInsumo = { idMedicamento: null as number | null, cantidad: 1, observacion: '' };
  busquedaInsumo = '';
  formServicio = { idServicio: null as number | null, descripcion: '', cantidad: 1, precioUnitario: null as number | null };
  formCirugia = { idServicio: null as number | null, idCirujano: null as number | null, fechaProgramada: '', costoFinal: null as number | null, observaciones: '' };
  editandoCirugiaId: number | null = null;
  formAlta = { fechaAlta: '', observaciones: '' };

  filtrada = computed(() => {
    const v = this.vista();
    return this.lista().filter(h => v === 'Recientes' ? (h.estado === 'Alta' || h.estado === 'Cancelada') : h.estado === v);
  });

  ngOnInit(): void {
    this.cargarCatalogos();
    this.cargarLista();
  }

  contar(v: Vista): number {
    return this.lista().filter(h => v === 'Recientes' ? (h.estado === 'Alta' || h.estado === 'Cancelada') : h.estado === v).length;
  }

  // ───────── Carga ─────────

  cargarLista(): void {
    this.service.listar(true).subscribe({
      next: (d) => this.lista.set(d ?? []),
      error: (err) => this.mostrarError(err, 'No se pudo cargar la lista de hospitalizaciones.')
    });
  }

  cargarCatalogos(): void {
    this.service.catalogos().subscribe({
      next: (c) => this.catalogos.set(c),
      error: (err) => console.error('Error al cargar catálogos:', err)
    });
  }

  seleccionar(h: HospitalizacionResumen): void {
    this.idSeleccionado.set(h.id);
    this.seccion.set('cargos');
    this.reiniciarFormularios();
    this.cargarDetalle();
  }

  cargarDetalle(): void {
    const id = this.idSeleccionado();
    if (id == null) return;
    this.cargandoDetalle.set(true);
    this.service.detalle(id).subscribe({
      next: (d) => {
        this.detalle.set(d);
        this.cargandoDetalle.set(false);
      },
      error: (err) => {
        this.cargandoDetalle.set(false);
        this.mostrarError(err, 'No se pudo cargar el detalle.');
      }
    });
  }

  private refrescarTodo(): void {
    this.cargarLista();
    this.cargarDetalle();
    this.cargarCatalogos(); // habitaciones y existencias cambian
  }

  private reiniciarFormularios(): void {
    this.ahora = this.ahoraLocal();
    this.formIngreso = { idHabitacion: null, fechaIngreso: this.ahora };
    this.formInsumo = { idMedicamento: null, cantidad: 1, observacion: '' };
    this.busquedaInsumo = '';
    this.formServicio = { idServicio: null, descripcion: '', cantidad: 1, precioUnitario: null };
    this.formCirugia = { idServicio: null, idCirujano: null, fechaProgramada: '', costoFinal: null, observaciones: '' };
    this.editandoCirugiaId = null;
    this.formAlta = { fechaAlta: this.ahora, observaciones: '' };
  }

  // ───────── Nueva hospitalización (directa) ─────────

  abrirNueva(): void {
    this.formNueva = { idPaciente: null, tipo: 'Vigilancia', motivo: '', idMedicoResponsable: null };
    this.busquedaPaciente = '';
    this.pacientes.set([]);
    this.modalNueva.set(true);
    this.buscarPacientes('');
  }

  onBuscarPaciente(valor: string): void {
    this.busquedaPaciente = valor;
    this.formNueva.idPaciente = null;
    clearTimeout(this.timerPaciente);
    this.timerPaciente = setTimeout(() => this.buscarPacientes(valor.trim()), 300);
  }

  private buscarPacientes(texto: string): void {
    this.service.buscarPacientes(texto).subscribe({
      next: (d) => this.pacientes.set(d ?? []),
      error: () => this.pacientes.set([])
    });
  }

  guardarNueva(): void {
    const f = this.formNueva;
    const errores: string[] = [];
    if (f.idPaciente == null) errores.push('Paciente');
    if (!f.motivo.trim()) errores.push('Motivo');
    if (f.idMedicoResponsable == null) errores.push('Médico responsable');
    if (this.hayErrores(errores)) return;

    this.ejecutar(this.service.solicitar({
      idPaciente: f.idPaciente!,
      idDiagnostico: null,
      idMedicoResponsable: f.idMedicoResponsable,
      tipo: f.tipo,
      motivo: f.motivo.trim()
    }), (res) => {
      this.modalNueva.set(false);
      this.vista.set('Solicitada');
      if (res?.id) {
        this.idSeleccionado.set(res.id);
        this.reiniciarFormularios();
      }
    });
  }

  // ───────── Ingreso ─────────

  ingresar(): void {
    const d = this.detalle();
    if (!d) return;
    const errores: string[] = [];
    if (this.formIngreso.idHabitacion == null) errores.push('Habitación');
    if (!this.formIngreso.fechaIngreso) errores.push('Fecha de ingreso');
    if (this.hayErrores(errores)) return;

    this.ejecutar(this.service.ingresar(d.hospitalizacion.id, {
      idHabitacion: this.formIngreso.idHabitacion!,
      fechaIngreso: this.formIngreso.fechaIngreso
    }), () => this.vista.set('Activa'));
  }

  cancelarSolicitud(): void {
    const d = this.detalle();
    if (!d || !confirm('¿Cancelar esta solicitud de hospitalización?')) return;
    this.ejecutar(this.service.cancelar(d.hospitalizacion.id));
  }

  // ───────── Insumos y servicios ─────────

  get insumosFiltrados() {
    const t = this.busquedaInsumo.trim().toLowerCase();
    const lista = this.catalogos()?.medicamentos ?? [];
    return t ? lista.filter(m => m.nombre.toLowerCase().includes(t) || (m.marca ?? '').toLowerCase().includes(t)) : lista;
  }

  registrarInsumo(): void {
    const d = this.detalle();
    const f = this.formInsumo;
    const med = this.catalogos()?.medicamentos.find(m => m.id === f.idMedicamento);
    const errores: string[] = [];
    if (!med) errores.push('Insumo o medicamento');
    if (!f.cantidad || f.cantidad < 1 || !Number.isInteger(f.cantidad)) errores.push('Cantidad (entero mayor a 0)');
    else if (med && f.cantidad > med.existencia) errores.push(`Cantidad (solo hay ${med.existencia} disponibles)`);
    if (!d || this.hayErrores(errores)) return;

    this.ejecutar(this.service.registrarInsumo(d.hospitalizacion.id, {
      idMedicamento: med!.id,
      cantidad: f.cantidad,
      observacion: f.observacion.trim()
    }), () => {
      this.formInsumo = { idMedicamento: null, cantidad: 1, observacion: '' };
      this.busquedaInsumo = '';
    });
  }

  onServicio(id: number | null): void {
    this.formServicio.idServicio = id;
    const servicio = this.catalogos()?.servicios.find(s => s.id === id);
    this.formServicio.precioUnitario = servicio ? servicio.precio : null;
  }

  registrarServicio(): void {
    const d = this.detalle();
    const f = this.formServicio;
    const errores: string[] = [];
    if (f.idServicio == null && !f.descripcion.trim()) errores.push('Servicio o descripción');
    if (!f.cantidad || f.cantidad < 1) errores.push('Cantidad');
    if (f.precioUnitario == null || f.precioUnitario < 0) errores.push('Precio');
    if (!d || this.hayErrores(errores)) return;

    this.ejecutar(this.service.registrarServicio(d.hospitalizacion.id, {
      idServicio: f.idServicio,
      descripcion: f.descripcion.trim(),
      cantidad: f.cantidad,
      precioUnitario: f.precioUnitario
    }), () => this.formServicio = { idServicio: null, descripcion: '', cantidad: 1, precioUnitario: null });
  }

  anularCargo(c: CargoHospitalario): void {
    if (!confirm(`¿Anular el cargo "${c.descripcion}"?${c.tipo === 'Insumo' ? '\n\nEl insumo regresará al inventario.' : ''}`)) return;
    this.ejecutar(this.cuentaService.anularCargo(c.id));
  }

  // ───────── Cirugías ─────────

  onCirugia(id: number | null): void {
    this.formCirugia.idServicio = id;
    const cirugia = this.catalogos()?.cirugias.find(c => c.id === id);
    this.formCirugia.costoFinal = cirugia ? cirugia.precio : null;
  }

  get costoBaseCirugia(): number | null {
    return this.catalogos()?.cirugias.find(c => c.id === this.formCirugia.idServicio)?.precio ?? null;
  }

  editarCirugia(c: CirugiaHospitalaria): void {
    this.editandoCirugiaId = c.id;
    this.formCirugia = {
      idServicio: c.idServicio ?? null,
      idCirujano: c.idCirujano ?? null,
      fechaProgramada: c.fechaProgramada.slice(0, 16),
      costoFinal: c.costoFinal,
      observaciones: c.observaciones ?? ''
    };
  }

  cancelarEdicionCirugia(): void {
    this.editandoCirugiaId = null;
    this.formCirugia = { idServicio: null, idCirujano: null, fechaProgramada: '', costoFinal: null, observaciones: '' };
  }

  guardarCirugia(): void {
    const d = this.detalle();
    const f = this.formCirugia;
    const errores: string[] = [];
    if (!this.editandoCirugiaId && f.idServicio == null) errores.push('Cirugía');
    if (f.idCirujano == null) errores.push('Cirujano');
    if (!f.fechaProgramada) errores.push('Fecha programada');
    if (f.costoFinal == null || f.costoFinal < 0) errores.push('Costo final');
    if (!d || this.hayErrores(errores)) return;

    const datos = {
      idServicio: f.idServicio,
      idCirujano: f.idCirujano,
      fechaProgramada: f.fechaProgramada,
      costoFinal: f.costoFinal,
      observaciones: f.observaciones.trim()
    };

    this.ejecutar(this.editandoCirugiaId
      ? this.service.actualizarCirugia(this.editandoCirugiaId, datos)
      : this.service.programarCirugia(d.hospitalizacion.id, datos), () => this.cancelarEdicionCirugia());
  }

  realizarCirugia(c: CirugiaHospitalaria): void {
    if (!confirm(`¿Marcar como realizada "${c.procedimiento}"?\n\nSe cargarán Q${c.costoFinal.toFixed(2)} a la cuenta del paciente.`)) return;
    this.ejecutar(this.service.realizarCirugia(c.id, { fechaRealizada: null, observaciones: '' }));
  }

  cancelarCirugia(c: CirugiaHospitalaria): void {
    if (!confirm(`¿Cancelar la cirugía "${c.procedimiento}"?`)) return;
    this.ejecutar(this.service.cancelarCirugia(c.id));
  }

  // ───────── Alta ─────────

  get cirugiasProgramadas(): number {
    return this.detalle()?.cirugias.filter(c => c.estado === 'Programada').length ?? 0;
  }

  darAlta(): void {
    const d = this.detalle();
    if (!d) return;
    if (!this.formAlta.fechaAlta) {
      alert('Indique la fecha de alta.');
      return;
    }
    if (!confirm('¿Confirmar el alta del paciente?\n\nSe liberará la habitación y se cargarán los días de estancia a su cuenta.')) return;

    this.ejecutar(this.service.darAlta(d.hospitalizacion.id, {
      fechaAlta: this.formAlta.fechaAlta,
      observaciones: this.formAlta.observaciones.trim()
    }), () => this.vista.set('Recientes'));
  }

  // ───────── Utilidades ─────────

  claseEstado(estado: string): string {
    return {
      Solicitada: 'b-ambar', Activa: 'b-rojo', Alta: 'b-verde', Cancelada: 'b-gris',
      Programada: 'b-ambar', Realizada: 'b-verde', Pendiente: 'b-ambar', Facturado: 'b-verde', Anulado: 'b-gris'
    }[estado] ?? 'b-gris';
  }

  diasDesde(fecha?: string | null): number {
    if (!fecha) return 0;
    return Math.max(1, Math.ceil((Date.now() - new Date(fecha).getTime()) / 86_400_000));
  }

  private ejecutar(peticion: Observable<any>, alTerminar?: (res: any) => void): void {
    if (this.procesando()) return;
    this.procesando.set(true);
    peticion.subscribe({
      next: (res) => {
        this.procesando.set(false);
        alTerminar?.(res);
        this.refrescarTodo();
        setTimeout(() => alert(res?.mensaje ?? 'Operación realizada.'));
      },
      error: (err) => {
        this.procesando.set(false);
        this.mostrarError(err, 'No se pudo completar la operación.');
      }
    });
  }

  private hayErrores(errores: string[]): boolean {
    if (errores.length === 0) return false;
    alert('Complete o corrija:\n\n• ' + errores.join('\n• '));
    return true;
  }

  private mostrarError(err: any, porDefecto: string): void {
    console.error(err);
    setTimeout(() => alert(err?.error?.mensaje ?? porDefecto));
  }

  private ahoraLocal(): string {
    const d = new Date();
    d.setMinutes(d.getMinutes() - d.getTimezoneOffset());
    return d.toISOString().slice(0, 16);
  }
}
