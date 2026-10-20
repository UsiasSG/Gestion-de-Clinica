export type EstadoHabitacion = 'Disponible' | 'Ocupada' | 'Limpieza' | 'Mantenimiento';

export interface Catalogo {
  id: number;
  nombre: string;
}

export interface CatalogosHabitaciones {
  sucursales: Catalogo[];
  idSucursalUsuario: number | null;
  tipos: string[];
}

export interface Ocupante {
  idAsignacion: number;
  idPaciente: number | null;
  pacienteNombre: string;
  fechaIngreso: string;
  observaciones?: string | null;
}

export interface Habitacion {
  id: number;
  numeroHabitacion: string;
  tipoHabitacion: string | null;
  capacidad: number;
  estado: EstadoHabitacion;
  precioDiario: number | null;
  idSucursal: number | null;
  sucursalNombre: string | null;
  ocupantes: Ocupante[];
}

export interface HabitacionPayload {
  numeroHabitacion: string;
  tipoHabitacion: string;
  capacidad: number | null;
  precioDiario: number | null;
  idSucursal: number | null;
}

export interface AsignacionPayload {
  idHabitacion: number;
  idPaciente: number;
  fechaIngreso: string;
  observaciones: string;
}

export interface EgresoPayload {
  fechaEgreso: string;
  observaciones: string;
}

export interface HistorialAsignacion {
  id: number;
  pacienteNombre: string;
  fechaIngreso: string;
  fechaEgreso: string | null;
  estado: string | null;
  observaciones: string | null;
}

export interface PacienteDisponible {
  id: number;
  nombreCompleto: string;
  documento?: string | null;
}

export interface RespuestaHabitacion {
  mensaje: string;
  habitacion?: Habitacion | null;
}
