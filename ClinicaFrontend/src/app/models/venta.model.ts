export interface Producto {
  id: number;
  nombre: string;
  codigoBarra?: string;
  precioUnitario: number;
  stock: number;
  categoria?: string;
}

export interface ItemCarrito {
  producto: Producto;
  cantidad: number;
  subtotal: number;
}

export interface DetalleVentaDto {
  productoId: number;
  cantidad: number;
  precioUnitario: number;
}

export interface RegistrarVentaDto {
  clienteNombre: string;
  detalles: DetalleVentaDto[];
  metodoPago: string;
}
