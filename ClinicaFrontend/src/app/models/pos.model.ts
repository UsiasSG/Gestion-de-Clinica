export interface ProductoPOS {
  id: number;
  codigo: string;
  nombre: string;
  marca?: string | null;
  descripcion?: string | null;
  categoria: string;
  precio: number;
  stock: number;
  stockMinimo?: number;
  imagenUrl?: string | null;
  requiereReceta?: boolean;
}

export interface PacientePOS {
  id: number;
  nombreCompleto: string;
  telefono?: string | null;
  nit?: string | null;
  dpi?: string | null;
}

export interface ItemCarrito {
  producto: ProductoPOS;
  cantidad: number;
  subtotal: number;
}

export type MetodoPago = 'Efectivo' | 'Tarjeta' | 'Transferencia';

// Sucursal, vendedor, precios y totales los determina el servidor
export interface VentaDTO {
  idPaciente: number | null;
  detalles: { productoId: number; cantidad: number }[];
}

export interface VentaRespuesta {
  mensaje: string;
  ventaId: number;
  subtotal: number;
  impuesto: number;
  total: number;
}

export interface TicketVenta {
  numero: number;
  fecha: Date;
  sucursal: string;
  cajero: string;
  nit: string;
  nombre: string;
  items: { nombre: string; cantidad: number; precio: number; subtotal: number }[];
  subtotal: number;
  impuesto: number;
  total: number;
  metodoPago: MetodoPago;
  efectivo: number | null;
  cambio: number | null;
  referencia: string | null;
}
