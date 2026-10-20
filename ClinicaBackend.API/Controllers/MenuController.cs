using ClinicaBackend.API.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ClinicaBackend.API.Filters;

namespace ClinicaBackend.API.Controllers
{
    [Authorize]
    [ApiController]
    [Route("api/[controller]")]
    public class MenuController : ControllerBase
    {
        private readonly ClinicaDbContext _db;

        public MenuController(ClinicaDbContext db) => _db = db;

        [HttpGet("mis-permisos")]
        public async Task<IActionResult> ObtenerMisPermisos()
        {
            var rolClaim = User.FindFirst("RolId")?.Value;
            if (!int.TryParse(rolClaim, out int rolId)) return BadRequest();

            var permisos = await _db.Permisos
                .Include(p => p.Modulo)
                .Where(p => p.IdRol == rolId && p.PuedeConsultar)
                .Select(p => new
                {
                    Modulo = p.Modulo!.Nombre,
                    Codigo = p.Modulo.Codigo,
                    p.PuedeConsultar,
                    p.PuedeCrear,
                    p.PuedeModificar,
                    p.PuedeEliminar
                })
                .ToListAsync();

            return Ok(permisos);
        }
    }
}
