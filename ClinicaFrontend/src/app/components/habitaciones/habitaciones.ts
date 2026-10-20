import { Component, OnDestroy, OnInit, WritableSignal, computed, inject, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { HabitacionesService } from '../../services/habitacion.service';
import {
  Catalogo, EstadoHabitacion, Habitacion, HabitacionPayload,
  HistorialAsignacion, Ocupante, PacienteDisponible
} from '../../models/habitacion.model';

const CAPACIDAD_SUGERIDA: Record<string, number> = {
  'Individual': 1, 'Doble': 2, 'Suite': 1, 'Cuidados Intensivos': 1
};

@Component({
  selector: 'app-habitaciones',
  standalone: true,
  imports: [CommonModule, FormsModule],
  templateUrl: './habitaciones.html',
  styleUrl: './habitaciones.css'
})
export class HabitacionesComponent implements OnInit, OnDestroy {
  private service = inject(HabitacionesService);

  // Estado compartido con el servicio (se actualiza por HTTP y por SignalR)
  habitaciones = this.service.habitaciones;
  conectado = this.service.conectado;
  cargando = this.service.cargando;

  // Catálogos y filtros
  sucursales = signal<Catalogo[]>([]);
  tipos = signal<string[]>([]);
  idSucursal = signal<number | null>(null);
  filtroEstado = signal<EstadoHabitacion | 'Todos'>('Todos');
  busqueda = signal('');
  readonly estados: EstadoHabitacion[] = ['Disponible', 'Ocupada', 'Limpieza', 'Mantenimiento'];

  resumen = computed(() => {
    const r: Record<EstadoHabitacion, number> = { Disponible: 0, Ocupada: 0, Limpieza: 0, Mantenimiento: 0 };
    for (const h of this.habitaciones()) r[h.estado] = (r[h.estado] ?? 0) + 1;
    return r;
  });

  pacientesInternados = computed(() => this.habitaciones().reduce((s, h) => s + h.ocupantes.length, 0));

  filtradas = computed(() => {
    const t = this.busqueda().trim().toLowerCase();
    const est = this.filtroEstado();
    return this.habitaciones().filter(h =>
      (est === 'Todos' || h.estado === est) &&
      (!t || h.numeroHabitacion.toLowerCase().includes(t) ||
             (h.tipoHabitacion ?? '').toLowerCase().includes(t) ||
             h.ocupantes.some(o => o.pacienteNombre.toLowerCase().includes(t)))
    );
  });

  // Modales
  modalHabitacion = signal(false);
  modalAsignar = signal(false);
  modalEgreso = signal(false);
  modalHistorial = signal(false);
  procesando = signal(false);

  ahora = '';

  // Formulario de habitación
  editandoId: number | null = null;
  formHab: HabitacionPayload = this.habVacia();

  // Asignación
  habSeleccionada: Habitacion | null = null;
  busquedaPaciente = '';
  pacientesEncontrados = signal<PacienteDisponible[]>([]);
  pacienteElegido: PacienteDisponible | null = null;
  fechaIngreso = '';
  observacionesIngreso = '';
  private timerPaciente?: ReturnType<typeof setTimeout>;

  // Egreso
  ocupanteEgreso: Ocupante | null = null;
  fechaEgreso = '';
  observacionesEgreso = '';

  // Historial
  historial = signal<HistorialAsignacion[]>([]);
  historialCargando = signal(false);

  ngOnInit(): void {
    this.service.obtenerCatalogos().subscribe({
      next: (c) => {
        this.sucursales.set(c?.sucursales ?? []);
        this.tipos.set(c?.tipos ?? []);
        const id = c?.idSucursalUsuario ?? c?.sucursales?.[0]?.id ?? null;
        this.idSucursal.set(id);
        this.service.cargar(id);
      },
      error: (err) => this.mostrarError(err, 'No se pudieron cargar los catálogos.')
    });
    this.service.conectar();
  }

  ngOnDestroy(): void {
    clearTimeout(this.timerPaciente);
    this.service.desconectar();
  }

  cambiarSucursal(id: number | null): void {
    this.idSucursal.set(id);
    this.service.cargar(id);
  }

  // ───────── Reglas de presentación ─────────

  puedeAsignar(h: Habitacion): boolean {
    return h.estado === 'Disponible' && h.ocupantes.length < h.capacidad;
  }

  estadosManuales(h: Habitacion): EstadoHabitacion[] {
    if (h.ocupantes.length > 0) return [];
    return (['Disponible', 'Limpieza', 'Mantenimiento'] as EstadoHabitacion[]).filter(e => e !== h.estado);
  }

  etiquetaEstado(e: EstadoHabitacion): string {
    return { Disponible: '✓ Disponible', Limpieza: '🧹 Limpieza', Mantenimiento: '🔧 Mantenimiento', Ocupada: 'Ocupada' }[e];
  }

  claseEstado(e: EstadoHabitacion): string {
    return { Disponible: 'e-disp', Ocupada: 'e-ocup', Limpieza: 'e-limp', Mantenimiento: 'e-mant' }[e] ?? 'e-mant';
  }

  colorEstado(e: EstadoHabitacion): string {
    return { Disponible: '#22c55e', Ocupada: '#ef4444', Limpieza: '#0ea5e9', Mantenimiento: '#f59e0b' }[e] ?? '#cbd5e1';
  }

  diasEstancia(ingreso: string, fin?: string | null): number {
    const inicio = new Date(ingreso).getTime();
    const final = fin ? new Date(fin).getTime() : Date.now();
    return Math.max(1, Math.ceil((final - inicio) / 86_400_000));
  }

  costoEstimado(o: Ocupante): number {
    return this.diasEstancia(o.fechaIngreso, this.fechaEgreso) * (this.habSeleccionada?.precioDiario ?? 0);
  }

  // ───────── Estado ─────────

  cambiarEstado(h: Habitacion, estado: EstadoHabitacion): void {
    this.service.cambiarEstado(h.id, estado).subscribe({
      error: (err) => this.mostrarError(err, 'No se pudo cambiar el estado.')
    });
  }

  // ───────── CRUD de habitaciones ─────────

  abrirNuevaHabitacion(): void {
    this.editandoId = null;
    this.formHab = this.habVacia();
    this.modalHabitacion.set(true);
  }

  editarHabitacion(h: Habitacion): void {
    this.editandoId = h.id;
    this.formHab = {
      numeroHabitacion: h.numeroHabitacion,
      tipoHabitacion: h.tipoHabitacion ?? '',
      capacidad: h.capacidad,
      precioDiario: h.precioDiario,
      idSucursal: h.idSucursal
    };
    this.modalHabitacion.set(true);
  }

  onTipoChange(tipo: string): void {
    this.formHab.tipoHabitacion = tipo;
    if (!this.editandoId) this.formHab.capacidad = CAPACIDAD_SUGERIDA[tipo] ?? 1;
  }

  guardarHabitacion(): void {
    if (this.procesando()) return;

    const f = this.formHab;
    const errores: string[] = [];
    if (!f.numeroHabitacion.trim()) errores.push('Número de habitación');
    if (!f.tipoHabitacion) errores.push('Tipo');
    if (f.capacidad == null || f.capacidad < 1 || f.capacidad > 6 || !Number.isInteger(f.capacidad)) errores.push('Capacidad (entero de 1 a 6)');
    if (f.precioDiario == null || f.precioDiario <= 0) errores.push('Precio diario (mayor a 0)');
    if (f.idSucursal == null) errores.push('Sucursal');
    if (this.hayErrores(errores)) return;

    this.procesando.set(true);
    const peticion = this.editandoId
      ? this.service.actualizar(this.editandoId, { ...f, numeroHabitacion: f.numeroHabitacion.trim() })
      : this.service.crear({ ...f, numeroHabitacion: f.numeroHabitacion.trim() });

    peticion.subscribe({
      next: (res) => this.alGuardar(this.modalHabitacion, res?.mensaje),
      error: (err) => this.alFallar(err)
    });
  }

  eliminarHabitacion(h: Habitacion): void {
    if (!confirm(`¿Eliminar la habitación ${h.numeroHabitacion}?`)) return;
    this.service.eliminar(h.id).subscribe({
      next: (res) => setTimeout(() => alert(res?.mensaje ?? 'Habitación eliminada.')),
      error: (err) => this.mostrarError(err, 'No se pudo eliminar la habitación.')
    });
  }

  // ───────── Asignación ─────────

  abrirAsignar(h: Habitacion): void {
    this.habSeleccionada = h;
    this.busquedaPaciente = '';
    this.pacienteElegido = null;
    this.pacientesEncontrados.set([]);
    this.ahora = this.ahoraLocal();
    this.fechaIngreso = this.ahora;
    this.observacionesIngreso = '';
    this.modalAsignar.set(true);
    this.buscarPacientes('');
  }

  onBuscarPaciente(valor: string): void {
    this.busquedaPaciente = valor;
    this.pacienteElegido = null;
    clearTimeout(this.timerPaciente);
    this.timerPaciente = setTimeout(() => this.buscarPacientes(valor.trim()), 300);
  }

  private buscarPacientes(texto: string): void {
    this.service.buscarPacientesDisponibles(texto).subscribe({
      next: (data) => this.pacientesEncontrados.set(data ?? []),
      error: () => this.pacientesEncontrados.set([])
    });
  }

  guardarAsignacion(): void {
    if (this.procesando() || !this.habSeleccionada) return;

    const errores: string[] = [];
    if (!this.pacienteElegido) errores.push('Paciente');
    if (!this.fechaIngreso) errores.push('Fecha de ingreso');
    else if (new Date(this.fechaIngreso) > new Date()) errores.push('Fecha de ingreso (no puede ser futura)');
    if (this.hayErrores(errores)) return;

    this.procesando.set(true);
    this.service.asignar({
      idHabitacion: this.habSeleccionada.id,
      idPaciente: this.pacienteElegido!.id,
      fechaIngreso: this.fechaIngreso,
      observaciones: this.observacionesIngreso.trim()
    }).subscribe({
      next: (res) => this.alGuardar(this.modalAsignar, res?.mensaje),
      error: (err) => this.alFallar(err)
    });
  }

  // ───────── Egreso ─────────

  abrirEgreso(h: Habitacion, o: Ocupante): void {
    this.habSeleccionada = h;
    this.ocupanteEgreso = o;
    this.ahora = this.ahoraLocal();
    this.fechaEgreso = this.ahora;
    this.observacionesEgreso = '';
    this.modalEgreso.set(true);
  }

  guardarEgreso(): void {
    if (this.procesando() || !this.ocupanteEgreso) return;

    const errores: string[] = [];
    if (!this.fechaEgreso) errores.push('Fecha de egreso');
    else {
      const egreso = new Date(this.fechaEgreso);
      if (egreso < new Date(this.ocupanteEgreso.fechaIngreso)) errores.push('Fecha de egreso (no puede ser anterior al ingreso)');
      if (egreso > new Date()) errores.push('Fecha de egreso (no puede ser futura)');
    }
    if (this.hayErrores(errores)) return;

    this.procesando.set(true);
    this.service.registrarEgreso(this.ocupanteEgreso.idAsignacion, {
      fechaEgreso: this.fechaEgreso,
      observaciones: this.observacionesEgreso.trim()
    }).subscribe({
      next: (res) => this.alGuardar(this.modalEgreso, res?.mensaje),
      error: (err) => this.alFallar(err)
    });
  }

  // ───────── Historial ─────────

  verHistorial(h: Habitacion): void {
    this.habSeleccionada = h;
    this.historial.set([]);
    this.historialCargando.set(true);
    this.modalHistorial.set(true);

    this.service.obtenerHistorial(h.id).subscribe({
      next: (data) => {
        this.historial.set(data ?? []);
        this.historialCargando.set(false);
      },
      error: (err) => {
        this.historialCargando.set(false);
        this.mostrarError(err, 'No se pudo cargar el historial.');
      }
    });
  }

  // ───────── Utilidades ─────────

  cerrarModales(): void {
    this.modalHabitacion.set(false);
    this.modalAsignar.set(false);
    this.modalEgreso.set(false);
    this.modalHistorial.set(false);
    this.procesando.set(false);
  }

  private alGuardar(modal: WritableSignal<boolean>, mensaje?: string): void {
    modal.set(false);
    this.procesando.set(false);
    setTimeout(() => alert(mensaje ?? 'Guardado correctamente.'));
  }

  private alFallar(err: any): void {
    this.procesando.set(false);
    this.mostrarError(err, 'Ocurrió un error al guardar.');
  }

  private mostrarError(err: any, porDefecto: string): void {
    console.error(err);
    setTimeout(() => alert(err?.error?.mensaje ?? porDefecto));
  }

  private hayErrores(errores: string[]): boolean {
    if (errores.length === 0) return false;
    alert('Complete o corrija los siguientes campos:\n\n• ' + errores.join('\n• '));
    return true;
  }

  private ahoraLocal(): string {
    const d = new Date();
    d.setMinutes(d.getMinutes() - d.getTimezoneOffset());
    return d.toISOString().slice(0, 16);
  }

  private habVacia(): HabitacionPayload {
    return { numeroHabitacion: '', tipoHabitacion: '', capacidad: 1, precioDiario: null, idSucursal: this.idSucursal() };
  }
}
