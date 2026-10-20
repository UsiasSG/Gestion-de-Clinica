using ClinicaBackend.API.Data;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using System.Threading.Tasks;

namespace ClinicaBackend.API.Extensions
{
    public static class UsuarioActualExtensions
    {
        public static int? ObtenerIdUsuario(this ClaimsPrincipal user)
        {
            var valor = user.FindFirstValue(ClaimTypes.NameIdentifier) ?? user.FindFirstValue("sub");
            return int.TryParse(valor, out var id) ? id : null;
        }

        public static async Task<int?> ObtenerIdSucursalAsync(this ClinicaDbContext db, ClaimsPrincipal user)
        {
            var idUsuario = user.ObtenerIdUsuario();
            if (idUsuario == null) return null;

            return await db.Usuarios
                .Where(u => u.Id == idUsuario)
                .Select(u => u.IdSucursal)
                .FirstOrDefaultAsync();
        }
    }
}