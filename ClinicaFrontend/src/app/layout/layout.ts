import { Component, OnDestroy, OnInit, computed, effect, inject, untracked } from '@angular/core';
import { Router, RouterLink, RouterLinkActive, RouterOutlet } from '@angular/router';
import { AuthService } from '../services/auth.service';
import { PermisosService } from '../services/permisos.service';

export interface ItemMenu {
  ruta: string;
  etiqueta: string;
  icono: string;
  modulo: string; // Código de la tabla Modulos
}

@Component({
  selector: 'app-layout',
  standalone: true,
  imports: [RouterOutlet, RouterLink, RouterLinkActive],
  templateUrl: './layout.html',
  styleUrl: './layout.css'
})
export class LayoutComponent implements OnInit, OnDestroy {
  private auth = inject(AuthService);
  private router = inject(Router);
  private permisos = inject(PermisosService);

  usuario = this.auth.usuarioActual;
  cargandoMenu = computed(() => !this.permisos.cargados());

  private readonly itemsMenu: ItemMenu[] = [
    { ruta: '/dashboard',    etiqueta: 'Dashboard',            icono: '📊', modulo: 'DASHBOARD' },
    { ruta: '/pacientes',    etiqueta: 'Gestión de Pacientes', icono: '🧑‍⚕️', modulo: 'PACIENTES' },
    { ruta: '/historial',    etiqueta: 'Historial Clínico',    icono: '📋', modulo: 'HISTORIAL' },
    { ruta: '/personal',     etiqueta: 'Gestión de Personal',  icono: '👥', modulo: 'PERSONAL' },
    { ruta: '/inventario',   etiqueta: 'Inventario',           icono: '📦', modulo: 'INVENTARIO' },
    { ruta: '/hospitalizacion', etiqueta: 'Hospitalización', icono: '🏥', modulo: 'HOSPITALIZACION' },
    { ruta: '/habitaciones', etiqueta: 'Habitaciones',         icono: '🛏️', modulo: 'HABITACIONES' },
    { ruta: '/pos',          etiqueta: 'Punto de Venta / POS', icono: '💊', modulo: 'POS' },
    { ruta: '/tarifario',    etiqueta: 'Tarifario',            icono: '🏷️', modulo: 'TARIFARIO' },
    { ruta: '/roles',        etiqueta: 'Roles y Permisos',     icono: '🔐', modulo: 'ROLES' }
  ];

  // Requisito 12a: el menú se genera según los permisos del usuario
  menu = computed(() => this.itemsMenu.filter(i => i.modulo === 'DASHBOARD' || this.permisos.tieneAcceso(i.modulo)));

  constructor() {
    // Si un administrador quita el acceso al módulo actual, se redirige al dashboard
    effect(() => {
      if (!this.permisos.cargados()) return;
      this.permisos.permisos(); // dependencia: se reevalúa cuando cambian
      untracked(() => {
        const actual = this.itemsMenu.find(i => this.router.url.startsWith(i.ruta));
        if (actual && actual.modulo !== 'DASHBOARD' && !this.permisos.tieneAcceso(actual.modulo)) {
          this.router.navigate(['/dashboard'], { queryParams: { sinAcceso: actual.modulo } });
        }
      });
    });
  }

  ngOnInit(): void {
    this.permisos.cargar().subscribe();
    this.permisos.iniciarTiempoReal();
  }

  ngOnDestroy(): void {
    this.permisos.detener();
  }

  cerrarSesion(): void {
    this.permisos.detener();
    this.auth.logout();
    this.router.navigate(['/login']);
  }
}
