export interface Catalogo {
  id: number;
  nombre: string;
}

export interface CatalogosInventario {
  categorias: Catalogo[];
  sucursales: Catalogo[];
  marcas: string[];
  idSucursalUsuario: number | null;
}

export interface MedicamentoInventario {
  id: number;
  codigo?: string | null;
  nombre: string;
  descripcion?: string | null;
  marca?: string | null;
  idCategoria?: number | null;
  categoria?: string | null;
  precioVenta: number;
  stockMinimo: number;
  requiereReceta: boolean;
  imagenUrl?: string | null;
  activo: boolean;
  existencia: number;
  cantidadVencida: number;
  proximoVencimiento?: string | null;
}

export interface MedicamentoPayload {
  codigo: string;
  nombre: string;
  descripcion: string;
  marca: string;
  idCategoria: number | null;
  precioVenta: number | null;
  stockMinimo: number | null;
  requiereReceta: boolean;
  imagenUrl: string;
  activo: boolean;
}

export type EstadoLote = 'Vigente' | 'Por vencer' | 'Vencido' | 'Agotado';

export interface Lote {
  id: number;
  numeroLote: string;
  idMedicamento: number;
  medicamento: string;
  idSucursal: number;
  sucursal: string;
  fechaIngreso: string;
  fechaVencimiento: string;
  cantidadInicial: number;
  cantidadDisponible: number;
  estado: EstadoLote;
  tieneSalidas: boolean;
}

export interface LotePayload {
  idMedicamento: number | null;
  idSucursal: number | null;
  numeroLote: string;
  fechaIngreso: string;
  fechaVencimiento: string;
  cantidad: number | null;
}

export type TipoSalida = 'Uso clínico' | 'Merma' | 'Baja por vencimiento';

export interface SalidaPayload {
  tipo: TipoSalida | '';
  cantidad: number | null;
  observacion: string;
}

export interface Movimiento {
  id: number;
  fecha: string;
  tipo: string;
  cantidad: number;
  observacion?: string | null;
  idVenta?: number | null;
  idLote: number;
  numeroLote: string;
  medicamento?: string | null;
  usuario?: string | null;
}
