import { Injectable, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import {
  ArchivoPayload, ConsultaPayload, ConsultaUpdatePayload, DiagnosticoPayload, DiagnosticoUpdatePayload,
  EvolucionPayload, ExamenPayload, HistorialClinico, TratamientoPayload
} from '../models/historial.model';

@Injectable({ providedIn: 'root' })
export class HistorialService {
  private http = inject(HttpClient);
  private apiUrl = 'http://localhost:5219/api/historial';

  obtenerHistorial(idPaciente: number): Observable<HistorialClinico> {
    return this.http.get<HistorialClinico>(`${this.apiUrl}/paciente/${idPaciente}`);
  }

  // ───── Consultas ─────
  registrarConsulta(datos: ConsultaPayload): Observable<any> {
    return this.http.post(`${this.apiUrl}/consultas`, datos);
  }
  actualizarConsulta(id: number, datos: ConsultaUpdatePayload): Observable<any> {
    return this.http.put(`${this.apiUrl}/consultas/${id}`, datos);
  }
  eliminarConsulta(id: number): Observable<any> {
    return this.http.delete(`${this.apiUrl}/consultas/${id}`);
  }

  // ───── Diagnósticos ─────
  registrarDiagnostico(datos: DiagnosticoPayload): Observable<any> {
    return this.http.post(`${this.apiUrl}/diagnosticos`, datos);
  }
  actualizarDiagnostico(id: number, datos: DiagnosticoUpdatePayload): Observable<any> {
    return this.http.put(`${this.apiUrl}/diagnosticos/${id}`, datos);
  }
  eliminarDiagnostico(id: number): Observable<any> {
    return this.http.delete(`${this.apiUrl}/diagnosticos/${id}`);
  }

  // ───── Tratamientos ─────
  registrarTratamiento(datos: TratamientoPayload): Observable<any> {
    return this.http.post(`${this.apiUrl}/tratamientos`, datos);
  }
  actualizarTratamiento(id: number, datos: TratamientoPayload): Observable<any> {
    return this.http.put(`${this.apiUrl}/tratamientos/${id}`, datos);
  }
  eliminarTratamiento(id: number): Observable<any> {
    return this.http.delete(`${this.apiUrl}/tratamientos/${id}`);
  }

  // ───── Exámenes ─────
  registrarExamen(datos: ExamenPayload): Observable<any> {
    return this.http.post(`${this.apiUrl}/examenes`, datos);
  }
  actualizarExamen(id: number, datos: ExamenPayload): Observable<any> {
    return this.http.put(`${this.apiUrl}/examenes/${id}`, datos);
  }
  eliminarExamen(id: number): Observable<any> {
    return this.http.delete(`${this.apiUrl}/examenes/${id}`);
  }

  // ───── Evolución ─────
  registrarEvolucion(datos: EvolucionPayload): Observable<any> {
    return this.http.post(`${this.apiUrl}/evolucion`, datos);
  }
  actualizarEvolucion(id: number, datos: EvolucionPayload): Observable<any> {
    return this.http.put(`${this.apiUrl}/evolucion/${id}`, datos);
  }
  eliminarEvolucion(id: number): Observable<any> {
    return this.http.delete(`${this.apiUrl}/evolucion/${id}`);
  }

  // ───── Archivos ─────
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
