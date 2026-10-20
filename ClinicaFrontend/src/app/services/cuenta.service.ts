import { Injectable, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import {
  CargoManualPayload, CobroPayload, CuentaPaciente, FacturaDetalle, ServicioPayload, ServicioTarifario, CargoMedicamentoPayload
} from '../models/cuenta.model';

@Injectable({ providedIn: 'root' })
export class CuentaService {
  private http = inject(HttpClient);
  private apiCuenta = 'http://localhost:5219/api/cuenta';
  private apiTarifario = 'http://localhost:5219/api/tarifario';

  // ───── Cuenta del paciente ─────
  obtenerCuenta(idPaciente: number): Observable<CuentaPaciente> {
    return this.http.get<CuentaPaciente>(`${this.apiCuenta}/paciente/${idPaciente}`);
  }
  agregarCargo(datos: CargoManualPayload): Observable<any> {
    return this.http.post(`${this.apiCuenta}/cargos`, datos);
  }
  cargarMedicamento(datos: CargoMedicamentoPayload): Observable<any> {
    return this.http.post(`${this.apiCuenta}/medicamentos`, datos);
  }
  anularCargo(id: number): Observable<any> {
    return this.http.post(`${this.apiCuenta}/cargos/${id}/anular`, {});
  }
  cobrar(datos: CobroPayload): Observable<{ mensaje: string; factura: FacturaDetalle }> {
    return this.http.post<{ mensaje: string; factura: FacturaDetalle }>(`${this.apiCuenta}/cobrar`, datos);
  }
  obtenerFactura(id: number): Observable<FacturaDetalle> {
    return this.http.get<FacturaDetalle>(`${this.apiCuenta}/facturas/${id}`);
  }
  anularFactura(id: number, motivo: string): Observable<any> {
    return this.http.post(`${this.apiCuenta}/facturas/${id}/anular`, { motivo });
  }

  // ───── Tarifario ─────
  obtenerTarifario(): Observable<{ servicios: ServicioTarifario[]; tipos: string[] }> {
    return this.http.get<{ servicios: ServicioTarifario[]; tipos: string[] }>(this.apiTarifario);
  }
  crearServicio(datos: ServicioPayload): Observable<any> {
    return this.http.post(this.apiTarifario, datos);
  }
  actualizarServicio(id: number, datos: ServicioPayload): Observable<any> {
    return this.http.put(`${this.apiTarifario}/${id}`, datos);
  }
  eliminarServicio(id: number): Observable<any> {
    return this.http.delete(`${this.apiTarifario}/${id}`);
  }
}
