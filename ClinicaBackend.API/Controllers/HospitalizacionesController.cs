using ClinicaBackend.API.Data;
using ClinicaBackend.API.Extensions;
using ClinicaBackend.API.Filters;
using ClinicaBackend.API.Hubs;
using ClinicaBackend.API.Models;
using ClinicaBackend.API.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using System;
using System.Data;
using System.Threading.Tasks;

namespace ClinicaBackend.API.Controllers
{
    [Authorize]
    [ApiController]
    [Route("api/hospitalizaciones")]
    public class HospitalizacionesController : ControllerBase
    {
        private const string Modulo = "HOSPITALIZACION";
        private const string AsignacionActiva = "Activa";
        private const decimal TasaIva = 0.12m;

        private readonly ClinicaDbContext _db;
        private readonly InventarioClinicoService _inventario;
        private readonly IHubContext<HabitacionesHub> _hubHabitaciones;

        public HospitalizacionesController(ClinicaDbContext db, InventarioClinicoService inventario, IHubContext<HabitacionesHub> hubHabitaciones)
        {
            _db = db;
            _inventario = inventario;
            _hubHabitaciones = hubHabitaciones;
        }

        // ═══════════════ CONSULTAS ═══════════════

        // GET: api/hospitalizaciones?incluirCerradas=true
        [HttpGet]
        [RequierePermiso(Modulo, "Consultar")]
        public async Task<IActionResult> Listar([FromQuery] bool incluirCerradas = true)
        {
            var sucursal = await _db.ObtenerIdSucursalAsync(User);
            var desde = DateTime.Today.AddDays(-30);

            var lista = await _db.Hospitalizaciones.AsNoTracking()
                .Where(h => h.IdSucursal == sucursal &&
                            (h.Estado == EstadosHospitalizacion.Solicitada || h.Estado == EstadosHospitalizacion.Activa ||
                             (incluirCerradas && h.FechaSolicitud >= desde)))
                .OrderByDescending(h => h.FechaSolicitud)
                .Select(h => new
                {
                    h.Id,
                    h.IdPaciente,
                    Paciente = h.Paciente != null ? h.Paciente.Nombres + " " + h.Paciente.Apellidos : "Paciente",
                    h.Tipo,
                    h.Motivo,
                    h.Estado,
                    h.FechaSolicitud,
                    h.FechaIngreso,
                    h.FechaAlta,
                    Medico = h.MedicoResponsable != null ? h.MedicoResponsable.Nombres + " " + h.MedicoResponsable.Apellidos : null,
                    Habitacion = _db.AsignacionHabitaciones
                        .Where(a => a.IdHospitalizacion == h.Id)
                        .OrderByDescending(a => a.FechaIngreso)
                        .Select(a => a.Habitacion != null ? a.Habitacion.NumeroHabitacion : null)
                        .FirstOrDefault(),
                    CargosPendientes = _db.CargosPaciente
                        .Where(c => c.IdHospitalizacion == h.Id && c.Estado == EstadosCargo.Pendiente)
                        .Sum(c => (decimal?)c.Subtotal) ?? 0
                })
                .ToListAsync();

            return Ok(lista);
        }

        // GET: api/hospitalizaciones/catalogos
        [HttpGet("catalogos")]
        [RequierePermiso(Modulo, "Consultar")]
        public async Task<IActionResult> ObtenerCatalogos()
        {
            var idSucursal = await _db.ObtenerIdSucursalAsync(User);
            var hoy = DateTime.Today;

            var medicos = await _db.Empleados.AsNoTracking()
                .Where(e => e.TipoEmpleado == "Doctor")
                .OrderBy(e => e.Nombres)
                .Select(e => new { e.Id, Nombre = e.Nombres + " " + e.Apellidos, Especialidad = e.Especialidad != null ? e.Especialidad.Nombre : null })
                .ToListAsync();

            var cirugias = await _db.ServiciosClinicos.AsNoTracking()
                .Where(s => s.Activo && s.Tipo == TiposServicio.Cirugia)
                .OrderBy(s => s.Nombre)
                .Select(s => new { s.Id, s.Nombre, s.Precio })
                .ToListAsync();

            var servicios = await _db.ServiciosClinicos.AsNoTracking()
                .Where(s => s.Activo && (s.Tipo == TiposServicio.Examen || s.Tipo == TiposServicio.Procedimiento || s.Tipo == TiposServicio.Otro))
                .OrderBy(s => s.Tipo).ThenBy(s => s.Nombre)
                .Select(s => new { s.Id, s.Nombre, s.Tipo, s.Precio })
                .ToListAsync();

            var medicamentos = await _db.Medicamentos.AsNoTracking()
                .Where(m => m.Activo)
                .Select(m => new
                {
                    m.Id,
                    m.Nombre,
                    m.Marca,
                    Precio = m.PrecioVenta,
                    Existencia = _db.LotesMedicamentos
                        .Where(l => l.IdMedicamento == m.Id && l.IdSucursal == idSucursal && l.FechaVencimiento > hoy)
                        .Sum(l => (int?)l.CantidadDisponible) ?? 0
                })
                .Where(m => m.Existencia > 0)
                .OrderBy(m => m.Nombre)
                .ToListAsync();

            var habitaciones = await _db.Habitaciones.AsNoTracking()
                .Where(h => h.IdSucursal == idSucursal && h.Estado == "Disponible")
                .Select(h => new
                {
                    h.Id,
                    h.NumeroHabitacion,
                    h.TipoHabitacion,
                    h.PrecioDiario,
                    h.Capacidad,
                    Ocupantes = _db.AsignacionHabitaciones.Count(a => a.IdHabitacion == h.Id && a.FechaEgreso == null && a.Estado == AsignacionActiva)
                })
                .Where(h => h.Ocupantes < h.Capacidad)
                .OrderBy(h => h.NumeroHabitacion)
                .ToListAsync();

            return Ok(new { medicos, cirugias, servicios, medicamentos, habitaciones, tipos = TiposHospitalizacion.Todos });
        }

        // GET: api/hospitalizaciones/pacientes?buscar=juan  (pacientes sin hospitalización abierta)
        [HttpGet("pacientes")]
        [RequierePermiso(Modulo, "Consultar")]
        public async Task<IActionResult> BuscarPacientes([FromQuery] string? buscar)
        {
            var consulta = _db.Pacientes.AsNoTracking()
                .Where(p => !_db.Hospitalizaciones.Any(h => h.IdPaciente == p.Id &&
                    (h.Estado == EstadosHospitalizacion.Solicitada || h.Estado == EstadosHospitalizacion.Activa)));

            if (!string.IsNullOrWhiteSpace(buscar))
            {
                var q = buscar.Trim();
                consulta = consulta.Where(p => (p.Nombres + " " + p.Apellidos).Contains(q) ||
                    (p.DocumentoIdentificacion != null && p.DocumentoIdentificacion.Contains(q)));
            }

            var lista = await consulta.OrderBy(p => p.Nombres).Take(15)
                .Select(p => new { p.Id, NombreCompleto = p.Nombres + " " + p.Apellidos, Documento = p.DocumentoIdentificacion })
                .ToListAsync();

            return Ok(lista);
        }

        // GET: api/hospitalizaciones/5
        [HttpGet("{id:int}")]
        [RequierePermiso(Modulo, "Consultar")]
        public async Task<IActionResult> ObtenerDetalle(int id)
        {
            var hospitalizacion = await _db.Hospitalizaciones.AsNoTracking()
                .Where(h => h.Id == id)
                .Select(h => new
                {
                    h.Id,
                    h.IdPaciente,
                    Paciente = h.Paciente != null ? h.Paciente.Nombres + " " + h.Paciente.Apellidos : "Paciente",
                    Documento = h.Paciente != null ? h.Paciente.DocumentoIdentificacion : null,
                    Alergias = h.Paciente != null ? h.Paciente.Alergias : null,
                    TipoSangre = h.Paciente != null ? h.Paciente.TipoSangre : null,
                    h.Tipo,
                    h.Motivo,
                    h.Estado,
                    h.FechaSolicitud,
                    h.FechaIngreso,
                    h.FechaAlta,
                    h.ObservacionesAlta,
                    h.IdMedicoResponsable,
                    Medico = h.MedicoResponsable != null ? h.MedicoResponsable.Nombres + " " + h.MedicoResponsable.Apellidos : null,
                    Diagnostico = h.Diagnostico != null ? h.Diagnostico.Descripcion : null
                })
                .FirstOrDefaultAsync();

            if (hospitalizacion == null) return NotFound(new { mensaje = "Hospitalización no encontrada." });

            var asignacion = await _db.AsignacionHabitaciones.AsNoTracking()
                .Where(a => a.IdHospitalizacion == id)
                .OrderByDescending(a => a.FechaIngreso)
                .Select(a => new
                {
                    a.Id,
                    a.IdHabitacion,
                    Numero = a.Habitacion != null ? a.Habitacion.NumeroHabitacion : null,
                    Tipo = a.Habitacion != null ? a.Habitacion.TipoHabitacion : null,
                    PrecioDiario = a.Habitacion != null ? a.Habitacion.PrecioDiario : null,
                    a.FechaIngreso,
                    a.FechaEgreso
                })
                .FirstOrDefaultAsync();

            var cirugias = await _db.Cirugias.AsNoTracking()
                .Where(c => c.IdHospitalizacion == id)
                .OrderBy(c => c.FechaProgramada)
                .Select(c => new
                {
                    c.Id,
                    c.IdServicio,
                    c.Procedimiento,
                    c.IdCirujano,
                    Cirujano = c.Cirujano != null ? c.Cirujano.Nombres + " " + c.Cirujano.Apellidos : null,
                    c.FechaProgramada,
                    c.FechaRealizada,
                    c.Estado,
                    c.CostoBase,
                    c.CostoFinal,
                    c.Observaciones
                })
                .ToListAsync();

            var cargos = await _db.CargosPaciente.AsNoTracking()
                .Where(c => c.IdHospitalizacion == id)
                .OrderByDescending(c => c.Fecha).ThenByDescending(c => c.Id)
                .Select(c => new { c.Id, c.Tipo, c.Descripcion, c.Cantidad, c.PrecioUnitario, c.Subtotal, c.Fecha, c.Estado })
                .ToListAsync();

            // Estimación de la estancia mientras sigue internado (al alta se convierte en cargo real)
            var dias = 0;
            decimal costoHabitacion = 0;
            if (asignacion != null && hospitalizacion.Estado == EstadosHospitalizacion.Activa)
            {
                dias = CalcularDias(asignacion.FechaIngreso, DateTime.Now);
                costoHabitacion = dias * (asignacion.PrecioDiario ?? 0);
            }

            var subtotalCargos = cargos.Where(c => c.Estado != EstadosCargo.Anulado).Sum(c => c.Subtotal);
            var totalEstimado = subtotalCargos + costoHabitacion;

            return Ok(new
            {
                hospitalizacion,
                asignacion,
                cirugias,
                cargos,
                estancia = new
                {
                    dias,
                    costoHabitacion,
                    subtotalCargos,
                    totalEstimado,
                    ivaEstimado = Math.Round(totalEstimado * TasaIva, 2)
                }
            });
        }

        // ═══════════════ SOLICITUD E INGRESO ═══════════════

        // POST: api/hospitalizaciones  (referencia desde diagnóstico o ingreso directo)
        [HttpPost]
        [RequierePermiso(Modulo, "Crear")]
        public async Task<IActionResult> Solicitar([FromBody] SolicitudHospitalizacionDto model)
        {
            if (model.IdPaciente == null || !await _db.Pacientes.AnyAsync(p => p.Id == model.IdPaciente))
                return BadRequest(new { mensaje = "Paciente no válido." });

            var tipo = TiposHospitalizacion.Todos.FirstOrDefault(t => t.Equals(model.Tipo?.Trim(), StringComparison.OrdinalIgnoreCase));
            if (tipo == null)
                return BadRequest(new { mensaje = "Indique si es para Vigilancia o Cirugía." });
            if (string.IsNullOrWhiteSpace(model.Motivo))
                return BadRequest(new { mensaje = "Indique el motivo de la hospitalización." });

            if (await _db.Hospitalizaciones.AnyAsync(h => h.IdPaciente == model.IdPaciente &&
                    (h.Estado == EstadosHospitalizacion.Solicitada || h.Estado == EstadosHospitalizacion.Activa)))
                return BadRequest(new { mensaje = "El paciente ya tiene una hospitalización solicitada o activa." });

            Diagnostico? diagnostico = null;
            if (model.IdDiagnostico != null)
            {
                diagnostico = await _db.Diagnosticos.AsNoTracking().Include(d => d.Consulta)
                    .FirstOrDefaultAsync(d => d.Id == model.IdDiagnostico);
                if (diagnostico == null || diagnostico.Consulta?.IdPaciente != model.IdPaciente)
                    return BadRequest(new { mensaje = "El diagnóstico no pertenece a este paciente." });
            }

            // Médico responsable: el indicado, o el que hizo el diagnóstico, o el usuario si es doctor
            var idMedico = model.IdMedicoResponsable ?? diagnostico?.IdDoctor;
            if (idMedico == null)
            {
                var idUsuario = User.ObtenerIdUsuario();
                idMedico = await _db.Empleados.Where(e => e.IdUsuario == idUsuario && e.TipoEmpleado == "Doctor")
                    .Select(e => (int?)e.Id).FirstOrDefaultAsync();
            }
            if (idMedico == null || !await _db.Empleados.AnyAsync(e => e.Id == idMedico && e.TipoEmpleado == "Doctor"))
                return BadRequest(new { mensaje = "Seleccione el médico responsable." });

            var hospitalizacion = new Hospitalizacion
            {
                IdPaciente = model.IdPaciente.Value,
                IdSucursal = await _db.ObtenerIdSucursalAsync(User),
                IdDiagnostico = model.IdDiagnostico,
                IdMedicoResponsable = idMedico,
                Tipo = tipo,
                Motivo = model.Motivo.Trim(),
                Estado = EstadosHospitalizacion.Solicitada,
                FechaSolicitud = DateTime.Now,
                IdUsuarioRegistro = User.ObtenerIdUsuario()
            };

            _db.Hospitalizaciones.Add(hospitalizacion);
            await _db.SaveChangesAsync();

            return Ok(new { mensaje = $"Solicitud de hospitalización ({tipo}) registrada. Asigne una habitación en el módulo de Hospitalización.", id = hospitalizacion.Id });
        }

        // POST: api/hospitalizaciones/5/ingreso  (asignación de habitación)
        [HttpPost("{id:int}/ingreso")]
        [RequierePermiso(Modulo, "Modificar")]
        public async Task<IActionResult> Ingresar(int id, [FromBody] IngresoDto model)
        {
            var fechaIngreso = model.FechaIngreso ?? DateTime.Now;
            if (fechaIngreso > DateTime.Now.AddMinutes(5))
                return BadRequest(new { mensaje = "La fecha de ingreso no puede ser futura." });

            var estrategia = _db.Database.CreateExecutionStrategy();

            try
            {
                var (rechazo, mensaje) = await estrategia.ExecuteAsync<(IActionResult? Rechazo, string? Mensaje)>(async () =>
                {
                    // Serializable: dos usuarios no pueden ocupar la misma cama al mismo tiempo (regla 14d)
                    await using var transaccion = await _db.Database.BeginTransactionAsync(IsolationLevel.Serializable);

                    var hospitalizacion = await _db.Hospitalizaciones.FirstOrDefaultAsync(h => h.Id == id);
                    if (hospitalizacion == null)
                        return (NotFound(new { mensaje = "Hospitalización no encontrada." }), null);
                    if (hospitalizacion.Estado != EstadosHospitalizacion.Solicitada)
                        return (BadRequest(new { mensaje = "Solo se puede ingresar una hospitalización en estado Solicitada." }), null);

                    var habitacion = await _db.Habitaciones.FirstOrDefaultAsync(h => h.Id == model.IdHabitacion);
                    if (habitacion == null)
                        return (BadRequest(new { mensaje = "Seleccione una habitación válida." }), null);
                    if (habitacion.IdSucursal != hospitalizacion.IdSucursal)
                        return (BadRequest(new { mensaje = "La habitación pertenece a otra sucursal." }), null);
                    if (habitacion.Estado is "Limpieza" or "Mantenimiento")
                        return (BadRequest(new { mensaje = $"La habitación {habitacion.NumeroHabitacion} está en {habitacion.Estado}." }), null);

                    var ocupantes = await _db.AsignacionHabitaciones.CountAsync(a =>
                        a.IdHabitacion == habitacion.Id && a.FechaEgreso == null && a.Estado == AsignacionActiva);
                    if (ocupantes >= habitacion.Capacidad)
                        return (BadRequest(new { mensaje = $"La habitación {habitacion.NumeroHabitacion} ya está ocupada." }), null);

                    if (await _db.AsignacionHabitaciones.AnyAsync(a =>
                            a.IdPaciente == hospitalizacion.IdPaciente && a.FechaEgreso == null && a.Estado == AsignacionActiva))
                        return (BadRequest(new { mensaje = "El paciente ya ocupa otra habitación." }), null);

                    _db.AsignacionHabitaciones.Add(new AsignacionHabitacion
                    {
                        IdHabitacion = habitacion.Id,
                        IdPaciente = hospitalizacion.IdPaciente,
                        IdHospitalizacion = hospitalizacion.Id,
                        FechaIngreso = fechaIngreso,
                        Observaciones = $"{hospitalizacion.Tipo}: {hospitalizacion.Motivo}",
                        Estado = AsignacionActiva
                    });

                    habitacion.Estado = ocupantes + 1 >= habitacion.Capacidad ? "Ocupada" : "Disponible";
                    hospitalizacion.Estado = EstadosHospitalizacion.Activa;
                    hospitalizacion.FechaIngreso = fechaIngreso;

                    await _db.SaveChangesAsync();
                    await transaccion.CommitAsync();

                    return (null, $"Paciente ingresado en la habitación {habitacion.NumeroHabitacion}.");
                });

                if (rechazo != null) return rechazo;

                await NotificarHabitacionesAsync();
                return Ok(new { mensaje });
            }
            catch (Exception ex)
            {
                return Conflict(new { mensaje = "Otro usuario modificó la habitación al mismo tiempo. Actualice e intente de nuevo.", detalle = ex.Message });
            }
        }

        // POST: api/hospitalizaciones/5/cancelar  (solo solicitudes que aún no ingresan)
        [HttpPost("{id:int}/cancelar")]
        [RequierePermiso(Modulo, "Eliminar")]
        public async Task<IActionResult> Cancelar(int id)
        {
            var hospitalizacion = await _db.Hospitalizaciones.FindAsync(id);
            if (hospitalizacion == null) return NotFound(new { mensaje = "Hospitalización no encontrada." });
            if (hospitalizacion.Estado != EstadosHospitalizacion.Solicitada)
                return BadRequest(new { mensaje = "Solo se pueden cancelar solicitudes que aún no han ingresado." });

            hospitalizacion.Estado = EstadosHospitalizacion.Cancelada;
            await _db.SaveChangesAsync();
            return Ok(new { mensaje = "Solicitud de hospitalización cancelada." });
        }

        // ═══════════════ INSUMOS Y SERVICIOS ═══════════════

        // POST: api/hospitalizaciones/5/insumos  (descuenta inventario como uso clínico)
        [HttpPost("{id:int}/insumos")]
        [RequierePermiso(Modulo, "Modificar")]
        public async Task<IActionResult> RegistrarInsumo(int id, [FromBody] InsumoDto model)
        {
            var (hospitalizacion, error) = await ObtenerActivaAsync(id);
            if (error != null) return error;

            if (model.Cantidad is null or < 1 or > 500)
                return BadRequest(new { mensaje = "La cantidad debe estar entre 1 y 500." });

            var medicamento = await _db.Medicamentos.AsNoTracking().FirstOrDefaultAsync(m => m.Id == model.IdMedicamento && m.Activo);
            if (medicamento == null) return BadRequest(new { mensaje = "Seleccione un insumo o medicamento activo." });

            var idSucursal = hospitalizacion!.IdSucursal ?? await _db.ObtenerIdSucursalAsync(User);
            if (idSucursal == null) return BadRequest(new { mensaje = "No se pudo determinar la sucursal." });

            var cantidad = model.Cantidad.Value;
            var idUsuario = User.ObtenerIdUsuario();

            var cargo = new CargoPaciente
            {
                IdPaciente = hospitalizacion.IdPaciente,
                IdHospitalizacion = id,
                Tipo = TiposServicio.Insumo,
                Descripcion = string.IsNullOrWhiteSpace(medicamento.Marca) ? medicamento.Nombre : $"{medicamento.Nombre} ({medicamento.Marca})",
                Cantidad = cantidad,
                PrecioUnitario = medicamento.PrecioVenta,
                Subtotal = Math.Round(medicamento.PrecioVenta * cantidad, 2),
                Fecha = DateTime.Now,
                IdMedicamento = medicamento.Id,
                IdUsuario = idUsuario,
                Estado = EstadosCargo.Pendiente
            };

            var observacion = $"Hospitalización #{id}" + (string.IsNullOrWhiteSpace(model.Observacion) ? "" : $": {model.Observacion.Trim()}");
            var errorInventario = await _inventario.DescontarAsync(medicamento.Id, idSucursal.Value, cantidad,
                TiposMovimiento.UsoClinico, idUsuario, cargo, observacion);
            if (errorInventario != null)
                return BadRequest(new { mensaje = $"{medicamento.Nombre}: {errorInventario}" });

            _db.CargosPaciente.Add(cargo);
            await _db.SaveChangesAsync();

            return Ok(new { mensaje = $"Insumo registrado: {cantidad} × {medicamento.Nombre} (Q{cargo.Subtotal:N2})." });
        }

        // POST: api/hospitalizaciones/5/servicios  (exámenes, procedimientos u otros durante la estancia)
        [HttpPost("{id:int}/servicios")]
        [RequierePermiso(Modulo, "Modificar")]
        public async Task<IActionResult> RegistrarServicio(int id, [FromBody] ServicioHospitalarioDto model)
        {
            var (hospitalizacion, error) = await ObtenerActivaAsync(id);
            if (error != null) return error;

            if (model.Cantidad is null or < 1 or > 100)
                return BadRequest(new { mensaje = "La cantidad debe estar entre 1 y 100." });

            ServicioClinico? servicio = null;
            if (model.IdServicio != null)
            {
                servicio = await _db.ServiciosClinicos.FirstOrDefaultAsync(s => s.Id == model.IdServicio && s.Activo);
                if (servicio == null) return BadRequest(new { mensaje = "El servicio seleccionado no es válido." });
            }

            var descripcion = string.IsNullOrWhiteSpace(model.Descripcion) ? servicio?.Nombre : model.Descripcion.Trim();
            if (string.IsNullOrWhiteSpace(descripcion)) return BadRequest(new { mensaje = "Indique la descripción del servicio." });

            var precio = model.PrecioUnitario ?? servicio?.Precio;
            if (precio is null or < 0m) return BadRequest(new { mensaje = "Indique un precio válido." });

            _db.CargosPaciente.Add(new CargoPaciente
            {
                IdPaciente = hospitalizacion!.IdPaciente,
                IdHospitalizacion = id,
                Tipo = servicio?.Tipo ?? TiposServicio.Otro,
                Descripcion = descripcion,
                Cantidad = model.Cantidad.Value,
                PrecioUnitario = Math.Round(precio.Value, 2),
                Subtotal = Math.Round(precio.Value * model.Cantidad.Value, 2),
                Fecha = DateTime.Now,
                IdServicio = servicio?.Id,
                IdUsuario = User.ObtenerIdUsuario(),
                Estado = EstadosCargo.Pendiente
            });

            await _db.SaveChangesAsync();
            return Ok(new { mensaje = $"Servicio registrado: {descripcion}." });
        }

        // ═══════════════ CIRUGÍAS ═══════════════

        // POST: api/hospitalizaciones/5/cirugias  (programar)
        [HttpPost("{id:int}/cirugias")]
        [RequierePermiso(Modulo, "Modificar")]
        public async Task<IActionResult> ProgramarCirugia(int id, [FromBody] CirugiaDto model)
        {
            var (hospitalizacion, error) = await ObtenerActivaAsync(id);
            if (error != null) return error;

            var servicio = await _db.ServiciosClinicos.FirstOrDefaultAsync(s => s.Id == model.IdServicio && s.Activo && s.Tipo == TiposServicio.Cirugia);
            if (servicio == null) return BadRequest(new { mensaje = "Seleccione la cirugía del tarifario." });

            var errorCampos = await ValidarCirugiaAsync(model);
            if (errorCampos != null) return BadRequest(new { mensaje = errorCampos });

            var cirugia = new Cirugia
            {
                IdHospitalizacion = id,
                IdServicio = servicio.Id,
                Procedimiento = servicio.Nombre,
                IdCirujano = model.IdCirujano,
                FechaProgramada = model.FechaProgramada!.Value,
                Estado = EstadosCirugia.Programada,
                CostoBase = servicio.Precio,
                CostoFinal = Math.Round(model.CostoFinal ?? servicio.Precio, 2),
                Observaciones = string.IsNullOrWhiteSpace(model.Observaciones) ? null : model.Observaciones.Trim()
            };

            _db.Cirugias.Add(cirugia);
            await _db.SaveChangesAsync();

            return Ok(new { mensaje = $"Cirugía programada: {cirugia.Procedimiento} (Q{cirugia.CostoFinal:N2})." });
        }

        // PUT: api/hospitalizaciones/cirugias/7  (ajustar fecha, cirujano o costo mientras esté programada)
        [HttpPut("cirugias/{idCirugia:int}")]
        [RequierePermiso(Modulo, "Modificar")]
        public async Task<IActionResult> ActualizarCirugia(int idCirugia, [FromBody] CirugiaDto model)
        {
            var cirugia = await _db.Cirugias.FindAsync(idCirugia);
            if (cirugia == null) return NotFound(new { mensaje = "Cirugía no encontrada." });
            if (cirugia.Estado != EstadosCirugia.Programada)
                return BadRequest(new { mensaje = "Solo se pueden modificar cirugías programadas." });

            var errorCampos = await ValidarCirugiaAsync(model);
            if (errorCampos != null) return BadRequest(new { mensaje = errorCampos });

            cirugia.IdCirujano = model.IdCirujano;
            cirugia.FechaProgramada = model.FechaProgramada!.Value;
            cirugia.CostoFinal = Math.Round(model.CostoFinal ?? cirugia.CostoBase, 2);
            cirugia.Observaciones = string.IsNullOrWhiteSpace(model.Observaciones) ? null : model.Observaciones.Trim();

            await _db.SaveChangesAsync();
            return Ok(new { mensaje = "Cirugía actualizada." });
        }

        // POST: api/hospitalizaciones/cirugias/7/realizar  (genera el cargo con el costo final)
        [HttpPost("cirugias/{idCirugia:int}/realizar")]
        [RequierePermiso(Modulo, "Modificar")]
        public async Task<IActionResult> RealizarCirugia(int idCirugia, [FromBody] RealizarCirugiaDto model)
        {
            var cirugia = await _db.Cirugias.Include(c => c.Hospitalizacion).FirstOrDefaultAsync(c => c.Id == idCirugia);
            if (cirugia == null) return NotFound(new { mensaje = "Cirugía no encontrada." });
            if (cirugia.Estado != EstadosCirugia.Programada)
                return BadRequest(new { mensaje = "La cirugía no está programada." });
            if (cirugia.Hospitalizacion?.Estado != EstadosHospitalizacion.Activa)
                return BadRequest(new { mensaje = "La hospitalización no está activa." });

            var fecha = model.FechaRealizada ?? DateTime.Now;
            if (fecha > DateTime.Now.AddMinutes(5))
                return BadRequest(new { mensaje = "La fecha de realización no puede ser futura." });

            cirugia.Estado = EstadosCirugia.Realizada;
            cirugia.FechaRealizada = fecha;
            if (!string.IsNullOrWhiteSpace(model.Observaciones))
                cirugia.Observaciones = string.IsNullOrWhiteSpace(cirugia.Observaciones)
                    ? model.Observaciones.Trim()
                    : $"{cirugia.Observaciones} | {model.Observaciones.Trim()}";

            _db.CargosPaciente.Add(new CargoPaciente
            {
                IdPaciente = cirugia.Hospitalizacion.IdPaciente,
                IdHospitalizacion = cirugia.IdHospitalizacion,
                IdCirugia = cirugia.Id,
                Tipo = TiposServicio.Cirugia,
                Descripcion = $"Cirugía: {cirugia.Procedimiento}",
                Cantidad = 1,
                PrecioUnitario = cirugia.CostoFinal,
                Subtotal = cirugia.CostoFinal,
                Fecha = fecha,
                IdServicio = cirugia.IdServicio,
                IdUsuario = User.ObtenerIdUsuario(),
                Estado = EstadosCargo.Pendiente
            });

            await _db.SaveChangesAsync();
            return Ok(new { mensaje = $"Cirugía realizada. Se cargaron Q{cirugia.CostoFinal:N2} a la cuenta." });
        }

        // POST: api/hospitalizaciones/cirugias/7/cancelar
        [HttpPost("cirugias/{idCirugia:int}/cancelar")]
        [RequierePermiso(Modulo, "Modificar")]
        public async Task<IActionResult> CancelarCirugia(int idCirugia)
        {
            var cirugia = await _db.Cirugias.FindAsync(idCirugia);
            if (cirugia == null) return NotFound(new { mensaje = "Cirugía no encontrada." });
            if (cirugia.Estado != EstadosCirugia.Programada)
                return BadRequest(new { mensaje = "Solo se pueden cancelar cirugías programadas." });

            cirugia.Estado = EstadosCirugia.Cancelada;
            await _db.SaveChangesAsync();
            return Ok(new { mensaje = "Cirugía cancelada." });
        }

        // ═══════════════ ALTA ═══════════════

        // POST: api/hospitalizaciones/5/alta  (libera la habitación y carga los días de estancia)
        [HttpPost("{id:int}/alta")]
        [RequierePermiso(Modulo, "Modificar")]
        public async Task<IActionResult> DarAlta(int id, [FromBody] AltaDto model)
        {
            var hospitalizacion = await _db.Hospitalizaciones.FirstOrDefaultAsync(h => h.Id == id);
            if (hospitalizacion == null) return NotFound(new { mensaje = "Hospitalización no encontrada." });
            if (hospitalizacion.Estado != EstadosHospitalizacion.Activa)
                return BadRequest(new { mensaje = "Solo se puede dar de alta una hospitalización activa." });

            if (await _db.Cirugias.AnyAsync(c => c.IdHospitalizacion == id && c.Estado == EstadosCirugia.Programada))
                return BadRequest(new { mensaje = "Hay cirugías programadas. Márquelas como realizadas o cancélelas antes de dar el alta." });

            var fechaAlta = model.FechaAlta ?? DateTime.Now;
            if (fechaAlta > DateTime.Now.AddMinutes(5))
                return BadRequest(new { mensaje = "La fecha de alta no puede ser futura." });
            if (hospitalizacion.FechaIngreso != null && fechaAlta < hospitalizacion.FechaIngreso)
                return BadRequest(new { mensaje = "La fecha de alta no puede ser anterior al ingreso." });

            var asignacion = await _db.AsignacionHabitaciones.Include(a => a.Habitacion)
                .FirstOrDefaultAsync(a => a.IdHospitalizacion == id && a.FechaEgreso == null);

            var dias = 0;
            if (asignacion != null)
            {
                asignacion.FechaEgreso = fechaAlta;
                asignacion.Estado = "Finalizada";
                dias = CalcularDias(asignacion.FechaIngreso, fechaAlta);

                var habitacion = asignacion.Habitacion;
                if (habitacion != null)
                {
                    var restantes = await _db.AsignacionHabitaciones.CountAsync(a =>
                        a.IdHabitacion == habitacion.Id && a.Id != asignacion.Id && a.FechaEgreso == null && a.Estado == AsignacionActiva);
                    habitacion.Estado = restantes == 0 ? "Limpieza" : restantes < habitacion.Capacidad ? "Disponible" : "Ocupada";

                    var precio = habitacion.PrecioDiario ?? 0;
                    if (precio > 0)
                    {
                        _db.CargosPaciente.Add(new CargoPaciente
                        {
                            IdPaciente = hospitalizacion.IdPaciente,
                            IdHospitalizacion = id,
                            Tipo = TiposServicio.Habitacion,
                            Descripcion = $"Habitación {habitacion.NumeroHabitacion} ({habitacion.TipoHabitacion}) · {dias} día(s)",
                            Cantidad = dias,
                            PrecioUnitario = precio,
                            Subtotal = Math.Round(precio * dias, 2),
                            Fecha = fechaAlta,
                            IdUsuario = User.ObtenerIdUsuario(),
                            Estado = EstadosCargo.Pendiente
                        });
                    }
                }
            }

            hospitalizacion.Estado = EstadosHospitalizacion.Alta;
            hospitalizacion.FechaAlta = fechaAlta;
            hospitalizacion.ObservacionesAlta = string.IsNullOrWhiteSpace(model.Observaciones) ? null : model.Observaciones.Trim();

            await _db.SaveChangesAsync();
            await NotificarHabitacionesAsync();

            var pendiente = await _db.CargosPaciente
                .Where(c => c.IdHospitalizacion == id && c.Estado == EstadosCargo.Pendiente)
                .SumAsync(c => (decimal?)c.Subtotal) ?? 0;
            var total = pendiente + Math.Round(pendiente * TasaIva, 2);

            return Ok(new { mensaje = $"Alta registrada. Estancia de {dias} día(s). Total a cobrar: Q{total:N2} (IVA incluido)." });
        }

        // ═══════════════ AUXILIARES ═══════════════

        private static int CalcularDias(DateTime inicio, DateTime fin) =>
            Math.Max(1, (int)Math.Ceiling((fin - inicio).TotalHours / 24));

        private async Task<(Hospitalizacion? Hospitalizacion, IActionResult? Error)> ObtenerActivaAsync(int id)
        {
            var hospitalizacion = await _db.Hospitalizaciones.AsNoTracking().FirstOrDefaultAsync(h => h.Id == id);
            if (hospitalizacion == null) return (null, NotFound(new { mensaje = "Hospitalización no encontrada." }));
            if (hospitalizacion.Estado != EstadosHospitalizacion.Activa)
                return (null, BadRequest(new { mensaje = "La hospitalización no está activa (debe haber ingresado y no tener alta)." }));
            return (hospitalizacion, null);
        }

        private async Task<string?> ValidarCirugiaAsync(CirugiaDto m)
        {
            if (m.FechaProgramada == null) return "Indique la fecha programada.";
            if (m.IdCirujano == null || !await _db.Empleados.AnyAsync(e => e.Id == m.IdCirujano && e.TipoEmpleado == "Doctor"))
                return "Seleccione el cirujano.";
            if (m.CostoFinal is < 0m) return "El costo final no puede ser negativo.";
            return null;
        }

        // Las pantallas de Habitaciones recargan su lista en tiempo real
        private Task NotificarHabitacionesAsync() => _hubHabitaciones.Clients.All.SendAsync("HabitacionesRecargar");
    }

    // ═══════════════ DTOs ═══════════════

    public class SolicitudHospitalizacionDto
    {
        public int? IdPaciente { get; set; }
        public int? IdDiagnostico { get; set; }
        public int? IdMedicoResponsable { get; set; }
        public string? Tipo { get; set; }
        public string? Motivo { get; set; }
    }

    public class IngresoDto
    {
        public int? IdHabitacion { get; set; }
        public DateTime? FechaIngreso { get; set; }
    }

    public class InsumoDto
    {
        public int? IdMedicamento { get; set; }
        public int? Cantidad { get; set; } = 1;
        public string? Observacion { get; set; }
    }

    public class ServicioHospitalarioDto
    {
        public int? IdServicio { get; set; }
        public string? Descripcion { get; set; }
        public int? Cantidad { get; set; } = 1;
        public decimal? PrecioUnitario { get; set; }
    }

    public class CirugiaDto
    {
        public int? IdServicio { get; set; }
        public int? IdCirujano { get; set; }
        public DateTime? FechaProgramada { get; set; }
        public decimal? CostoFinal { get; set; }
        public string? Observaciones { get; set; }
    }

    public class RealizarCirugiaDto
    {
        public DateTime? FechaRealizada { get; set; }
        public string? Observaciones { get; set; }
    }

    public class AltaDto
    {
        public DateTime? FechaAlta { get; set; }
        public string? Observaciones { get; set; }
    }
}