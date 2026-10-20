import { Component, effect, inject, input, output, signal, untracked } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { Observable } from 'rxjs';
import { HistorialService } from '../../services/historial.service';
import { ArchivoClinico, ExamenRegistro, HistorialClinico } from '../../models/historial.model';

type Pestana = 'consultas' | 'examenes' | 'diagnosticos' | 'evolucion' | 'archivos';
type Formulario = 'consulta' | 'diagnostico' | 'examen' | 'evolucion' | 'archivo' | null;

interface FilaDiagnostico { codigoCie: string; descripcion: string; }
interface FilaTratamiento { descripcion: string; indicaciones: string; fechaInicio: string; fechaFin: string; }

@Component({
  selector: 'app-historial-clinico',
  standalone: true,
  imports: [CommonModule, FormsModule],
  templateUrl: './historial-clinico.html',
  styleUrl: './historial-clinico.css'
})
export class HistorialClinicoComponent {
  private service = inject(HistorialService);

  idPaciente = input.required<number>();
  embebido = input(false);
  cerrar = output<void>();

  readonly hoy = new Date().toISOString().slice(0, 10);
  readonly estadosSalud = ['Estable', 'Mejorando', 'Delicado', 'Crítico', 'Recuperado'];
  readonly categoriasArchivo = ['Resultado de examen', 'Imagen diagnóstica', 'Receta', 'Consentimiento', 'Referencia médica', 'Otro'];
  readonly tamanoMaximo = 5 * 1024 * 1024;

  historial = signal<HistorialClinico | null>(null);
  cargando = signal(true);
  error = signal<string | null>(null);
  pestana = signal<Pestana>('consultas');
  formAbierto = signal<Formulario>(null);
  procesando = signal(false);

  formConsulta = this.consultaVacia();
  formDiagnostico = { idConsulta: null as number | null, codigoCie: '', descripcion: '', idDoctor: null as number | null };
  formExamen = this.examenVacio();
  formEvolucion = { notaEvolucion: '', estadoSalud: 'Estable', idResponsable: null as number | null };
  formArchivo = { categoria: 'Resultado de examen', idExamen: null as number | null, descripcion: '' };
  archivoSeleccionado: File | null = null;

  examenResultadoId: number | null = null;
  textoResultado = '';

  constructor() {
    // Se recarga cada vez que cambia el paciente seleccionado
    effect(() => {
      const id = this.idPaciente();
      untracked(() => {
        this.historial.set(null);
        this.cargando.set(true);
        this.error.set(null);
        this.pestana.set('consultas');
        this.formAbierto.set(null);
        this.cargar(id);
      });
    });
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
    this.formAbierto.set(null);
    this.examenResultadoId = null;
  }

  abrirForm(tipo: Formulario): void {
    if (tipo === 'consulta') this.formConsulta = this.consultaVacia();
    if (tipo === 'diagnostico') this.formDiagnostico = { idConsulta: this.historial()?.consultas[0]?.id ?? null, codigoCie: '', descripcion: '', idDoctor: null };
    if (tipo === 'examen') this.formExamen = this.examenVacio();
    if (tipo === 'evolucion') this.formEvolucion = { notaEvolucion: '', estadoSalud: 'Estable', idResponsable: null };
    if (tipo === 'archivo') {
      this.formArchivo = { categoria: 'Resultado de examen', idExamen: null, descripcion: '' };
      this.archivoSeleccionado = null;
    }
    this.formAbierto.set(tipo);
  }

  cancelarForm(): void {
    this.formAbierto.set(null);
    this.examenResultadoId = null;
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

  // ───────── Consultas, diagnósticos, exámenes, evolución ─────────

  guardarConsulta(): void {
    const f = this.formConsulta;
    if (!f.motivoConsulta.trim()) return this.avisar('Ingrese el motivo de la consulta.');
    if (!this.historial()?.permisos.esDoctor && f.idDoctor == null) return this.avisar('Seleccione el médico tratante.');

    const diagnosticos = f.diagnosticos.filter(d => d.descripcion.trim());
    const tratamientos = f.tratamientos.filter(t => t.descripcion.trim());
    if (tratamientos.some(t => t.fechaInicio && t.fechaFin && t.fechaFin < t.fechaInicio)) {
      return this.avisar('En los tratamientos, la fecha de fin no puede ser anterior a la de inicio.');
    }

    this.enviar(this.service.registrarConsulta({
      idPaciente: this.idPaciente(),
      idDoctor: f.idDoctor,
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
    if (f.idConsulta == null) return this.avisar('Seleccione la consulta a la que pertenece el diagnóstico.');
    if (!f.descripcion.trim()) return this.avisar('Ingrese la descripción del diagnóstico.');
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

    this.enviar(this.service.registrarExamen({
      idPaciente: this.idPaciente(),
      idConsulta: f.idConsulta,
      nombreExamen: f.nombreExamen.trim(),
      fechaExamen: f.fechaExamen,
      resultado: f.resultado.trim(),
      observaciones: f.observaciones.trim()
    }));
  }

  iniciarResultado(ex: ExamenRegistro): void {
    this.examenResultadoId = ex.id;
    this.textoResultado = ex.resultado ?? '';
  }

  guardarResultado(ex: ExamenRegistro): void {
    if (!this.textoResultado.trim()) return this.avisar('Ingrese el resultado del examen.');
    this.enviar(this.service.registrarResultado(ex.id, this.textoResultado.trim()));
  }

  guardarEvolucion(): void {
  const f = this.formEvolucion;
  if (!f.notaEvolucion.trim()) return this.avisar('Escriba la nota de evolución.');
  if (!this.historial()?.permisos.esPersonalClinico && f.idResponsable == null) {
    return this.avisar('Seleccione el responsable de la nota.');
  }

  this.enviar(this.service.registrarEvolucion({
    idPaciente: this.idPaciente(),
    notaEvolucion: f.notaEvolucion.trim(),
    estadoSalud: f.estadoSalud,
    idResponsable: f.idResponsable
  }));
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

  private enviar(peticion: Observable<any>): void {
    if (this.procesando()) return;
    this.procesando.set(true);

    peticion.subscribe({
      next: (res) => {
        this.procesando.set(false);
        this.formAbierto.set(null);
        this.examenResultadoId = null;
        this.archivoSeleccionado = null;
        this.cargar();
        setTimeout(() => alert(res?.mensaje ?? 'Registro guardado correctamente.'));
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
      diagnosticos: [] as FilaDiagnostico[],
      tratamientos: [] as FilaTratamiento[]
    };
  }

  private examenVacio() {
    return { idConsulta: null as number | null, nombreExamen: '', fechaExamen: this.hoy, resultado: '', observaciones: '' };
  }
}
