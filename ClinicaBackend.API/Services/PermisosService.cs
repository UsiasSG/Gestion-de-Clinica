using ClinicaBackend.API.Data;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace ClinicaBackend.API.Services
{
    public class PermisoModuloDto
    {
        public string Codigo { get; set; } = string.Empty;
        public string Nombre { get; set; } = string.Empty;
        public bool Consultar { get; set; }
        public bool Crear { get; set; }
        public bool Modificar { get; set; }
        public bool Eliminar { get; set; }
    }

    public class PermisosService
    {
        private readonly ClinicaDbContext _db;

        public PermisosService(ClinicaDbContext db) => _db = db;

        // Devuelve el rol solo si el usuario existe y está activo
        private async Task<int?> RolActivoAsync(int idUsuario)
        {
            var usuario = await _db.Usuarios.AsNoTracking()
                .Where(u => u.Id == idUsuario)
                .Select(u => new { u.IdRol, u.Estado })
                .FirstOrDefaultAsync();

            return usuario != null && usuario.Estado ? usuario.IdRol : null;
        }

        public async Task<bool> TienePermisoAsync(int idUsuario, string moduloCodigo, string accion)
        {
            var idRol = await RolActivoAsync(idUsuario);
            if (idRol == null) return false;

            var permiso = await _db.Permisos.AsNoTracking()
                .FirstOrDefaultAsync(p => p.IdRol == idRol && p.Modulo != null && p.Modulo.Codigo == moduloCodigo);

            if (permiso == null) return false;

            return accion.ToLowerInvariant() switch
            {
                "consultar" => permiso.PuedeConsultar,
                "crear" => permiso.PuedeCrear,
                "modificar" => permiso.PuedeModificar,
                "eliminar" => permiso.PuedeEliminar,
                _ => false
            };
        }

        // Permisos del usuario para construir el menú dinámico
        public async Task<List<PermisoModuloDto>> ObtenerPermisosUsuarioAsync(int idUsuario)
        {
            var idRol = await RolActivoAsync(idUsuario);
            if (idRol == null) return new();

            return await _db.Permisos.AsNoTracking()
                .Where(p => p.IdRol == idRol && p.Modulo != null && p.PuedeConsultar)
                .Select(p => new PermisoModuloDto
                {
                    Codigo = p.Modulo!.Codigo,
                    Nombre = p.Modulo.Nombre,
                    Consultar = p.PuedeConsultar,
                    Crear = p.PuedeCrear,
                    Modificar = p.PuedeModificar,
                    Eliminar = p.PuedeEliminar
                })
                .ToListAsync();
        }
    }
}