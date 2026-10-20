using ClinicaBackend.API.Data;
using ClinicaBackend.API.Filters;
using ClinicaBackend.API.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System;
using System.Threading.Tasks;

namespace ClinicaBackend.API.Controllers
{
    [Authorize]
    [ApiController]
    [Route("api/tarifario")]
    public class TarifarioController : ControllerBase
    {
        private readonly ClinicaDbContext _db;

        public TarifarioController(ClinicaDbContext db) => _db = db;

        // GET: api/tarifario
        [HttpGet]
        [RequierePermiso("TARIFARIO", "Consultar")]
        public async Task<IActionResult> ObtenerServicios()
        {
            var lista = await _db.ServiciosClinicos.AsNoTracking()
                .OrderBy(s => s.Tipo).ThenBy(s => s.Nombre)
                .Select(s => new { s.Id, s.Codigo, s.Nombre, s.Tipo, s.Descripcion, s.Precio, s.Activo })
                .ToListAsync();

            return Ok(new { servicios = lista, tipos = TiposServicio.Todos });
        }

        // POST: api/tarifario
        [HttpPost]
        [RequierePermiso("TARIFARIO", "Crear")]
        public async Task<IActionResult> CrearServicio([FromBody] ServicioDto model)
        {
            var (error, tipo) = await ValidarAsync(model, null);
            if (error != null) return BadRequest(new { mensaje = error });

            var servicio = new ServicioClinico();
            Asignar(servicio, model, tipo!);
            servicio.Activo = true;

            _db.ServiciosClinicos.Add(servicio);
            await _db.SaveChangesAsync();
            return Ok(new { mensaje = $"Servicio \"{servicio.Nombre}\" agregado al tarifario.", id = servicio.Id });
        }

        // PUT: api/tarifario/5
        [HttpPut("{id:int}")]
        [RequierePermiso("TARIFARIO", "Modificar")]
        public async Task<IActionResult> ActualizarServicio(int id, [FromBody] ServicioDto model)
        {
            var servicio = await _db.ServiciosClinicos.FindAsync(id);
            if (servicio == null) return NotFound(new { mensaje = "Servicio no encontrado." });

            var (error, tipo) = await ValidarAsync(model, id);
            if (error != null) return BadRequest(new { mensaje = error });

            Asignar(servicio, model, tipo!);
            servicio.Activo = model.Activo;

            await _db.SaveChangesAsync();
            return Ok(new { mensaje = "Servicio actualizado. El nuevo precio aplica a los cargos que se generen desde ahora." });
        }

        // DELETE: api/tarifario/5
        [HttpDelete("{id:int}")]
        [RequierePermiso("TARIFARIO", "Eliminar")]
        public async Task<IActionResult> EliminarServicio(int id)
        {
            var servicio = await _db.ServiciosClinicos.FindAsync(id);
            if (servicio == null) return NotFound(new { mensaje = "Servicio no encontrado." });

            var enUso = await _db.CargosPaciente.AnyAsync(c => c.IdServicio == id)
                     || await _db.ConsultasMedicas.AnyAsync(c => c.IdServicio == id)
                     || await _db.Examenes.AnyAsync(e => e.IdServicio == id);

            if (enUso)
            {
                servicio.Activo = false;
                await _db.SaveChangesAsync();
                return Ok(new { mensaje = "El servicio ya se usó en cargos, por lo que se desactivó en lugar de eliminarse." });
            }

            _db.ServiciosClinicos.Remove(servicio);
            await _db.SaveChangesAsync();
            return Ok(new { mensaje = "Servicio eliminado del tarifario." });
        }

        private async Task<(string? Error, string? Tipo)> ValidarAsync(ServicioDto m, int? id)
        {
            if (string.IsNullOrWhiteSpace(m.Nombre)) return ("El nombre del servicio es obligatorio.", null);

            var tipo = TiposServicio.Todos.FirstOrDefault(t => t.Equals(m.Tipo?.Trim(), StringComparison.OrdinalIgnoreCase));
            if (tipo == null) return ($"Tipo no válido. Opciones: {string.Join(", ", TiposServicio.Todos)}.", null);

            if (m.Precio is null or < 0m) return ("El precio no puede ser negativo.", null);

            var codigo = m.Codigo?.Trim();
            if (!string.IsNullOrEmpty(codigo) && await _db.ServiciosClinicos.AnyAsync(s => s.Codigo == codigo && s.Id != id))
                return ("Ya existe un servicio con ese código.", null);

            return (null, tipo);
        }

        private static void Asignar(ServicioClinico s, ServicioDto m, string tipo)
        {
            s.Codigo = string.IsNullOrWhiteSpace(m.Codigo) ? null : m.Codigo.Trim().ToUpperInvariant();
            s.Nombre = m.Nombre!.Trim();
            s.Tipo = tipo;
            s.Descripcion = string.IsNullOrWhiteSpace(m.Descripcion) ? null : m.Descripcion.Trim();
            s.Precio = Math.Round(m.Precio!.Value, 2);
        }
    }

    public class ServicioDto
    {
        public string? Codigo { get; set; }
        public string? Nombre { get; set; }
        public string? Tipo { get; set; }
        public string? Descripcion { get; set; }
        public decimal? Precio { get; set; }
        public bool Activo { get; set; } = true;
    }
}