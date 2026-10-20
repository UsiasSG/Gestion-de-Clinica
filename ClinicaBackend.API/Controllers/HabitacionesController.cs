using System.Data;
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
    public class HabitacionesController : ControllerBase
    {
        private const string Disponible = "Disponible";
        private const string Ocupada = "Ocupada";
        private const string Limpieza = "Limpieza";
        private const string Mantenimiento = "Mantenimiento";
        private const string AsignacionActiva = "Activa";
        private const string AsignacionFinalizada = "Finalizada";

        private static readonly string[] EstadosManuales = { Disponible, Limpieza, Mantenimiento };
        private static readonly string[] TiposValidos = { "Individual", "Doble", "Suite", "Cuidados Intensivos" };

        private readonly ClinicaDbContext _db;
        private readonly IHubContext<HabitacionesHub> _hub;

        public HabitacionesController(ClinicaDbContext db, IHubContext<HabitacionesHub> hub)
        {
            _db = db;
            _hub = hub;
        }

        // ═══════════════ CONSULTAS ═══════════════

        // GET: api/Habitaciones/catalogos
        [HttpGet("catalogos")]
        [RequierePermiso("HABITACIONES", "Consultar")]
        public async Task<IActionResult> ObtenerCatalogos()
        {
            var sucursales = await _db.Sucursales.AsNoTracking()
                .Where(s => s.Estado).OrderBy(s => s.Nombre)
                .Select(s => new { s.Id, s.Nombre }).ToListAsync();

            var idSucursalUsuario = await _db.ObtenerIdSucursalAsync(User);

            return Ok(new { sucursales, idSucursalUsuario, tipos = TiposValidos });
        }

        // GET: api/Habitaciones?idSucursal=1
        [HttpGet]
        [RequierePermiso("HABITACIONES", "Consultar")]
        public async Task<IActionResult> ObtenerHabitaciones([FromQuery] int? idSucursal)
        {
            var sucursal = idSucursal ?? await _db.ObtenerIdSucursalAsync(User);
            var lista = await Proyectar(_db.Habitaciones.AsNoTracking().Where(h => h.IdSucursal == sucursal)).ToListAsync();
            return Ok(lista);
        }

        // GET: api/Habitaciones/5/historial
        [HttpGet("{id:int}/historial")]
        [RequierePermiso("HABITACIONES", "Consultar")]
        public async Task<IActionResult> ObtenerHistorial(int id)
        {
            var lista = await _db.AsignacionHabitaciones.AsNoTracking()
                .Where(a => a.IdHabitacion == id)
                .OrderByDescending(a => a.FechaIngreso)
                .Take(50)
                .Select(a => new
                {
                    a.Id,
                    PacienteNombre = a.Paciente != null ? a.Paciente.Nombres + " " + a.Paciente.Apellidos : "Paciente",
                    a.FechaIngreso,
                    a.FechaEgreso,
                    a.Estado,
                    a.Observaciones
                })
                .ToListAsync();

            return Ok(lista);
        }

        // GET: api/Habitaciones/pacientes-disponibles?buscar=juan
        [HttpGet("pacientes-disponibles")]
        [RequierePermiso("HABITACIONES", "Consultar")]
        public async Task<IActionResult> ObtenerPacientesDisponibles([FromQuery] string? buscar)
        {
            var consulta = _db.Pacientes.AsNoTracking()
                .Where(p => !_db.AsignacionHabitaciones.Any(a =>
                    a.IdPaciente == p.Id && a.FechaEgreso == null && a.Estado == AsignacionActiva));

            if (!string.IsNullOrWhiteSpace(buscar))
            {
                var q = buscar.Trim();
                consulta = consulta.Where(p =>
                    (p.Nombres + " " + p.Apellidos).Contains(q) ||
                    (p.DocumentoIdentificacion != null && p.DocumentoIdentificacion.Contains(q)));
            }

            var lista = await consulta
                .OrderBy(p => p.Nombres)
                .Take(15)
                .Select(p => new
                {
                    p.Id,
                    NombreCompleto = p.Nombres + " " + p.Apellidos,
                    Documento = p.DocumentoIdentificacion
                })
                .ToListAsync();

            return Ok(lista);
        }

        // ═══════════════ CRUD DE HABITACIONES ═══════════════

        // POST: api/Habitaciones
        [HttpPost]
        [RequierePermiso("HABITACIONES", "Crear")]
        public async Task<IActionResult> CrearHabitacion([FromBody] HabitacionRequestDto model)
        {
            var (error, tipo) = await ValidarHabitacionAsync(model, null);
            if (error != null) return BadRequest(new { mensaje = error });

            var habitacion = new Habitacion
            {
                NumeroHabitacion = model.NumeroHabitacion!.Trim(),
                TipoHabitacion = tipo,
                Capacidad = model.Capacidad!.Value,
                PrecioDiario = Math.Round(model.PrecioDiario!.Value, 2),
                IdSucursal = model.IdSucursal,
                Estado = Disponible
            };

            _db.Habitaciones.Add(habitacion);
            await _db.SaveChangesAsync();

            var dto = await NotificarAsync(habitacion.Id);
            return Ok(new { mensaje = $"Habitación {habitacion.NumeroHabitacion} registrada correctamente.", habitacion = dto });
        }

        // PUT: api/Habitaciones/5
        [HttpPut("{id:int}")]
        [RequierePermiso("HABITACIONES", "Modificar")]
        public async Task<IActionResult> ActualizarHabitacion(int id, [FromBody] HabitacionRequestDto model)
        {
            var habitacion = await _db.Habitaciones.FindAsync(id);
            if (habitacion == null) return NotFound(new { mensaje = "Habitación no encontrada." });

            var (error, tipo) = await ValidarHabitacionAsync(model, id);
            if (error != null) return BadRequest(new { mensaje = error });

            var ocupantes = await ContarOcupantesAsync(id);
            if (model.Capacidad < ocupantes)
                return BadRequest(new { mensaje = $"La habitación tiene {ocupantes} paciente(s); la capacidad no puede ser menor." });
            if (ocupantes > 0 && model.IdSucursal != habitacion.IdSucursal)
                return BadRequest(new { mensaje = "No se puede cambiar de sucursal una habitación con pacientes internados." });

            habitacion.NumeroHabitacion = model.NumeroHabitacion!.Trim();
            habitacion.TipoHabitacion = tipo;
            habitacion.Capacidad = model.Capacidad!.Value;
            habitacion.PrecioDiario = Math.Round(model.PrecioDiario!.Value, 2);
            habitacion.IdSucursal = model.IdSucursal;

            // Si cambió la capacidad, se recalcula si queda llena o con cupo
            if (ocupantes > 0)
                habitacion.Estado = ocupantes >= habitacion.Capacidad ? Ocupada : Disponible;

            await _db.SaveChangesAsync();

            var dto = await NotificarAsync(id);
            return Ok(new { mensaje = "Habitación actualizada correctamente.", habitacion = dto });
        }

        // DELETE: api/Habitaciones/5
        [HttpDelete("{id:int}")]
        [RequierePermiso("HABITACIONES", "Eliminar")]
        public async Task<IActionResult> EliminarHabitacion(int id)
        {
            var habitacion = await _db.Habitaciones.FindAsync(id);
            if (habitacion == null) return NotFound(new { mensaje = "Habitación no encontrada." });

            if (await _db.AsignacionHabitaciones.AnyAsync(a => a.IdHabitacion == id))
                return BadRequest(new { mensaje = "La habitación tiene historial de pacientes y no puede eliminarse. Puede marcarla en Mantenimiento." });

            _db.Habitaciones.Remove(habitacion);
            await _db.SaveChangesAsync();

            await _hub.Clients.All.SendAsync("HabitacionEliminada", id);
            return Ok(new { mensaje = $"Habitación {habitacion.NumeroHabitacion} eliminada." });
        }

        // ═══════════════ ESTADO, INGRESO Y EGRESO ═══════════════

        // PUT: api/Habitaciones/5/estado  (solo Disponible, Limpieza o Mantenimiento y sin pacientes)
        [HttpPut("{id:int}/estado")]
        [RequierePermiso("HABITACIONES", "Modificar")]
        public async Task<IActionResult> CambiarEstado(int id, [FromBody] CambioEstadoDto request)
        {
            var habitacion = await _db.Habitaciones.FindAsync(id);
            if (habitacion == null) return NotFound(new { mensaje = "Habitación no encontrada." });

            var estado = EstadosManuales.FirstOrDefault(e => e.Equals(request.Estado?.Trim(), StringComparison.OrdinalIgnoreCase));
            if (estado == null)
                return BadRequest(new { mensaje = "Estado no válido. El estado Ocupada se asigna automáticamente al ingresar un paciente." });

            if (await ContarOcupantesAsync(id) > 0)
                return BadRequest(new { mensaje = "La habitación tiene pacientes internados. Registre el egreso antes de cambiar su estado." });

            habitacion.Estado = estado;
            await _db.SaveChangesAsync();

            var dto = await NotificarAsync(id);
            return Ok(new { mensaje = $"Habitación {habitacion.NumeroHabitacion} marcada como {estado}.", habitacion = dto });
        }

        // POST: api/Habitaciones/asignar
        [HttpPost("asignar")]
        [RequierePermiso("HABITACIONES", "Modificar")]
        public async Task<IActionResult> AsignarPaciente([FromBody] AsignacionRequestDto dto)
        {
            if (dto.IdPaciente == null || !await _db.Pacientes.AnyAsync(p => p.Id == dto.IdPaciente))
                return BadRequest(new { mensaje = "Seleccione un paciente válido." });

            var fechaIngreso = dto.FechaIngreso ?? DateTime.Now;
            if (fechaIngreso > DateTime.Now.AddMinutes(5))
                return BadRequest(new { mensaje = "La fecha de ingreso no puede ser futura." });

            var estrategia = _db.Database.CreateExecutionStrategy();

            try
            {
                var (rechazo, mensaje) = await estrategia.ExecuteAsync<(IActionResult? Rechazo, string? Mensaje)>(async () =>
                {
                    // Serializable: impide que dos usuarios asignen la misma cama al mismo tiempo (regla 14d)
                    await using var transaccion = await _db.Database.BeginTransactionAsync(IsolationLevel.Serializable);

                    var habitacion = await _db.Habitaciones.FirstOrDefaultAsync(h => h.Id == dto.IdHabitacion);
                    if (habitacion == null)
                        return (NotFound(new { mensaje = "Habitación no encontrada." }), null);

                    if (habitacion.Estado is Limpieza or Mantenimiento)
                        return (BadRequest(new { mensaje = $"La habitación {habitacion.NumeroHabitacion} está en {habitacion.Estado} y no puede recibir pacientes." }), null);

                    var ocupantes = await ContarOcupantesAsync(habitacion.Id);
                    if (ocupantes >= habitacion.Capacidad)
                        return (BadRequest(new { mensaje = $"La habitación {habitacion.NumeroHabitacion} ya está ocupada (capacidad {habitacion.Capacidad})." }), null);

                    var habitacionActual = await _db.AsignacionHabitaciones
                        .Where(a => a.IdPaciente == dto.IdPaciente && a.FechaEgreso == null && a.Estado == AsignacionActiva)
                        .Select(a => a.Habitacion != null ? a.Habitacion.NumeroHabitacion : "?")
                        .FirstOrDefaultAsync();
                    if (habitacionActual != null)
                        return (BadRequest(new { mensaje = $"El paciente ya está internado en la habitación {habitacionActual}. Registre su egreso primero." }), null);

                    _db.AsignacionHabitaciones.Add(new AsignacionHabitacion
                    {
                        IdHabitacion = habitacion.Id,
                        IdPaciente = dto.IdPaciente,
                        FechaIngreso = fechaIngreso,
                        Observaciones = string.IsNullOrWhiteSpace(dto.Observaciones) ? null : dto.Observaciones.Trim(),
                        Estado = AsignacionActiva
                    });

                    habitacion.Estado = ocupantes + 1 >= habitacion.Capacidad ? Ocupada : Disponible;

                    await _db.SaveChangesAsync();
                    await transaccion.CommitAsync();

                    return (null, $"Paciente asignado a la habitación {habitacion.NumeroHabitacion}.");
                });

                if (rechazo != null) return rechazo;

                var habitacionDto = await NotificarAsync(dto.IdHabitacion);
                return Ok(new { mensaje, habitacion = habitacionDto });
            }
            catch (Exception ex)
            {
                return Conflict(new { mensaje = "Otro usuario modificó la habitación al mismo tiempo. Actualice e intente de nuevo.", detalle = ex.Message });
            }
        }

        // POST: api/Habitaciones/egreso/12
        [HttpPost("egreso/{idAsignacion:int}")]
        [RequierePermiso("HABITACIONES", "Modificar")]
        public async Task<IActionResult> RegistrarEgreso(int idAsignacion, [FromBody] EgresoDto? dto)
        {
            var asignacion = await _db.AsignacionHabitaciones
                .Include(a => a.Habitacion)
                .FirstOrDefaultAsync(a => a.Id == idAsignacion);

            if (asignacion == null) return NotFound(new { mensaje = "Registro de asignación no encontrado." });
            if (asignacion.FechaEgreso != null) return BadRequest(new { mensaje = "Este paciente ya tiene registrado su egreso." });

            var fechaEgreso = dto?.FechaEgreso ?? DateTime.Now;
            if (fechaEgreso < asignacion.FechaIngreso)
                return BadRequest(new { mensaje = "La fecha de egreso no puede ser anterior a la de ingreso." });
            if (fechaEgreso > DateTime.Now.AddMinutes(5))
                return BadRequest(new { mensaje = "La fecha de egreso no puede ser futura." });

            asignacion.FechaEgreso = fechaEgreso;
            asignacion.Estado = AsignacionFinalizada;

            var observacion = dto?.Observaciones?.Trim();
            if (!string.IsNullOrEmpty(observacion))
            {
                asignacion.Observaciones = string.IsNullOrWhiteSpace(asignacion.Observaciones)
                    ? $"Egreso: {observacion}"
                    : $"{asignacion.Observaciones} | Egreso: {observacion}";
            }

            var mensaje = "Egreso registrado correctamente.";
            var habitacion = asignacion.Habitacion;

            if (habitacion != null)
            {
                var restantes = await _db.AsignacionHabitaciones.CountAsync(a =>
                    a.IdHabitacion == habitacion.Id && a.Id != idAsignacion && a.FechaEgreso == null && a.Estado == AsignacionActiva);

                // Vacía → Limpieza; con pacientes pero con cupo → Disponible
                habitacion.Estado = restantes == 0 ? Limpieza
                                  : restantes < habitacion.Capacidad ? Disponible
                                  : Ocupada;

                if (restantes == 0) mensaje = $"Egreso registrado. La habitación {habitacion.NumeroHabitacion} pasó a Limpieza.";
            }

            await _db.SaveChangesAsync();

            var habitacionDto = habitacion != null ? await NotificarAsync(habitacion.Id) : null;
            return Ok(new { mensaje, habitacion = habitacionDto });
        }

        // ═══════════════ AUXILIARES ═══════════════

        private IQueryable<HabitacionDto> Proyectar(IQueryable<Habitacion> consulta) =>
            consulta.Select(h => new HabitacionDto
            {
                Id = h.Id,
                NumeroHabitacion = h.NumeroHabitacion,
                TipoHabitacion = h.TipoHabitacion,
                Capacidad = h.Capacidad,
                Estado = h.Estado ?? Disponible,
                PrecioDiario = h.PrecioDiario,
                IdSucursal = h.IdSucursal,
                SucursalNombre = h.Sucursal != null ? h.Sucursal.Nombre : null,
                Ocupantes = _db.AsignacionHabitaciones
                    .Where(a => a.IdHabitacion == h.Id && a.FechaEgreso == null && a.Estado == AsignacionActiva)
                    .OrderBy(a => a.FechaIngreso)
                    .Select(a => new OcupanteDto
                    {
                        IdAsignacion = a.Id,
                        IdPaciente = a.IdPaciente,
                        PacienteNombre = a.Paciente != null ? a.Paciente.Nombres + " " + a.Paciente.Apellidos : "Paciente",
                        FechaIngreso = a.FechaIngreso,
                        Observaciones = a.Observaciones
                    })
                    .ToList()
            });

        // Emite el estado completo y actualizado de la habitación a todas las pantallas conectadas
        private async Task<HabitacionDto?> NotificarAsync(int idHabitacion)
        {
            var dto = await Proyectar(_db.Habitaciones.AsNoTracking().Where(h => h.Id == idHabitacion)).FirstOrDefaultAsync();
            if (dto != null) await _hub.Clients.All.SendAsync("HabitacionActualizada", dto);
            return dto;
        }

        private Task<int> ContarOcupantesAsync(int idHabitacion) =>
            _db.AsignacionHabitaciones.CountAsync(a =>
                a.IdHabitacion == idHabitacion && a.FechaEgreso == null && a.Estado == AsignacionActiva);

        private async Task<(string? Error, string? Tipo)> ValidarHabitacionAsync(HabitacionRequestDto m, int? id)
        {
            var numero = m.NumeroHabitacion?.Trim();
            if (string.IsNullOrWhiteSpace(numero))
                return ("El número de habitación es obligatorio.", null);

            var tipo = TiposValidos.FirstOrDefault(t => t.Equals(m.TipoHabitacion?.Trim(), StringComparison.OrdinalIgnoreCase));
            if (tipo == null)
                return ($"Tipo de habitación no válido. Opciones: {string.Join(", ", TiposValidos)}.", null);

            if (m.Capacidad is null or < 1 or > 6)
                return ("La capacidad debe estar entre 1 y 6 pacientes.", null);
            if (m.PrecioDiario is null or <= 0m)
                return ("El precio diario debe ser mayor a 0.", null);
            if (m.IdSucursal == null || !await _db.Sucursales.AnyAsync(s => s.Id == m.IdSucursal))
                return ("Seleccione una sucursal válida.", null);

            if (await _db.Habitaciones.AnyAsync(h => h.Id != id && h.IdSucursal == m.IdSucursal && h.NumeroHabitacion == numero))
                return ($"Ya existe la habitación {numero} en esa sucursal.", null);

            return (null, tipo);
        }
    }

    // ═══════════════ DTOs ═══════════════

    public class HabitacionDto
    {
        public int Id { get; set; }
        public string NumeroHabitacion { get; set; } = string.Empty;
        public string? TipoHabitacion { get; set; }
        public int Capacidad { get; set; }
        public string Estado { get; set; } = string.Empty;
        public decimal? PrecioDiario { get; set; }
        public int? IdSucursal { get; set; }
        public string? SucursalNombre { get; set; }
        public List<OcupanteDto> Ocupantes { get; set; } = new();
    }

    public class OcupanteDto
    {
        public int IdAsignacion { get; set; }
        public int? IdPaciente { get; set; }
        public string PacienteNombre { get; set; } = string.Empty;
        public DateTime FechaIngreso { get; set; }
        public string? Observaciones { get; set; }
    }

    public class HabitacionRequestDto
    {
        public string? NumeroHabitacion { get; set; }
        public string? TipoHabitacion { get; set; }
        public int? Capacidad { get; set; }
        public decimal? PrecioDiario { get; set; }
        public int? IdSucursal { get; set; }
    }

    public class CambioEstadoDto
    {
        public string? Estado { get; set; }
    }

    public class AsignacionRequestDto
    {
        public int IdHabitacion { get; set; }
        public int? IdPaciente { get; set; }
        public DateTime? FechaIngreso { get; set; }
        public string? Observaciones { get; set; }
    }

    public class EgresoDto
    {
        public DateTime? FechaEgreso { get; set; }
        public string? Observaciones { get; set; }
    }
}