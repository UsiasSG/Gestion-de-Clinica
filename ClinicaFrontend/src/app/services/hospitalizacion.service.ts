import { Injectable, inject } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { Observable } from 'rxjs';
import {
  CatalogosHospitalizacion, DetalleHospitalizacion, HospitalizacionResumen,
  PacienteHospitalizable, SolicitudHospitalizacionPayload
} from '../models/hospitalizacion.model';

@Injectable({ providedIn: 'root' })
export class HospitalizacionService {
  private http = inject(HttpClient);
  private api = 'http://localhost:5219/api/hospitalizaciones';

  listar(incluirCerradas = true): Observable<HospitalizacionResumen[]> {
    return this.http.get<HospitalizacionResumen[]>(this.api, { params: new HttpParams().set('incluirCerradas', incluirCerradas) });
  }
  catalogos(): Observable<CatalogosHospitalizacion> {
    return this.http.get<CatalogosHospitalizacion>(`${this.api}/catalogos`);
  }
  buscarPacientes(buscar: string): Observable<PacienteHospitalizable[]> {
    return this.http.get<PacienteHospitalizable[]>(`${this.api}/pacientes`, { params: new HttpParams().set('buscar', buscar) });
  }
  detalle(id: number): Observable<DetalleHospitalizacion> {
    return this.http.get<DetalleHospitalizacion>(`${this.api}/${id}`);
  }
  solicitar(datos: SolicitudHospitalizacionPayload): Observable<any> {
    return this.http.post(this.api, datos);
  }
  ingresar(id: number, datos: { idHabitacion: number; fechaIngreso: string }): Observable<any> {
    return this.http.post(`${this.api}/${id}/ingreso`, datos);
  }
  cancelar(id: number): Observable<any> {
    return this.http.post(`${this.api}/${id}/cancelar`, {});
  }
  registrarInsumo(id: number, datos: { idMedicamento: number; cantidad: number; observacion: string }): Observable<any> {
    return this.http.post(`${this.api}/${id}/insumos`, datos);
  }
  registrarServicio(id: number, datos: { idServicio: number | null; descripcion: string; cantidad: number; precioUnitario: number | null }): Observable<any> {
    return this.http.post(`${this.api}/${id}/servicios`, datos);
  }
  programarCirugia(id: number, datos: any): Observable<any> {
    return this.http.post(`${this.api}/${id}/cirugias`, datos);
  }
  actualizarCirugia(idCirugia: number, datos: any): Observable<any> {
    return this.http.put(`${this.api}/cirugias/${idCirugia}`, datos);
  }
  realizarCirugia(idCirugia: number, datos: { fechaRealizada: string | null; observaciones: string }): Observable<any> {
    return this.http.post(`${this.api}/cirugias/${idCirugia}/realizar`, datos);
  }
  cancelarCirugia(idCirugia: number): Observable<any> {
    return this.http.post(`${this.api}/cirugias/${idCirugia}/cancelar`, {});
  }
  darAlta(id: number, datos: { fechaAlta: string; observaciones: string }): Observable<any> {
    return this.http.post(`${this.api}/${id}/alta`, datos);
  }
}
