import { Injectable, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { Paciente } from '../models/paciente.model';

@Injectable({
  providedIn: 'root'
})
export class PacienteService {
  private http = inject(HttpClient);
  private apiUrl = 'http://localhost:5219/api/Pacientes';

  obtenerPacientes(busqueda?: string): Observable<Paciente[]> {
  const url = busqueda ? `${this.apiUrl}?busqueda=${encodeURIComponent(busqueda)}` : this.apiUrl;
  return this.http.get<Paciente[]>(url);
  }

  obtenerPacientePorId(id: number): Observable<Paciente> {
    return this.http.get<Paciente>(`${this.apiUrl}/${id}`);
  }

  registrarPaciente(paciente: any): Observable<any> {
    return this.http.post<any>(this.apiUrl, paciente);
  }

  actualizarPaciente(id: number, paciente: any): Observable<any> {
    return this.http.put<any>(`${this.apiUrl}/${id}`, paciente);
  }

  eliminarPaciente(id: number): Observable<any> {
    return this.http.delete<any>(`${this.apiUrl}/${id}`);
  }

  obtenerExpediente(id: number): Observable<any> {
    return this.http.get<any>(`${this.apiUrl}/${id}/expediente`);
  }
}
