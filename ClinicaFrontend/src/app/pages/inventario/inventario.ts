import { Component, OnInit, WritableSignal, computed, inject, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { Observable, map, of, switchMap } from 'rxjs';
import { InventarioService } from '../../services/inventario.service';
import {
  Catalogo, EstadoLote, Lote, LotePayload, MedicamentoInventario,
  MedicamentoPayload, Movimiento, SalidaPayload, TipoSalida
} from '../../models/inventario.model';

type Pestana = 'medicamentos' | 'lotes' | 'movimientos';
type FormMedicamento = Omit<MedicamentoPayload, 'idCategoria'> & { idCategoria: number | 'NUEVA' | null };

@Component({
  selector: 'app-inventario',
  standalone: true,
  imports: [CommonModule, FormsModule],
  templateUrl: './inventario.html',
  styleUrl: './inventario.css'
})
export class InventarioComponent implements OnInit {
  private service = inject(InventarioService);

  readonly hoy = (() => {
    const d = new Date();
    return `${d.getFullYear()}-${String(d.getMonth() + 1).padStart(2, '0')}-${String(d.getDate()).padStart(2, '0')}`;
  })();

  // Catálogos
  sucursales = signal<Catalogo[]>([]);
  categorias = signal<Catalogo[]>([]);
  marcas = signal<string[]>([]);
  idSucursal = signal<number | null>(null);

  // Datos
  medicamentos = signal<MedicamentoInventario[]>([]);
  lotes = signal<Lote[]>([]);
  movimientos = signal<Movimiento[]>([]);

  // Filtros
  pestana = signal<Pestana>('medicamentos');
  busquedaMed = signal('');
  filtroCategoria = signal<number | null>(null);
  soloStockBajo = signal(false);
  verInactivos = signal(false);
  busquedaLote = signal('');
  filtroMedicamento = signal<number | null>(null);
  filtroEstado = signal<EstadoLote | 'Todos'>('Todos');
  readonly estadosLote: (EstadoLote | 'Todos')[] = ['Todos', 'Vigente', 'Por vencer', 'Vencido', 'Agotado'];

  // Modales
  modalMedicamento = signal(false);
  modalLote = signal(false);
  modalSalida = signal(false);
  procesando = signal(false);

  editandoMedId: number | null = null;
  editandoLoteId: number | null = null;
  loteSalida: Lote | null = null;
  nuevaCategoria = '';
  formMed: FormMedicamento = this.medVacio();
  formLote: LotePayload = this.loteVacio();
  formSalida: SalidaPayload = { tipo: '', cantidad: null, observacion: '' };

  // Derivados
  medicamentosActivos = computed(() => this.medicamentos().filter(m => m.activo));
  stockBajo = computed(() => this.medicamentosActivos().filter(m => m.existencia < m.stockMinimo));
  porVencer = computed(() => this.lotes().filter(l => l.estado === 'Por vencer'));
  vencidos = computed(() => this.lotes().filter(l => l.estado === 'Vencido'));

  medicamentosFiltrados = computed(() => {
    const t = this.busquedaMed().trim().toLowerCase();
    const cat = this.filtroCategoria();
    return this.medicamentos().filter(m =>
      (this.verInactivos() || m.activo) &&
      (cat == null || m.idCategoria === cat) &&
      (!this.soloStockBajo() || (m.activo && m.existencia < m.stockMinimo)) &&
      (!t || [m.nombre, m.codigo, m.marca, m.categoria].some(v => v?.toLowerCase().includes(t)))
    );
  });

  lotesFiltrados = computed(() => {
    const t = this.busquedaLote().trim().toLowerCase();
    const med = this.filtroMedicamento();
    const est = this.filtroEstado();
    return this.lotes().filter(l =>
      (med == null || l.idMedicamento === med) &&
      (est === 'Todos' || l.estado === est) &&
      (!t || [l.numeroLote, l.medicamento].some(v => v?.toLowerCase().includes(t)))
    );
  });

  get tiposSalida(): TipoSalida[] {
    return this.loteSalida?.estado === 'Vencido' ? ['Baja por vencimiento', 'Merma'] : ['Uso clínico', 'Merma'];
  }

  ngOnInit(): void {
    this.cargarCatalogos(true);
  }

  // ───────── Carga de datos ─────────

  cargarCatalogos(inicial = false): void {
    this.service.obtenerCatalogos().subscribe({
      next: (c) => {
        this.sucursales.set(c?.sucursales ?? []);
        this.categorias.set(c?.categorias ?? []);
        this.marcas.set(c?.marcas ?? []);
        if (inicial) {
          this.idSucursal.set(c?.idSucursalUsuario ?? c?.sucursales?.[0]?.id ?? null);
          this.cargarDatos();
        }
      },
      error: (err) => this.mostrarError(err, 'No se pudieron cargar los catálogos.')
    });
  }

  cargarDatos(): void {
    const id = this.idSucursal();
    this.service.obtenerMedicamentos(id).subscribe({
      next: (d) => this.medicamentos.set(d ?? []),
      error: (err) => console.error('Error al cargar medicamentos:', err)
    });
    this.service.obtenerLotes(id).subscribe({
      next: (d) => this.lotes.set(d ?? []),
      error: (err) => console.error('Error al cargar lotes:', err)
    });
    this.service.obtenerMovimientos(id).subscribe({
      next: (d) => this.movimientos.set(d ?? []),
      error: (err) => console.error('Error al cargar movimientos:', err)
    });
  }

  cambiarSucursal(id: number | null): void {
    this.idSucursal.set(id);
    this.filtroMedicamento.set(null);
    this.cargarDatos();
  }

  // ───────── Navegación desde alertas ─────────

  verStockBajo(): void {
    this.soloStockBajo.set(true);
    this.pestana.set('medicamentos');
  }

  verLotes(estado: EstadoLote): void {
    this.filtroMedicamento.set(null);
    this.busquedaLote.set('');
    this.filtroEstado.set(estado);
    this.pestana.set('lotes');
  }

  verLotesDe(m: MedicamentoInventario): void {
    this.filtroMedicamento.set(m.id);
    this.filtroEstado.set('Todos');
    this.busquedaLote.set('');
    this.pestana.set('lotes');
  }

  // ───────── Medicamentos ─────────

  abrirNuevoMedicamento(): void {
    this.editandoMedId = null;
    this.nuevaCategoria = '';
    this.formMed = this.medVacio();
    this.modalMedicamento.set(true);
  }

  editarMedicamento(m: MedicamentoInventario): void {
    this.editandoMedId = m.id;
    this.nuevaCategoria = '';
    this.formMed = {
      codigo: m.codigo ?? '',
      nombre: m.nombre,
      descripcion: m.descripcion ?? '',
      marca: m.marca ?? '',
      idCategoria: m.idCategoria ?? null,
      precioVenta: m.precioVenta,
      stockMinimo: m.stockMinimo,
      requiereReceta: m.requiereReceta,
      imagenUrl: m.imagenUrl ?? '',
      activo: m.activo
    };
    this.modalMedicamento.set(true);
  }

  guardarMedicamento(): void {
    if (this.procesando()) return;

    const f = this.formMed;
    const errores: string[] = [];
    if (!f.nombre?.trim()) errores.push('Nombre');
    if (f.precioVenta == null || f.precioVenta <= 0) errores.push('Precio de venta (mayor a 0)');
    if (f.stockMinimo == null || f.stockMinimo < 0 || !Number.isInteger(f.stockMinimo)) errores.push('Stock mínimo (entero, 0 o más)');
    if (f.idCategoria === 'NUEVA' && !this.nuevaCategoria.trim()) errores.push('Nombre de la nueva categoría');
    if (this.hayErrores(errores)) return;

    this.procesando.set(true);

    const categoria$: Observable<number | null> = f.idCategoria === 'NUEVA'
      ? this.service.crearCategoria(this.nuevaCategoria.trim()).pipe(
          map(c => {
            this.categorias.update(l => l.some(x => x.id === c.id) ? l : [...l, c].sort((a, b) => a.nombre.localeCompare(b.nombre)));
            this.formMed.idCategoria = c.id;
            this.nuevaCategoria = '';
            return c.id;
          })
        )
      : of(f.idCategoria as number | null);

    categoria$.pipe(
      switchMap(idCategoria => {
        const datos: MedicamentoPayload = { ...this.formMed, idCategoria };
        return this.editandoMedId
          ? this.service.actualizarMedicamento(this.editandoMedId, datos)
          : this.service.crearMedicamento(datos);
      })
    ).subscribe({
      next: (res) => this.alGuardar(this.modalMedicamento, res, true),
      error: (err) => this.alFallar(err)
    });
  }

  eliminarMedicamento(m: MedicamentoInventario): void {
    if (!confirm(`¿Eliminar "${m.nombre}"?\n\nSi tiene lotes registrados, se desactivará en lugar de borrarse.`)) return;
    this.service.eliminarMedicamento(m.id).subscribe({
      next: (res) => {
        this.cargarDatos();
        setTimeout(() => alert(res?.mensaje ?? 'Medicamento eliminado.'));
      },
      error: (err) => this.mostrarError(err, 'No se pudo eliminar el medicamento.')
    });
  }

  // ───────── Lotes ─────────

  abrirNuevoLote(): void {
    this.editandoLoteId = null;
    this.formLote = { ...this.loteVacio(), idMedicamento: this.filtroMedicamento() };
    this.modalLote.set(true);
  }

  editarLote(l: Lote): void {
    this.editandoLoteId = l.id;
    this.formLote = {
      idMedicamento: l.idMedicamento,
      idSucursal: l.idSucursal,
      numeroLote: l.numeroLote,
      fechaIngreso: l.fechaIngreso.split('T')[0],
      fechaVencimiento: l.fechaVencimiento.split('T')[0],
      cantidad: l.cantidadInicial
    };
    this.modalLote.set(true);
  }

  guardarLote(): void {
    if (this.procesando()) return;

    const f = this.formLote;
    const esNuevo = !this.editandoLoteId;
    const errores: string[] = [];

    if (esNuevo && f.idMedicamento == null) errores.push('Medicamento');
    if (esNuevo && f.idSucursal == null) errores.push('Sucursal');
    if (!f.numeroLote?.trim()) errores.push('Número de lote');
    if (!f.fechaIngreso) errores.push('Fecha de ingreso');
    else if (esNuevo && f.fechaIngreso > this.hoy) errores.push('Fecha de ingreso (no puede ser futura)');
    if (!f.fechaVencimiento) errores.push('Fecha de vencimiento');
    else {
      if (f.fechaIngreso && f.fechaVencimiento <= f.fechaIngreso) errores.push('Vencimiento (debe ser posterior al ingreso)');
      if (esNuevo && f.fechaVencimiento <= this.hoy) errores.push('Vencimiento (no se pueden ingresar lotes vencidos)');
    }
    if (esNuevo && (f.cantidad == null || f.cantidad <= 0 || !Number.isInteger(f.cantidad))) errores.push('Cantidad (entero mayor a 0)');
    if (this.hayErrores(errores)) return;

    this.procesando.set(true);

    const peticion = esNuevo
      ? this.service.registrarEntrada({ ...f, numeroLote: f.numeroLote.trim() })
      : this.service.actualizarLote(this.editandoLoteId!, {
          numeroLote: f.numeroLote.trim(),
          fechaIngreso: f.fechaIngreso,
          fechaVencimiento: f.fechaVencimiento
        });

    peticion.subscribe({
      next: (res) => this.alGuardar(this.modalLote, res),
      error: (err) => this.alFallar(err)
    });
  }

  abrirSalida(l: Lote): void {
    this.loteSalida = l;
    this.formSalida = { tipo: this.tiposSalida[0], cantidad: null, observacion: '' };
    this.modalSalida.set(true);
  }

  guardarSalida(): void {
    if (this.procesando() || !this.loteSalida) return;

    const f = this.formSalida;
    const errores: string[] = [];
    if (!f.tipo) errores.push('Tipo de salida');
    if (f.cantidad == null || f.cantidad <= 0 || !Number.isInteger(f.cantidad)) errores.push('Cantidad (entero mayor a 0)');
    else if (f.cantidad > this.loteSalida.cantidadDisponible) errores.push(`Cantidad (máximo ${this.loteSalida.cantidadDisponible})`);
    if (!f.observacion.trim()) errores.push('Motivo / observación');
    if (this.hayErrores(errores)) return;

    this.procesando.set(true);
    this.service.registrarSalida(this.loteSalida.id, { ...f, observacion: f.observacion.trim() }).subscribe({
      next: (res) => this.alGuardar(this.modalSalida, res),
      error: (err) => this.alFallar(err)
    });
  }

  eliminarLote(l: Lote): void {
    if (!confirm(`¿Eliminar el lote ${l.numeroLote} de ${l.medicamento}?`)) return;
    this.service.eliminarLote(l.id).subscribe({
      next: (res) => {
        this.cargarDatos();
        setTimeout(() => alert(res?.mensaje ?? 'Lote eliminado.'));
      },
      error: (err) => this.mostrarError(err, 'No se pudo eliminar el lote.')
    });
  }

  // ───────── Utilidades ─────────

  cerrarModales(): void {
    this.modalMedicamento.set(false);
    this.modalLote.set(false);
    this.modalSalida.set(false);
    this.procesando.set(false);
  }

  claseEstado(estado: EstadoLote): string {
    return { 'Vigente': 'b-verde', 'Por vencer': 'b-amarillo', 'Vencido': 'b-rojo', 'Agotado': 'b-gris' }[estado] ?? 'b-gris';
  }

  claseMovimiento(tipo: string): string {
    if (tipo === 'Entrada') return 'b-verde';
    if (tipo === 'Venta') return 'b-azul';
    if (tipo === 'Uso clínico') return 'b-amarillo';
    return 'b-rojo';
  }

  private alGuardar(modal: WritableSignal<boolean>, res: any, recargarCatalogos = false): void {
    modal.set(false);
    this.procesando.set(false);
    this.cargarDatos();
    if (recargarCatalogos) this.cargarCatalogos();
    setTimeout(() => alert(res?.mensaje ?? 'Guardado correctamente.'));
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

  private medVacio(): FormMedicamento {
    return {
      codigo: '', nombre: '', descripcion: '', marca: '', idCategoria: null,
      precioVenta: null, stockMinimo: 10, requiereReceta: false, imagenUrl: '', activo: true
    };
  }

  private loteVacio(): LotePayload {
    return {
      idMedicamento: null, idSucursal: this.idSucursal(), numeroLote: '',
      fechaIngreso: this.hoy, fechaVencimiento: '', cantidad: null
    };
  }
}
