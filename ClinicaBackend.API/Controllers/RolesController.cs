using ClinicaBackend.API.Data;
using ClinicaBackend.API.Extensions;
using ClinicaBackend.API.Filters;
using ClinicaBackend.API.Hubs;
using ClinicaBackend.API.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;


namespace ClinicaBackend.API.Controllers
{
    [Authorize]
    [ApiController]
    [Route("api/[controller]")]
    public class RolesController : ControllerBase
    {
        private const string ModuloRoles = "ROLES";

        private readonly ClinicaDbContext _db;
        private readonly IHubContext<NotificacionesHub> _hub;

        public RolesController(ClinicaDbContext db, IHubContext<NotificacionesHub> hub)
        {
            _db = db;
            _hub = hub;
        }

        // GET: api/Roles
        [HttpGet]
        [RequierePermiso(ModuloRoles, "Consultar")]
        public async Task<IActionResult> ObtenerRoles()
        {
            var roles = await _db.Roles.AsNoTracking()
                .OrderBy(r => r.Id)
                .Select(r => new
                {
                    r.Id,
                    r.Nombre,
                    r.Descripcion,
                    Usuarios = _db.Usuarios.Count(u => u.IdRol == r.Id),
                    ModulosConAcceso = _db.Permisos.Count(p => p.IdRol == r.Id && p.PuedeConsultar)
                })
                .ToListAsync();

            return Ok(roles);
        }

        // GET: api/Roles/5/permisos  (matriz completa: todos los módulos, con o sin permiso)
        [HttpGet("{id:int}/permisos")]
        [RequierePermiso(ModuloRoles, "Consultar")]
        public async Task<IActionResult> ObtenerMatriz(int id)
        {
            if (!await _db.Roles.AnyAsync(r => r.Id == id))
                return NotFound(new { mensaje = "Rol no encontrado." });

            var permisos = await _db.Permisos.AsNoTracking().Where(p => p.IdRol == id).ToListAsync();
            var modulos = await _db.Modulos.AsNoTracking().OrderBy(m => m.Id).ToListAsync();

            var matriz = modulos.Select(m =>
            {
                var p = permisos.FirstOrDefault(x => x.IdModulo == m.Id);
                return new PermisoMatrizDto
                {
                    IdModulo = m.Id,
                    Modulo = m.Nombre,
                    Codigo = m.Codigo,
                    Consultar = p?.PuedeConsultar ?? false,
                    Crear = p?.PuedeCrear ?? false,
                    Modificar = p?.PuedeModificar ?? false,
                    Eliminar = p?.PuedeEliminar ?? false
                };
            });

            return Ok(matriz);
        }

        // POST: api/Roles
        [HttpPost]
        [RequierePermiso(ModuloRoles, "Crear")]
        public async Task<IActionResult> CrearRol([FromBody] RolRequestDto model)
        {
            var nombre = model.Nombre?.Trim();
            if (string.IsNullOrWhiteSpace(nombre))
                return BadRequest(new { mensaje = "El nombre del rol es obligatorio." });
            if (await _db.Roles.AnyAsync(r => r.Nombre == nombre))
                return BadRequest(new { mensaje = "Ya existe un rol con ese nombre." });

            var rol = new Rol { Nombre = nombre, Descripcion = string.IsNullOrWhiteSpace(model.Descripcion) ? null : model.Descripcion.Trim() };
            _db.Roles.Add(rol);
            await _db.SaveChangesAsync();

            return Ok(new { mensaje = $"Rol \"{rol.Nombre}\" creado. Ahora asígnele permisos en la matriz.", id = rol.Id });
        }

        // PUT: api/Roles/5
        [HttpPut("{id:int}")]
        [RequierePermiso(ModuloRoles, "Modificar")]
        public async Task<IActionResult> ActualizarRol(int id, [FromBody] RolRequestDto model)
        {
            var rol = await _db.Roles.FindAsync(id);
            if (rol == null) return NotFound(new { mensaje = "Rol no encontrado." });

            var nombre = model.Nombre?.Trim();
            if (string.IsNullOrWhiteSpace(nombre))
                return BadRequest(new { mensaje = "El nombre del rol es obligatorio." });
            if (await _db.Roles.AnyAsync(r => r.Id != id && r.Nombre == nombre))
                return BadRequest(new { mensaje = "Ya existe un rol con ese nombre." });

            rol.Nombre = nombre;
            rol.Descripcion = string.IsNullOrWhiteSpace(model.Descripcion) ? null : model.Descripcion.Trim();
            await _db.SaveChangesAsync();

            return Ok(new { mensaje = "Rol actualizado correctamente." });
        }

        // DELETE: api/Roles/5
        [HttpDelete("{id:int}")]
        [RequierePermiso(ModuloRoles, "Eliminar")]
        public async Task<IActionResult> EliminarRol(int id)
        {
            var rol = await _db.Roles.FindAsync(id);
            if (rol == null) return NotFound(new { mensaje = "Rol no encontrado." });

            var usuarios = await _db.Usuarios.CountAsync(u => u.IdRol == id);
            if (usuarios > 0)
                return BadRequest(new { mensaje = $"El rol tiene {usuarios} usuario(s) asignado(s). Cámbielos de rol antes de eliminarlo." });

            _db.Permisos.RemoveRange(_db.Permisos.Where(p => p.IdRol == id));
            _db.Roles.Remove(rol);
            await _db.SaveChangesAsync();

            return Ok(new { mensaje = $"Rol \"{rol.Nombre}\" eliminado." });
        }

        // PUT: api/Roles/5/permisos  (guarda la matriz y avisa en tiempo real)
        [HttpPut("{id:int}/permisos")]
        [RequierePermiso(ModuloRoles, "Modificar")]
        public async Task<IActionResult> GuardarMatriz(int id, [FromBody] List<PermisoMatrizDto> matriz)
        {
            var rol = await _db.Roles.FindAsync(id);
            if (rol == null) return NotFound(new { mensaje = "Rol no encontrado." });
            if (matriz == null) return BadRequest(new { mensaje = "La matriz de permisos es obligatoria." });

            var idsModulos = await _db.Modulos.Select(m => m.Id).ToListAsync();
            if (matriz.Any(f => !idsModulos.Contains(f.IdModulo)))
                return BadRequest(new { mensaje = "La matriz contiene módulos que no existen." });

            // Evita que el usuario se bloquee a sí mismo el acceso a esta pantalla
            var idUsuario = User.ObtenerIdUsuario();
            var rolPropio = await _db.Usuarios.Where(u => u.Id == idUsuario).Select(u => u.IdRol).FirstOrDefaultAsync();
            if (rolPropio == id)
            {
                var idModuloRoles = await _db.Modulos.Where(m => m.Codigo == ModuloRoles).Select(m => m.Id).FirstOrDefaultAsync();
                var filaRoles = matriz.FirstOrDefault(f => f.IdModulo == idModuloRoles);
                if (filaRoles == null || !filaRoles.Modificar)
                    return BadRequest(new { mensaje = "No puede quitarse a sí mismo el permiso de modificar Roles: perdería el acceso a esta pantalla." });
            }

            var existentes = await _db.Permisos.Where(p => p.IdRol == id).ToListAsync();

            foreach (var fila in matriz)
            {
                // Quien puede crear, modificar o eliminar necesita poder consultar
                var consultar = fila.Consultar || fila.Crear || fila.Modificar || fila.Eliminar;
                var permiso = existentes.FirstOrDefault(p => p.IdModulo == fila.IdModulo);

                if (permiso == null)
                {
                    if (!consultar) continue;
                    permiso = new Permiso { IdRol = id, IdModulo = fila.IdModulo };
                    _db.Permisos.Add(permiso);
                }

                permiso.PuedeConsultar = consultar;
                permiso.PuedeCrear = fila.Crear;
                permiso.PuedeModificar = fila.Modificar;
                permiso.PuedeEliminar = fila.Eliminar;
            }

            await _db.SaveChangesAsync();

            // Requisito 12b: los menús de los usuarios conectados se actualizan sin recargar
            await _hub.Clients.All.SendAsync("PermisosActualizados", id);

            return Ok(new { mensaje = $"Permisos del rol \"{rol.Nombre}\" actualizados. Los usuarios conectados verán el cambio de inmediato." });
        }
    }

    public class RolRequestDto
    {
        public string? Nombre { get; set; }
        public string? Descripcion { get; set; }
    }

    public class PermisoMatrizDto
    {
        public int IdModulo { get; set; }
        public string? Modulo { get; set; }
        public string? Codigo { get; set; }
        public bool Consultar { get; set; }
        public bool Crear { get; set; }
        public bool Modificar { get; set; }
        public bool Eliminar { get; set; }
    }
}