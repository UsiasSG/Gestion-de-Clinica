using Microsoft.AspNetCore.SignalR;
using Microsoft.AspNetCore.Authorization;

namespace ClinicaBackend.API.Hubs
{
    [Authorize]
    public class HabitacionesHub : Hub
    {
        public async Task CambiarEstadoHabitacion(int habitacionId, string nuevoEstado)
        {
            await Clients.All.SendAsync("EstadoHabitacionActualizado", habitacionId, nuevoEstado);
        }
    }
}
