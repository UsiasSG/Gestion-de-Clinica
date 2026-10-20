import { Component, OnInit, computed, inject, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { Observable, map, of, switchMap } from 'rxjs';
import { Catalogo, Empleado, EmpleadoPayload, EmpleadosService } from '../../services/empleados.service';

type FormEmpleado = Omit<EmpleadoPayload, 'idEspecialidad'> & { idEspecialidad: number | 'NUEVA' | null };

const PUESTOS: Record<string, string[]> = {
  Enfermera: ['Enfermera Jefe', 'Enfermera Profesional', 'Auxiliar de Enfermería'],
  Administrativo: ['Administrador del Sistema', 'Gerente de Sucursal', 'Recepcionista', 'Cajero', 'Encargado de Farmacia', 'Contador']
};

@Component({
  selector: 'app-personal',
  standalone: true,
  imports: [CommonModule, FormsModule],
  templateUrl: './personal.html',
  styleUrl: './personal.css'
})
export class PersonalComponent implements OnInit {
  private service = inject(EmpleadosService);

  // Estado asíncrono → signals
  empleados = signal<Empleado[]>([]);
  especialidades = signal<Catalogo[]>([]);
  sucursales = signal<Catalogo[]>([]);
  roles = signal<Catalogo[]>([]);
  busqueda = signal('');
  mostrarModal = signal(false);
  procesando = signal(false);

  // Estado del formulario → cambia por eventos del usuario
  editandoId: number | null = null;
  teniaAcceso = false;
  nuevaEspecialidadTexto = '';
  form: FormEmpleado = this.formVacio();

  readonly tiposEmpleado = ['Doctor', 'Enfermera', 'Administrativo'];

  empleadosFiltrados = computed(() => {
    const t = this.busqueda().trim().toLowerCase();
    const lista = this.empleados();
    if (!t) return lista;
    return lista.filter(e =>
      [e.nombreCompleto, e.correo, e.telefono, e.tipoEmpleado, e.puesto,
       e.especialidadNombre, e.sucursalNombre, e.nombreUsuario]
        .some(v => v?.toLowerCase().includes(t))
    );
  });

  get puestosDisponibles(): string[] {
    return PUESTOS[this.form.tipoEmpleado] ?? [];
  }

  get creandoEspecialidad(): boolean {
    return this.form.idEspecialidad === 'NUEVA';
  }

  ngOnInit(): void {
    this.cargarEmpleados();
    this.cargarAuxiliares();
  }

  cargarEmpleados(): void {
    this.service.obtenerEmpleados().subscribe({
      next: (data) => this.empleados.set(data ?? []),
      error: (err) => console.error('Error al cargar empleados:', err)
    });
  }

  cargarAuxiliares(): void {
    this.service.obtenerAuxiliares().subscribe({
      next: (res) => {
        this.especialidades.set(res?.especialidades ?? []);
        this.sucursales.set(res?.sucursales ?? []);
        this.roles.set(res?.roles ?? []);
      },
      error: (err) => console.error('Error al cargar catálogos:', err)
    });
  }

  abrirModalCrear(): void {
    this.editandoId = null;
    this.teniaAcceso = false;
    this.nuevaEspecialidadTexto = '';
    this.form = this.formVacio();
    this.mostrarModal.set(true);
  }

  editarEmpleado(e: Empleado): void {
    this.editandoId = e.id;
    this.teniaAcceso = !!e.nombreUsuario;
    this.nuevaEspecialidadTexto = '';
    this.form = {
      nombres: e.nombres,
      apellidos: e.apellidos,
      tipoEmpleado: e.tipoEmpleado,
      puesto: e.puesto ?? '',
      telefono: e.telefono ?? '',
      correo: e.correo ?? '',
      idEspecialidad: e.idEspecialidad ?? null,
      idSucursal: e.idSucursal ?? null,
      crearAcceso: e.tieneAcceso,
      nombreUsuario: e.nombreUsuario ?? '',
      password: '',
      idRol: e.idRol ?? null
    };
    this.mostrarModal.set(true);
  }

  cerrarModal(): void {
    this.mostrarModal.set(false);
    this.procesando.set(false);
  }

  onTipoChange(tipo: string): void {
    this.form.tipoEmpleado = tipo;
    this.form.puesto = '';
    if (tipo !== 'Doctor') {
      this.form.idEspecialidad = null;
      this.nuevaEspecialidadTexto = '';
    }
  }

  private validar(): string[] {
    const f = this.form;
    const errores: string[] = [];
    const vacio = (v: any) => !v || String(v).trim() === '';

    if (vacio(f.nombres)) errores.push('Nombres');
    if (vacio(f.apellidos)) errores.push('Apellidos');
    if (vacio(f.telefono)) errores.push('Teléfono');
    else if (!/^\d{8}$/.test(f.telefono.replace(/[-\s]/g, ''))) errores.push('Teléfono (debe tener 8 dígitos)');
    if (vacio(f.correo)) errores.push('Correo');
    else if (!/^[^\s@]+@[^\s@]+\.[^\s@]+$/.test(f.correo.trim())) errores.push('Correo (formato no válido)');
    if (vacio(f.tipoEmpleado)) errores.push('Tipo de empleado');
    if (f.idSucursal == null) errores.push('Sucursal');

    if (f.tipoEmpleado === 'Doctor') {
      if (f.idEspecialidad == null) errores.push('Especialidad');
      if (this.creandoEspecialidad && vacio(this.nuevaEspecialidadTexto)) errores.push('Nombre de la nueva especialidad');
    } else if (f.tipoEmpleado && vacio(f.puesto)) {
      errores.push('Puesto');
    }

    if (f.crearAcceso) {
      if (vacio(f.nombreUsuario)) errores.push('Nombre de usuario');
      if (!this.teniaAcceso && vacio(f.password)) errores.push('Contraseña');
      if (f.password && f.password.length < 6) errores.push('Contraseña (mínimo 6 caracteres)');
      if (f.idRol == null) errores.push('Rol');
    }

    return errores;
  }

  guardarEmpleado(): void {
    if (this.procesando()) return;

    const errores = this.validar();
    if (errores.length > 0) {
      alert('Complete o corrija los siguientes campos:\n\n• ' + errores.join('\n• '));
      return;
    }

    this.procesando.set(true);

    // Si eligió "Agregar nueva especialidad", primero se crea y luego se guarda el empleado
    const especialidad$: Observable<number | null> = this.creandoEspecialidad
      ? this.service.crearEspecialidad(this.nuevaEspecialidadTexto.trim()).pipe(
          map(esp => {
            this.especialidades.update(lista => lista.some(x => x.id === esp.id) ? lista : [...lista, esp]);
            this.form.idEspecialidad = esp.id;
            this.nuevaEspecialidadTexto = '';
            return esp.id;
          })
        )
      : of(this.form.idEspecialidad as number | null);

    especialidad$.pipe(
      switchMap(idEspecialidad => {
        const datos: EmpleadoPayload = { ...this.form, idEspecialidad };
        return this.editandoId
          ? this.service.actualizarEmpleado(this.editandoId, datos)
          : this.service.crearEmpleado(datos);
      })
    ).subscribe({
      next: (res) => {
        this.cerrarModal();
        this.cargarEmpleados();
        setTimeout(() => alert(res?.mensaje ?? 'Empleado guardado correctamente.'));
      },
      error: (err) => {
        console.error('Error al guardar empleado:', err);
        this.procesando.set(false);
        setTimeout(() => alert(err?.error?.mensaje ?? 'Ocurrió un error al guardar el empleado.'));
      }
    });
  }

  eliminarEmpleado(e: Empleado): void {
    if (!confirm(`¿Desea eliminar a ${e.nombreCompleto}?\n\nSi tiene ventas registradas, su usuario se desactivará en lugar de borrarse.`)) return;

    this.service.eliminarEmpleado(e.id).subscribe({
      next: (res) => {
        this.cargarEmpleados();
        setTimeout(() => alert(res?.mensaje ?? 'Empleado eliminado.'));
      },
      error: (err) => {
        console.error('Error al eliminar empleado:', err);
        setTimeout(() => alert(err?.error?.mensaje ?? 'No se pudo eliminar el empleado.'));
      }
    });
  }

  private formVacio(): FormEmpleado {
    return {
      nombres: '',
      apellidos: '',
      tipoEmpleado: '',
      puesto: '',
      telefono: '',
      correo: '',
      idEspecialidad: null,
      idSucursal: null,
      crearAcceso: false,
      nombreUsuario: '',
      password: '',
      idRol: null
    };
  }
}
