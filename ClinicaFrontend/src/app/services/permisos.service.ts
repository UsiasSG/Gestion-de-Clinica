import { Injectable, computed, inject, signal } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable, catchError, finalize, of, shareReplay, tap } from 'rxjs';
import * as signalR from '@microsoft/signalr';

export interface PermisoModulo {
  codigo: string;
  nombre: string;
  consultar: boolean;
  crear: boolean;
  modificar: boolean;
  eliminar: boolean;
}

export type Accion = 'consultar' | 'crear' | 'modificar' | 'eliminar';

@Injectable({ providedIn: 'root' })
export class PermisosService {
  private http = inject(HttpClient);
  private apiUrl = 'http://localhost:5219/api/Auth/permisos';
  private hubUrl = 'http://localhost:5219/hubs/notificaciones';

  private conexion: signalR.HubConnection | null = null;
  private cargaEnCurso: Observable<PermisoModulo[]> | null = null;

  permisos = signal<PermisoModulo[]>([]);
  cargados = signal(false);

  private porCodigo = computed(() => new Map(this.permisos().map(p => [p.codigo, p])));

  puede(codigo: string, accion: Accion = 'consultar'): boolean {
    return this.porCodigo().get(codigo)?.[accion] ?? false;
  }

  tieneAcceso(codigo: string): boolean {
    return this.puede(codigo, 'consultar');
  }

  cargar(): Observable<PermisoModulo[]> {
    if (!this.cargaEnCurso) {
      this.cargaEnCurso = this.http.get<PermisoModulo[]>(this.apiUrl).pipe(
        tap(p => {
          this.permisos.set(p ?? []);
          this.cargados.set(true);
        }),
        catchError(() => {
          this.permisos.set([]);
          this.cargados.set(true);
          return of([] as PermisoModulo[]);
        }),
        finalize(() => (this.cargaEnCurso = null)),
        shareReplay(1)
      );
    }
    return this.cargaEnCurso;
  }

  // Escucha cambios de permisos para refrescar el menú sin recargar (requisito 12b)
  async iniciarTiempoReal(): Promise<void> {
    if (this.conexion && this.conexion.state !== signalR.HubConnectionState.Disconnected) return;

    this.conexion = new signalR.HubConnectionBuilder()
      .withUrl(this.hubUrl, { accessTokenFactory: () => localStorage.getItem('jwt_token') ?? '' })
      .withAutomaticReconnect()
      .build();

    this.conexion.on('PermisosActualizados', () => this.cargar().subscribe());
    this.conexion.onreconnected(() => this.cargar().subscribe());

    try {
      await this.conexion.start();
    } catch (err) {
      console.error('Error al conectar notificaciones:', err);
    }
  }

  async detener(): Promise<void> {
    try {
      await this.conexion?.stop();
    } finally {
      this.conexion = null;
      this.permisos.set([]);
      this.cargados.set(false);
    }
  }
}
