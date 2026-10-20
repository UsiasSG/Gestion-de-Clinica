import { Component, OnInit, inject, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { FilaPermiso, Rol, RolesService } from '../../services/roles.service';
import { Accion, PermisosService } from '../../services/permisos.service';

@Component({
  selector: 'app-roles',
  standalone: true,
  imports: [CommonModule, FormsModule],
  templateUrl: './roles.html',
  styleUrl: './roles.css'
})
export class RolesComponent implements OnInit {
  private service = inject(RolesService);
  permisos = inject(PermisosService);

  roles = signal<Rol[]>([]);
  rolSeleccionado = signal<Rol | null>(null);
  matriz = signal<FilaPermiso[]>([]);
  cargandoMatriz = signal(false);
  sinGuardar = signal(false);
  procesando = signal(false);
  modalRol = signal(false);

  editandoRolId: number | null = null;
  formRol = { nombre: '', descripcion: '' };

  readonly acciones: Accion[] = ['consultar', 'crear', 'modificar', 'eliminar'];
  readonly etiquetas: Record<Accion, string> = {
    consultar: 'Consultar', crear: 'Crear', modificar: 'Modificar', eliminar: 'Eliminar'
  };

  ngOnInit(): void {
    this.cargarRoles();
  }

  cargarRoles(idSeleccionar?: number): void {
    this.service.obtenerRoles().subscribe({
      next: (lista) => {
        this.roles.set(lista ?? []);
        const objetivo = lista.find(r => r.id === (idSeleccionar ?? this.rolSeleccionado()?.id)) ?? lista[0] ?? null;
        if (!objetivo) return;
        if (objetivo.id !== this.rolSeleccionado()?.id) this.seleccionarRol(objetivo, true);
        else this.rolSeleccionado.set(objetivo); // refresca los contadores
      },
      error: (err) => this.mostrarError(err, 'No se pudieron cargar los roles.')
    });
  }

  seleccionarRol(rol: Rol, forzar = false): void {
    if (!forzar && this.sinGuardar() && !confirm('Hay cambios sin guardar en la matriz. ¿Descartarlos?')) return;

    this.rolSeleccionado.set(rol);
    this.sinGuardar.set(false);
    this.cargandoMatriz.set(true);

    this.service.obtenerMatriz(rol.id).subscribe({
      next: (m) => {
        this.matriz.set(m ?? []);
        this.cargandoMatriz.set(false);
      },
      error: (err) => {
        this.cargandoMatriz.set(false);
        this.mostrarError(err, 'No se pudo cargar la matriz de permisos.');
      }
    });
  }

  // ───────── Matriz ─────────

  private aplicar(fila: FilaPermiso, accion: Accion, valor: boolean): FilaPermiso {
    const nueva = { ...fila, [accion]: valor };
    if (accion !== 'consultar' && valor) nueva.consultar = true; // crear/modificar/eliminar requieren consultar
    if (accion === 'consultar' && !valor) {
      nueva.crear = false;
      nueva.modificar = false;
      nueva.eliminar = false;
    }
    return nueva;
  }

  cambiar(fila: FilaPermiso, accion: Accion, valor: boolean): void {
    this.matriz.update(lista => lista.map(f => f.idModulo === fila.idModulo ? this.aplicar(f, accion, valor) : f));
    this.sinGuardar.set(true);
  }

  filaCompleta(f: FilaPermiso): boolean {
    return f.consultar && f.crear && f.modificar && f.eliminar;
  }

  toggleFila(fila: FilaPermiso, valor: boolean): void {
    this.matriz.update(lista => lista.map(f => f.idModulo === fila.idModulo
      ? { ...f, consultar: valor, crear: valor, modificar: valor, eliminar: valor }
      : f));
    this.sinGuardar.set(true);
  }

  columnaCompleta(accion: Accion): boolean {
    const lista = this.matriz();
    return lista.length > 0 && lista.every(f => f[accion]);
  }

  toggleColumna(accion: Accion, valor: boolean): void {
    this.matriz.update(lista => lista.map(f => this.aplicar(f, accion, valor)));
    this.sinGuardar.set(true);
  }

  guardarMatriz(): void {
    const rol = this.rolSeleccionado();
    if (!rol || this.procesando()) return;

    this.procesando.set(true);
    this.service.guardarMatriz(rol.id, this.matriz()).subscribe({
      next: (res) => {
        this.procesando.set(false);
        this.sinGuardar.set(false);
        this.cargarRoles(rol.id);
        setTimeout(() => alert(res?.mensaje ?? 'Permisos actualizados.'));
      },
      error: (err) => {
        this.procesando.set(false);
        this.mostrarError(err, 'No se pudo guardar la matriz de permisos.');
      }
    });
  }

  // ───────── CRUD de roles ─────────

  abrirNuevoRol(): void {
    this.editandoRolId = null;
    this.formRol = { nombre: '', descripcion: '' };
    this.modalRol.set(true);
  }

  editarRol(rol: Rol): void {
    this.editandoRolId = rol.id;
    this.formRol = { nombre: rol.nombre, descripcion: rol.descripcion ?? '' };
    this.modalRol.set(true);
  }

  guardarRol(): void {
    if (this.procesando()) return;
    if (!this.formRol.nombre.trim()) {
      alert('Ingrese el nombre del rol.');
      return;
    }

    const datos = { nombre: this.formRol.nombre.trim(), descripcion: this.formRol.descripcion.trim() };
    const peticion = this.editandoRolId
      ? this.service.actualizarRol(this.editandoRolId, datos)
      : this.service.crearRol(datos);

    this.procesando.set(true);
    peticion.subscribe({
      next: (res) => {
        this.procesando.set(false);
        this.modalRol.set(false);
        this.cargarRoles(res?.id ?? this.editandoRolId ?? undefined);
        setTimeout(() => alert(res?.mensaje ?? 'Rol guardado.'));
      },
      error: (err) => {
        this.procesando.set(false);
        this.mostrarError(err, 'No se pudo guardar el rol.');
      }
    });
  }

  eliminarRol(rol: Rol): void {
    if (!confirm(`¿Eliminar el rol "${rol.nombre}"? Se borrarán también sus permisos.`)) return;

    this.service.eliminarRol(rol.id).subscribe({
      next: (res) => {
        this.rolSeleccionado.set(null);
        this.matriz.set([]);
        this.sinGuardar.set(false);
        this.cargarRoles();
        setTimeout(() => alert(res?.mensaje ?? 'Rol eliminado.'));
      },
      error: (err) => this.mostrarError(err, 'No se pudo eliminar el rol.')
    });
  }

  private mostrarError(err: any, porDefecto: string): void {
    console.error(err);
    setTimeout(() => alert(err?.error?.mensaje ?? porDefecto));
  }
}
