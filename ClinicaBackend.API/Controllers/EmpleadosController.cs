using ClinicaBackend.API.Data;
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
    public class EmpleadosController : ControllerBase
    {
        private static readonly string[] TiposValidos = { "Doctor", "Enfermera", "Administrativo" };

        private readonly ClinicaDbContext _db;
        private readonly IHubContext<NotificacionesHub> _hub;

        public EmpleadosController(ClinicaDbContext db, IHubContext<NotificacionesHub> hub)
        {
            _db = db;
            _hub = hub;
        }

        // GET: api/Empleados
        [HttpGet]
        [RequierePermiso("PERSONAL", "Consultar")]
        public async Task<IActionResult> ObtenerEmpleados()
        {
            var empleados = await _db.Empleados
                .AsNoTracking()
                .OrderBy(e => e.Apellidos).ThenBy(e => e.Nombres)
                .Select(e => new
                {
                    e.Id,
                    e.Nombres,
                    e.Apellidos,
                    NombreCompleto = e.Nombres + " " + e.Apellidos,
                    e.TipoEmpleado,
                    e.Puesto,
                    e.Telefono,
                    e.Correo,
                    e.IdEspecialidad,
                    EspecialidadNombre = _db.Especialidades
                        .Where(x => x.Id == e.IdEspecialidad).Select(x => x.Nombre).FirstOrDefault(),
                    e.IdSucursal,
                    SucursalNombre = _db.Sucursales
                        .Where(x => x.Id == e.IdSucursal).Select(x => x.Nombre).FirstOrDefault(),
                    TieneAcceso = e.Usuario != null && e.Usuario.Estado,
                    NombreUsuario = e.Usuario != null ? e.Usuario.NombreUsuario : null,
                    IdRol = e.Usuario != null ? e.Usuario.IdRol : null,
                    RolNombre = e.Usuario != null && e.Usuario.Rol != null ? e.Usuario.Rol.Nombre : null
                })
                .ToListAsync();

            return Ok(empleados);
        }

        // GET: api/Empleados/auxiliares
        [HttpGet("auxiliares")]
        [RequierePermiso("PERSONAL", "Consultar")]
        public async Task<IActionResult> ObtenerDatosAuxiliares()
        {
            var especialidades = await _db.Especialidades.AsNoTracking()
                .OrderBy(x => x.Nombre).Select(x => new { x.Id, x.Nombre }).ToListAsync();

            var sucursales = await _db.Sucursales.AsNoTracking()
                .Where(x => x.Estado).OrderBy(x => x.Nombre).Select(x => new { x.Id, x.Nombre }).ToListAsync();

            var roles = await _db.Roles.AsNoTracking()
                .OrderBy(x => x.Nombre).Select(x => new { x.Id, x.Nombre }).ToListAsync();

            return Ok(new { especialidades, sucursales, roles });
        }

        // POST: api/Empleados/especialidades
        [HttpPost("especialidades")]
        [RequierePermiso("PERSONAL", "Crear")]
        public async Task<IActionResult> CrearEspecialidad([FromBody] NuevaEspecialidadDto model)
        {
            var nombre = model.Nombre?.Trim();
            if (string.IsNullOrWhiteSpace(nombre))
                return BadRequest(new { mensaje = "El nombre de la especialidad es obligatorio." });

            var existente = await _db.Especialidades.FirstOrDefaultAsync(x => x.Nombre == nombre);
            if (existente != null) return Ok(new { existente.Id, existente.Nombre });

            var especialidad = new Especialidad { Nombre = nombre };
            _db.Especialidades.Add(especialidad);
            await _db.SaveChangesAsync();

            return Ok(new { especialidad.Id, especialidad.Nombre });
        }

        // POST: api/Empleados
        [HttpPost]
        [RequierePermiso("PERSONAL", "Crear")]
        public async Task<IActionResult> CrearEmpleado([FromBody] CreacionEmpleadoDto model)
        {
            var error = await ValidarEmpleadoAsync(model, null);
            if (error != null) return BadRequest(new { mensaje = error });

            var empleado = new Empleado();
            AsignarDatos(empleado, model);

            if (model.CrearAcceso)
            {
                var errorAcceso = await ValidarAccesoAsync(model, null, requierePassword: true);
                if (errorAcceso != null) return BadRequest(new { mensaje = errorAcceso });

                // Se asigna por navegación: EF inserta usuario y empleado en una sola transacción
                empleado.Usuario = NuevoUsuario(model);
            }

            _db.Empleados.Add(empleado);
            await _db.SaveChangesAsync();

            return Ok(new { mensaje = "Empleado registrado con éxito.", id = empleado.Id });
        }

        // PUT: api/Empleados/5
        [HttpPut("{id:int}")]
        [RequierePermiso("PERSONAL", "Modificar")]
        public async Task<IActionResult> ActualizarEmpleado(int id, [FromBody] CreacionEmpleadoDto model)
        {
            var empleado = await _db.Empleados.Include(e => e.Usuario).FirstOrDefaultAsync(e => e.Id == id);
            if (empleado == null) return NotFound(new { mensaje = "Empleado no encontrado." });

            var error = await ValidarEmpleadoAsync(model, id);
            if (error != null) return BadRequest(new { mensaje = error });

            AsignarDatos(empleado, model);

            if (model.CrearAcceso)
            {
                if (empleado.Usuario == null)
                {
                    var errorAcceso = await ValidarAccesoAsync(model, null, requierePassword: true);
                    if (errorAcceso != null) return BadRequest(new { mensaje = errorAcceso });
                    empleado.Usuario = NuevoUsuario(model);
                }
                else
                {
                    var errorAcceso = await ValidarAccesoAsync(model, empleado.Usuario.Id, requierePassword: false);
                    if (errorAcceso != null) return BadRequest(new { mensaje = errorAcceso });

                    empleado.Usuario.NombreUsuario = model.NombreUsuario!.Trim();
                    empleado.Usuario.Correo = model.Correo.Trim();
                    empleado.Usuario.IdRol = model.IdRol;
                    empleado.Usuario.IdSucursal = model.IdSucursal;
                    empleado.Usuario.Estado = true;

                    if (!string.IsNullOrWhiteSpace(model.Password))
                        empleado.Usuario.PasswordHash = BCrypt.Net.BCrypt.HashPassword(model.Password);
                }
            }
            else if (empleado.Usuario != null)
            {
                // Revocar acceso: se desactiva en vez de borrar, para conservar el historial
                empleado.Usuario.Estado = false;
            }

            await _db.SaveChangesAsync();

            // Si cambió el rol o se revocó el acceso, los menús conectados se actualizan
            await _hub.Clients.All.SendAsync("PermisosActualizados", model.IdRol);

            return Ok(new { mensaje = "Empleado actualizado correctamente." });
        }

        // DELETE: api/Empleados/5
        [HttpDelete("{id:int}")]
        [RequierePermiso("PERSONAL", "Eliminar")]
        public async Task<IActionResult> EliminarEmpleado(int id)
        {
            var empleado = await _db.Empleados.Include(e => e.Usuario).FirstOrDefaultAsync(e => e.Id == id);
            if (empleado == null) return NotFound(new { mensaje = "Empleado no encontrado." });

            if (empleado.Usuario != null)
            {
                var idUsuario = empleado.Usuario.Id;
                var tieneHistorial = await _db.Ventas.AnyAsync(v => v.IdUsuarioVendedor == idUsuario)
                                  || await _db.MovimientosInventario.AnyAsync(m => m.IdUsuario == idUsuario)
                                  || await _db.ArchivosClinicos.AnyAsync(a => a.IdUsuario == idUsuario);

                if (tieneHistorial)
                    empleado.Usuario.Estado = false; // Se conserva para no romper el historial
                else
                    _db.Usuarios.Remove(empleado.Usuario);
            }

            _db.Empleados.Remove(empleado);

            try
            {
                await _db.SaveChangesAsync();
            }
            catch (DbUpdateException)
            {
                return BadRequest(new { mensaje = "No se puede eliminar: el empleado tiene registros asociados (consultas, diagnósticos, evolución u otros)." });
            }

            return Ok(new { mensaje = "Empleado eliminado correctamente." });
        }

        // ───────── Auxiliares privados ─────────

        private static void AsignarDatos(Empleado empleado, CreacionEmpleadoDto model)
        {
            var esDoctor = model.TipoEmpleado == "Doctor";

            empleado.Nombres = model.Nombres.Trim();
            empleado.Apellidos = model.Apellidos.Trim();
            empleado.TipoEmpleado = model.TipoEmpleado;
            empleado.Puesto = esDoctor ? null : model.Puesto?.Trim();
            empleado.IdEspecialidad = esDoctor ? model.IdEspecialidad : null;
            empleado.Telefono = model.Telefono?.Trim();
            empleado.Correo = model.Correo.Trim();
            empleado.IdSucursal = model.IdSucursal;
        }

        private static Usuario NuevoUsuario(CreacionEmpleadoDto model) => new()
        {
            NombreUsuario = model.NombreUsuario!.Trim(),
            Correo = model.Correo.Trim(),
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(model.Password),
            IdRol = model.IdRol,
            IdSucursal = model.IdSucursal,
            Estado = true,
            FechaCreacion = DateTime.Now
        };

        private async Task<string?> ValidarEmpleadoAsync(CreacionEmpleadoDto m, int? idEmpleado)
        {
            if (string.IsNullOrWhiteSpace(m.Nombres) || string.IsNullOrWhiteSpace(m.Apellidos))
                return "Nombres y apellidos son obligatorios.";
            if (string.IsNullOrWhiteSpace(m.Correo))
                return "El correo es obligatorio.";
            if (string.IsNullOrWhiteSpace(m.Telefono))
                return "El teléfono es obligatorio.";
            if (!TiposValidos.Contains(m.TipoEmpleado))
                return "Tipo de empleado no válido.";
            if (m.TipoEmpleado == "Doctor" && m.IdEspecialidad == null)
                return "Un doctor debe tener una especialidad asignada.";
            if (m.TipoEmpleado != "Doctor" && string.IsNullOrWhiteSpace(m.Puesto))
                return "Debe indicar el puesto del empleado.";
            if (m.IdSucursal == null || !await _db.Sucursales.AnyAsync(s => s.Id == m.IdSucursal))
                return "Seleccione una sucursal válida.";
            if (m.IdEspecialidad != null && !await _db.Especialidades.AnyAsync(s => s.Id == m.IdEspecialidad))
                return "La especialidad seleccionada no existe.";

            var correo = m.Correo.Trim();
            if (await _db.Empleados.AnyAsync(e => e.Correo == correo && e.Id != idEmpleado))
                return "Ya existe un empleado registrado con ese correo.";

            return null;
        }

        private async Task<string?> ValidarAccesoAsync(CreacionEmpleadoDto m, int? idUsuarioActual, bool requierePassword)
        {
            if (string.IsNullOrWhiteSpace(m.NombreUsuario))
                return "El nombre de usuario es obligatorio.";

            var nombreUsuario = m.NombreUsuario.Trim();
            if (await _db.Usuarios.AnyAsync(u => u.NombreUsuario == nombreUsuario && u.Id != idUsuarioActual))
                return "El nombre de usuario ya existe.";

            if (requierePassword && string.IsNullOrWhiteSpace(m.Password))
                return "La contraseña es obligatoria.";
            if (!string.IsNullOrWhiteSpace(m.Password) && m.Password.Length < 6)
                return "La contraseña debe tener al menos 6 caracteres.";

            if (m.IdRol == null || !await _db.Roles.AnyAsync(r => r.Id == m.IdRol))
                return "Seleccione un rol válido.";

            return null;
        }
    }

    public class CreacionEmpleadoDto
    {
        public string Nombres { get; set; } = null!;
        public string Apellidos { get; set; } = null!;
        public string TipoEmpleado { get; set; } = null!;
        public string? Puesto { get; set; }
        public string? Telefono { get; set; }
        public string Correo { get; set; } = null!;
        public int? IdEspecialidad { get; set; }
        public int? IdSucursal { get; set; }

        public bool CrearAcceso { get; set; }
        public string? NombreUsuario { get; set; }
        public string? Password { get; set; }
        public int? IdRol { get; set; }
    }

    public class NuevaEspecialidadDto
    {
        public string? Nombre { get; set; }
    }
}