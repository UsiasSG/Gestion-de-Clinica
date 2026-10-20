export type EstadoCargo = 'Pendiente' | 'Facturado' | 'Anulado';
export type MetodoPagoCuenta = 'Efectivo' | 'Tarjeta' | 'Transferencia';

export interface CargoCuenta {
  id: number;
  tipo: string;
  descripcion: string;
  cantidad: number;
  precioUnitario: number;
  subtotal: number;
  fecha: string;
  estado: EstadoCargo;
  idFactura?: number | null;
  idHospitalizacion?: number | null;
  cobroAlAlta: boolean;
}

export interface FacturaResumen {
  id: number;
  fecha: string;
  tipo: string;
  total: number;
  metodoPago: string;
  estado: 'Pagada' | 'Anulada';
  items: number;
}

export interface VentaFarmacia {
  id: number;
  fechaVenta: string;
  total: number;
  estado?: string | null;
  productos: string[];
}

export interface ResumenCuenta {
  pendienteSubtotal: number;
  pendienteIva: number;
  pendienteTotal: number;
  totalFacturado: number;
  totalFarmacia: number;
}

export interface ServicioCargo {
  id: number;
  nombre: string;
  tipo: string;
  precio: number;
}

export interface CuentaPaciente {
  cargos: CargoCuenta[];
  facturas: FacturaResumen[];
  ventas: VentaFarmacia[];
  resumen: ResumenCuenta;
  servicios: ServicioCargo[];
  medicamentos: MedicamentoCargo[];
}

export interface FacturaDetalle {
  id: number;
  fecha: string;
  tipo: string;
  nit: string;
  nombreFactura: string;
  subtotal: number;
  impuesto: number;
  total: number;
  metodoPago: string;
  referencia?: string | null;
  montoRecibido?: number | null;
  cambio?: number | null;
  estado: string;
  motivoAnulacion?: string | null;
  paciente?: string | null;
  sucursal?: string | null;
  cajero?: string | null;
  items: { tipo: string; descripcion: string; cantidad: number; precioUnitario: number; subtotal: number }[];
}

export interface CobroPayload {
  idPaciente: number;
  idsCargos: number[];
  nit: string;
  nombreFactura: string;
  metodoPago: MetodoPagoCuenta;
  montoRecibido: number | null;
  referencia: string;
}

export interface CargoManualPayload {
  idPaciente: number;
  idServicio: number | null;
  descripcion: string;
  cantidad: number;
  precioUnitario: number | null;
}

// ───── Tarifario ─────

export interface ServicioTarifario {
  id: number;
  codigo?: string | null;
  nombre: string;
  tipo: string;
  descripcion?: string | null;
  precio: number;
  activo: boolean;
}

export interface ServicioPayload {
  codigo: string;
  nombre: string;
  tipo: string;
  descripcion: string;
  precio: number | null;
  activo: boolean;
}
export interface MedicamentoCargo {
  id: number;
  nombre: string;
  marca?: string | null;
  precio: number;
  existencia: number;
}

export interface CargoMedicamentoPayload {
  idPaciente: number;
  idMedicamento: number;
  cantidad: number;
  observacion: string;
}
