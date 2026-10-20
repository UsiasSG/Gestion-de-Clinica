export interface LoginRequest {
  nombreUsuario: string;
  clave: string;
}

export interface Usuario {
  id: number;
  nombreUsuario: string;
  correo: string;
  rol: string;
}

export interface LoginResponse {
  token: string;
  usuario: Usuario;
}
