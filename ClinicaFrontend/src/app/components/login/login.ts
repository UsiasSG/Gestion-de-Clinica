import { Component, inject, ChangeDetectorRef } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { Router } from '@angular/router';
import { AuthService } from '../../services/auth.service';

@Component({
  selector: 'app-login',
  standalone: true,
  imports: [CommonModule, FormsModule],
  templateUrl: './login.html'
})
export class LoginComponent {
  private authService = inject(AuthService);
  private router = inject(Router);
  private cdr = inject(ChangeDetectorRef);

  nombreUsuario: string = '';
  clave: string = '';
  isLoading: boolean = false;
  errorMessage: string = '';

  login(): void {
    if (!this.nombreUsuario || !this.clave) {
      this.errorMessage = 'Por favor ingresa usuario y contraseña.';
      this.cdr.markForCheck();
      return;
    }

    this.isLoading = true;
    this.errorMessage = '';
    this.cdr.markForCheck();

    const payload = {
      username: this.nombreUsuario,
      password: this.clave,
      nombreUsuario: this.nombreUsuario,
      clave: this.clave
    };

    this.authService.login(payload).subscribe({
      next: () => {
        this.isLoading = false;
        this.cdr.markForCheck();

        // Navegación limpia
        this.router.navigate(['/dashboard']).then((navigated) => {
          if (!navigated) {
            // Alternativa directa si existe un Guard bloqueando la primera emisión
            window.location.href = '/dashboard';
          }
        });
      },
      error: () => {
        this.isLoading = false;
        this.errorMessage = 'Credenciales no válidas o error de conexión.';
        this.cdr.markForCheck();
      }
    });
  }
}
