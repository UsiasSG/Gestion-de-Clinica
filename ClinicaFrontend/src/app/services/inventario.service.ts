import { Injectable, inject } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { Observable } from 'rxjs';
import { environment } from '../../environments/environment';
import {
  Catalogo, CatalogosInventario, Lote, LotePayload, MedicamentoInventario,
  MedicamentoPayload, Movimiento, SalidaPayload
} from '../models/inventario.model';

@Injectable({ providedIn: 'root' })
export class InventarioService {
  private http = inject(HttpClient);
  private apiUrl = `${environment.apiUrl}/api/Inventario`;

  private params(idSucursal: number | null): HttpParams {
    return idSucursal != null ? new HttpParams().set('idSucursal', idSucursal) : new HttpParams();
  }

  obtenerCatalogos(): Observable<CatalogosInventario> {
    return this.http.get<CatalogosInventario>(`${this.apiUrl}/catalogos`);
  }

  crearCategoria(nombre: string): Observable<Catalogo> {
    return this.http.post<Catalogo>(`${this.apiUrl}/categorias`, { nombre });
  }

  obtenerMedicamentos(idSucursal: number | null): Observable<MedicamentoInventario[]> {
    return this.http.get<MedicamentoInventario[]>(`${this.apiUrl}/medicamentos`, { params: this.params(idSucursal) });
  }

  crearMedicamento(datos: MedicamentoPayload): Observable<any> {
    return this.http.post(`${this.apiUrl}/medicamentos`, datos);
  }

  actualizarMedicamento(id: number, datos: MedicamentoPayload): Observable<any> {
    return this.http.put(`${this.apiUrl}/medicamentos/${id}`, datos);
  }

  eliminarMedicamento(id: number): Observable<any> {
    return this.http.delete(`${this.apiUrl}/medicamentos/${id}`);
  }

  obtenerLotes(idSucursal: number | null): Observable<Lote[]> {
    return this.http.get<Lote[]>(`${this.apiUrl}/lotes`, { params: this.params(idSucursal) });
  }

  registrarEntrada(datos: LotePayload): Observable<any> {
    return this.http.post(`${this.apiUrl}/lotes`, datos);
  }

  actualizarLote(id: number, datos: Pick<LotePayload, 'numeroLote' | 'fechaIngreso' | 'fechaVencimiento'>): Observable<any> {
    return this.http.put(`${this.apiUrl}/lotes/${id}`, datos);
  }

  registrarSalida(id: number, datos: SalidaPayload): Observable<any> {
    return this.http.post(`${this.apiUrl}/lotes/${id}/salidas`, datos);
  }

  eliminarLote(id: number): Observable<any> {
    return this.http.delete(`${this.apiUrl}/lotes/${id}`);
  }

  obtenerMovimientos(idSucursal: number | null): Observable<Movimiento[]> {
    return this.http.get<Movimiento[]>(`${this.apiUrl}/movimientos`, { params: this.params(idSucursal) });
  }
}
