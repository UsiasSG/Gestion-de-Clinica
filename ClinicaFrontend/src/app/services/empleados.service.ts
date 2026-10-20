import { Injectable, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';

export interface Empleado {
  id: number;
  nombres: string;
  apellidos: string;
  nombreCompleto: string;
  tipoEmpleado: string;
  puesto?: string | null;
  telefono?: string | null;
  correo: string;
  idEspecialidad?: number | null;
  especialidadNombre?: string | null;
  idSucursal?: number | null;
  sucursalNombre?: string | null;
  tieneAcceso: boolean;
  nombreUsuario?: string | null;
  idRol?: number | null;
  rolNombre?: string | null;
}

export interface EmpleadoPayload {
  nombres: string;
  apellidos: string;
  tipoEmpleado: string;
  puesto: string;
  telefono: string;
  correo: string;
  idEspecialidad: number | null;
  idSucursal: number | null;
  crearAcceso: boolean;
  nombreUsuario: string;
  password: string;
  idRol: number | null;
}

export interface Catalogo {
  id: number;
  nombre: string;
}

export interface DatosAuxiliares {
  especialidades: Catalogo[];
  sucursales: Catalogo[];
  roles: Catalogo[];
}

@Injectable({ providedIn: 'root' })
export class EmpleadosService {
  private http = inject(HttpClient);
  private apiUrl = 'http://localhost:5219/api/Empleados';

  obtenerEmpleados(): Observable<Empleado[]> {
    return this.http.get<Empleado[]>(this.apiUrl);
  }

  obtenerAuxiliares(): Observable<DatosAuxiliares> {
    return this.http.get<DatosAuxiliares>(`${this.apiUrl}/auxiliares`);
  }

  crearEmpleado(datos: EmpleadoPayload): Observable<any> {
    return this.http.post(this.apiUrl, datos);
  }

  actualizarEmpleado(id: number, datos: EmpleadoPayload): Observable<any> {
    return this.http.put(`${this.apiUrl}/${id}`, datos);
  }

  eliminarEmpleado(id: number): Observable<any> {
    return this.http.delete(`${this.apiUrl}/${id}`);
  }

  crearEspecialidad(nombre: string): Observable<Catalogo> {
    return this.http.post<Catalogo>(`${this.apiUrl}/especialidades`, { nombre });
  }
}
