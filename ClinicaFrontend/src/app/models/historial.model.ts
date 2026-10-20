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

export interface TratamientoRegistro {
  id: number;
  idConsulta?: number | null;
  idDiagnostico?: number | null;
  descripcion: string;
  indicaciones?: string | null;
  fechaInicio?: string | null;
  fechaFin?: string | null;
  estado: string;
  fechaRegistro: string;
  doctor: string;
  diagnostico?: string | null;
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

export interface EmpleadoClinico {
  id: number;
  nombre: string;
  especialidad?: string | null;
  tipo?: string | null;
}

export interface PermisosHistorial {
  crear: boolean;
  modificar: boolean;
  eliminar: boolean;
  diagnosticar: boolean;
  modificarDiagnostico: boolean;
  eliminarDiagnostico: boolean;
  esDoctor: boolean;
  esPersonalClinico: boolean;
  empleado?: string | null;
}

export interface HistorialClinico {
  paciente: PacienteResumen;
  consultas: Consulta[];
  diagnosticos: DiagnosticoRegistro[];
  tratamientos: TratamientoRegistro[];
  examenes: ExamenRegistro[];
  evolucion: EvolucionRegistro[];
  archivos: ArchivoClinico[];
  medicos: EmpleadoClinico[];
  personalClinico: EmpleadoClinico[];
  permisos: PermisosHistorial;
}

// ───────── Payloads ─────────

export interface ConsultaPayload {
  idPaciente: number;
  idDoctor: number | null;
  motivoConsulta: string;
  sintomatologia: string;
  observaciones: string;
  diagnosticos: DiagnosticoItem[];
  tratamientos: TratamientoItem[];
}

export interface ConsultaUpdatePayload {
  motivoConsulta: string;
  sintomatologia: string;
  observaciones: string;
}

export interface DiagnosticoPayload {
  idConsulta: number | null;
  idDoctor: number | null;
  codigoCie: string;
  descripcion: string;
}

export interface DiagnosticoUpdatePayload {
  codigoCie: string;
  descripcion: string;
}

export interface TratamientoPayload {
  idPaciente: number;
  idConsulta: number | null;
  idDiagnostico: number | null;
  idDoctor: number | null;
  descripcion: string;
  indicaciones: string;
  fechaInicio: string | null;
  fechaFin: string | null;
  estado: string;
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
  idResponsable: number | null;
  notaEvolucion: string;
  estadoSalud: string;
}

export interface ArchivoPayload {
  archivo: File;
  idPaciente: number;
  idExamen: number | null;
  categoria: string;
  descripcion: string;
}
