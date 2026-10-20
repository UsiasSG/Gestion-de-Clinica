import { Component, OnInit, computed, inject, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { CuentaService } from '../../services/cuenta.service';
import { PermisosService } from '../../services/permisos.service';
import { ServicioPayload, ServicioTarifario } from '../../models/cuenta.model';

@Component({
  selector: 'app-tarifario',
  standalone: true,
  imports: [CommonModule, FormsModule],
  templateUrl: './tarifario.html',
  styleUrl: './tarifario.css'
})
export class TarifarioComponent implements OnInit {
  private service = inject(CuentaService);
  permisos = inject(PermisosService);

  servicios = signal<ServicioTarifario[]>([]);
  tipos = signal<string[]>([]);
  cargando = signal(true);
  filtroTipo = signal('Todos');
  busqueda = signal('');
  verInactivos = signal(false);
  modal = signal(false);
  procesando = signal(false);

  editandoId: number | null = null;
  form: ServicioPayload = this.vacio();

  filtrados = computed(() => {
    const t = this.busqueda().trim().toLowerCase();
    return this.servicios().filter(s =>
      (this.verInactivos() || s.activo) &&
      (this.filtroTipo() === 'Todos' || s.tipo === this.filtroTipo()) &&
      (!t || s.nombre.toLowerCase().includes(t) || (s.codigo ?? '').toLowerCase().includes(t))
    );
  });

  ngOnInit(): void {
    this.cargar();
  }

  cargar(): void {
    this.service.obtenerTarifario().subscribe({
      next: (r) => {
        this.servicios.set(r.servicios ?? []);
        this.tipos.set(r.tipos ?? []);
        this.cargando.set(false);
      },
      error: (err) => {
        this.cargando.set(false);
        this.mostrarError(err, 'No se pudo cargar el tarifario.');
      }
    });
  }

  contar(tipo: string): number {
    return this.servicios().filter(s => s.activo && (tipo === 'Todos' || s.tipo === tipo)).length;
  }

  nuevo(): void {
    this.editandoId = null;
    this.form = this.vacio();
    if (this.filtroTipo() !== 'Todos') this.form.tipo = this.filtroTipo();
    this.modal.set(true);
  }

  editar(s: ServicioTarifario): void {
    this.editandoId = s.id;
    this.form = {
      codigo: s.codigo ?? '',
      nombre: s.nombre,
      tipo: s.tipo,
      descripcion: s.descripcion ?? '',
      precio: s.precio,
      activo: s.activo
    };
    this.modal.set(true);
  }

  guardar(): void {
    if (this.procesando()) return;
    const f = this.form;
    const errores: string[] = [];
    if (!f.nombre.trim()) errores.push('Nombre');
    if (!f.tipo) errores.push('Tipo');
    if (f.precio == null || f.precio < 0) errores.push('Precio (0 o mayor)');
    if (errores.length) {
      alert('Complete o corrija:\n\n• ' + errores.join('\n• '));
      return;
    }

    this.procesando.set(true);
    const peticion = this.editandoId
      ? this.service.actualizarServicio(this.editandoId, f)
      : this.service.crearServicio(f);

    peticion.subscribe({
      next: (res) => {
        this.procesando.set(false);
        this.modal.set(false);
        this.cargar();
        setTimeout(() => alert(res?.mensaje ?? 'Guardado.'));
      },
      error: (err) => {
        this.procesando.set(false);
        this.mostrarError(err, 'No se pudo guardar el servicio.');
      }
    });
  }

  eliminar(s: ServicioTarifario): void {
    if (!confirm(`¿Eliminar "${s.nombre}" del tarifario?\n\nSi ya se usó en cobros, se desactivará en lugar de borrarse.`)) return;
    this.service.eliminarServicio(s.id).subscribe({
      next: (res) => {
        this.cargar();
        setTimeout(() => alert(res?.mensaje ?? 'Eliminado.'));
      },
      error: (err) => this.mostrarError(err, 'No se pudo eliminar el servicio.')
    });
  }

  claseTipo(tipo: string): string {
    return { 'Consulta': 'b-azul', 'Examen': 'b-verde', 'Procedimiento': 'b-ambar', 'Cirugía': 'b-rojo' }[tipo] ?? 'b-gris';
  }

  private vacio(): ServicioPayload {
    return { codigo: '', nombre: '', tipo: '', descripcion: '', precio: null, activo: true };
  }

  private mostrarError(err: any, porDefecto: string): void {
    console.error(err);
    setTimeout(() => alert(err?.error?.mensaje ?? porDefecto));
  }
}
