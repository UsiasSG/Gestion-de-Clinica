import { Injectable, inject, signal } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable, tap } from 'rxjs';
import { environment } from '../../environments/environment';

export interface UsuarioSesion {
  id: number;
  nombreUsuario: string;
  idSucursal: number;
  nombreSucursal: string;
  rolId?: number | null;
  rolNombre?: string | null;
}

@Injectable({
  providedIn: 'root'
})
export class AuthService {
  private http = inject(HttpClient);
  private apiUrl = `${environment.apiUrl}/api/Auth`;

  private readonly TOKEN_KEY = 'jwt_token';
  private readonly USER_KEY = 'user_info';

  public usuarioActual = signal<UsuarioSesion | null>(this.obtenerSesionGuardada());

  // ───────── Sesión guardada ─────────

  private obtenerSesionGuardada(): UsuarioSesion | null {
    try {
      const usuarioGuardado = localStorage.getItem(this.USER_KEY);
      if (usuarioGuardado) {
        const normalizado = this.normalizarUsuario(JSON.parse(usuarioGuardado));
        if (normalizado) return normalizado;
      }

      const token = this.getToken();
      if (token) return this.obtenerUsuarioDesdeToken(token);

      return null;
    } catch {
      return null;
    }
  }

  private decodificarPayload(token: string): any {
    const payload = token.split('.')[1].replace(/-/g, '+').replace(/_/g, '/');
    return JSON.parse(atob(payload));
  }

  private obtenerUsuarioDesdeToken(token: string): UsuarioSesion | null {
    try {
      const claims = this.decodificarPayload(token);

      const claimNameIdentifier = 'http://schemas.xmlsoap.org/ws/2005/05/identity/claims/nameidentifier';
      const claimName = 'http://schemas.xmlsoap.org/ws/2005/05/identity/claims/name';

      const id = Number(claims[claimNameIdentifier] ?? claims.nameid ?? claims.sub);
      if (isNaN(id) || id <= 0) return null;

      const idSucursal = Number(claims.SucursalId ?? 0);
      const rolId = Number(claims.RolId ?? 0);

      return {
        id,
        nombreUsuario: claims[claimName] ?? claims.NombreUsuario ?? 'Usuario',
        idSucursal: !isNaN(idSucursal) ? idSucursal : 0,
        nombreSucursal: claims.NombreSucursal || 'Sin sucursal asignada',
        rolId: !isNaN(rolId) && rolId > 0 ? rolId : null,
        rolNombre: claims.RolNombre || null
      };
    } catch (e) {
      console.error('Error al decodificar token JWT:', e);
      return null;
    }
  }

  private normalizarUsuario(usuario: any): UsuarioSesion | null {
    if (!usuario || typeof usuario !== 'object') return null;

    const id = Number(usuario.id ?? usuario.idUsuario);
    if (isNaN(id) || id <= 0) return null;

    const idSucursal = Number(usuario.idSucursal ?? 0);

    return {
      id,
      nombreUsuario: usuario.nombreUsuario ?? usuario.nombre ?? 'Usuario',
      idSucursal: !isNaN(idSucursal) ? idSucursal : 0,
      nombreSucursal: usuario.nombreSucursal ?? 'Sin sucursal asignada',
      rolId: usuario.rolId ?? null,
      rolNombre: usuario.rolNombre ?? null
    };
  }

  // ───────── Login / logout ─────────

  login(credentials: any): Observable<any> {
    return this.http.post<any>(`${this.apiUrl}/login`, credentials).pipe(
      tap((response) => {
        const token = response?.token;
        if (!token) return;

        localStorage.setItem(this.TOKEN_KEY, token);

        const sesion = this.normalizarUsuario(response?.usuario) ?? this.obtenerUsuarioDesdeToken(token);
        if (sesion) {
          localStorage.setItem(this.USER_KEY, JSON.stringify(sesion));
          this.usuarioActual.set(sesion);
        }
      })
    );
  }

  logout(): void {
    localStorage.removeItem(this.TOKEN_KEY);
    localStorage.removeItem(this.USER_KEY);
    this.usuarioActual.set(null);
  }

  // ───────── Token ─────────

  getToken(): string | null {
    return localStorage.getItem(this.TOKEN_KEY);
  }

  isLoggedIn(): boolean {
    return this.tokenVigente();
  }

  tokenVigente(): boolean {
    const token = this.getToken();
    if (!token || token === 'undefined' || token === 'null') return false;
    try {
      const { exp } = this.decodificarPayload(token);
      return !exp || exp * 1000 > Date.now();
    } catch {
      return false;
    }
  }

  // ───────── Accesos rápidos ─────────

  getUser(): UsuarioSesion | null {
    return this.usuarioActual();
  }

  get idUsuario(): number | null {
    return this.usuarioActual()?.id ?? null;
  }

  get idSucursal(): number | null {
    return this.usuarioActual()?.idSucursal || null;
  }
}
