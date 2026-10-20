import { Component, inject, OnInit, OnDestroy, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { RouterModule } from '@angular/router';
import { PacienteService } from '../../services/paciente.service';
import { Paciente } from '../../models/paciente.model';
import { Router } from '@angular/router';

@Component({
  selector: 'app-pacientes',
  standalone: true,
  imports: [CommonModule, FormsModule],
  templateUrl: './pacientes.html'
})
export class PacientesComponent implements OnInit, OnDestroy {
  private pacienteService = inject(PacienteService);

  private router = inject(Router);
  listaPacientes = signal<Paciente[]>([]);
  mostrarModalForm = signal(false);
  procesando = signal(false);
  

  busqueda = '';
  editando = false;
  idPacienteSeleccionado: number | null = null;
  edadCalculada: number | null = null;
  esMenorDeEdad = false;
  formPaciente: any = this.getFormInicial();

  private timerBusqueda?: ReturnType<typeof setTimeout>;

  ngOnInit(): void {
    this.cargarPacientes();
  }

  ngOnDestroy(): void {
    clearTimeout(this.timerBusqueda);
  }

  cargarPacientes(): void {
    this.pacienteService.obtenerPacientes(this.busqueda.trim()).subscribe({
      next: (data) => this.listaPacientes.set(data ?? []),
      error: (err) => console.error('Error al cargar pacientes:', err)
    });
  }

  onBuscar(): void {
    clearTimeout(this.timerBusqueda);
    this.timerBusqueda = setTimeout(() => this.cargarPacientes(), 300);
  }

  abrirNuevoPaciente(): void {
    this.editando = false;
    this.idPacienteSeleccionado = null;
    this.formPaciente = this.getFormInicial();
    this.edadCalculada = null;
    this.esMenorDeEdad = false;
    this.mostrarModalForm.set(true);
  }

  abrirEditarPaciente(paciente: Paciente): void {
    if (!paciente.id) return;
    this.editando = true;
    this.idPacienteSeleccionado = paciente.id;

    this.formPaciente = {
      nombres: paciente.nombres ?? '',
      apellidos: paciente.apellidos ?? '',
      documentoIdentificacion: paciente.documentoIdentificacion ?? (paciente as any).documento_identificacion ?? '',
      fechaNacimiento: paciente.fechaNacimiento ? paciente.fechaNacimiento.split('T')[0] : '',
      genero: paciente.genero ?? '',
      tipoSangre: paciente.tipoSangre ?? (paciente as any).tipo_sangre ?? '',
      alergias: paciente.alergias ?? '',
      telefono: paciente.telefono ?? '',
      direccion: paciente.direccion ?? '',
      contactoEmergencia: paciente.contactoEmergencia ?? '',
      esMenorEdad: paciente.esMenorEdad ?? (paciente as any).es_menor_edad ?? false,
      nombreEncargado: paciente.nombreEncargado ?? (paciente as any).nombre_encargado ?? '',
      dpiEncargado: paciente.dpiEncargado ?? (paciente as any).dpi_encargado ?? ''
    };
    this.calcularEdad();
    this.mostrarModalForm.set(true);
  }

  cerrarModales(): void {
    this.mostrarModalForm.set(false);
    this.procesando.set(false);
  }

  cerrarModal(): void {
    this.cerrarModales();
  }

  calcularEdad(): void {
    const valor = this.formPaciente.fechaNacimiento;
    if (!valor) { 
      this.edadCalculada = null; 
      this.esMenorDeEdad = false;
      this.formPaciente.esMenorEdad = false;
      return; 
    }

    const nac = new Date(valor + 'T00:00:00');
    const hoy = new Date();
    let edad = hoy.getFullYear() - nac.getFullYear();
    const aunNoCumple = hoy.getMonth() < nac.getMonth() ||
      (hoy.getMonth() === nac.getMonth() && hoy.getDate() < nac.getDate());
    if (aunNoCumple) edad--;

    this.edadCalculada = edad >= 0 ? edad : null;
    this.esMenorDeEdad = this.edadCalculada !== null && this.edadCalculada < 18;
    this.formPaciente.esMenorEdad = this.esMenorDeEdad;

    // Si pasa a ser menor de edad, limpiar el DPI de paciente si se desea opcional
    if (this.esMenorDeEdad && (!this.formPaciente.documentoIdentificacion || this.formPaciente.documentoIdentificacion === '')) {
      this.formPaciente.documentoIdentificacion = '';
    }
  }

  private validarFormulario(): string[] {
    const f = this.formPaciente;
    const errores: string[] = [];
    const vacio = (v: any) => !v || String(v).trim() === '';

    if (vacio(f.nombres)) errores.push('Nombres');
    if (vacio(f.apellidos)) errores.push('Apellidos');
    if (vacio(f.fechaNacimiento)) errores.push('Fecha de nacimiento');
    else if (new Date(f.fechaNacimiento) > new Date()) errores.push('Fecha de nacimiento (no puede ser futura)');

    // Validación según si es Mayor o Menor de Edad
    if (!this.esMenorDeEdad) {
      if (vacio(f.documentoIdentificacion)) {
        errores.push('DPI / Documento del paciente (Obligatorio para mayores de edad)');
      } else if (!/^\d{13}$/.test(f.documentoIdentificacion.trim())) {
        errores.push('DPI del paciente (debe contener 13 dígitos)');
      }
    } else {
      if (vacio(f.nombreEncargado)) {
        errores.push('Nombre del Encargado / Responsable (Obligatorio para menores)');
      }
      if (vacio(f.dpiEncargado)) {
        errores.push('DPI del Encargado / Responsable (Obligatorio para menores)');
      } else if (!/^\d{13}$/.test(f.dpiEncargado.trim())) {
        errores.push('DPI del Encargado (debe contener 13 dígitos)');
      }
    }

    if (vacio(f.genero)) errores.push('Género');
    if (vacio(f.telefono)) errores.push('Teléfono');
    else if (!/^\d{8}$/.test(f.telefono.replace(/[-\s]/g, ''))) errores.push('Teléfono (debe contener 8 dígitos)');
    if (vacio(f.direccion)) errores.push('Dirección');
    if (vacio(f.contactoEmergencia)) errores.push('Contacto de emergencia');

    return errores;
  }

  guardarPaciente(): void {
    if (this.procesando()) return;

    const errores = this.validarFormulario();
    if (errores.length > 0) {
      alert('Complete o corrija los siguientes campos:\n\n' + errores.join('\n'));
      return;
    }

    this.procesando.set(true);

    const payload = {
      ...this.formPaciente,
      documentoIdentificacion: this.formPaciente.documentoIdentificacion ? this.formPaciente.documentoIdentificacion.trim() : null,
      tipoSangre: this.formPaciente.tipoSangre || 'N/R',
      alergias: this.formPaciente.alergias || 'Ninguna',
      esMenorEdad: this.esMenorDeEdad,
      nombreEncargado: this.esMenorDeEdad ? this.formPaciente.nombreEncargado.trim() : null,
      dpiEncargado: this.esMenorDeEdad ? this.formPaciente.dpiEncargado.trim() : null
    };

    const peticion = this.editando && this.idPacienteSeleccionado
      ? this.pacienteService.actualizarPaciente(this.idPacienteSeleccionado, payload)
      : this.pacienteService.registrarPaciente(payload);

    const mensaje = this.editando ? 'Paciente actualizado exitosamente.' : 'Paciente registrado exitosamente.';

    peticion.subscribe({
      next: () => this.alGuardarExito(mensaje),
      error: (err) => this.alGuardarError(err)
    });
  }

  private alGuardarExito(mensaje: string): void {
    this.cerrarModales();
    this.cargarPacientes();
    setTimeout(() => alert(mensaje));
  }

  private alGuardarError(err: any): void {
    console.error('Error al guardar paciente:', err);
    this.procesando.set(false);
    const detalle = err?.error?.mensaje ?? err?.error?.title ?? 'Ocurrió un error al procesar la solicitud.';
    setTimeout(() => alert(detalle));
  }

  confirmarEliminacion(paciente: Paciente): void {
    if (!paciente.id) return;
    if (!confirm(`¿Está seguro de eliminar al paciente ${paciente.nombres} ${paciente.apellidos}?`)) return;

    this.pacienteService.eliminarPaciente(paciente.id).subscribe({
      next: () => {
        this.cargarPacientes();
        setTimeout(() => alert('Paciente eliminado correctamente.'));
      },
      error: (err) => {
        console.error('Error al eliminar paciente:', err);
        const detalle = err?.error?.mensaje ?? 'No se pudo eliminar el paciente.';
        setTimeout(() => alert(detalle));
      }
    });
  }

 abrirHistorialClinico(paciente: Paciente): void {
  if (paciente.id) this.router.navigate(['/historial'], { queryParams: { paciente: paciente.id } });
}

  private getFormInicial() {
    return {
      nombres: '',
      apellidos: '',
      documentoIdentificacion: '',
      fechaNacimiento: '',
      genero: '',
      tipoSangre: '',
      alergias: '',
      telefono: '',
      direccion: '',
      contactoEmergencia: '',
      esMenorEdad: false,
      nombreEncargado: '',
      dpiEncargado: ''
    };
  }
}
