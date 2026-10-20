import { Component, OnDestroy, OnInit, inject, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { ActivatedRoute, Router } from '@angular/router';
import { PacienteService } from '../../services/paciente.service';
import { Paciente } from '../../models/paciente.model';
import { HistorialClinicoComponent } from '../historial-clinico/historial-clinico';

@Component({
  selector: 'app-historial',
  standalone: true,
  imports: [CommonModule, FormsModule, HistorialClinicoComponent],
  templateUrl: './historial.html',
  styleUrl: './historial.css'
})
export class HistorialComponent implements OnInit, OnDestroy {
  private pacienteService = inject(PacienteService);
  private route = inject(ActivatedRoute);
  private router = inject(Router);

  pacientes = signal<Paciente[]>([]);
  cargando = signal(true);
  idSeleccionado = signal<number | null>(null);

  busqueda = '';
  private timerBusqueda?: ReturnType<typeof setTimeout>;

  ngOnInit(): void {
    // Permite llegar desde Pacientes con ?paciente=ID
    const id = Number(this.route.snapshot.queryParamMap.get('paciente'));
    if (id > 0) this.idSeleccionado.set(id);
    this.cargarPacientes();
  }

  ngOnDestroy(): void {
    clearTimeout(this.timerBusqueda);
  }

  cargarPacientes(): void {
    this.pacienteService.obtenerPacientes(this.busqueda.trim()).subscribe({
      next: (data) => {
        this.pacientes.set(data ?? []);
        this.cargando.set(false);
      },
      error: (err) => {
        console.error('Error al cargar pacientes:', err);
        this.cargando.set(false);
      }
    });
  }

  onBuscar(): void {
    clearTimeout(this.timerBusqueda);
    this.timerBusqueda = setTimeout(() => this.cargarPacientes(), 300);
  }

  seleccionar(p: Paciente): void {
    if (!p.id) return;
    this.idSeleccionado.set(p.id);
    this.router.navigate([], { relativeTo: this.route, queryParams: { paciente: p.id }, replaceUrl: true });
  }

  edad(fechaNacimiento?: string): number | null {
    if (!fechaNacimiento) return null;
    const nac = new Date(fechaNacimiento);
    const hoy = new Date();
    let edad = hoy.getFullYear() - nac.getFullYear();
    if (hoy.getMonth() < nac.getMonth() || (hoy.getMonth() === nac.getMonth() && hoy.getDate() < nac.getDate())) edad--;
    return edad;
  }
}
