export interface Paciente {
  id?: number;
  documentoIdentificacion?: string; 
  documento_identificacion?: string;
  nombres: string;
  apellidos: string;
  nombreCompleto?: string;
  fechaNacimiento: string;
  genero?: string;
  telefono?: string;
  direccion?: string;
  tipoSangre?: string;
  alergias?: string;
  esMenorEdad?: boolean;
  nombreEncargado?: string;
  dpiEncargado?: string;
  contactoEmergencia?: string;
  fechaRegistro?: string;
}

export interface ConsultaHistorial {
  id: number;
  fechaConsulta: string;
  doctorNombre: string;
  motivoConsulta?: string;
  sintomatologia?: string;
  observaciones?: string;
  diagnosticos: { codigoCie?: string; descripcion: string }[];
  tratamientos: { descripcion: string; indicaciones?: string; fechaInicio?: string; fechaFin?: string }[];
}

export interface VentaHistorial {
  id: number;
  fechaVenta: string;
  subtotal: number;
  impuesto: number;
  total: number;
  estado?: string;
  detalles: { medicamentoNombre: string; cantidad: number; precioUnitario: number; subtotal: number }[];
}

export interface ExpedienteClinico {
  paciente?: Paciente;
  consultas?: any[];
  diagnosticos?: any[];
  tratamientos?: any[];
  ventas?: any[];
}
