import { Component, effect, inject, input, output, signal, untracked } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { Observable } from 'rxjs';
import { CuentaPacienteComponent } from '../cuenta-paciente/cuenta-paciente';
import { HistorialService } from '../../services/historial.service';
import { HospitalizacionService } from '../../services/hospitalizacion.service';
import {
  ArchivoClinico, Consulta, DiagnosticoRegistro, EvolucionRegistro, ExamenRegistro,
  HistorialClinico, TratamientoRegistro
} from '../../models/historial.model';

type Pestana = 'consultas' | 'examenes' | 'diagnosticos' | 'tratamientos' | 'evolucion' | 'archivos' | 'cuenta';
type Formulario = 'consulta' | 'diagnostico' | 'examen' | 'tratamiento' | 'evolucion' | 'archivo' | null;


interface FilaDiagnostico { codigoCie: string; descripcion: string; }
interface FilaTratamiento { descripcion: string; indicaciones: string; fechaInicio: string; fechaFin: string; }

@Component({
  selector: 'app-historial-clinico',
  standalone: true,
     imports: [CommonModule, FormsModule, CuentaPacienteComponent],
  templateUrl: './historial-clinico.html',
  styleUrl: './historial-clinico.css'
})
export class HistorialClinicoComponent {
  private hospitalizacionService = inject(HospitalizacionService);
  private service = inject(HistorialService);

  idPaciente = input.required<number>();
  embebido = input(false);
  cerrar = output<void>();

  readonly hoy = new Date().toISOString().slice(0, 10);
  readonly estadosSalud = ['Estable', 'Mejorando', 'Delicado', 'Crítico', 'Recuperado'];
  readonly estadosTratamiento = ['Activo', 'Finalizado', 'Suspendido'];
  readonly categoriasArchivo = ['Resultado de examen', 'Imagen diagnóstica', 'Receta', 'Consentimiento', 'Referencia médica', 'Otro'];
  readonly tamanoMaximo = 5 * 1024 * 1024;

  historial = signal<HistorialClinico | null>(null);
  cargando = signal(true);
  error = signal<string | null>(null);
  pestana = signal<Pestana>('consultas');
  formAbierto = signal<Formulario>(null);
  procesando = signal(false);

  // Id del registro que se está editando (null = creando uno nuevo)
  editandoId: number | null = null;
  referirDiagnosticoId: number | null = null;
  formReferencia = { tipo: 'Vigilancia', motivo: '' };

  formConsulta = this.consultaVacia();
  formDiagnostico = this.diagnosticoVacio();
  formExamen = this.examenVacio();
  formTratamiento = this.tratamientoVacio();
  formEvolucion = this.evolucionVacia();
  formArchivo = { categoria: 'Resultado de examen', idExamen: null as number | null, descripcion: '' };
  archivoSeleccionado: File | null = null;

  constructor() {
    // Se recarga cada vez que cambia el paciente seleccionado
    effect(() => {
      const id = this.idPaciente();
      untracked(() => {
        this.historial.set(null);
        this.cargando.set(true);
        this.error.set(null);
        this.pestana.set('consultas');
        this.cancelarForm();
        this.cargar(id);
      });
    });
  }

  get enEdicion(): boolean {
    return this.editandoId !== null;
  }

  cargar(id: number = this.idPaciente()): void {
    this.service.obtenerHistorial(id).subscribe({
      next: (h) => {
        if (id !== this.idPaciente()) return; // se cambió de paciente mientras cargaba
        this.historial.set(h);
        this.error.set(null);
        this.cargando.set(false);
      },
      error: (err) => {
        this.cargando.set(false);
        this.error.set(err?.status === 403
          ? 'No tiene permiso para consultar el historial clínico.'
          : (err?.error?.mensaje ?? 'No se pudo cargar el historial clínico.'));
      }
    });
  }

  cambiarPestana(p: Pestana): void {
    this.pestana.set(p);
    this.cancelarForm();
  }

  // ───────── Abrir formularios (crear) ─────────

  abrirForm(tipo: Formulario): void {
    this.editandoId = null;
    switch (tipo) {
      case 'consulta': this.formConsulta = this.consultaVacia(); break;
      case 'diagnostico': this.formDiagnostico = this.diagnosticoVacio(); break;
      case 'examen': this.formExamen = this.examenVacio(); break;
      case 'tratamiento': this.formTratamiento = this.tratamientoVacio(); break;
      case 'evolucion': this.formEvolucion = this.evolucionVacia(); break;
      case 'archivo':
        this.formArchivo = { categoria: 'Resultado de examen', idExamen: null, descripcion: '' };
        this.archivoSeleccionado = null;
        break;
    }
    this.formAbierto.set(tipo);
    this.irAlFormulario();
  }

  cancelarForm(): void {
    this.formAbierto.set(null);
    this.editandoId = null;
  }

  // ───────── Abrir formularios (editar) ─────────

  editarConsulta(c: Consulta): void {
    this.formConsulta = {
      ...this.consultaVacia(),
      motivoConsulta: c.motivoConsulta ?? '',
      sintomatologia: c.sintomatologia ?? '',
      observaciones: c.observaciones ?? ''
    };
    this.abrirEdicion('consulta', c.id);
  }

  editarDiagnostico(d: DiagnosticoRegistro): void {
    this.formDiagnostico = { idConsulta: d.idConsulta, codigoCie: d.codigoCie ?? '', descripcion: d.descripcion, idDoctor: null };
    this.abrirEdicion('diagnostico', d.id);
  }

     editarExamen(ex: ExamenRegistro): void {
     this.formExamen = {
       idConsulta: ex.idConsulta ?? null,
       idServicio: null,
       nombreExamen: ex.nombreExamen,
       fechaExamen: ex.fechaExamen.slice(0, 10),
       resultado: ex.resultado ?? '',
       observaciones: ex.observaciones ?? ''
     };
     this.abrirEdicion('examen', ex.id);
   }

   onServicioExamen(id: number | null): void {
     this.formExamen.idServicio = id;
     const servicio = this.historial()?.serviciosExamen.find(s => s.id === id);
     if (servicio) this.formExamen.nombreExamen = servicio.nombre;
   }

  editarTratamiento(t: TratamientoRegistro): void {
    this.formTratamiento = {
      descripcion: t.descripcion,
      indicaciones: t.indicaciones ?? '',
      fechaInicio: t.fechaInicio?.slice(0, 10) ?? '',
      fechaFin: t.fechaFin?.slice(0, 10) ?? '',
      idConsulta: t.idConsulta ?? null,
      idDiagnostico: t.idDiagnostico ?? null,
      idDoctor: null,
      estado: t.estado || 'Activo'
    };
    this.abrirEdicion('tratamiento', t.id);
  }

  editarEvolucion(ev: EvolucionRegistro): void {
    this.formEvolucion = { notaEvolucion: ev.notaEvolucion, estadoSalud: ev.estadoSalud ?? 'Estable', idResponsable: null };
    this.abrirEdicion('evolucion', ev.id);
  }

  private abrirEdicion(tipo: Formulario, id: number): void {
    this.editandoId = id;
    this.formAbierto.set(tipo);
    this.irAlFormulario();
  }

  private irAlFormulario(): void {
    setTimeout(() => document.querySelector('.panel')?.scrollIntoView({ behavior: 'smooth', block: 'start' }));
  }

  // ───────── Filas dinámicas de la consulta ─────────

  agregarDiagnosticoFila(): void {
    this.formConsulta.diagnosticos.push({ codigoCie: '', descripcion: '' });
  }

  quitarDiagnosticoFila(i: number): void {
    this.formConsulta.diagnosticos.splice(i, 1);
  }

  agregarTratamientoFila(): void {
    this.formConsulta.tratamientos.push({ descripcion: '', indicaciones: '', fechaInicio: this.hoy, fechaFin: '' });
  }

  quitarTratamientoFila(i: number): void {
    this.formConsulta.tratamientos.splice(i, 1);
  }

  // ───────── Guardar ─────────

  guardarConsulta(): void {
    const f = this.formConsulta;
    if (!f.motivoConsulta.trim()) return this.avisar('Ingrese el motivo de la consulta.');

    if (this.enEdicion) {
      this.enviar(this.service.actualizarConsulta(this.editandoId!, {
        motivoConsulta: f.motivoConsulta.trim(),
        sintomatologia: f.sintomatologia.trim(),
        observaciones: f.observaciones.trim()
      }));
      return;
    }

    if (!this.historial()?.permisos.esDoctor && f.idDoctor == null) return this.avisar('Seleccione el médico tratante.');

    const diagnosticos = f.diagnosticos.filter(d => d.descripcion.trim());
    const tratamientos = f.tratamientos.filter(t => t.descripcion.trim());
    if (tratamientos.some(t => t.fechaInicio && t.fechaFin && t.fechaFin < t.fechaInicio)) {
      return this.avisar('En los tratamientos, la fecha de fin no puede ser anterior a la de inicio.');
    }
    if ((this.historial()?.serviciosConsulta.length ?? 0) > 0 && f.idServicio == null) {
    return this.avisar('Seleccione el tipo de consulta.');
   }


    this.enviar(this.service.registrarConsulta({
      idPaciente: this.idPaciente(),
      idDoctor: f.idDoctor,
      idServicio: f.idServicio,
      motivoConsulta: f.motivoConsulta.trim(),
      sintomatologia: f.sintomatologia.trim(),
      observaciones: f.observaciones.trim(),
      diagnosticos: diagnosticos.map(d => ({ codigoCie: d.codigoCie.trim(), descripcion: d.descripcion.trim() })),
      tratamientos: tratamientos.map(t => ({
        descripcion: t.descripcion.trim(),
        indicaciones: t.indicaciones.trim(),
        fechaInicio: t.fechaInicio || null,
        fechaFin: t.fechaFin || null
      }))
    }));
  }

  guardarDiagnostico(): void {
    const f = this.formDiagnostico;
    if (!f.descripcion.trim()) return this.avisar('Ingrese la descripción del diagnóstico.');

    if (this.enEdicion) {
      this.enviar(this.service.actualizarDiagnostico(this.editandoId!, {
        codigoCie: f.codigoCie.trim(),
        descripcion: f.descripcion.trim()
      }));
      return;
    }

    if (f.idConsulta == null) return this.avisar('Seleccione la consulta a la que pertenece el diagnóstico.');
    if (!this.historial()?.permisos.esDoctor && f.idDoctor == null) return this.avisar('Seleccione el médico que asigna el diagnóstico.');

    this.enviar(this.service.registrarDiagnostico({
      idConsulta: f.idConsulta,
      idDoctor: f.idDoctor,
      codigoCie: f.codigoCie.trim(),
      descripcion: f.descripcion.trim()
    }));
  }

  guardarExamen(): void {
    const f = this.formExamen;
    if (!f.nombreExamen.trim()) return this.avisar('Ingrese el nombre del examen.');
    if (!f.fechaExamen) return this.avisar('Ingrese la fecha del examen.');
    if (f.fechaExamen > this.hoy) return this.avisar('La fecha del examen no puede ser futura.');

    const datos = {
      idPaciente: this.idPaciente(),
      idConsulta: f.idConsulta,
      idServicio: this.enEdicion ? null : f.idServicio,
      nombreExamen: f.nombreExamen.trim(),
      fechaExamen: f.fechaExamen,
      resultado: f.resultado.trim(),
      observaciones: f.observaciones.trim()
    };

    this.enviar(this.enEdicion
      ? this.service.actualizarExamen(this.editandoId!, datos)
      : this.service.registrarExamen(datos));
  }

  guardarTratamiento(): void {
    const f = this.formTratamiento;
    if (!f.descripcion.trim()) return this.avisar('Ingrese la descripción del tratamiento.');
    if (f.fechaInicio && f.fechaFin && f.fechaFin < f.fechaInicio) return this.avisar('La fecha de fin no puede ser anterior a la de inicio.');
    if (!this.enEdicion && !this.historial()?.permisos.esDoctor && f.idDoctor == null) return this.avisar('Seleccione el médico que indica el tratamiento.');

    const datos = {
      idPaciente: this.idPaciente(),
      idConsulta: f.idConsulta,
      idDiagnostico: f.idDiagnostico,
      idDoctor: f.idDoctor,
      descripcion: f.descripcion.trim(),
      indicaciones: f.indicaciones.trim(),
      fechaInicio: f.fechaInicio || null,
      fechaFin: f.fechaFin || null,
      estado: f.estado
    };

    this.enviar(this.enEdicion
      ? this.service.actualizarTratamiento(this.editandoId!, datos)
      : this.service.registrarTratamiento(datos));
  }

  guardarEvolucion(): void {
    const f = this.formEvolucion;
    if (!f.notaEvolucion.trim()) return this.avisar('Escriba la nota de evolución.');
    if (!this.enEdicion && !this.historial()?.permisos.esPersonalClinico && f.idResponsable == null) {
      return this.avisar('Seleccione el responsable de la nota.');
    }

    const datos = {
      idPaciente: this.idPaciente(),
      idResponsable: f.idResponsable,
      notaEvolucion: f.notaEvolucion.trim(),
      estadoSalud: f.estadoSalud
    };

    this.enviar(this.enEdicion
      ? this.service.actualizarEvolucion(this.editandoId!, datos)
      : this.service.registrarEvolucion(datos));
  }

  // ───────── Eliminar ─────────

  eliminarConsulta(c: Consulta): void {
    const fecha = new Date(c.fechaConsulta).toLocaleDateString('es-GT');
    if (!confirm(`¿Eliminar la consulta del ${fecha}?\n\nSus diagnósticos también se eliminarán. Los tratamientos y exámenes se conservarán en el historial.`)) return;
    this.enviar(this.service.eliminarConsulta(c.id));
  }

  eliminarDiagnostico(d: DiagnosticoRegistro): void {
    if (!confirm(`¿Eliminar el diagnóstico "${d.descripcion}"?`)) return;
    this.enviar(this.service.eliminarDiagnostico(d.id));
  }

     abrirReferencia(d: DiagnosticoRegistro): void {
     this.referirDiagnosticoId = d.id;
     this.formReferencia = { tipo: 'Vigilancia', motivo: d.descripcion };
   }

   guardarReferencia(): void {
     if (!this.referirDiagnosticoId) return;
     if (!this.formReferencia.motivo.trim()) return this.avisar('Indique el motivo de la hospitalización.');
     this.enviar(this.hospitalizacionService.solicitar({
       idPaciente: this.idPaciente(),
       idDiagnostico: this.referirDiagnosticoId,
       idMedicoResponsable: null,
       tipo: this.formReferencia.tipo,
       motivo: this.formReferencia.motivo.trim()
     }));
     this.referirDiagnosticoId = null;
   }

  eliminarExamen(ex: ExamenRegistro): void {
    if (!confirm(`¿Eliminar el examen "${ex.nombreExamen}"?${ex.adjuntos > 0 ? '\n\nSus archivos adjuntos se conservarán en la pestaña Archivos.' : ''}`)) return;
    this.enviar(this.service.eliminarExamen(ex.id));
  }

  eliminarTratamiento(t: TratamientoRegistro): void {
    if (!confirm(`¿Eliminar el tratamiento "${t.descripcion}"?`)) return;
    this.enviar(this.service.eliminarTratamiento(t.id));
  }

  eliminarEvolucion(ev: EvolucionRegistro): void {
    if (!confirm('¿Eliminar esta nota de evolución?')) return;
    this.enviar(this.service.eliminarEvolucion(ev.id));
  }

  // ───────── Archivos adjuntos ─────────

  onArchivoSeleccionado(event: Event): void {
    const input = event.target as HTMLInputElement;
    const archivo = input.files?.[0] ?? null;

    if (archivo && !/\.(pdf|jpe?g|png)$/i.test(archivo.name)) {
      alert('Solo se permiten archivos PDF, JPG o PNG.');
      input.value = '';
      this.archivoSeleccionado = null;
      return;
    }
    if (archivo && archivo.size > this.tamanoMaximo) {
      alert('El archivo supera el tamaño máximo de 5 MB.');
      input.value = '';
      this.archivoSeleccionado = null;
      return;
    }
    this.archivoSeleccionado = archivo;
  }

  adjuntarAExamen(ex: ExamenRegistro): void {
    this.pestana.set('archivos');
    this.abrirForm('archivo');
    this.formArchivo.idExamen = ex.id;
    this.formArchivo.categoria = 'Resultado de examen';
  }

  guardarArchivo(): void {
    if (!this.archivoSeleccionado) return this.avisar('Seleccione el archivo a subir.');

    this.enviar(this.service.subirArchivo({
      archivo: this.archivoSeleccionado,
      idPaciente: this.idPaciente(),
      idExamen: this.formArchivo.idExamen,
      categoria: this.formArchivo.categoria,
      descripcion: this.formArchivo.descripcion.trim()
    }));
  }

  verArchivo(a: ArchivoClinico): void {
    // Se abre la pestaña de inmediato (desde el clic) para que el navegador no la bloquee
    const ventana = window.open('', '_blank');
    this.service.descargarArchivo(a.id).subscribe({
      next: (blob) => {
        const url = URL.createObjectURL(blob);
        if (ventana) ventana.location.href = url;
        setTimeout(() => URL.revokeObjectURL(url), 60_000);
      },
      error: () => {
        ventana?.close();
        alert('No se pudo abrir el archivo.');
      }
    });
  }

  descargarArchivo(a: ArchivoClinico): void {
    this.service.descargarArchivo(a.id).subscribe({
      next: (blob) => {
        const url = URL.createObjectURL(blob);
        const enlace = document.createElement('a');
        enlace.href = url;
        enlace.download = a.nombreArchivo;
        enlace.click();
        setTimeout(() => URL.revokeObjectURL(url), 10_000);
      },
      error: () => alert('No se pudo descargar el archivo.')
    });
  }

  eliminarArchivo(a: ArchivoClinico): void {
    if (!confirm(`¿Eliminar el archivo "${a.nombreArchivo}"?`)) return;
    this.enviar(this.service.eliminarArchivo(a.id));
  }

  tamanoLegible(bytes: number): string {
    if (bytes < 1024) return `${bytes} B`;
    if (bytes < 1024 * 1024) return `${(bytes / 1024).toFixed(0)} KB`;
    return `${(bytes / (1024 * 1024)).toFixed(1)} MB`;
  }

  iconoArchivo(tipo: string): string {
    return tipo === 'application/pdf' ? '📄' : '🖼️';
  }

  // ───────── Utilidades ─────────

  edad(fechaNacimiento: string): number | null {
    if (!fechaNacimiento) return null;
    const nac = new Date(fechaNacimiento);
    const hoy = new Date();
    let edad = hoy.getFullYear() - nac.getFullYear();
    if (hoy.getMonth() < nac.getMonth() || (hoy.getMonth() === nac.getMonth() && hoy.getDate() < nac.getDate())) edad--;
    return edad;
  }

  claseSalud(estado?: string | null): string {
    if (estado === 'Crítico') return 'b-rojo';
    if (estado === 'Delicado') return 'b-ambar';
    return 'b-verde';
  }

  claseTratamiento(estado: string): string {
    if (estado === 'Finalizado') return 'b-azul';
    if (estado === 'Suspendido') return 'b-rojo';
    return 'b-verde';
  }

  private enviar(peticion: Observable<any>): void {
    if (this.procesando()) return;
    this.procesando.set(true);

    peticion.subscribe({
      next: (res) => {
        this.procesando.set(false);
        this.cancelarForm();
        this.archivoSeleccionado = null;
        this.cargar();
        setTimeout(() => alert(res?.mensaje ?? 'Operación realizada correctamente.'));
      },
      error: (err) => {
        console.error(err);
        this.procesando.set(false);
        setTimeout(() => alert(err?.error?.mensaje ?? 'No se pudo completar la operación.'));
      }
    });
  }

  private avisar(mensaje: string): void {
    alert(mensaje);
  }

  private consultaVacia() {
     return {
       motivoConsulta: '',
       sintomatologia: '',
       observaciones: '',
       idDoctor: null as number | null,
       idServicio: null as number | null,
       diagnosticos: [] as FilaDiagnostico[],
       tratamientos: [] as FilaTratamiento[]
     };
   }

  private diagnosticoVacio() {
    return {
      idConsulta: this.historial()?.consultas[0]?.id ?? null as number | null,
      codigoCie: '',
      descripcion: '',
      idDoctor: null as number | null
    };
  }

  private examenVacio() {
     return { idConsulta: null as number | null, idServicio: null as number | null, nombreExamen: '', fechaExamen: this.hoy, resultado: '', observaciones: '' };
   }

  private tratamientoVacio() {
    return {
      descripcion: '',
      indicaciones: '',
      fechaInicio: this.hoy,
      fechaFin: '',
      idConsulta: null as number | null,
      idDiagnostico: null as number | null,
      idDoctor: null as number | null,
      estado: 'Activo'
    };
  }

  private evolucionVacia() {
    return { notaEvolucion: '', estadoSalud: 'Estable', idResponsable: null as number | null };
  }
}
