import { Injectable, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import {
  ArchivoPayload, ConsultaPayload, DiagnosticoPayload, EvolucionPayload, ExamenPayload, HistorialClinico
} from '../models/historial.model';

@Injectable({ providedIn: 'root' })
export class HistorialService {
  private http = inject(HttpClient);
  private apiUrl = 'http://localhost:5219/api/historial';

  obtenerHistorial(idPaciente: number): Observable<HistorialClinico> {
    return this.http.get<HistorialClinico>(`${this.apiUrl}/paciente/${idPaciente}`);
  }

  registrarConsulta(datos: ConsultaPayload): Observable<any> {
    return this.http.post(`${this.apiUrl}/consultas`, datos);
  }

  registrarDiagnostico(datos: DiagnosticoPayload): Observable<any> {
    return this.http.post(`${this.apiUrl}/diagnosticos`, datos);
  }

  registrarExamen(datos: ExamenPayload): Observable<any> {
    return this.http.post(`${this.apiUrl}/examenes`, datos);
  }

  registrarResultado(idExamen: number, resultado: string): Observable<any> {
    return this.http.put(`${this.apiUrl}/examenes/${idExamen}/resultado`, { resultado });
  }

  registrarEvolucion(datos: EvolucionPayload): Observable<any> {
    return this.http.post(`${this.apiUrl}/evolucion`, datos);
  }

  subirArchivo(datos: ArchivoPayload): Observable<any> {
    const form = new FormData();
    form.append('archivo', datos.archivo);
    form.append('idPaciente', String(datos.idPaciente));
    if (datos.idExamen != null) form.append('idExamen', String(datos.idExamen));
    form.append('categoria', datos.categoria);
    if (datos.descripcion) form.append('descripcion', datos.descripcion);
    return this.http.post(`${this.apiUrl}/archivos`, form);
  }

  descargarArchivo(id: number): Observable<Blob> {
    return this.http.get(`${this.apiUrl}/archivos/${id}`, { responseType: 'blob' });
  }

  eliminarArchivo(id: number): Observable<any> {
    return this.http.delete(`${this.apiUrl}/archivos/${id}`);
  }
}
