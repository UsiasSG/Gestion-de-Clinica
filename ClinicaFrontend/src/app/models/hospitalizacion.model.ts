export type EstadoHospitalizacion = 'Solicitada' | 'Activa' | 'Alta' | 'Cancelada';

export interface HospitalizacionResumen {
  id: number;
  idPaciente: number;
  paciente: string;
  tipo: string;
  motivo: string;
  estado: EstadoHospitalizacion;
  fechaSolicitud: string;
  fechaIngreso?: string | null;
  fechaAlta?: string | null;
  medico?: string | null;
  habitacion?: string | null;
  cargosPendientes: number;
}

export interface CatalogosHospitalizacion {
  medicos: { id: number; nombre: string; especialidad?: string | null }[];
  cirugias: { id: number; nombre: string; precio: number }[];
  servicios: { id: number; nombre: string; tipo: string; precio: number }[];
  medicamentos: { id: number; nombre: string; marca?: string | null; precio: number; existencia: number }[];
  habitaciones: { id: number; numeroHabitacion: string; tipoHabitacion?: string | null; precioDiario?: number | null; capacidad: number; ocupantes: number }[];
  tipos: string[];
}

export interface PacienteHospitalizable {
  id: number;
  nombreCompleto: string;
  documento?: string | null;
}

export interface CirugiaHospitalaria {
  id: number;
  idServicio?: number | null;
  procedimiento: string;
  idCirujano?: number | null;
  cirujano?: string | null;
  fechaProgramada: string;
  fechaRealizada?: string | null;
  estado: 'Programada' | 'Realizada' | 'Cancelada';
  costoBase: number;
  costoFinal: number;
  observaciones?: string | null;
}

export interface CargoHospitalario {
  id: number;
  tipo: string;
  descripcion: string;
  cantidad: number;
  precioUnitario: number;
  subtotal: number;
  fecha: string;
  estado: 'Pendiente' | 'Facturado' | 'Anulado';
}

export interface DetalleHospitalizacion {
  hospitalizacion: {
    id: number;
    idPaciente: number;
    paciente: string;
    documento?: string | null;
    alergias?: string | null;
    tipoSangre?: string | null;
    tipo: string;
    motivo: string;
    estado: EstadoHospitalizacion;
    fechaSolicitud: string;
    fechaIngreso?: string | null;
    fechaAlta?: string | null;
    observacionesAlta?: string | null;
    idMedicoResponsable?: number | null;
    medico?: string | null;
    diagnostico?: string | null;
  };
  asignacion: {
    id: number;
    idHabitacion: number;
    numero?: string | null;
    tipo?: string | null;
    precioDiario?: number | null;
    fechaIngreso: string;
    fechaEgreso?: string | null;
  } | null;
  cirugias: CirugiaHospitalaria[];
  cargos: CargoHospitalario[];
  estancia: {
    dias: number;
    costoHabitacion: number;
    subtotalCargos: number;
    totalEstimado: number;
    ivaEstimado: number;
  };
}

export interface SolicitudHospitalizacionPayload {
  idPaciente: number;
  idDiagnostico: number | null;
  idMedicoResponsable: number | null;
  tipo: string;
  motivo: string;
}
