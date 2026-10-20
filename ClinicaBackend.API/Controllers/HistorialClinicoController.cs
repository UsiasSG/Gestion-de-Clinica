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

            var permisos = await PermisosDelUsuarioAsync();

            return Ok(new { paciente, consultas, diagnosticos, examenes, evolucion, archivos, medicos, personalClinico, permisos });
        }

        // ═══════════════ CONSULTAS Y TRATAMIENTOS ═══════════════

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

            var diagnosticos = (model.Diagnosticos ?? new()).Where(d => !string.IsNullOrWhiteSpace(d.Descripcion)).ToList();
            var tratamientos = (model.Tratamientos ?? new()).Where(t => !string.IsNullOrWhiteSpace(t.Descripcion)).ToList();

            // Regla 14f: los diagnósticos requieren su propio permiso
            if (diagnosticos.Count > 0 && !await _permisos.TienePermisoAsync(User.ObtenerIdUsuario()!.Value, "DIAGNOSTICOS", "Crear"))
                return StatusCode(403, new { mensaje = "Su rol no tiene permiso para registrar diagnósticos. Guarde la consulta sin diagnósticos." });

            if (tratamientos.Any(t => t.FechaInicio != null && t.FechaFin != null && t.FechaFin < t.FechaInicio))
                return BadRequest(new { mensaje = "En los tratamientos, la fecha de fin no puede ser anterior a la de inicio." });

            var consulta = new ConsultaMedica
            {
                IdPaciente = model.IdPaciente,
                IdDoctor = doctor.Id,
                IdSucursal = await _db.ObtenerIdSucursalAsync(User) ?? doctor.IdSucursal,
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
                    Descripcion = t.Descripcion!.Trim(),
                    Indicaciones = Limpiar(t.Indicaciones),
                    FechaInicio = t.FechaInicio?.Date,
                    FechaFin = t.FechaFin?.Date
                });
            }

            // Un solo SaveChanges: consulta, diagnósticos y tratamientos se guardan juntos o no se guarda nada
            await _db.SaveChangesAsync();
            return Ok(new { mensaje = "Consulta registrada correctamente.", id = consulta.Id });
        }

        // POST: api/historial/tratamientos
        [HttpPost("tratamientos")]
        [RequierePermiso("HISTORIAL", "Crear")]
        public async Task<IActionResult> RegistrarTratamiento([FromBody] TratamientoRequestDto model)
        {
            if (model.IdConsulta == null || !await _db.ConsultasMedicas.AnyAsync(c => c.Id == model.IdConsulta))
                return BadRequest(new { mensaje = "Consulta no válida." });
            if (string.IsNullOrWhiteSpace(model.Descripcion))
                return BadRequest(new { mensaje = "La descripción del tratamiento es obligatoria." });
            if (model.FechaInicio != null && model.FechaFin != null && model.FechaFin < model.FechaInicio)
                return BadRequest(new { mensaje = "La fecha de fin no puede ser anterior a la de inicio." });

            _db.Tratamientos.Add(new Tratamiento
            {
                IdConsulta = model.IdConsulta,
                Descripcion = model.Descripcion.Trim(),
                Indicaciones = Limpiar(model.Indicaciones),
                FechaInicio = model.FechaInicio?.Date,
                FechaFin = model.FechaFin?.Date
            });

            await _db.SaveChangesAsync();
            return Ok(new { mensaje = "Tratamiento registrado correctamente." });
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

        // ═══════════════ EXÁMENES ═══════════════

        // POST: api/historial/examenes  (el resultado puede quedar pendiente)
        [HttpPost("examenes")]
        [RequierePermiso("HISTORIAL", "Crear")]
        public async Task<IActionResult> RegistrarExamen([FromBody] ExamenRequestDto model)
        {
            if (model.IdPaciente == null || !await _db.Pacientes.AnyAsync(p => p.Id == model.IdPaciente))
                return BadRequest(new { mensaje = "Paciente no válido." });
            if (string.IsNullOrWhiteSpace(model.NombreExamen))
                return BadRequest(new { mensaje = "El nombre del examen es obligatorio." });

            var fecha = model.FechaExamen ?? DateTime.Now;
            if (fecha > DateTime.Now.AddMinutes(5))
                return BadRequest(new { mensaje = "La fecha del examen no puede ser futura." });

            if (model.IdConsulta != null && !await _db.ConsultasMedicas.AnyAsync(c => c.Id == model.IdConsulta && c.IdPaciente == model.IdPaciente))
                return BadRequest(new { mensaje = "La consulta seleccionada no pertenece a este paciente." });

            _db.Examenes.Add(new Examen
            {
                IdPaciente = model.IdPaciente,
                IdConsulta = model.IdConsulta,
                NombreExamen = model.NombreExamen.Trim(),
                Resultado = Limpiar(model.Resultado),
                FechaExamen = fecha,
                Observaciones = Limpiar(model.Observaciones)
            });

            await _db.SaveChangesAsync();
            return Ok(new { mensaje = "Examen registrado correctamente." });
        }

        // PUT: api/historial/examenes/5/resultado
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

        // ═══════════════ EVOLUCIÓN ═══════════════

        // POST: api/historial/evolucion
        [HttpPost("evolucion")]
        [RequierePermiso("HISTORIAL", "Crear")]
        public async Task<IActionResult> RegistrarEvolucion([FromBody] EvolucionRequestDto model)
        {
            if (model.IdPaciente == null || !await _db.Pacientes.AnyAsync(p => p.Id == model.IdPaciente))
                return BadRequest(new { mensaje = "Paciente no válido." });
            if (string.IsNullOrWhiteSpace(model.NotaEvolucion))
                return BadRequest(new { mensaje = "La nota de evolución es obligatoria." });

            var estado = string.IsNullOrWhiteSpace(model.EstadoSalud) ? null
                : EstadosSalud.FirstOrDefault(e => e.Equals(model.EstadoSalud.Trim(), StringComparison.OrdinalIgnoreCase));
            if (!string.IsNullOrWhiteSpace(model.EstadoSalud) && estado == null)
                return BadRequest(new { mensaje = "Estado de salud no válido." });

            var (responsable, errorResponsable) = await ResolverResponsableAsync(model.IdResponsable, soloDoctores: false);
            if (responsable == null) return BadRequest(new { mensaje = errorResponsable });

            _db.EvolucionesPaciente.Add(new EvolucionPaciente
            {
                IdPaciente = model.IdPaciente,
                IdDoctor = responsable.Id,
                FechaRegistro = DateTime.Now,
                NotaEvolucion = model.NotaEvolucion.Trim(),
                EstadoSalud = estado
            });

            await _db.SaveChangesAsync();
            return Ok(new { mensaje = "Nota de evolución registrada correctamente." });
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

        private static bool FirmaValida(byte[] b, string extension) => extension.ToLowerInvariant() switch
        {
            ".pdf" => b.Length > 4 && b[0] == 0x25 && b[1] == 0x50 && b[2] == 0x44 && b[3] == 0x46,  // %PDF
            ".png" => b.Length > 8 && b[0] == 0x89 && b[1] == 0x50 && b[2] == 0x4E && b[3] == 0x47,  // ‰PNG
            ".jpg" or ".jpeg" => b.Length > 3 && b[0] == 0xFF && b[1] == 0xD8 && b[2] == 0xFF,       // JPEG
            _ => false
        };

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
            var idUsuario = User.ObtenerIdUsuario() ?? 0;
            var empleado = await EmpleadoActualAsync();
            var tipo = empleado?.TipoEmpleado;

            return new
            {
                Crear = await _permisos.TienePermisoAsync(idUsuario, "HISTORIAL", "Crear"),
                Modificar = await _permisos.TienePermisoAsync(idUsuario, "HISTORIAL", "Modificar"),
                Eliminar = await _permisos.TienePermisoAsync(idUsuario, "HISTORIAL", "Eliminar"),
                Diagnosticar = await _permisos.TienePermisoAsync(idUsuario, "DIAGNOSTICOS", "Crear"),
                EsDoctor = tipo == "Doctor",
                EsPersonalClinico = tipo == "Doctor" || tipo == "Enfermera",
                Empleado = empleado != null ? $"{empleado.Nombres} {empleado.Apellidos} ({empleado.TipoEmpleado})" : null
            };
        }

        private static string? Limpiar(string? valor) => string.IsNullOrWhiteSpace(valor) ? null : valor.Trim();
    }

    // ═══════════════ DTOs ═══════════════

    public class ConsultaRequestDto
    {
        public int? IdPaciente { get; set; }
        public int? IdDoctor { get; set; }
        public string? MotivoConsulta { get; set; }
        public string? Sintomatologia { get; set; }
        public string? Observaciones { get; set; }
        public List<DiagnosticoItemDto>? Diagnosticos { get; set; }
        public List<TratamientoItemDto>? Tratamientos { get; set; }
    }

    public class DiagnosticoItemDto
    {
        public string? CodigoCie { get; set; }
        public string? Descripcion { get; set; }
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
        public int? IdConsulta { get; set; }
    }

    public class DiagnosticoRequestDto : DiagnosticoItemDto
    {
        public int? IdConsulta { get; set; }
        public int? IdDoctor { get; set; }
    }

    public class ExamenRequestDto
    {
        public int? IdPaciente { get; set; }
        public int? IdConsulta { get; set; }
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