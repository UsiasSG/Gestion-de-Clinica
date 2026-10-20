import { Injectable, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';

export interface Rol {
  id: number;
  nombre: string;
  descripcion?: string | null;
  usuarios: number;
  modulosConAcceso: number;
}

export interface FilaPermiso {
  idModulo: number;
  modulo: string;
  codigo: string;
  consultar: boolean;
  crear: boolean;
  modificar: boolean;
  eliminar: boolean;
}

@Injectable({ providedIn: 'root' })
export class RolesService {
  private http = inject(HttpClient);
  private apiUrl = 'http://localhost:5219/api/Roles';

  obtenerRoles(): Observable<Rol[]> {
    return this.http.get<Rol[]>(this.apiUrl);
  }

  obtenerMatriz(idRol: number): Observable<FilaPermiso[]> {
    return this.http.get<FilaPermiso[]>(`${this.apiUrl}/${idRol}/permisos`);
  }

  guardarMatriz(idRol: number, matriz: FilaPermiso[]): Observable<any> {
    return this.http.put(`${this.apiUrl}/${idRol}/permisos`, matriz);
  }

  crearRol(datos: { nombre: string; descripcion: string }): Observable<any> {
    return this.http.post(this.apiUrl, datos);
  }

  actualizarRol(id: number, datos: { nombre: string; descripcion: string }): Observable<any> {
    return this.http.put(`${this.apiUrl}/${id}`, datos);
  }

  eliminarRol(id: number): Observable<any> {
    return this.http.delete(`${this.apiUrl}/${id}`);
  }
}
