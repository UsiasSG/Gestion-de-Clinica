import { Component, OnDestroy, OnInit, computed, inject, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { PosService } from '../../services/pos.service';
import { AuthService } from '../../services/auth.service';
import { ItemCarrito, MetodoPago, PacientePOS, ProductoPOS, TicketVenta } from '../../models/pos.model';

const TASA_IVA = 0.12;
const redondear = (n: number) => Math.round(n * 100) / 100;

@Component({
  selector: 'app-pos',
  standalone: true,
  imports: [CommonModule, FormsModule],
  templateUrl: './pos.html',
  styleUrl: './pos.css'
})
export class PosComponent implements OnInit, OnDestroy {
  private posService = inject(PosService);
  private auth = inject(AuthService);

  usuario = this.auth.usuarioActual;

  // Catálogo
  productos = signal<ProductoPOS[]>([]);
  cargando = signal(true);
  errorCarga = signal<string | null>(null);
  busquedaProducto = signal('');
  categoriaSeleccionada = signal('Todas');

  // Carrito
  carrito = signal<ItemCarrito[]>([]);

  // Paciente
  busquedaPaciente = '';
  pacientes = signal<PacientePOS[]>([]);
  mostrarDropdown = signal(false);
  pacienteSeleccionado = signal<PacientePOS | null>(null);
  private timerPaciente?: ReturnType<typeof setTimeout>;

  // Cobro
  modalCobro = signal(false);
  procesando = signal(false);
  ticket = signal<TicketVenta | null>(null);

  readonly metodosPago: { valor: MetodoPago; etiqueta: string }[] = [
    { valor: 'Efectivo', etiqueta: '💵 Efectivo' },
    { valor: 'Tarjeta', etiqueta: '💳 Tarjeta' },
    { valor: 'Transferencia', etiqueta: '🏦 Transferencia' }
  ];

  nit = 'CF';
  nombreFactura = 'Consumidor Final';
  metodoPago: MetodoPago = 'Efectivo';
  montoEfectivo: number | null = null;
  numeroVoucher = '';
  referenciaTransferencia = '';

  // Derivados
  categorias = computed(() => {
    const unicas = [...new Set(this.productos().map(p => p.categoria || 'General'))];
    return ['Todas', ...unicas.sort((a, b) => a.localeCompare(b))];
  });

  productosFiltrados = computed(() => {
    const t = this.busquedaProducto().trim().toLowerCase();
    const cat = this.categoriaSeleccionada();
    return this.productos().filter(p =>
      (cat === 'Todas' || (p.categoria || 'General') === cat) &&
      (!t || [p.nombre, p.codigo, p.marca].some(v => v?.toLowerCase().includes(t)))
    );
  });

  subtotal = computed(() => redondear(this.carrito().reduce((s, i) => s + i.subtotal, 0)));
  impuesto = computed(() => redondear(this.subtotal() * TASA_IVA));
  total = computed(() => redondear(this.subtotal() + this.impuesto()));
  totalArticulos = computed(() => this.carrito().reduce((s, i) => s + i.cantidad, 0));

  get cambio(): number {
    return this.montoEfectivo != null ? redondear(this.montoEfectivo - this.total()) : 0;
  }

  ngOnInit(): void {
    this.cargarProductos();
  }

  ngOnDestroy(): void {
    clearTimeout(this.timerPaciente);
  }

  // ───────── Catálogo ─────────

  cargarProductos(): void {
    this.cargando.set(true);
    this.posService.obtenerProductos().subscribe({
      next: (data) => {
        this.productos.set(data ?? []);
        this.errorCarga.set(null);
        this.cargando.set(false);
        this.sincronizarCarritoConStock();
      },
      error: (err) => {
        console.error('Error al cargar productos:', err);
        this.productos.set([]);
        this.errorCarga.set(err?.error?.mensaje ?? 'No se pudo cargar el catálogo. Verifique la conexión con el servidor.');
        this.cargando.set(false);
      }
    });
  }

  // Si el stock cambió (otra caja vendió), ajusta el carrito a lo disponible
  private sincronizarCarritoConStock(): void {
    const stockPorId = new Map(this.productos().map(p => [p.id, p]));
    this.carrito.update(lista => lista
      .map(i => {
        const actual = stockPorId.get(i.producto.id);
        if (!actual || actual.stock <= 0) return null;
        const cantidad = Math.min(i.cantidad, actual.stock);
        return { producto: actual, cantidad, subtotal: redondear(cantidad * actual.precio) };
      })
      .filter((i): i is ItemCarrito => i !== null));
  }

  // ───────── Carrito ─────────

  cantidadEnCarrito(idProducto: number): number {
    return this.carrito().find(i => i.producto.id === idProducto)?.cantidad ?? 0;
  }

  agregarAlCarrito(prod: ProductoPOS): void {
    if (prod.stock <= 0) return;

    if (this.cantidadEnCarrito(prod.id) >= prod.stock) {
      alert(`Solo hay ${prod.stock} unidades disponibles de ${prod.nombre}.`);
      return;
    }

    this.carrito.update(lista => {
      const existe = lista.some(i => i.producto.id === prod.id);
      return existe
        ? lista.map(i => i.producto.id === prod.id
            ? { ...i, cantidad: i.cantidad + 1, subtotal: redondear((i.cantidad + 1) * prod.precio) }
            : i)
        : [...lista, { producto: prod, cantidad: 1, subtotal: redondear(prod.precio) }];
    });
  }

  actualizarCantidad(item: ItemCarrito, delta: number): void {
    const nueva = item.cantidad + delta;
    if (nueva <= 0) {
      this.quitarDelCarrito(item);
      return;
    }
    if (nueva > item.producto.stock) {
      alert(`Solo hay ${item.producto.stock} unidades disponibles de ${item.producto.nombre}.`);
      return;
    }
    this.carrito.update(lista => lista.map(i => i.producto.id === item.producto.id
      ? { ...i, cantidad: nueva, subtotal: redondear(nueva * i.producto.precio) }
      : i));
  }

  quitarDelCarrito(item: ItemCarrito): void {
    this.carrito.update(lista => lista.filter(i => i.producto.id !== item.producto.id));
  }

  limpiarCarrito(): void {
    if (this.carrito().length === 0) return;
    if (!confirm('¿Vaciar la orden actual?')) return;
    this.carrito.set([]);
    this.limpiarPaciente();
  }

  // ───────── Paciente ─────────

  onBuscarPaciente(valor: string): void {
    this.busquedaPaciente = valor;
    this.pacienteSeleccionado.set(null);
    clearTimeout(this.timerPaciente);

    const q = valor.trim();
    if (q.length < 2) {
      this.pacientes.set([]);
      this.mostrarDropdown.set(false);
      return;
    }

    this.timerPaciente = setTimeout(() => {
      this.posService.buscarPacientes(q).subscribe({
        next: (data) => {
          this.pacientes.set(data ?? []);
          this.mostrarDropdown.set(true);
        },
        error: () => this.pacientes.set([])
      });
    }, 300);
  }

  seleccionarPaciente(p: PacientePOS): void {
    this.pacienteSeleccionado.set(p);
    this.busquedaPaciente = p.nombreCompleto;
    this.pacientes.set([]);
    this.mostrarDropdown.set(false);
  }

  limpiarPaciente(): void {
    this.pacienteSeleccionado.set(null);
    this.busquedaPaciente = '';
    this.pacientes.set([]);
    this.mostrarDropdown.set(false);
  }

  // ───────── Cobro ─────────

  iniciarProcesoCobro(): void {
    if (this.carrito().length === 0) return;

    const conReceta = this.carrito().filter(i => i.producto.requiereReceta).map(i => i.producto.nombre);
    if (conReceta.length > 0 &&
        !confirm(`Los siguientes productos requieren receta médica:\n\n• ${conReceta.join('\n• ')}\n\n¿Confirma que verificó la receta?`)) {
      return;
    }

    const p = this.pacienteSeleccionado();
    this.nit = p?.nit?.trim() ? p.nit : 'CF';
    this.nombreFactura = p?.nombreCompleto ?? 'Consumidor Final';
    this.metodoPago = 'Efectivo';
    this.montoEfectivo = null;
    this.numeroVoucher = '';
    this.referenciaTransferencia = '';
    this.modalCobro.set(true);
  }

  cerrarModal(): void {
    this.modalCobro.set(false);
    this.procesando.set(false);
  }

  pagoExacto(): void {
    this.montoEfectivo = this.total();
  }

  confirmarVenta(): void {
    if (this.procesando()) return;

    const nit = this.nit.trim().toUpperCase();
    const total = this.total();
    const errores: string[] = [];

    if (!nit) errores.push('NIT');
    else if (nit !== 'CF' && !/^\d+-?[\dK]$/.test(nit)) errores.push('NIT (use CF o un NIT válido, ej. 1234567-8)');
    if (!this.nombreFactura.trim()) errores.push('Nombre a facturar');

    if (this.metodoPago === 'Efectivo' && (this.montoEfectivo == null || this.montoEfectivo < total)) {
      errores.push(`Efectivo recibido (mínimo Q${total.toFixed(2)})`);
    }
    if (this.metodoPago === 'Tarjeta' && !this.numeroVoucher.trim()) errores.push('Número de voucher / autorización');
    if (this.metodoPago === 'Transferencia' && !this.referenciaTransferencia.trim()) errores.push('Número de referencia bancaria');

    if (errores.length > 0) {
      alert('Complete o corrija los siguientes campos:\n\n• ' + errores.join('\n• '));
      return;
    }

    this.procesando.set(true);

    const items = this.carrito();
    const metodo = this.metodoPago;
    const efectivo = metodo === 'Efectivo' ? this.montoEfectivo : null;
    const referencia = metodo === 'Tarjeta' ? this.numeroVoucher.trim()
                     : metodo === 'Transferencia' ? this.referenciaTransferencia.trim()
                     : null;
    const nombre = this.nombreFactura.trim();

    this.posService.registrarVenta({
      idPaciente: this.pacienteSeleccionado()?.id ?? null,
      detalles: items.map(i => ({ productoId: i.producto.id, cantidad: i.cantidad }))
    }).subscribe({
      next: (res) => {
        this.ticket.set({
          numero: res.ventaId,
          fecha: new Date(),
          sucursal: this.usuario()?.nombreSucursal ?? 'Sucursal',
          cajero: this.usuario()?.nombreUsuario ?? '',
          nit,
          nombre,
          items: items.map(i => ({ nombre: i.producto.nombre, cantidad: i.cantidad, precio: i.producto.precio, subtotal: i.subtotal })),
          subtotal: res.subtotal,
          impuesto: res.impuesto,
          total: res.total,
          metodoPago: metodo,
          efectivo,
          cambio: efectivo != null ? redondear(efectivo - res.total) : null,
          referencia
        });

        this.carrito.set([]);
        this.limpiarPaciente();
        this.modalCobro.set(false);
        this.procesando.set(false);
        this.cargarProductos();
      },
      error: (err) => {
        console.error('Error al registrar venta:', err);
        this.procesando.set(false);
        const mensaje = err?.error?.mensaje ?? 'No se pudo registrar la venta.';
        setTimeout(() => alert(mensaje));
        if (err?.status === 400) this.cargarProductos(); // refresca stock si fue por existencias
      }
    });
  }

  // ───────── Ticket ─────────

  imprimirTicket(): void {
    window.print();
  }

  nuevaVenta(): void {
    this.ticket.set(null);
  }
}
