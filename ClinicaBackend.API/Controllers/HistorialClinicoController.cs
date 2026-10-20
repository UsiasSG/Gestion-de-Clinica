using ClinicaBackend.API.Data;
using ClinicaBackend.API.Extensions;
using ClinicaBackend.API.Filters;
using ClinicaBackend.API.Models;
using ClinicaBackend.API.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace ClinicaBackend.API.Controllers
{
    [Authorize]
    [ApiController]
    [Route("api/historial")]
    public class HistorialClinicoController : ControllerBase
    {
        private static readonly string[] EstadosSalud = { "Estable", "Mejorando", "Delicado", "Crítico", "Recuperado" };
        private static readonly string[] EstadosTratamiento = { "Activo", "Finalizado", "Suspendido" };

        private const long TamanoMaximoArchivo = 5 * 1024 * 1024; // 5 MB

        private static readonly Dictionary<string, string> TiposPermitidos = new(StringComparer.OrdinalIgnoreCase)
        {
            [".pdf"] = "application/pdf",
            [".jpg"] = "image/jpeg",
            [".jpeg"] = "image/jpeg",
            [".png"] = "image/png"
        };

        private static readonly string[] CategoriasArchivo =
            { "Resultado de examen", "Imagen diagnóstica", "Receta", "Consentimiento", "Referencia médica", "Otro" };

        private readonly ClinicaDbContext _db;
        private readonly PermisosService _permisos;

        public HistorialClinicoController(ClinicaDbContext db, PermisosService permisos)
        {
            _db = db;
            _permisos = permisos;
        }

        // ═══════════════ CONSULTA DEL EXPEDIENTE ═══════════════

        // GET: api/historial/paciente/5
        [HttpGet("paciente/{idPaciente:int}")]
        [RequierePermiso("HISTORIAL", "Consultar")]
        public async Task<IActionResult> ObtenerHistorial(int idPaciente)
        {
            var paciente = await _db.Pacientes.AsNoTracking()
                .Where(p => p.Id == idPaciente)
                .Select(p => new
                {
                    p.Id,
                    NombreCompleto = p.Nombres + " " + p.Apellidos,
                    p.DocumentoIdentificacion,
                    p.FechaNacimiento,
                    p.Genero,
                    p.TipoSangre,
                    p.Alergias,
                    p.Telefono,
                    p.ContactoEmergencia
                })
                .FirstOrDefaultAsync();

            if (paciente == null) return NotFound(new { mensaje = "Paciente no encontrado." });

            var consultas = await _db.ConsultasMedicas.AsNoTracking()
                .Where(c => c.IdPaciente == idPaciente)
                .OrderByDescending(c => c.FechaConsulta)
                .Select(c => new
                {
                    c.Id,
                    c.FechaConsulta,
                    Doctor = c.Doctor != null ? c.Doctor.Nombres + " " + c.Doctor.Apellidos : "No asignado",
                    Especialidad = c.Doctor != null && c.Doctor.Especialidad != null ? c.Doctor.Especialidad.Nombre : null,
                    Sucursal = c.Sucursal != null ? c.Sucursal.Nombre : null,
                    c.MotivoConsulta,
                    c.Sintomatologia,
                    c.Observaciones,
                    Diagnosticos = _db.Diagnosticos
                        .Where(d => d.IdConsulta == c.Id)
                        .OrderBy(d => d.Id)
                        .Select(d => new { d.Id, d.CodigoCie, d.Descripcion })
                        .ToList(),
                    Tratamientos = _db.Tratamientos
                        .Where(t => t.IdConsulta == c.Id)
                        .OrderBy(t => t.Id)
                        .Select(t => new { t.Id, t.Descripcion, t.Indicaciones, t.FechaInicio, t.FechaFin })
                        .ToList()
                })
                .ToListAsync();

            var diagnosticos = await _db.Diagnosticos.AsNoTracking()
                .Where(d => d.Consulta != null && d.Consulta.IdPaciente == idPaciente)
                .OrderByDescending(d => d.FechaDiagnostico)
                .Select(d => new
                {
                    d.Id,
                    d.IdConsulta,
                    d.CodigoCie,
                    d.Descripcion,
                    d.FechaDiagnostico,
                    Doctor = d.Doctor != null ? d.Doctor.Nombres + " " + d.Doctor.Apellidos : "No asignado",
                    MotivoConsulta = d.Consulta!.MotivoConsulta
                })
                .ToListAsync();

            var tratamientos = await _db.Tratamientos.AsNoTracking()
                .Where(t => t.IdPaciente == idPaciente
                         || (t.IdPaciente == null && t.Consulta != null && t.Consulta.IdPaciente == idPaciente))
                .OrderByDescending(t => t.FechaRegistro)
                .Select(t => new
                {
                    t.Id,
                    t.IdConsulta,
                    t.IdDiagnostico,
                    t.Descripcion,
                    t.Indicaciones,
                    t.FechaInicio,
                    t.FechaFin,
                    Estado = t.Estado ?? "Activo",
                    t.FechaRegistro,
                    Doctor = t.Doctor != null
                        ? t.Doctor.Nombres + " " + t.Doctor.Apellidos
                        : (t.Consulta != null && t.Consulta.Doctor != null
                            ? t.Consulta.Doctor.Nombres + " " + t.Consulta.Doctor.Apellidos
                            : "No asignado"),
                    Diagnostico = t.Diagnostico != null ? t.Diagnostico.Descripcion : null,
                    MotivoConsulta = t.Consulta != null ? t.Consulta.MotivoConsulta : null
                })
                .ToListAsync();

            var examenes = await _db.Examenes.AsNoTracking()
                .Where(e => e.IdPaciente == idPaciente)
                .OrderByDescending(e => e.FechaExamen)
                .Select(e => new
                {
                    e.Id,
                    e.IdConsulta,
                    e.NombreExamen,
                    e.Resultado,
                    e.FechaExamen,
                    e.Observaciones,
                    Estado = e.Resultado == null || e.Resultado == "" ? "Pendiente" : "Completado",
                    Adjuntos = _db.ArchivosClinicos.Count(a => a.IdExamen == e.Id)
                })
                .ToListAsync();

            var evolucion = await _db.EvolucionesPaciente.AsNoTracking()
                .Where(e => e.IdPaciente == idPaciente)
                .OrderByDescending(e => e.FechaRegistro)
                .Select(e => new
                {
                    e.Id,
                    e.FechaRegistro,
                    e.NotaEvolucion,
                    e.EstadoSalud,
                    Responsable = e.Doctor != null ? e.Doctor.Nombres + " " + e.Doctor.Apellidos : "No asignado",
                    TipoResponsable = e.Doctor != null ? e.Doctor.TipoEmpleado : null
                })
                .ToListAsync();

            // Solo los datos del archivo, sin el contenido, para que la respuesta sea liviana
            var archivos = await _db.ArchivosClinicos.AsNoTracking()
                .Where(a => a.IdPaciente == idPaciente)
                .OrderByDescending(a => a.FechaSubida)
                .Select(a => new
                {
                    a.Id,
                    a.IdExamen,
                    Examen = a.Examen != null ? a.Examen.NombreExamen : null,
                    a.NombreArchivo,
                    a.TipoContenido,
                    a.TamanoBytes,
                    a.Categoria,
                    a.Descripcion,
                    a.FechaSubida,
                    SubidoPor = a.Usuario != null ? a.Usuario.NombreUsuario : null
                })
                .ToListAsync();

            // Listas para elegir quién atendió cuando el usuario no es personal clínico (ej. administrador)
            var medicos = await _db.Empleados.AsNoTracking()
                .Where(e => e.TipoEmpleado == "Doctor")
                .OrderBy(e => e.Nombres)
                .Select(e => new
                {
                    e.Id,
                    Nombre = e.Nombres + " " + e.Apellidos,
                    Especialidad = e.Especialidad != null ? e.Especialidad.Nombre : null
                })
                .ToListAsync();

            var personalClinico = await _db.Empleados.AsNoTracking()
                .Where(e => e.TipoEmpleado == "Doctor" || e.TipoEmpleado == "Enfermera")
                .OrderBy(e => e.Nombres)
                .Select(e => new { e.Id, Nombre = e.Nombres + " " + e.Apellidos, Tipo = e.TipoEmpleado })
                .ToListAsync();
            var serviciosConsulta = await _db.ServiciosClinicos.AsNoTracking()
                .Where(s => s.Activo && s.Tipo == TiposServicio.Consulta)
                .OrderBy(s => s.Precio)
                .Select(s => new { s.Id, s.Nombre, s.Precio })
                .ToListAsync();

            var serviciosExamen = await _db.ServiciosClinicos.AsNoTracking()
                .Where(s => s.Activo && s.Tipo == TiposServicio.Examen)
                .OrderBy(s => s.Nombre)
                .Select(s => new { s.Id, s.Nombre, s.Precio })
                .ToListAsync();

            var permisos = await PermisosDelUsuarioAsync();

            return Ok(new { paciente, consultas, diagnosticos, tratamientos, examenes, evolucion, archivos, medicos, personalClinico, serviciosConsulta, serviciosExamen, permisos });
        }

        // ═══════════════ CONSULTAS ═══════════════

        // POST: api/historial/consultas
        [HttpPost("consultas")]
        [RequierePermiso("HISTORIAL", "Crear")]
        public async Task<IActionResult> RegistrarConsulta([FromBody] ConsultaRequestDto model)
        {
            if (model.IdPaciente == null || !await _db.Pacientes.AnyAsync(p => p.Id == model.IdPaciente))
                return BadRequest(new { mensaje = "Paciente no válido." });
            if (string.IsNullOrWhiteSpace(model.MotivoConsulta))
                return BadRequest(new { mensaje = "El motivo de la consulta es obligatorio." });

            var (doctor, errorDoctor) = await ResolverResponsableAsync(model.IdDoctor, soloDoctores: true);
            if (doctor == null) return BadRequest(new { mensaje = errorDoctor });

            // Tipo de consulta del tarifario (genera el cobro)
            ServicioClinico? servicio = null;
            if (model.IdServicio != null)
            {
                servicio = await _db.ServiciosClinicos.FirstOrDefaultAsync(s => s.Id == model.IdServicio && s.Activo && s.Tipo == TiposServicio.Consulta);
                if (servicio == null) return BadRequest(new { mensaje = "El tipo de consulta seleccionado no es válido." });
            }
            else if (await _db.ServiciosClinicos.AnyAsync(s => s.Activo && s.Tipo == TiposServicio.Consulta))
            {
                return BadRequest(new { mensaje = "Seleccione el tipo de consulta para registrar su cobro." });
            }

            var diagnosticos = (model.Diagnosticos ?? new()).Where(d => !string.IsNullOrWhiteSpace(d.Descripcion)).ToList();
            var tratamientos = (model.Tratamientos ?? new()).Where(t => !string.IsNullOrWhiteSpace(t.Descripcion)).ToList();

            // Regla 14f: los diagnósticos requieren su propio permiso
            if (diagnosticos.Count > 0 && !await _permisos.TienePermisoAsync(IdUsuarioActual(), "DIAGNOSTICOS", "Crear"))
                return StatusCode(403, new { mensaje = "Su rol no tiene permiso para registrar diagnósticos. Guarde la consulta sin diagnósticos." });

            if (tratamientos.Any(t => t.FechaInicio != null && t.FechaFin != null && t.FechaFin < t.FechaInicio))
                return BadRequest(new { mensaje = "En los tratamientos, la fecha de fin no puede ser anterior a la de inicio." });

            var consulta = new ConsultaMedica
            {
                IdPaciente = model.IdPaciente,
                IdDoctor = doctor.Id,
                IdSucursal = await _db.ObtenerIdSucursalAsync(User) ?? doctor.IdSucursal,
                IdServicio = servicio?.Id,
                FechaConsulta = DateTime.Now,
                MotivoConsulta = model.MotivoConsulta.Trim(),
                Sintomatologia = Limpiar(model.Sintomatologia),
                Observaciones = Limpiar(model.Observaciones)
            };
            _db.ConsultasMedicas.Add(consulta);

            foreach (var d in diagnosticos)
            {
                _db.Diagnosticos.Add(new Diagnostico
                {
                    Consulta = consulta,
                    IdDoctor = doctor.Id,
                    CodigoCie = Limpiar(d.CodigoCie)?.ToUpperInvariant(),
                    Descripcion = d.Descripcion!.Trim(),
                    FechaDiagnostico = DateTime.Now
                });
            }

            foreach (var t in tratamientos)
            {
                _db.Tratamientos.Add(new Tratamiento
                {
                    Consulta = consulta,
                    IdPaciente = model.IdPaciente,
                    IdDoctor = doctor.Id,
                    Descripcion = t.Descripcion!.Trim(),
                    Indicaciones = Limpiar(t.Indicaciones),
                    FechaInicio = t.FechaInicio?.Date,
                    FechaFin = t.FechaFin?.Date,
                    Estado = "Activo",
                    FechaRegistro = DateTime.Now
                });
            }

            if (servicio != null)
            {
                _db.CargosPaciente.Add(new CargoPaciente
                {
                    IdPaciente = model.IdPaciente.Value,
                    Tipo = TiposServicio.Consulta,
                    Descripcion = servicio.Nombre,
                    Cantidad = 1,
                    PrecioUnitario = servicio.Precio,
                    Subtotal = servicio.Precio,
                    Fecha = DateTime.Now,
                    IdServicio = servicio.Id,
                    Consulta = consulta,
                    IdUsuario = User.ObtenerIdUsuario(),
                    Estado = EstadosCargo.Pendiente
                });
            }

            // Un solo SaveChanges: consulta, diagnósticos, tratamientos y cargo se guardan juntos o no se guarda nada
            await _db.SaveChangesAsync();

            var mensaje = servicio != null
                ? $"Consulta registrada. Se generó un cargo de Q{servicio.Precio:N2} (más IVA) en la cuenta del paciente."
                : "Consulta registrada correctamente.";
            return Ok(new { mensaje, id = consulta.Id });
        }

        // PUT: api/historial/consultas/5
        [HttpPut("consultas/{id:int}")]
        [RequierePermiso("HISTORIAL", "Modificar")]
        public async Task<IActionResult> ActualizarConsulta(int id, [FromBody] ConsultaUpdateDto model)
        {
            var consulta = await _db.ConsultasMedicas.FindAsync(id);
            if (consulta == null) return NotFound(new { mensaje = "Consulta no encontrada." });
            if (string.IsNullOrWhiteSpace(model.MotivoConsulta))
                return BadRequest(new { mensaje = "El motivo de la consulta es obligatorio." });

            consulta.MotivoConsulta = model.MotivoConsulta.Trim();
            consulta.Sintomatologia = Limpiar(model.Sintomatologia);
            consulta.Observaciones = Limpiar(model.Observaciones);

            await _db.SaveChangesAsync();
            return Ok(new { mensaje = "Consulta actualizada correctamente." });
        }

        // DELETE: api/historial/consultas/5
        [HttpDelete("consultas/{id:int}")]
        [RequierePermiso("HISTORIAL", "Eliminar")]
        public async Task<IActionResult> EliminarConsulta(int id)
        {
            var consulta = await _db.ConsultasMedicas.FindAsync(id);
            if (consulta == null) return NotFound(new { mensaje = "Consulta no encontrada." });

            var diagnosticos = await _db.Diagnosticos.Where(d => d.IdConsulta == id).ToListAsync();
            if (diagnosticos.Count > 0 && !await _permisos.TienePermisoAsync(IdUsuarioActual(), "DIAGNOSTICOS", "Eliminar"))
                return StatusCode(403, new { mensaje = "La consulta tiene diagnósticos y su rol no tiene permiso para eliminarlos." });

            

            var idsDiagnosticos = diagnosticos.Select(d => d.Id).ToList();

            // Los tratamientos se conservan: solo se desvinculan de la consulta y sus diagnósticos
            var tratamientos = await _db.Tratamientos
                .Where(t => t.IdConsulta == id || (t.IdDiagnostico != null && idsDiagnosticos.Contains(t.IdDiagnostico.Value)))
                .ToListAsync();
            foreach (var t in tratamientos)
            {
                if (t.IdConsulta == id)
                {
                    t.IdPaciente ??= consulta.IdPaciente;
                    t.IdDoctor ??= consulta.IdDoctor;
                    t.IdConsulta = null;
                }
                if (t.IdDiagnostico != null && idsDiagnosticos.Contains(t.IdDiagnostico.Value))
                    t.IdDiagnostico = null;
            }

            // Los exámenes también se conservan
            var examenes = await _db.Examenes.Where(e => e.IdConsulta == id).ToListAsync();
            foreach (var e in examenes) e.IdConsulta = null;

            // Cobros: si ya se facturó, no se puede eliminar; si está pendiente, el cargo se anula
            var cargosConsulta = await _db.CargosPaciente.Where(c => c.IdConsulta == id).ToListAsync();
            if (cargosConsulta.Any(c => c.Estado == EstadosCargo.Facturado))
                return BadRequest(new { mensaje = "La consulta ya fue cobrada. Anule primero la factura en la pestaña Cuenta." });
            foreach (var c in cargosConsulta)
            {
                if (c.Estado == EstadosCargo.Pendiente) c.Estado = EstadosCargo.Anulado;
                c.IdConsulta = null;
            }


            _db.Diagnosticos.RemoveRange(diagnosticos);
            _db.ConsultasMedicas.Remove(consulta);
            await _db.SaveChangesAsync();

            return Ok(new { mensaje = "Consulta eliminada. Sus tratamientos y exámenes se conservaron en el historial." });
        }

        // ═══════════════ TRATAMIENTOS ═══════════════

        // POST: api/historial/tratamientos
        [HttpPost("tratamientos")]
        [RequierePermiso("HISTORIAL", "Crear")]
        public async Task<IActionResult> RegistrarTratamiento([FromBody] TratamientoRequestDto model)
        {
            var error = await ValidarTratamientoAsync(model);
            if (error != null) return BadRequest(new { mensaje = error });

            var (doctor, errorDoctor) = await ResolverResponsableAsync(model.IdDoctor, soloDoctores: true);
            if (doctor == null) return BadRequest(new { mensaje = errorDoctor });

            var tratamiento = new Tratamiento
            {
                IdPaciente = model.IdPaciente,
                IdConsulta = model.IdConsulta,
                IdDiagnostico = model.IdDiagnostico,
                IdDoctor = doctor.Id,
                Descripcion = model.Descripcion!.Trim(),
                Indicaciones = Limpiar(model.Indicaciones),
                FechaInicio = model.FechaInicio?.Date,
                FechaFin = model.FechaFin?.Date,
                Estado = NormalizarEstadoTratamiento(model.Estado),
                FechaRegistro = DateTime.Now
            };

            _db.Tratamientos.Add(tratamiento);
            await _db.SaveChangesAsync();
            return Ok(new { mensaje = "Tratamiento registrado correctamente.", id = tratamiento.Id });
        }

        // PUT: api/historial/tratamientos/5
        [HttpPut("tratamientos/{id:int}")]
        [RequierePermiso("HISTORIAL", "Modificar")]
        public async Task<IActionResult> ActualizarTratamiento(int id, [FromBody] TratamientoRequestDto model)
        {
            var tratamiento = await _db.Tratamientos.Include(t => t.Consulta).FirstOrDefaultAsync(t => t.Id == id);
            if (tratamiento == null) return NotFound(new { mensaje = "Tratamiento no encontrado." });

            model.IdPaciente = tratamiento.IdPaciente ?? tratamiento.Consulta?.IdPaciente;
            var error = await ValidarTratamientoAsync(model);
            if (error != null) return BadRequest(new { mensaje = error });

            tratamiento.IdPaciente = model.IdPaciente;
            tratamiento.IdConsulta = model.IdConsulta;
            tratamiento.IdDiagnostico = model.IdDiagnostico;
            tratamiento.Descripcion = model.Descripcion!.Trim();
            tratamiento.Indicaciones = Limpiar(model.Indicaciones);
            tratamiento.FechaInicio = model.FechaInicio?.Date;
            tratamiento.FechaFin = model.FechaFin?.Date;
            tratamiento.Estado = NormalizarEstadoTratamiento(model.Estado);

            await _db.SaveChangesAsync();
            return Ok(new { mensaje = "Tratamiento actualizado correctamente." });
        }

        // DELETE: api/historial/tratamientos/5
        [HttpDelete("tratamientos/{id:int}")]
        [RequierePermiso("HISTORIAL", "Eliminar")]
        public async Task<IActionResult> EliminarTratamiento(int id)
        {
            var tratamiento = await _db.Tratamientos.FindAsync(id);
            if (tratamiento == null) return NotFound(new { mensaje = "Tratamiento no encontrado." });

            _db.Tratamientos.Remove(tratamiento);
            await _db.SaveChangesAsync();
            return Ok(new { mensaje = "Tratamiento eliminado correctamente." });
        }

        // ═══════════════ DIAGNÓSTICOS (regla 14f) ═══════════════

        // POST: api/historial/diagnosticos
        [HttpPost("diagnosticos")]
        [RequierePermiso("DIAGNOSTICOS", "Crear")]
        public async Task<IActionResult> RegistrarDiagnostico([FromBody] DiagnosticoRequestDto model)
        {
            if (model.IdConsulta == null || !await _db.ConsultasMedicas.AnyAsync(c => c.Id == model.IdConsulta))
                return BadRequest(new { mensaje = "Seleccione la consulta a la que pertenece el diagnóstico." });
            if (string.IsNullOrWhiteSpace(model.Descripcion))
                return BadRequest(new { mensaje = "La descripción del diagnóstico es obligatoria." });

            var (doctor, errorDoctor) = await ResolverResponsableAsync(model.IdDoctor, soloDoctores: true);
            if (doctor == null) return BadRequest(new { mensaje = errorDoctor });

            _db.Diagnosticos.Add(new Diagnostico
            {
                IdConsulta = model.IdConsulta,
                IdDoctor = doctor.Id,
                CodigoCie = Limpiar(model.CodigoCie)?.ToUpperInvariant(),
                Descripcion = model.Descripcion.Trim(),
                FechaDiagnostico = DateTime.Now
            });

            await _db.SaveChangesAsync();
            return Ok(new { mensaje = "Diagnóstico registrado correctamente." });
        }

        // PUT: api/historial/diagnosticos/5
        [HttpPut("diagnosticos/{id:int}")]
        [RequierePermiso("DIAGNOSTICOS", "Modificar")]
        public async Task<IActionResult> ActualizarDiagnostico(int id, [FromBody] DiagnosticoItemDto model)
        {
            var diagnostico = await _db.Diagnosticos.FindAsync(id);
            if (diagnostico == null) return NotFound(new { mensaje = "Diagnóstico no encontrado." });
            if (string.IsNullOrWhiteSpace(model.Descripcion))
                return BadRequest(new { mensaje = "La descripción del diagnóstico es obligatoria." });

            diagnostico.CodigoCie = Limpiar(model.CodigoCie)?.ToUpperInvariant();
            diagnostico.Descripcion = model.Descripcion.Trim();

            await _db.SaveChangesAsync();
            return Ok(new { mensaje = "Diagnóstico actualizado correctamente." });
        }

        // DELETE: api/historial/diagnosticos/5
        [HttpDelete("diagnosticos/{id:int}")]
        [RequierePermiso("DIAGNOSTICOS", "Eliminar")]
        public async Task<IActionResult> EliminarDiagnostico(int id)
        {
            var diagnostico = await _db.Diagnosticos.FindAsync(id);
            if (diagnostico == null) return NotFound(new { mensaje = "Diagnóstico no encontrado." });

            // Los tratamientos basados en este diagnóstico se conservan, solo se desvinculan
            var tratamientos = await _db.Tratamientos.Where(t => t.IdDiagnostico == id).ToListAsync();
            foreach (var t in tratamientos) t.IdDiagnostico = null;

            _db.Diagnosticos.Remove(diagnostico);
            await _db.SaveChangesAsync();
            return Ok(new { mensaje = "Diagnóstico eliminado correctamente." });
        }

        // ═══════════════ EXÁMENES ═══════════════

        // POST: api/historial/examenes  (el resultado puede quedar pendiente)
        [HttpPost("examenes")]
        [RequierePermiso("HISTORIAL", "Crear")]
        public async Task<IActionResult> RegistrarExamen([FromBody] ExamenRequestDto model)
        {
            if (model.IdPaciente == null || !await _db.Pacientes.AnyAsync(p => p.Id == model.IdPaciente))
                return BadRequest(new { mensaje = "Paciente no válido." });

            ServicioClinico? servicio = null;
            if (model.IdServicio != null)
            {
                servicio = await _db.ServiciosClinicos.FirstOrDefaultAsync(s => s.Id == model.IdServicio && s.Activo && s.Tipo == TiposServicio.Examen);
                if (servicio == null) return BadRequest(new { mensaje = "El examen seleccionado del tarifario no es válido." });
                if (string.IsNullOrWhiteSpace(model.NombreExamen)) model.NombreExamen = servicio.Nombre;
            }

            var error = await ValidarExamenAsync(model, model.IdPaciente.Value);
            if (error != null) return BadRequest(new { mensaje = error });

            var examen = new Examen
            {
                IdPaciente = model.IdPaciente,
                IdConsulta = model.IdConsulta,
                IdServicio = servicio?.Id,
                NombreExamen = model.NombreExamen!.Trim(),
                Resultado = Limpiar(model.Resultado),
                FechaExamen = model.FechaExamen ?? DateTime.Now,
                Observaciones = Limpiar(model.Observaciones)
            };
            _db.Examenes.Add(examen);

            if (servicio != null)
            {
                _db.CargosPaciente.Add(new CargoPaciente
                {
                    IdPaciente = model.IdPaciente.Value,
                    Tipo = TiposServicio.Examen,
                    Descripcion = servicio.Nombre,
                    Cantidad = 1,
                    PrecioUnitario = servicio.Precio,
                    Subtotal = servicio.Precio,
                    Fecha = DateTime.Now,
                    IdServicio = servicio.Id,
                    Examen = examen,
                    IdUsuario = User.ObtenerIdUsuario(),
                    Estado = EstadosCargo.Pendiente
                });
            }

            await _db.SaveChangesAsync();

            var mensaje = servicio != null
                ? $"Examen registrado. Se generó un cargo de Q{servicio.Precio:N2} (más IVA) en la cuenta del paciente."
                : "Examen registrado correctamente.";
            return Ok(new { mensaje });
        }

        // PUT: api/historial/examenes/5
        [HttpPut("examenes/{id:int}")]
        [RequierePermiso("HISTORIAL", "Modificar")]
        public async Task<IActionResult> ActualizarExamen(int id, [FromBody] ExamenRequestDto model)
        {
            var examen = await _db.Examenes.FindAsync(id);
            if (examen == null) return NotFound(new { mensaje = "Examen no encontrado." });

            var error = await ValidarExamenAsync(model, examen.IdPaciente ?? 0);
            if (error != null) return BadRequest(new { mensaje = error });

            examen.IdConsulta = model.IdConsulta;
            examen.NombreExamen = model.NombreExamen!.Trim();
            examen.Resultado = Limpiar(model.Resultado);
            examen.FechaExamen = model.FechaExamen ?? examen.FechaExamen;
            examen.Observaciones = Limpiar(model.Observaciones);

            await _db.SaveChangesAsync();
            return Ok(new { mensaje = "Examen actualizado correctamente." });
        }

        // PUT: api/historial/examenes/5/resultado  (acceso rápido para registrar solo el resultado)
        [HttpPut("examenes/{id:int}/resultado")]
        [RequierePermiso("HISTORIAL", "Modificar")]
        public async Task<IActionResult> RegistrarResultado(int id, [FromBody] ResultadoExamenDto model)
        {
            var examen = await _db.Examenes.FindAsync(id);
            if (examen == null) return NotFound(new { mensaje = "Examen no encontrado." });
            if (string.IsNullOrWhiteSpace(model.Resultado))
                return BadRequest(new { mensaje = "Ingrese el resultado del examen." });

            examen.Resultado = model.Resultado.Trim();
            if (!string.IsNullOrWhiteSpace(model.Observaciones)) examen.Observaciones = model.Observaciones.Trim();

            await _db.SaveChangesAsync();
            return Ok(new { mensaje = "Resultado registrado correctamente." });
        }

        // DELETE: api/historial/examenes/5
        [HttpDelete("examenes/{id:int}")]
        [RequierePermiso("HISTORIAL", "Eliminar")]
        public async Task<IActionResult> EliminarExamen(int id)
        {
            var examen = await _db.Examenes.FindAsync(id);
            if (examen == null) return NotFound(new { mensaje = "Examen no encontrado." });

            // Los archivos adjuntos se conservan en el historial, solo se desvinculan del examen
            var archivos = await _db.ArchivosClinicos.Where(a => a.IdExamen == id).ToListAsync();
            foreach (var a in archivos) a.IdExamen = null;

            var cargosExamen = await _db.CargosPaciente.Where(c => c.IdExamen == id).ToListAsync();
            if (cargosExamen.Any(c => c.Estado == EstadosCargo.Facturado))
                return BadRequest(new { mensaje = "El examen ya fue cobrado. Anule primero la factura en la pestaña Cuenta." });
            foreach (var c in cargosExamen)
            {
                if (c.Estado == EstadosCargo.Pendiente) c.Estado = EstadosCargo.Anulado;
                c.IdExamen = null;
            }

            _db.Examenes.Remove(examen);
            await _db.SaveChangesAsync();
            return Ok(new
            {
                mensaje = archivos.Count > 0
                ? "Examen eliminado. Sus archivos adjuntos se conservaron en la pestaña Archivos."
                : "Examen eliminado correctamente."
            });
        }

        // ═══════════════ EVOLUCIÓN ═══════════════

        // POST: api/historial/evolucion
        [HttpPost("evolucion")]
        [RequierePermiso("HISTORIAL", "Crear")]
        public async Task<IActionResult> RegistrarEvolucion([FromBody] EvolucionRequestDto model)
        {
            if (model.IdPaciente == null || !await _db.Pacientes.AnyAsync(p => p.Id == model.IdPaciente))
                return BadRequest(new { mensaje = "Paciente no válido." });

            var (estado, errorEstado) = ValidarEvolucion(model);
            if (errorEstado != null) return BadRequest(new { mensaje = errorEstado });

            var (responsable, errorResponsable) = await ResolverResponsableAsync(model.IdResponsable, soloDoctores: false);
            if (responsable == null) return BadRequest(new { mensaje = errorResponsable });

            _db.EvolucionesPaciente.Add(new EvolucionPaciente
            {
                IdPaciente = model.IdPaciente,
                IdDoctor = responsable.Id,
                FechaRegistro = DateTime.Now,
                NotaEvolucion = model.NotaEvolucion!.Trim(),
                EstadoSalud = estado
            });

            await _db.SaveChangesAsync();
            return Ok(new { mensaje = "Nota de evolución registrada correctamente." });
        }

        // PUT: api/historial/evolucion/5
        [HttpPut("evolucion/{id:int}")]
        [RequierePermiso("HISTORIAL", "Modificar")]
        public async Task<IActionResult> ActualizarEvolucion(int id, [FromBody] EvolucionRequestDto model)
        {
            var nota = await _db.EvolucionesPaciente.FindAsync(id);
            if (nota == null) return NotFound(new { mensaje = "Nota de evolución no encontrada." });

            var (estado, errorEstado) = ValidarEvolucion(model);
            if (errorEstado != null) return BadRequest(new { mensaje = errorEstado });

            nota.NotaEvolucion = model.NotaEvolucion!.Trim();
            nota.EstadoSalud = estado;

            await _db.SaveChangesAsync();
            return Ok(new { mensaje = "Nota de evolución actualizada correctamente." });
        }

        // DELETE: api/historial/evolucion/5
        [HttpDelete("evolucion/{id:int}")]
        [RequierePermiso("HISTORIAL", "Eliminar")]
        public async Task<IActionResult> EliminarEvolucion(int id)
        {
            var nota = await _db.EvolucionesPaciente.FindAsync(id);
            if (nota == null) return NotFound(new { mensaje = "Nota de evolución no encontrada." });

            _db.EvolucionesPaciente.Remove(nota);
            await _db.SaveChangesAsync();
            return Ok(new { mensaje = "Nota de evolución eliminada correctamente." });
        }

        // ═══════════════ ARCHIVOS ADJUNTOS ═══════════════

        // POST: api/historial/archivos  (multipart/form-data)
        [HttpPost("archivos")]
        [RequierePermiso("HISTORIAL", "Crear")]
        [RequestSizeLimit(6 * 1024 * 1024)]
        public async Task<IActionResult> SubirArchivo([FromForm] SubirArchivoDto model)
        {
            if (model.Archivo == null || model.Archivo.Length == 0)
                return BadRequest(new { mensaje = "Seleccione un archivo." });
            if (model.Archivo.Length > TamanoMaximoArchivo)
                return BadRequest(new { mensaje = "El archivo supera el tamaño máximo de 5 MB." });

            var extension = Path.GetExtension(model.Archivo.FileName);
            if (!TiposPermitidos.TryGetValue(extension, out var tipoContenido))
                return BadRequest(new { mensaje = "Solo se permiten archivos PDF, JPG o PNG." });

            if (model.IdPaciente == null || !await _db.Pacientes.AnyAsync(p => p.Id == model.IdPaciente))
                return BadRequest(new { mensaje = "Paciente no válido." });
            if (model.IdExamen != null && !await _db.Examenes.AnyAsync(e => e.Id == model.IdExamen && e.IdPaciente == model.IdPaciente))
                return BadRequest(new { mensaje = "El examen seleccionado no pertenece a este paciente." });

            var categoria = CategoriasArchivo.FirstOrDefault(c => c.Equals(model.Categoria?.Trim(), StringComparison.OrdinalIgnoreCase));
            if (categoria == null)
                return BadRequest(new { mensaje = "Categoría de archivo no válida." });

            byte[] contenido;
            await using (var memoria = new MemoryStream())
            {
                await model.Archivo.CopyToAsync(memoria);
                contenido = memoria.ToArray();
            }

            // Verifica que el contenido real coincida con la extensión (evita archivos renombrados)
            if (!FirmaValida(contenido, extension))
                return BadRequest(new { mensaje = "El contenido del archivo no corresponde a su extensión." });

            var nombre = Path.GetFileName(model.Archivo.FileName);
            if (nombre.Length > 200) nombre = nombre[^200..];

            var archivo = new ArchivoClinico
            {
                IdPaciente = model.IdPaciente.Value,
                IdExamen = model.IdExamen,
                NombreArchivo = nombre,
                TipoContenido = tipoContenido,
                TamanoBytes = contenido.LongLength,
                Contenido = contenido,
                Categoria = categoria,
                Descripcion = Limpiar(model.Descripcion),
                FechaSubida = DateTime.Now,
                IdUsuario = User.ObtenerIdUsuario()
            };

            _db.ArchivosClinicos.Add(archivo);
            await _db.SaveChangesAsync();

            return Ok(new { mensaje = $"Archivo \"{nombre}\" subido correctamente.", id = archivo.Id });
        }

        // GET: api/historial/archivos/5  (descarga el contenido)
        [HttpGet("archivos/{id:int}")]
        [RequierePermiso("HISTORIAL", "Consultar")]
        public async Task<IActionResult> DescargarArchivo(int id)
        {
            var archivo = await _db.ArchivosClinicos.AsNoTracking().FirstOrDefaultAsync(a => a.Id == id);
            if (archivo == null) return NotFound(new { mensaje = "Archivo no encontrado." });

            return File(archivo.Contenido, archivo.TipoContenido, archivo.NombreArchivo);
        }

        // DELETE: api/historial/archivos/5
        [HttpDelete("archivos/{id:int}")]
        [RequierePermiso("HISTORIAL", "Eliminar")]
        public async Task<IActionResult> EliminarArchivo(int id)
        {
            var archivo = await _db.ArchivosClinicos.FindAsync(id);
            if (archivo == null) return NotFound(new { mensaje = "Archivo no encontrado." });

            _db.ArchivosClinicos.Remove(archivo);
            await _db.SaveChangesAsync();
            return Ok(new { mensaje = "Archivo eliminado correctamente." });
        }

        // ═══════════════ AUXILIARES ═══════════════

        private int IdUsuarioActual() => User.ObtenerIdUsuario() ?? 0;

        private static bool FirmaValida(byte[] b, string extension) => extension.ToLowerInvariant() switch
        {
            ".pdf" => b.Length > 4 && b[0] == 0x25 && b[1] == 0x50 && b[2] == 0x44 && b[3] == 0x46,  // %PDF
            ".png" => b.Length > 8 && b[0] == 0x89 && b[1] == 0x50 && b[2] == 0x4E && b[3] == 0x47,  // ‰PNG
            ".jpg" or ".jpeg" => b.Length > 3 && b[0] == 0xFF && b[1] == 0xD8 && b[2] == 0xFF,       // JPEG
            _ => false
        };

        private async Task<string?> ValidarTratamientoAsync(TratamientoRequestDto m)
        {
            if (m.IdPaciente == null || !await _db.Pacientes.AnyAsync(p => p.Id == m.IdPaciente))
                return "Paciente no válido.";
            if (string.IsNullOrWhiteSpace(m.Descripcion))
                return "La descripción del tratamiento es obligatoria.";
            if (m.FechaInicio != null && m.FechaFin != null && m.FechaFin < m.FechaInicio)
                return "La fecha de fin no puede ser anterior a la de inicio.";
            if (!string.IsNullOrWhiteSpace(m.Estado) && !EstadosTratamiento.Contains(m.Estado.Trim(), StringComparer.OrdinalIgnoreCase))
                return "Estado de tratamiento no válido.";
            if (m.IdConsulta != null && !await _db.ConsultasMedicas.AnyAsync(c => c.Id == m.IdConsulta && c.IdPaciente == m.IdPaciente))
                return "La consulta seleccionada no pertenece a este paciente.";
            if (m.IdDiagnostico != null && !await _db.Diagnosticos.AnyAsync(d => d.Id == m.IdDiagnostico && d.Consulta != null && d.Consulta.IdPaciente == m.IdPaciente))
                return "El diagnóstico seleccionado no pertenece a este paciente.";
            return null;
        }

        private static string NormalizarEstadoTratamiento(string? estado) =>
            EstadosTratamiento.FirstOrDefault(e => e.Equals(estado?.Trim(), StringComparison.OrdinalIgnoreCase)) ?? "Activo";

        private async Task<string?> ValidarExamenAsync(ExamenRequestDto m, int idPaciente)
        {
            if (string.IsNullOrWhiteSpace(m.NombreExamen))
                return "El nombre del examen es obligatorio.";
            if (m.FechaExamen != null && m.FechaExamen > DateTime.Now.AddMinutes(5))
                return "La fecha del examen no puede ser futura.";
            if (m.IdConsulta != null && !await _db.ConsultasMedicas.AnyAsync(c => c.Id == m.IdConsulta && c.IdPaciente == idPaciente))
                return "La consulta seleccionada no pertenece a este paciente.";
            return null;
        }

        private static (string? Estado, string? Error) ValidarEvolucion(EvolucionRequestDto m)
        {
            if (string.IsNullOrWhiteSpace(m.NotaEvolucion))
                return (null, "La nota de evolución es obligatoria.");
            if (string.IsNullOrWhiteSpace(m.EstadoSalud))
                return (null, null);

            var estado = EstadosSalud.FirstOrDefault(e => e.Equals(m.EstadoSalud.Trim(), StringComparison.OrdinalIgnoreCase));
            return estado == null ? (null, "Estado de salud no válido.") : (estado, null);
        }

        private async Task<Empleado?> EmpleadoActualAsync()
        {
            var idUsuario = User.ObtenerIdUsuario();
            if (idUsuario == null) return null;
            return await _db.Empleados.AsNoTracking().FirstOrDefaultAsync(e => e.IdUsuario == idUsuario);
        }

        // Si el usuario es personal clínico, firma él mismo; si no (ej. administrador), debe indicar quién atendió
        private async Task<(Empleado? Empleado, string? Error)> ResolverResponsableAsync(int? idSolicitado, bool soloDoctores)
        {
            bool EsValido(Empleado e) => soloDoctores
                ? e.TipoEmpleado == "Doctor"
                : e.TipoEmpleado == "Doctor" || e.TipoEmpleado == "Enfermera";

            var actual = await EmpleadoActualAsync();
            if (actual != null && EsValido(actual)) return (actual, null);

            var etiqueta = soloDoctores ? "el médico tratante" : "el responsable (Doctor o Enfermera)";
            if (idSolicitado == null) return (null, $"Seleccione {etiqueta}.");

            var elegido = await _db.Empleados.AsNoTracking().FirstOrDefaultAsync(e => e.Id == idSolicitado);
            if (elegido == null || !EsValido(elegido)) return (null, $"Seleccione {etiqueta} válido.");

            return (elegido, null);
        }

        // Permisos del usuario para que la pantalla muestre u oculte los botones
        private async Task<object> PermisosDelUsuarioAsync()
        {
            var idUsuario = IdUsuarioActual();
            var empleado = await EmpleadoActualAsync();
            var tipo = empleado?.TipoEmpleado;

            return new
            {
                Crear = await _permisos.TienePermisoAsync(idUsuario, "HISTORIAL", "Crear"),
                Modificar = await _permisos.TienePermisoAsync(idUsuario, "HISTORIAL", "Modificar"),
                Eliminar = await _permisos.TienePermisoAsync(idUsuario, "HISTORIAL", "Eliminar"),
                Diagnosticar = await _permisos.TienePermisoAsync(idUsuario, "DIAGNOSTICOS", "Crear"),
                ModificarDiagnostico = await _permisos.TienePermisoAsync(idUsuario, "DIAGNOSTICOS", "Modificar"),
                EliminarDiagnostico = await _permisos.TienePermisoAsync(idUsuario, "DIAGNOSTICOS", "Eliminar"),
                VerCuenta = await _permisos.TienePermisoAsync(idUsuario, "COBROS", "Consultar"),
                EsDoctor = tipo == "Doctor",
                EsPersonalClinico = tipo == "Doctor" || tipo == "Enfermera",
                Empleado = empleado != null ? $"{empleado.Nombres} {empleado.Apellidos} ({empleado.TipoEmpleado})" : null,
                Referir = await _permisos.TienePermisoAsync(idUsuario, "HOSPITALIZACION", "Crear"),
            };
        }

        private static string? Limpiar(string? valor) => string.IsNullOrWhiteSpace(valor) ? null : valor.Trim();
    }

    // ═══════════════ DTOs ═══════════════

    public class ConsultaRequestDto
    {
        public int? IdPaciente { get; set; }
        public int? IdDoctor { get; set; }
        public int? IdServicio { get; set; }
        public string? MotivoConsulta { get; set; }
        public string? Sintomatologia { get; set; }
        public string? Observaciones { get; set; }
        public List<DiagnosticoItemDto>? Diagnosticos { get; set; }
        public List<TratamientoItemDto>? Tratamientos { get; set; }
    }

    public class ConsultaUpdateDto
    {
        public string? MotivoConsulta { get; set; }
        public string? Sintomatologia { get; set; }
        public string? Observaciones { get; set; }
    }

    public class DiagnosticoItemDto
    {
        public string? CodigoCie { get; set; }
        public string? Descripcion { get; set; }
    }

    public class DiagnosticoRequestDto : DiagnosticoItemDto
    {
        public int? IdConsulta { get; set; }
        public int? IdDoctor { get; set; }
    }

    public class TratamientoItemDto
    {
        public string? Descripcion { get; set; }
        public string? Indicaciones { get; set; }
        public DateTime? FechaInicio { get; set; }
        public DateTime? FechaFin { get; set; }
    }

    public class TratamientoRequestDto : TratamientoItemDto
    {
        public int? IdPaciente { get; set; }
        public int? IdConsulta { get; set; }
        public int? IdDiagnostico { get; set; }
        public int? IdDoctor { get; set; }
        public string? Estado { get; set; }
    }

    public class ExamenRequestDto
    {
        public int? IdPaciente { get; set; }
        public int? IdConsulta { get; set; }
        public int? IdServicio { get; set; }
        public string? NombreExamen { get; set; }
        public DateTime? FechaExamen { get; set; }
        public string? Resultado { get; set; }
        public string? Observaciones { get; set; }
    }

    public class ResultadoExamenDto
    {
        public string? Resultado { get; set; }
        public string? Observaciones { get; set; }
    }

    public class EvolucionRequestDto
    {
        public int? IdPaciente { get; set; }
        public int? IdResponsable { get; set; }
        public string? NotaEvolucion { get; set; }
        public string? EstadoSalud { get; set; }
    }

    public class SubirArchivoDto
    {
        public IFormFile? Archivo { get; set; }
        public int? IdPaciente { get; set; }
        public int? IdExamen { get; set; }
        public string? Categoria { get; set; }
        public string? Descripcion { get; set; }
    }
}