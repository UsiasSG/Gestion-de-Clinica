using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using ClinicaBackend.API.Data;
using ClinicaBackend.API.Extensions;
using ClinicaBackend.API.Models;
using ClinicaBackend.API.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;

namespace ClinicaBackend.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AuthController : ControllerBase
    {
        private readonly ClinicaDbContext _db;
        private readonly IConfiguration _config;
        private readonly PermisosService _permisos;

        public AuthController(ClinicaDbContext db, IConfiguration config, PermisosService permisos)
        {
            _db = db;
            _config = config;
            _permisos = permisos;
        }

        // POST: api/Auth/login
        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] LoginDto request)
        {
            if (string.IsNullOrWhiteSpace(request?.NombreUsuario) || string.IsNullOrWhiteSpace(request.Password))
                return BadRequest(new { mensaje = "Ingrese usuario y contraseña." });

            var usuario = await _db.Usuarios
                .Include(u => u.Sucursal)
                .Include(u => u.Rol)
                .FirstOrDefaultAsync(u => u.NombreUsuario == request.NombreUsuario.Trim());

            if (usuario == null || !await VerificarPasswordAsync(usuario, request.Password))
                return Unauthorized(new { mensaje = "Usuario o contraseña incorrectos." });

            if (!usuario.Estado)
                return Unauthorized(new { mensaje = "El usuario se encuentra inactivo." });

            if (usuario.IdRol == null)
                return Unauthorized(new { mensaje = "El usuario no tiene un rol asignado. Contacte al administrador." });

            var empleado = await _db.Empleados.AsNoTracking().FirstOrDefaultAsync(e => e.IdUsuario == usuario.Id);
            var nombreMostrar = empleado != null ? $"{empleado.Nombres} {empleado.Apellidos}".Trim() : usuario.NombreUsuario;
            var nombreSucursal = usuario.Sucursal?.Nombre;

            var claims = new List<Claim>
            {
                new(ClaimTypes.NameIdentifier, usuario.Id.ToString()),
                new(ClaimTypes.Name, nombreMostrar),
                new("NombreUsuario", usuario.NombreUsuario),
                new("RolId", usuario.IdRol.Value.ToString()),
                new("RolNombre", usuario.Rol?.Nombre ?? string.Empty)
            };

            // Ya no se asigna la sucursal 1 por defecto: si no tiene, no se incluye
            if (usuario.IdSucursal != null)
            {
                claims.Add(new("SucursalId", usuario.IdSucursal.Value.ToString()));
                claims.Add(new("NombreSucursal", nombreSucursal ?? string.Empty));
            }

            var secretKey = _config["Jwt:Key"] ?? "ClaveSecretaSuperSeguraParaJWTDeMiClinica2026!";
            var credenciales = new SigningCredentials(new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secretKey)), SecurityAlgorithms.HmacSha256);

            var token = new JwtSecurityToken(
                issuer: _config["Jwt:Issuer"] ?? "ClinicaBackendAPI",
                audience: _config["Jwt:Audience"] ?? "ClinicaFrontendClient",
                claims: claims,
                expires: DateTime.UtcNow.AddHours(8),
                signingCredentials: credenciales);

            return Ok(new
            {
                token = new JwtSecurityTokenHandler().WriteToken(token),
                usuario = new
                {
                    id = usuario.Id,
                    nombreUsuario = nombreMostrar,
                    idSucursal = usuario.IdSucursal,
                    nombreSucursal = nombreSucursal ?? "Sin sucursal asignada",
                    rolId = usuario.IdRol,
                    rolNombre = usuario.Rol?.Nombre
                }
            });
        }

        // GET: api/Auth/permisos  (para el menú dinámico)
        [Authorize]
        [HttpGet("permisos")]
        public async Task<IActionResult> ObtenerMisPermisos()
        {
            var idUsuario = User.ObtenerIdUsuario();
            if (idUsuario == null) return Unauthorized(new { mensaje = "Sesión no válida." });

            return Ok(await _permisos.ObtenerPermisosUsuarioAsync(idUsuario.Value));
        }

        private async Task<bool> VerificarPasswordAsync(Usuario usuario, string password)
        {
            var hash = usuario.PasswordHash ?? string.Empty;

            if (hash.StartsWith("$2"))
            {
                try { return BCrypt.Net.BCrypt.Verify(password, hash); }
                catch { return false; }
            }

            // Contraseña heredada en texto plano (ej. el admin de la semilla):
            // se acepta una única vez y se convierte a BCrypt de inmediato
            if (hash.Length > 0 && hash == password)
            {
                usuario.PasswordHash = BCrypt.Net.BCrypt.HashPassword(password);
                await _db.SaveChangesAsync();
                return true;
            }

            return false;
        }
    }

    public class LoginDto
    {
        public string NombreUsuario { get; set; } = null!;
        public string Password { get; set; } = null!;
    }
}