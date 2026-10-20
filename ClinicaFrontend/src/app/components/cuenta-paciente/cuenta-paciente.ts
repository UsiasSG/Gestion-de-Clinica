import { Component, computed, effect, inject, input, signal, untracked } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { CuentaService } from '../../services/cuenta.service';
import { PermisosService } from '../../services/permisos.service';
import { CargoCuenta, CuentaPaciente, FacturaDetalle, FacturaResumen, MetodoPagoCuenta, MedicamentoCargo } from '../../models/cuenta.model';

const TASA_IVA = 0.12;
const redondear = (n: number) => Math.round(n * 100) / 100;

@Component({
  selector: 'app-cuenta-paciente',
  standalone: true,
  imports: [CommonModule, FormsModule],
  templateUrl: './cuenta-paciente.html',
  styleUrl: './cuenta-paciente.css'
})
export class CuentaPacienteComponent {
  private service = inject(CuentaService);
  permisos = inject(PermisosService);

  idPaciente = input.required<number>();
  nombrePaciente = input<string>('');

  cuenta = signal<CuentaPaciente | null>(null);
  cargando = signal(true);
  seleccion = signal<Set<number>>(new Set());
  procesando = signal(false);

  modalCobro = signal(false);
  modalCargo = signal(false);
  modalMedicamento = signal(false);
  busquedaMedicamento = '';
  ticket = signal<FacturaDetalle | null>(null);

  readonly metodosPago: MetodoPagoCuenta[] = ['Efectivo', 'Tarjeta', 'Transferencia'];

  formCobro = { nit: 'CF', nombreFactura: '', metodoPago: 'Efectivo' as MetodoPagoCuenta, montoRecibido: null as number | null, referencia: '' };
  formCargo = { idServicio: null as number | null, descripcion: '', cantidad: 1, precioUnitario: null as number | null };
  formMedicamento = { idMedicamento: null as number | null, cantidad: 1, observacion: '' };

  // ───────── Derivados ─────────

  pendientes = computed(() => this.cuenta()?.cargos.filter(c => c.estado === 'Pendiente') ?? []);
  cobrables = computed(() => this.pendientes().filter(c => !c.cobroAlAlta));
  historialCargos = computed(() => this.cuenta()?.cargos.filter(c => c.estado !== 'Pendiente') ?? []);

  subtotalSeleccion = computed(() => redondear(
    this.cobrables().filter(c => this.seleccion().has(c.id)).reduce((s, c) => s + c.subtotal, 0)));
  ivaSeleccion = computed(() => redondear(this.subtotalSeleccion() * TASA_IVA));
  totalSeleccion = computed(() => redondear(this.subtotalSeleccion() + this.ivaSeleccion()));

  todosSeleccionados = computed(() => this.cobrables().length > 0 && this.cobrables().every(c => this.seleccion().has(c.id)));

  get cambio(): number {
    return this.formCobro.montoRecibido != null ? redondear(this.formCobro.montoRecibido - this.totalSeleccion()) : 0;
  }

  constructor() {
    effect(() => {
      const id = this.idPaciente();
      untracked(() => this.cargar(id));
    });
  }

  cargar(id: number = this.idPaciente()): void {
    this.cargando.set(true);
    this.service.obtenerCuenta(id).subscribe({
      next: (c) => {
        this.cuenta.set(c);
        // Por defecto se seleccionan todos los pendientes (cobro al momento)
        this.seleccion.set(new Set(c.cargos.filter(x => x.estado === 'Pendiente' && !x.cobroAlAlta).map(x => x.id)));
        this.cargando.set(false);
      },
      error: (err) => {
        this.cargando.set(false);
        this.mostrarError(err, 'No se pudo cargar la cuenta del paciente.');
      }
    });
  }

  // ───────── Selección ─────────

  alternar(c: CargoCuenta): void {
    this.seleccion.update(s => {
      const nuevo = new Set(s);
      nuevo.has(c.id) ? nuevo.delete(c.id) : nuevo.add(c.id);
      return nuevo;
    });
  }

  alternarTodos(marcar: boolean): void {
    this.seleccion.set(marcar ? new Set(this.cobrables().map(c => c.id)) : new Set());
  }

  // ───────── Cobro ─────────

  abrirCobro(): void {
    if (this.seleccion().size === 0) return;
    this.formCobro = { nit: 'CF', nombreFactura: this.nombrePaciente() || 'Consumidor Final', metodoPago: 'Efectivo', montoRecibido: null, referencia: '' };
    this.modalCobro.set(true);
  }

  pagoExacto(): void {
    this.formCobro.montoRecibido = this.totalSeleccion();
  }

  confirmarCobro(): void {
    if (this.procesando()) return;

    const f = this.formCobro;
    const nit = f.nit.trim().toUpperCase() || 'CF';
    const errores: string[] = [];
    if (nit !== 'CF' && !/^\d+-?[\dK]$/.test(nit)) errores.push('NIT (use CF o un NIT válido, ej. 1234567-8)');
    if (f.metodoPago === 'Efectivo' && (f.montoRecibido == null || f.montoRecibido < this.totalSeleccion())) {
      errores.push(`Efectivo recibido (mínimo Q${this.totalSeleccion().toFixed(2)})`);
    }
    if (f.metodoPago !== 'Efectivo' && !f.referencia.trim()) errores.push('Número de voucher o referencia');
    if (errores.length) {
      alert('Complete o corrija:\n\n• ' + errores.join('\n• '));
      return;
    }

    this.procesando.set(true);
    this.service.cobrar({
      idPaciente: this.idPaciente(),
      idsCargos: [...this.seleccion()],
      nit,
      nombreFactura: f.nombreFactura.trim(),
      metodoPago: f.metodoPago,
      montoRecibido: f.metodoPago === 'Efectivo' ? f.montoRecibido : null,
      referencia: f.referencia.trim()
    }).subscribe({
      next: (res) => {
        this.procesando.set(false);
        this.modalCobro.set(false);
        this.ticket.set(res.factura);
        this.cargar();
      },
      error: (err) => {
        this.procesando.set(false);
        this.mostrarError(err, 'No se pudo registrar el cobro.');
      }
    });
  }

  // ───────── Facturas ─────────

  verFactura(f: FacturaResumen): void {
    this.service.obtenerFactura(f.id).subscribe({
      next: (detalle) => this.ticket.set(detalle),
      error: (err) => this.mostrarError(err, 'No se pudo cargar la factura.')
    });
  }

  anularFactura(f: FacturaResumen): void {
    const motivo = prompt(`Motivo de anulación de la factura No. ${f.id}:`);
    if (motivo === null) return;
    if (!motivo.trim()) {
      alert('Debe indicar el motivo.');
      return;
    }
    this.service.anularFactura(f.id, motivo.trim()).subscribe({
      next: (res) => {
        this.cargar();
        setTimeout(() => alert(res?.mensaje ?? 'Factura anulada.'));
      },
      error: (err) => this.mostrarError(err, 'No se pudo anular la factura.')
    });
  }

  imprimir(): void {
    window.print();
  }
  gruposTicket(t: FacturaDetalle) {
  const mapa = new Map<string, FacturaDetalle['items']>();
  for (const i of t.items) mapa.set(i.tipo, [...(mapa.get(i.tipo) ?? []), i]);
  return [...mapa].map(([tipo, items]) => ({
    tipo,
    items,
    subtotal: redondear(items.reduce((s, i) => s + i.subtotal, 0))
  }));
}

  // ───────── Cargos manuales ─────────

  abrirCargo(): void {
    this.formCargo = { idServicio: null, descripcion: '', cantidad: 1, precioUnitario: null };
    this.modalCargo.set(true);
  }

  onServicioCargo(id: number | null): void {
    this.formCargo.idServicio = id;
    const servicio = this.cuenta()?.servicios.find(s => s.id === id);
    this.formCargo.precioUnitario = servicio ? servicio.precio : null;
    if (servicio) this.formCargo.descripcion = '';
  }

  guardarCargo(): void {
    if (this.procesando()) return;
    const f = this.formCargo;
    const errores: string[] = [];
    if (f.idServicio == null && !f.descripcion.trim()) errores.push('Servicio o descripción');
    if (!f.cantidad || f.cantidad < 1 || !Number.isInteger(f.cantidad)) errores.push('Cantidad (entero mayor a 0)');
    if (f.precioUnitario == null || f.precioUnitario < 0) errores.push('Precio unitario');
    if (errores.length) {
      alert('Complete o corrija:\n\n• ' + errores.join('\n• '));
      return;
    }

    this.procesando.set(true);
    this.service.agregarCargo({
      idPaciente: this.idPaciente(),
      idServicio: f.idServicio,
      descripcion: f.descripcion.trim(),
      cantidad: f.cantidad,
      precioUnitario: f.precioUnitario
    }).subscribe({
      next: (res) => {
        this.procesando.set(false);
        this.modalCargo.set(false);
        this.cargar();
        setTimeout(() => alert(res?.mensaje ?? 'Cargo agregado.'));
      },
      error: (err) => {
        this.procesando.set(false);
        this.mostrarError(err, 'No se pudo agregar el cargo.');
      }
    });
  }

  anularCargo(c: CargoCuenta): void {
    if (!confirm(`¿Anular el cargo "${c.descripcion}"?`)) return;
    this.service.anularCargo(c.id).subscribe({
      next: () => this.cargar(),
      error: (err) => this.mostrarError(err, 'No se pudo anular el cargo.')
    });
  }

  // ───────── Medicamentos a cuenta ─────────

  get medicamentosFiltrados(): MedicamentoCargo[] {
    const t = this.busquedaMedicamento.trim().toLowerCase();
    const lista = this.cuenta()?.medicamentos ?? [];
    return t ? lista.filter(m => m.nombre.toLowerCase().includes(t) || (m.marca ?? '').toLowerCase().includes(t)) : lista;
  }

  get medicamentoSeleccionado(): MedicamentoCargo | undefined {
    return this.cuenta()?.medicamentos.find(m => m.id === this.formMedicamento.idMedicamento);
  }

  get subtotalMedicamento(): number {
    const m = this.medicamentoSeleccionado;
    return m ? redondear(m.precio * (this.formMedicamento.cantidad || 0)) : 0;
  }

  abrirMedicamento(): void {
    this.busquedaMedicamento = '';
    this.formMedicamento = { idMedicamento: null, cantidad: 1, observacion: '' };
    this.modalMedicamento.set(true);
  }

  guardarMedicamento(): void {
    if (this.procesando()) return;
    const f = this.formMedicamento;
    const med = this.medicamentoSeleccionado;
    const errores: string[] = [];
    if (!med) errores.push('Medicamento');
    if (!f.cantidad || f.cantidad < 1 || !Number.isInteger(f.cantidad)) errores.push('Cantidad (entero mayor a 0)');
    else if (med && f.cantidad > med.existencia) errores.push(`Cantidad (solo hay ${med.existencia} disponibles)`);
    if (errores.length) {
      alert('Complete o corrija:\n\n• ' + errores.join('\n• '));
      return;
    }

    this.procesando.set(true);
    this.service.cargarMedicamento({
      idPaciente: this.idPaciente(),
      idMedicamento: med!.id,
      cantidad: f.cantidad,
      observacion: f.observacion.trim()
    }).subscribe({
      next: (res) => {
        this.procesando.set(false);
        this.modalMedicamento.set(false);
        this.cargar();
        setTimeout(() => alert(res?.mensaje ?? 'Medicamento cargado a la cuenta.'));
      },
      error: (err) => {
        this.procesando.set(false);
        this.mostrarError(err, 'No se pudo cargar el medicamento.');
      }
    });
  }

  // ───────── Utilidades ─────────

  productosUnicos(lista: string[]): string {
    return [...new Set(lista)].join(', ') || '—';
  }

  claseEstado(estado: string): string {
    if (estado === 'Pendiente') return 'b-ambar';
    if (estado === 'Facturado' || estado === 'Pagada') return 'b-verde';
    return 'b-gris';
  }

  private mostrarError(err: any, porDefecto: string): void {
    console.error(err);
    setTimeout(() => alert(err?.error?.mensaje ?? porDefecto));
  }
}
