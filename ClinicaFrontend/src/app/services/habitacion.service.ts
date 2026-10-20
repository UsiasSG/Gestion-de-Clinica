import { Injectable, inject, signal } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { Observable, tap } from 'rxjs';
import * as signalR from '@microsoft/signalr';
import { environment } from '../../environments/environment';
import {
  AsignacionPayload, CatalogosHabitaciones, EgresoPayload, EstadoHabitacion, Habitacion,
  HabitacionPayload, HistorialAsignacion, PacienteDisponible, RespuestaHabitacion
} from '../models/habitacion.model';

@Injectable({ providedIn: 'root' })
export class HabitacionesService {
  private http = inject(HttpClient);
  private apiUrl = `${environment.apiUrl}/api/Habitaciones`;
  private hubUrl = `${environment.apiUrl}/hubs/habitaciones`;

  private conexion: signalR.HubConnection | null = null;
  private idSucursalActual: number | null = null;

  habitaciones = signal<Habitacion[]>([]);
  conectado = signal(false);
  cargando = signal(false);

  // ───────── Carga y tiempo real ─────────

  cargar(idSucursal: number | null): void {
    this.idSucursalActual = idSucursal;
    this.cargando.set(true);

    let params = new HttpParams();
    if (idSucursal != null) params = params.set('idSucursal', idSucursal);

    this.http.get<Habitacion[]>(this.apiUrl, { params }).subscribe({
      next: (data) => {
        this.habitaciones.set(this.ordenar(data ?? []));
        this.cargando.set(false);
      },
      error: (err) => {
        console.error('Error al cargar habitaciones:', err);
        this.cargando.set(false);
      }
    });
  }

  async conectar(): Promise<void> {
    if (this.conexion && this.conexion.state !== signalR.HubConnectionState.Disconnected) return;

    this.conexion = new signalR.HubConnectionBuilder()
      .withUrl(this.hubUrl, {
        // Se evalúa en cada conexión y reconexión: siempre usa el token vigente
        accessTokenFactory: () => localStorage.getItem('jwt_token') ?? ''
      })
      .withAutomaticReconnect()
      .build();

    this.conexion.on('HabitacionActualizada', (h: Habitacion) => this.aplicarCambio(h));
    this.conexion.on('HabitacionEliminada', (id: number) =>
      this.habitaciones.update(lista => lista.filter(x => x.id !== id)));
    this.conexion.on('HabitacionesRecargar', () => this.cargar(this.idSucursalActual));

    this.conexion.onreconnecting(() => this.conectado.set(false));
    this.conexion.onreconnected(() => {
      this.conectado.set(true);
      this.cargar(this.idSucursalActual); // recupera cambios ocurridos durante la desconexión
    });
    this.conexion.onclose(() => this.conectado.set(false));

    try {
      await this.conexion.start();
      this.conectado.set(true);
    } catch (err) {
      console.error('Error al conectar SignalR:', err);
      this.conectado.set(false);
    }
  }

  async desconectar(): Promise<void> {
    try {
      await this.conexion?.stop();
    } finally {
      this.conexion = null;
      this.conectado.set(false);
    }
  }

  private aplicarCambio(h: Habitacion): void {
    this.habitaciones.update(lista => {
      const sinEsta = lista.filter(x => x.id !== h.id);
      // Si la habitación no es de la sucursal que se está viendo, solo se quita de la lista
      return h.idSucursal === this.idSucursalActual ? this.ordenar([...sinEsta, h]) : sinEsta;
    });
  }

  private ordenar(lista: Habitacion[]): Habitacion[] {
    return [...lista].sort((a, b) =>
      a.numeroHabitacion.localeCompare(b.numeroHabitacion, undefined, { numeric: true }));
  }

  private actualizarDesde = tap<RespuestaHabitacion>(res => {
    if (res?.habitacion) this.aplicarCambio(res.habitacion);
  });

  // ───────── Operaciones ─────────

  obtenerCatalogos(): Observable<CatalogosHabitaciones> {
    return this.http.get<CatalogosHabitaciones>(`${this.apiUrl}/catalogos`);
  }

  crear(datos: HabitacionPayload): Observable<RespuestaHabitacion> {
    return this.http.post<RespuestaHabitacion>(this.apiUrl, datos).pipe(this.actualizarDesde);
  }

  actualizar(id: number, datos: HabitacionPayload): Observable<RespuestaHabitacion> {
    return this.http.put<RespuestaHabitacion>(`${this.apiUrl}/${id}`, datos).pipe(this.actualizarDesde);
  }

  eliminar(id: number): Observable<RespuestaHabitacion> {
    return this.http.delete<RespuestaHabitacion>(`${this.apiUrl}/${id}`).pipe(
      tap(() => this.habitaciones.update(lista => lista.filter(x => x.id !== id)))
    );
  }

  cambiarEstado(id: number, estado: EstadoHabitacion): Observable<RespuestaHabitacion> {
    return this.http.put<RespuestaHabitacion>(`${this.apiUrl}/${id}/estado`, { estado }).pipe(this.actualizarDesde);
  }

  asignar(datos: AsignacionPayload): Observable<RespuestaHabitacion> {
    return this.http.post<RespuestaHabitacion>(`${this.apiUrl}/asignar`, datos).pipe(this.actualizarDesde);
  }

  registrarEgreso(idAsignacion: number, datos: EgresoPayload): Observable<RespuestaHabitacion> {
    return this.http.post<RespuestaHabitacion>(`${this.apiUrl}/egreso/${idAsignacion}`, datos).pipe(this.actualizarDesde);
  }

  obtenerHistorial(id: number): Observable<HistorialAsignacion[]> {
    return this.http.get<HistorialAsignacion[]>(`${this.apiUrl}/${id}/historial`);
  }

  buscarPacientesDisponibles(buscar: string): Observable<PacienteDisponible[]> {
    const params = new HttpParams().set('buscar', buscar);
    return this.http.get<PacienteDisponible[]>(`${this.apiUrl}/pacientes-disponibles`, { params });
  }
}
