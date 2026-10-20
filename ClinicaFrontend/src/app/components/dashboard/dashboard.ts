import { Component, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ActivatedRoute, RouterModule } from '@angular/router';
import { toSignal } from '@angular/core/rxjs-interop';
import { map } from 'rxjs';
import { PermisosService } from '../../services/permisos.service';

@Component({
  selector: 'app-dashboard',
  standalone: true,
  imports: [CommonModule, RouterModule],
  templateUrl: './dashboard.html'
})
export class DashboardComponent {
  permisos = inject(PermisosService);
  sinAcceso = toSignal(inject(ActivatedRoute).queryParamMap.pipe(map(p => p.get('sinAcceso'))));
}
