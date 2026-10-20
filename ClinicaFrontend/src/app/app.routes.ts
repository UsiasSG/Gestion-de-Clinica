import { Routes } from '@angular/router';
import { authGuard, permisoGuard } from './guards/auth.guard';

export const routes: Routes = [
  {
    path: 'login',
    loadComponent: () => import('./components/login/login').then(m => m.LoginComponent)
  },
  {
    path: '',
    loadComponent: () => import('./layout/layout').then(m => m.LayoutComponent),
    canActivate: [authGuard],
    canActivateChild: [permisoGuard],
    children: [
      {
        path: 'dashboard',
        loadComponent: () => import('./components/dashboard/dashboard').then(m => m.DashboardComponent)
      },
      {
        path: 'pacientes',
        data: { modulo: 'PACIENTES' },
        loadComponent: () => import('./components/pacientes/pacientes').then(m => m.PacientesComponent)
      },
      {
        path: 'historial',
        data: { modulo: 'HISTORIAL' },
        loadComponent: () => import('./components/historial/historial').then(m => m.HistorialComponent)
      },
      {
        path: 'personal',
        data: { modulo: 'PERSONAL' },
        loadComponent: () => import('./components/personal/personal').then(m => m.PersonalComponent)
      },
      {
        path: 'inventario',
        data: { modulo: 'INVENTARIO' },
        loadComponent: () => import('./pages/inventario/inventario').then(m => m.InventarioComponent)
      },
      {
        path: 'habitaciones',
        data: { modulo: 'HABITACIONES' },
        loadComponent: () => import('./components/habitaciones/habitaciones').then(m => m.HabitacionesComponent)
      },
      {
        path: 'pos',
        data: { modulo: 'POS' },
        loadComponent: () => import('./components/pos/pos').then(m => m.PosComponent)
      },
      {
        path: 'roles',
        data: { modulo: 'ROLES' },
        loadComponent: () => import('./components/roles/roles').then(m => m.RolesComponent)
      },
      { path: '', redirectTo: 'dashboard', pathMatch: 'full' }
    ]
  },
  { path: '**', redirectTo: 'login' }
];
