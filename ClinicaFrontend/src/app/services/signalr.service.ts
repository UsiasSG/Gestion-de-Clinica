import { Injectable } from '@angular/core';
import * as signalR from '@microsoft/signalr';
import { BehaviorSubject, Observable } from 'rxjs';

export interface EstadoHabitacionCambiado {
  habitacionId: number;
  nuevoEstado: string;
}

@Injectable({
  providedIn: 'root'
})
export class SignalRService {
  private hubConnection!: signalR.HubConnection;
  private estadoHabitacionSubject = new BehaviorSubject<EstadoHabitacionCambiado | null>(null);

  public estadoHabitacion$: Observable<EstadoHabitacionCambiado | null> = this.estadoHabitacionSubject.asObservable();

  public iniciarConexion(): void {
    const token = localStorage.getItem('jwt_token');

    this.hubConnection = new signalR.HubConnectionBuilder()
      .withUrl('http://localhost:5219/hubs/habitaciones', {
        accessTokenFactory: () => token || ''
      })
      .withAutomaticReconnect()
      .build();

    this.hubConnection
      .start()
      .then(() => console.log('Conexión SignalR establecida correctamente'))
      .catch(err => console.error('Error al conectar con SignalR Hub:', err));

    // Escuchar el evento que emite el backend .NET
    this.hubConnection.on('EstadoHabitacionCambiado', (habitacionId: number, nuevoEstado: string) => {
      this.estadoHabitacionSubject.next({ habitacionId, nuevoEstado });
    });
  }

  public detenerConexion(): void {
    if (this.hubConnection) {
      this.hubConnection.stop();
    }
  }
}
