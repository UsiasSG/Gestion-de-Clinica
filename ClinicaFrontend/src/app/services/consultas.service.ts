import { Injectable, inject } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { Observable } from 'rxjs';

export interface DiagnosticoDto {
  codigoCie: string;
  descripcion: string;
}

export interface TratamientoDto {
  descripcion: string;
  indicaciones: string;
  diasTratamiento?: number;
}

export interface RegistroConsultaDto {
  idPaciente: number;
  idDoctor: number;
  motivoConsulta: string;
  sintomatologia: string;
  observaciones?: string;
  diagnosticos: DiagnosticoDto[];
  tratamientos: TratamientoDto[];
}

@Injectable({
  providedIn: 'root'
})
export class ConsultasService {
  private http = inject(HttpClient);
  private apiUrl = 'http://localhost:5219/api/Consultas';

  buscarCie10(busqueda: string): Observable<DiagnosticoDto[]> {
    const params = new HttpParams().set('busqueda', busqueda);
    return this.http.get<DiagnosticoDto[]>(`${this.apiUrl}/cie10`, { params });
  }

  registrarConsulta(datos: RegistroConsultaDto): Observable<any> {
    return this.http.post(this.apiUrl, datos);
  }
}
