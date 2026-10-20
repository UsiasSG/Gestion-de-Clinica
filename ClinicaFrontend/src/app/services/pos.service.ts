import { Injectable, inject } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { Observable } from 'rxjs';
import { PacientePOS, ProductoPOS, VentaDTO, VentaRespuesta } from '../models/pos.model';

@Injectable({ providedIn: 'root' })
export class PosService {
  private http = inject(HttpClient);
  private apiUrl = 'http://localhost:5219/api/POS';

  obtenerProductos(): Observable<ProductoPOS[]> {
    return this.http.get<ProductoPOS[]>(`${this.apiUrl}/productos`);
  }

  buscarPacientes(query: string): Observable<PacientePOS[]> {
    const params = new HttpParams().set('query', query);
    return this.http.get<PacientePOS[]>(`${this.apiUrl}/pacientes`, { params });
  }

  registrarVenta(venta: VentaDTO): Observable<VentaRespuesta> {
    return this.http.post<VentaRespuesta>(`${this.apiUrl}/ventas`, venta);
  }
}
