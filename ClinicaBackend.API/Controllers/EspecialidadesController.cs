using ClinicaBackend.API.Data;
using ClinicaBackend.API.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Threading.Tasks;
using ClinicaBackend.API.Filters;

namespace ClinicaBackend.API.Controllers
{
    [Authorize]
    [ApiController]
    [Route("api/[controller]")]
    public class EspecialidadesController : ControllerBase
    {
        private readonly ClinicaDbContext _db;

        public EspecialidadesController(ClinicaDbContext db)
        {
            _db = db;
        }

        [HttpGet]
        public async Task<IActionResult> ObtenerTodas()
        {
            var especialidades = await _db.Especialidades.ToListAsync();
            return Ok(especialidades);
        }

        [HttpPost]
        public async Task<IActionResult> CrearEspecialidad([FromBody] Especialidad model)
        {
            if (string.IsNullOrWhiteSpace(model.Nombre))
                return BadRequest("El nombre de la especialidad es requerido.");

            var existe = await _db.Especialidades
                .FirstOrDefaultAsync(e => e.Nombre.ToLower() == model.Nombre.Trim().ToLower());

            if (existe != null)
                return Ok(existe);

            _db.Especialidades.Add(model);
            await _db.SaveChangesAsync();

            return Ok(model);
        }
    }
}