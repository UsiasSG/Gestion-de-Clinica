import { inject } from '@angular/core';
import { CanActivateChildFn, CanActivateFn, Router } from '@angular/router';
import { map } from 'rxjs';
import { AuthService } from '../services/auth.service';
import { PermisosService } from '../services/permisos.service';

// Exige sesión iniciada y token vigente
export const authGuard: CanActivateFn = () => {
  const auth = inject(AuthService);
  const router = inject(Router);

  if (auth.isLoggedIn()) return true;
  auth.logout();
  return router.createUrlTree(['/login']);
};

// Exige permiso de Consultar en el módulo indicado en data.modulo
export const permisoGuard: CanActivateChildFn = (ruta) => {
  const permisos = inject(PermisosService);
  const router = inject(Router);
  const modulo = ruta.data?.['modulo'] as string | undefined;

  if (!modulo) return true;

  const verificar = () => permisos.tieneAcceso(modulo)
    ? true
    : router.createUrlTree(['/dashboard'], { queryParams: { sinAcceso: modulo } });

  return permisos.cargados() ? verificar() : permisos.cargar().pipe(map(verificar));
};
