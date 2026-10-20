using ClinicaBackend.API.Data;
using ClinicaBackend.API.Filters;
using ClinicaBackend.API.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ClinicaBackend.API.Controllers
{
    [Authorize]
    [ApiController]
    [Route("api/[controller]")]
    public class PacientesController : ControllerBase
    {
        private readonly ClinicaDbContext _db;

        public PacientesController(ClinicaDbContext db)
        {
            _db = db;
        }

        // GET: api/Pacientes?busqueda=juan
        [HttpGet]
        [RequierePermiso("PACIENTES", "Consultar")]
        public async Task<IActionResult> ObtenerPacientes([FromQuery] string? busqueda)
        {
            var query = _db.Pacientes.AsNoTracking().AsQueryable();

            if (!string.IsNullOrWhiteSpace(busqueda))
            {
                var b = busqueda.Trim();
                query = query.Where(p =>
                    (p.Nombres + " " + p.Apellidos).Contains(b) ||
                    (p.DocumentoIdentificacion != null && p.DocumentoIdentificacion.Contains(b)) ||
                    (p.DpiEncargado != null && p.DpiEncargado.Contains(b)) ||
                    (p.NombreEncargado != null && p.NombreEncargado.Contains(b)) ||
                    (p.Telefono != null && p.Telefono.Contains(b)));
            }

            var pacientes = await query
                .OrderByDescending(p => p.FechaRegistro)
                .Select(p => new
                {
                    p.Id,
                    p.Nombres,
                    p.Apellidos,
                    NombreCompleto = p.Nombres + " " + p.Apellidos,
                    p.DocumentoIdentificacion,
                    p.FechaNacimiento,
                    p.Genero,
                    p.TipoSangre,
                    p.Alergias,
                    p.Telefono,
                    p.Direccion,
                    p.ContactoEmergencia,
                    p.EsMenorEdad,
                    p.NombreEncargado,
                    p.DpiEncargado,
                    p.FechaRegistro
                })
                .ToListAsync();

            return Ok(pacientes);
        }

        // GET: api/Pacientes/5
        [HttpGet("{id:int}")]
        [RequierePermiso("PACIENTES", "Consultar")]
        public async Task<IActionResult> ObtenerPacientePorId(int id)
        {
            var paciente = await _db.Pacientes.AsNoTracking().FirstOrDefaultAsync(p => p.Id == id);
            if (paciente == null) return NotFound(new { mensaje = "Paciente no encontrado." });

            return Ok(paciente);
        }

        // POST: api/Pacientes
        [HttpPost]
        [RequierePermiso("PACIENTES", "Crear")]
        public async Task<IActionResult> CrearPaciente([FromBody] PacienteDto model)
        {
            var error = await ValidarAsync(model, null);
            if (error != null) return BadRequest(new { mensaje = error });

            var paciente = new Paciente { FechaRegistro = DateTime.Now };
            AsignarDatos(paciente, model);

            _db.Pacientes.Add(paciente);
            await _db.SaveChangesAsync();

            return Ok(new { mensaje = "Paciente registrado con éxito.", id = paciente.Id });
        }

        // PUT: api/Pacientes/5
        [HttpPut("{id:int}")]
        [RequierePermiso("PACIENTES", "Modificar")]
        public async Task<IActionResult> ActualizarPaciente(int id, [FromBody] PacienteDto model)
        {
            var paciente = await _db.Pacientes.FindAsync(id);
            if (paciente == null) return NotFound(new { mensaje = "Paciente no encontrado." });

            var error = await ValidarAsync(model, id);
            if (error != null) return BadRequest(new { mensaje = error });

            AsignarDatos(paciente, model);
            await _db.SaveChangesAsync();

            return Ok(new { mensaje = "Datos del paciente actualizados con éxito." });
        }

        // DELETE: api/Pacientes/5
        [HttpDelete("{id:int}")]
        [RequierePermiso("PACIENTES", "Eliminar")]
        public async Task<IActionResult> EliminarPaciente(int id)
        {
            var paciente = await _db.Pacientes.FindAsync(id);
            if (paciente == null) return NotFound(new { mensaje = "Paciente no encontrado." });

            var tieneRegistros = await _db.ConsultasMedicas.AnyAsync(c => c.IdPaciente == id)
                              || await _db.Examenes.AnyAsync(e => e.IdPaciente == id)
                              || await _db.EvolucionesPaciente.AnyAsync(e => e.IdPaciente == id)
                              || await _db.ArchivosClinicos.AnyAsync(a => a.IdPaciente == id)
                              || await _db.AsignacionHabitaciones.AnyAsync(a => a.IdPaciente == id)
                              || await _db.Ventas.AnyAsync(v => v.IdPaciente == id);

            if (tieneRegistros)
                return BadRequest(new { mensaje = "El paciente tiene historial clínico, internamientos o compras registradas. Los expedientes médicos deben conservarse, por lo que no puede eliminarse." });

            _db.Pacientes.Remove(paciente);
            await _db.SaveChangesAsync();

            return Ok(new { mensaje = "Paciente eliminado con éxito." });
        }

        // ───────── Auxiliares ─────────

        private async Task<string?> ValidarAsync(PacienteDto m, int? idPaciente)
        {
            if (string.IsNullOrWhiteSpace(m.Nombres) || string.IsNullOrWhiteSpace(m.Apellidos))
                return "Los nombres y apellidos son obligatorios.";
            if (m.FechaNacimiento == default || m.FechaNacimiento.Date > DateTime.Today)
                return "La fecha de nacimiento no es válida.";

            if (!m.EsMenorEdad && !string.IsNullOrWhiteSpace(m.DocumentoIdentificacion))
            {
                var dpi = m.DocumentoIdentificacion.Trim();
                if (await _db.Pacientes.AnyAsync(p => p.DocumentoIdentificacion == dpi && p.Id != idPaciente))
                    return "Ya existe un paciente registrado con ese DPI.";
            }

            return null;
        }

        private static void AsignarDatos(Paciente paciente, PacienteDto m)
        {
            paciente.Nombres = m.Nombres.Trim();
            paciente.Apellidos = m.Apellidos.Trim();
            paciente.DocumentoIdentificacion = m.EsMenorEdad ? null : m.DocumentoIdentificacion?.Trim();
            paciente.FechaNacimiento = m.FechaNacimiento;
            paciente.Genero = m.Genero;
            paciente.TipoSangre = m.TipoSangre;
            paciente.Alergias = m.Alergias?.Trim();
            paciente.Telefono = m.Telefono?.Trim();
            paciente.Direccion = m.Direccion?.Trim();
            paciente.ContactoEmergencia = m.ContactoEmergencia?.Trim();
            paciente.EsMenorEdad = m.EsMenorEdad;
            paciente.NombreEncargado = m.EsMenorEdad ? m.NombreEncargado?.Trim() : null;
            paciente.DpiEncargado = m.EsMenorEdad ? m.DpiEncargado?.Trim() : null;
        }
    }

    public class PacienteDto
    {
        public string Nombres { get; set; } = string.Empty;
        public string Apellidos { get; set; } = string.Empty;
        public string? DocumentoIdentificacion { get; set; }
        public DateTime FechaNacimiento { get; set; }
        public string? Genero { get; set; }
        public string? TipoSangre { get; set; }
        public string? Alergias { get; set; }
        public string? Telefono { get; set; }
        public string? Direccion { get; set; }
        public string? ContactoEmergencia { get; set; }
        public bool EsMenorEdad { get; set; }
        public string? NombreEncargado { get; set; }
        public string? DpiEncargado { get; set; }
    }
}