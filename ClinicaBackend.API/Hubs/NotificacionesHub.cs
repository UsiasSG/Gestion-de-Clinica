using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;

namespace ClinicaBackend.API.Hubs
{
    // Avisos generales del sistema (por ahora: cambios de permisos para refrescar el menú)
    [Authorize]
    public class NotificacionesHub : Hub
    {
    }
}