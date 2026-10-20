export interface PacienteResumen {
  id: number;
  nombreCompleto: string;
  documentoIdentificacion?: string | null;
  fechaNacimiento: string;
  genero?: string | null;
  tipoSangre?: string | null;
  alergias?: string | null;
  telefono?: string | null;
  contactoEmergencia?: string | null;
}

export interface DiagnosticoItem {
  id?: number;
  codigoCie?: string | null;
  descripcion: string;
}

export interface TratamientoItem {
  id?: number;
  descripcion: string;
  indicaciones?: string | null;
  fechaInicio?: string | null;
  fechaFin?: string | null;
}

export interface Consulta {
  id: number;
  fechaConsulta: string;
  doctor: string;
  especialidad?: string | null;
  sucursal?: string | null;
  motivoConsulta?: string | null;
  sintomatologia?: string | null;
  observaciones?: string | null;
  diagnosticos: DiagnosticoItem[];
  tratamientos: TratamientoItem[];
}

export interface DiagnosticoRegistro {
  id: number;
  idConsulta: number;
  codigoCie?: string | null;
  descripcion: string;
  fechaDiagnostico: string;
  doctor: string;
  motivoConsulta?: string | null;
}

export interface ExamenRegistro {
  id: number;
  idConsulta?: number | null;
  nombreExamen: string;
  resultado?: string | null;
  fechaExamen: string;
  observaciones?: string | null;
  estado: 'Pendiente' | 'Completado';
  adjuntos: number;
}

export interface EvolucionRegistro {
  id: number;
  fechaRegistro: string;
  notaEvolucion: string;
  estadoSalud?: string | null;
  responsable: string;
  tipoResponsable?: string | null;
}

export interface ArchivoClinico {
  id: number;
  idExamen?: number | null;
  examen?: string | null;
  nombreArchivo: string;
  tipoContenido: string;
  tamanoBytes: number;
  categoria: string;
  descripcion?: string | null;
  fechaSubida: string;
  subidoPor?: string | null;
}

export interface PermisosHistorial {
  crear: boolean;
  modificar: boolean;
  eliminar: boolean;
  diagnosticar: boolean;
  esDoctor: boolean;
  esPersonalClinico: boolean;
  empleado?: string | null;
}

export interface HistorialClinico {
  paciente: PacienteResumen;
  consultas: Consulta[];
  diagnosticos: DiagnosticoRegistro[];
  examenes: ExamenRegistro[];
  evolucion: EvolucionRegistro[];
  archivos: ArchivoClinico[];
  permisos: PermisosHistorial;
  medicos: EmpleadoClinico[];
  personalClinico: EmpleadoClinico[];
}

export interface ConsultaPayload {
  idPaciente: number;
  motivoConsulta: string;
  sintomatologia: string;
  observaciones: string;
  diagnosticos: DiagnosticoItem[];
  tratamientos: TratamientoItem[];
  idDoctor: number | null;
}

export interface DiagnosticoPayload {
  idConsulta: number | null;
  codigoCie: string;
  descripcion: string;
  idDoctor: number | null;
}

export interface ExamenPayload {
  idPaciente: number;
  idConsulta: number | null;
  nombreExamen: string;
  fechaExamen: string;
  resultado: string;
  observaciones: string;
}

export interface EvolucionPayload {
  idPaciente: number;
  notaEvolucion: string;
  estadoSalud: string;
  idResponsable: number | null;
}

export interface ArchivoPayload {
  archivo: File;
  idPaciente: number;
  idExamen: number | null;
  categoria: string;
  descripcion: string;
}

export interface EmpleadoClinico {
  id: number;
  nombre: string;
  especialidad?: string | null;
  tipo?: string | null;
}
