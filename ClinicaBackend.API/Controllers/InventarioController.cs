using ClinicaBackend.API.Data;
using ClinicaBackend.API.Extensions;
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
    [Route("api/[controller]")]
    public class InventarioController : ControllerBase
    {
        private const int DiasAlertaVencimiento = 30;
        private readonly ClinicaDbContext _db;

        public InventarioController(ClinicaDbContext db) => _db = db;

        // ═══════════════ CATÁLOGOS ═══════════════

        // GET: api/Inventario/catalogos
        [HttpGet("catalogos")]
        [RequierePermiso("INVENTARIO", "Consultar")]
        public async Task<IActionResult> ObtenerCatalogos()
        {
            var categorias = await _db.CategoriasMedicamentos.AsNoTracking()
                .OrderBy(c => c.Nombre).Select(c => new { c.Id, c.Nombre }).ToListAsync();

            var sucursales = await _db.Sucursales.AsNoTracking()
                .Where(s => s.Estado).OrderBy(s => s.Nombre).Select(s => new { s.Id, s.Nombre }).ToListAsync();

            var marcas = await _db.Medicamentos.AsNoTracking()
                .Where(m => m.Marca != null && m.Marca != "")
                .Select(m => m.Marca!).Distinct().OrderBy(m => m).ToListAsync();

            var idSucursalUsuario = await _db.ObtenerIdSucursalAsync(User);

            return Ok(new { categorias, sucursales, marcas, idSucursalUsuario });
        }

        // POST: api/Inventario/categorias
        [HttpPost("categorias")]
        [RequierePermiso("INVENTARIO", "Crear")]
        public async Task<IActionResult> CrearCategoria([FromBody] CategoriaDto model)
        {
            var nombre = model.Nombre?.Trim();
            if (string.IsNullOrWhiteSpace(nombre))
                return BadRequest(new { mensaje = "El nombre de la categoría es obligatorio." });

            var existente = await _db.CategoriasMedicamentos.FirstOrDefaultAsync(c => c.Nombre == nombre);
            if (existente != null) return Ok(new { existente.Id, existente.Nombre });

            var categoria = new CategoriaMedicamento { Nombre = nombre };
            _db.CategoriasMedicamentos.Add(categoria);
            await _db.SaveChangesAsync();

            return Ok(new { categoria.Id, categoria.Nombre });
        }

        // ═══════════════ MEDICAMENTOS ═══════════════

        // GET: api/Inventario/medicamentos?idSucursal=1
        [HttpGet("medicamentos")]
        [RequierePermiso("INVENTARIO", "Consultar")]
        public async Task<IActionResult> ObtenerMedicamentos([FromQuery] int? idSucursal)
        {
            var sucursal = idSucursal ?? await _db.ObtenerIdSucursalAsync(User);
            var hoy = DateTime.Today;

            var lista = await _db.Medicamentos.AsNoTracking()
                .OrderBy(m => m.Nombre)
                .Select(m => new
                {
                    m.Id,
                    m.Codigo,
                    m.Nombre,
                    m.Descripcion,
                    m.Marca,
                    m.IdCategoria,
                    Categoria = m.Categoria != null ? m.Categoria.Nombre : null,
                    m.PrecioVenta,
                    m.StockMinimo,
                    m.RequiereReceta,
                    m.ImagenUrl,
                    m.Activo,
                    Existencia = _db.LotesMedicamentos
                        .Where(l => l.IdMedicamento == m.Id && l.IdSucursal == sucursal && l.FechaVencimiento > hoy)
                        .Sum(l => (int?)l.CantidadDisponible) ?? 0,
                    CantidadVencida = _db.LotesMedicamentos
                        .Where(l => l.IdMedicamento == m.Id && l.IdSucursal == sucursal && l.FechaVencimiento <= hoy)
                        .Sum(l => (int?)l.CantidadDisponible) ?? 0,
                    ProximoVencimiento = _db.LotesMedicamentos
                        .Where(l => l.IdMedicamento == m.Id && l.IdSucursal == sucursal && l.CantidadDisponible > 0 && l.FechaVencimiento > hoy)
                        .Min(l => (DateTime?)l.FechaVencimiento)
                })
                .ToListAsync();

            return Ok(lista);
        }

        // POST: api/Inventario/medicamentos
        [HttpPost("medicamentos")]
        [RequierePermiso("INVENTARIO", "Crear")]
        public async Task<IActionResult> CrearMedicamento([FromBody] MedicamentoDto model)
        {
            var error = await ValidarMedicamentoAsync(model, null);
            if (error != null) return BadRequest(new { mensaje = error });

            var medicamento = new Medicamento();
            AsignarMedicamento(medicamento, model);
            medicamento.Activo = true;

            _db.Medicamentos.Add(medicamento);
            await _db.SaveChangesAsync();

            return Ok(new { mensaje = "Medicamento registrado correctamente.", id = medicamento.Id });
        }

        // PUT: api/Inventario/medicamentos/5
        [HttpPut("medicamentos/{id:int}")]
        [RequierePermiso("INVENTARIO", "Modificar")]
        public async Task<IActionResult> ActualizarMedicamento(int id, [FromBody] MedicamentoDto model)
        {
            var medicamento = await _db.Medicamentos.FindAsync(id);
            if (medicamento == null) return NotFound(new { mensaje = "Medicamento no encontrado." });

            var error = await ValidarMedicamentoAsync(model, id);
            if (error != null) return BadRequest(new { mensaje = error });

            AsignarMedicamento(medicamento, model);
            medicamento.Activo = model.Activo;

            await _db.SaveChangesAsync();
            return Ok(new { mensaje = "Medicamento actualizado correctamente." });
        }

        // DELETE: api/Inventario/medicamentos/5
        [HttpDelete("medicamentos/{id:int}")]
        [RequierePermiso("INVENTARIO", "Eliminar")]
        public async Task<IActionResult> EliminarMedicamento(int id)
        {
            var medicamento = await _db.Medicamentos.FindAsync(id);
            if (medicamento == null) return NotFound(new { mensaje = "Medicamento no encontrado." });

            if (await _db.LotesMedicamentos.AnyAsync(l => l.IdMedicamento == id))
            {
                medicamento.Activo = false;
                await _db.SaveChangesAsync();
                return Ok(new { mensaje = "El medicamento tiene lotes registrados, por lo que se desactivó en lugar de eliminarse." });
            }

            _db.Medicamentos.Remove(medicamento);
            await _db.SaveChangesAsync();
            return Ok(new { mensaje = "Medicamento eliminado correctamente." });
        }

        // ═══════════════ LOTES ═══════════════

        // GET: api/Inventario/lotes?idSucursal=1
        [HttpGet("lotes")]
        [RequierePermiso("INVENTARIO", "Consultar")]
        public async Task<IActionResult> ObtenerLotes([FromQuery] int? idSucursal)
        {
            var sucursal = idSucursal ?? await _db.ObtenerIdSucursalAsync(User);
            var hoy = DateTime.Today;
            var limite = hoy.AddDays(DiasAlertaVencimiento);

            var lotes = await _db.LotesMedicamentos.AsNoTracking()
                .Where(l => l.IdSucursal == sucursal)
                .OrderBy(l => l.FechaVencimiento)
                .Select(l => new
                {
                    l.Id,
                    l.NumeroLote,
                    l.IdMedicamento,
                    Medicamento = l.Medicamento != null ? l.Medicamento.Nombre : null,
                    l.IdSucursal,
                    Sucursal = l.Sucursal != null ? l.Sucursal.Nombre : null,
                    l.FechaIngreso,
                    l.FechaVencimiento,
                    l.CantidadInicial,
                    l.CantidadDisponible,
                    Estado = l.CantidadDisponible == 0 ? "Agotado"
                           : l.FechaVencimiento <= hoy ? "Vencido"
                           : l.FechaVencimiento <= limite ? "Por vencer"
                           : "Vigente",
                    TieneSalidas = l.CantidadDisponible != l.CantidadInicial
                })
                .ToListAsync();

            return Ok(lotes);
        }

        // POST: api/Inventario/lotes  (entrada de mercadería)
        [HttpPost("lotes")]
        [RequierePermiso("INVENTARIO", "Crear")]
        public async Task<IActionResult> RegistrarEntrada([FromBody] LoteDto model)
        {
            var idSucursal = model.IdSucursal ?? await _db.ObtenerIdSucursalAsync(User);
            var hoy = DateTime.Today;
            var fechaIngreso = (model.FechaIngreso ?? hoy).Date;
            var numeroLote = model.NumeroLote?.Trim();

            if (model.IdMedicamento == null || !await _db.Medicamentos.AnyAsync(m => m.Id == model.IdMedicamento && m.Activo))
                return BadRequest(new { mensaje = "Seleccione un medicamento activo." });
            if (idSucursal == null || !await _db.Sucursales.AnyAsync(s => s.Id == idSucursal))
                return BadRequest(new { mensaje = "Seleccione una sucursal válida." });
            if (string.IsNullOrWhiteSpace(numeroLote))
                return BadRequest(new { mensaje = "El número de lote es obligatorio." });
            if (model.Cantidad is null or <= 0)
                return BadRequest(new { mensaje = "La cantidad debe ser mayor a 0." });
            if (model.FechaVencimiento == null)
                return BadRequest(new { mensaje = "La fecha de vencimiento es obligatoria." });

            var vencimiento = model.FechaVencimiento.Value.Date;
            if (fechaIngreso > hoy)
                return BadRequest(new { mensaje = "La fecha de ingreso no puede ser futura." });
            if (vencimiento <= hoy)
                return BadRequest(new { mensaje = "No se puede ingresar un lote vencido o que vence hoy." });
            if (vencimiento <= fechaIngreso)
                return BadRequest(new { mensaje = "La fecha de vencimiento debe ser posterior a la de ingreso." });

            var existente = await _db.LotesMedicamentos.FirstOrDefaultAsync(l =>
                l.IdMedicamento == model.IdMedicamento && l.IdSucursal == idSucursal && l.NumeroLote == numeroLote);

            if (existente != null)
            {
                // Un lote vencido nunca se modifica por una entrada
                if (existente.FechaVencimiento.Date <= hoy)
                    return BadRequest(new { mensaje = $"El lote {numeroLote} ya existe y está vencido. Registre el nuevo ingreso con otro número de lote." });

                // Un mismo lote no puede tener dos fechas de vencimiento
                if (existente.FechaVencimiento.Date != vencimiento)
                    return BadRequest(new { mensaje = $"El lote {numeroLote} ya existe con vencimiento {existente.FechaVencimiento:dd/MM/yyyy}. Verifique el número de lote o la fecha." });

                // Reabastecimiento del mismo lote vigente: solo se suma la cantidad
                existente.CantidadInicial += model.Cantidad.Value;
                existente.CantidadDisponible += model.Cantidad.Value;

                _db.MovimientosInventario.Add(new MovimientoInventario
                {
                    IdLote = existente.Id,
                    Tipo = TiposMovimiento.Entrada,
                    Cantidad = model.Cantidad.Value,
                    Fecha = DateTime.Now,
                    IdUsuario = User.ObtenerIdUsuario(),
                    Observacion = "Reingreso al mismo lote"
                });

                await _db.SaveChangesAsync();
                return Ok(new { mensaje = $"Se agregaron {model.Cantidad.Value} unidades al lote {numeroLote}. Disponible: {existente.CantidadDisponible}.", id = existente.Id });
            }

            var lote = new LoteMedicamento
            {
                IdMedicamento = model.IdMedicamento,
                IdSucursal = idSucursal,
                NumeroLote = numeroLote,
                FechaIngreso = fechaIngreso,
                FechaVencimiento = vencimiento,
                CantidadInicial = model.Cantidad.Value,
                CantidadDisponible = model.Cantidad.Value
            };

            _db.LotesMedicamentos.Add(lote);
            _db.MovimientosInventario.Add(new MovimientoInventario
            {
                Lote = lote,
                Tipo = TiposMovimiento.Entrada,
                Cantidad = lote.CantidadInicial,
                Fecha = DateTime.Now,
                IdUsuario = User.ObtenerIdUsuario(),
                Observacion = "Ingreso de lote"
            });

            await _db.SaveChangesAsync();
            return Ok(new { mensaje = $"Lote {numeroLote} registrado con {lote.CantidadInicial} unidades.", id = lote.Id });
        }

        // PUT: api/Inventario/lotes/5  (solo número y fechas, para conservar la trazabilidad)
        [HttpPut("lotes/{id:int}")]
        [RequierePermiso("INVENTARIO", "Modificar")]
        public async Task<IActionResult> ActualizarLote(int id, [FromBody] LoteEdicionDto model)
        {
            var lote = await _db.LotesMedicamentos.FindAsync(id);
            if (lote == null) return NotFound(new { mensaje = "Lote no encontrado." });

            var numeroLote = model.NumeroLote?.Trim();
            if (string.IsNullOrWhiteSpace(numeroLote))
                return BadRequest(new { mensaje = "El número de lote es obligatorio." });
            if (model.FechaIngreso == null || model.FechaVencimiento == null)
                return BadRequest(new { mensaje = "Las fechas de ingreso y vencimiento son obligatorias." });
            if (model.FechaVencimiento.Value.Date <= model.FechaIngreso.Value.Date)
                return BadRequest(new { mensaje = "La fecha de vencimiento debe ser posterior a la de ingreso." });

            if (await _db.LotesMedicamentos.AnyAsync(l => l.Id != id && l.IdMedicamento == lote.IdMedicamento && l.IdSucursal == lote.IdSucursal && l.NumeroLote == numeroLote))
                return BadRequest(new { mensaje = "Ya existe ese número de lote para este medicamento en la sucursal." });

            lote.NumeroLote = numeroLote;
            lote.FechaIngreso = model.FechaIngreso.Value.Date;
            lote.FechaVencimiento = model.FechaVencimiento.Value.Date;

            await _db.SaveChangesAsync();
            return Ok(new { mensaje = "Lote actualizado correctamente." });
        }

        // POST: api/Inventario/lotes/5/salidas  (uso clínico, merma o baja por vencimiento)
        [HttpPost("lotes/{id:int}/salidas")]
        [RequierePermiso("INVENTARIO", "Modificar")]
        public async Task<IActionResult> RegistrarSalida(int id, [FromBody] SalidaDto model)
        {
            var lote = await _db.LotesMedicamentos.FindAsync(id);
            if (lote == null) return NotFound(new { mensaje = "Lote no encontrado." });

            if (model.Tipo == null || !TiposMovimiento.SalidasManuales.Contains(model.Tipo))
                return BadRequest(new { mensaje = "Tipo de salida no válido." });
            if (model.Cantidad is null or <= 0)
                return BadRequest(new { mensaje = "La cantidad debe ser mayor a 0." });
            if (model.Cantidad > lote.CantidadDisponible)
                return BadRequest(new { mensaje = $"Solo hay {lote.CantidadDisponible} unidades disponibles en este lote." });
            if (string.IsNullOrWhiteSpace(model.Observacion))
                return BadRequest(new { mensaje = "Indique el motivo de la salida." });

            var vencido = lote.FechaVencimiento.Date <= DateTime.Today;
            if (model.Tipo == TiposMovimiento.UsoClinico && vencido)
                return BadRequest(new { mensaje = "No se puede utilizar un medicamento de un lote vencido." });
            if (model.Tipo == TiposMovimiento.BajaVencimiento && !vencido)
                return BadRequest(new { mensaje = "Solo se puede dar de baja por vencimiento un lote vencido." });

            lote.CantidadDisponible -= model.Cantidad.Value;
            _db.MovimientosInventario.Add(new MovimientoInventario
            {
                IdLote = lote.Id,
                Tipo = model.Tipo,
                Cantidad = -model.Cantidad.Value,
                Fecha = DateTime.Now,
                IdUsuario = User.ObtenerIdUsuario(),
                Observacion = model.Observacion.Trim()
            });

            await _db.SaveChangesAsync();
            return Ok(new { mensaje = "Salida registrada correctamente." });
        }

        // DELETE: api/Inventario/lotes/5  (solo si no tuvo ventas ni salidas)
        [HttpDelete("lotes/{id:int}")]
        [RequierePermiso("INVENTARIO", "Eliminar")]
        public async Task<IActionResult> EliminarLote(int id)
        {
            var lote = await _db.LotesMedicamentos.FindAsync(id);
            if (lote == null) return NotFound(new { mensaje = "Lote no encontrado." });

            var tieneVentas = await _db.DetalleVentas.AnyAsync(d => d.IdLote == id);
            var tieneSalidas = await _db.MovimientosInventario.AnyAsync(m => m.IdLote == id && m.Tipo != TiposMovimiento.Entrada);

            if (tieneVentas || tieneSalidas || lote.CantidadDisponible != lote.CantidadInicial)
                return BadRequest(new { mensaje = "No se puede eliminar un lote con ventas o salidas registradas. Si hubo un error, registre una salida tipo Merma." });

            _db.LotesMedicamentos.Remove(lote);
            await _db.SaveChangesAsync();
            return Ok(new { mensaje = "Lote eliminado correctamente." });
        }

        // ═══════════════ MOVIMIENTOS ═══════════════

        // GET: api/Inventario/movimientos?idSucursal=1&top=200
        [HttpGet("movimientos")]
        [RequierePermiso("INVENTARIO", "Consultar")]
        public async Task<IActionResult> ObtenerMovimientos([FromQuery] int? idSucursal, [FromQuery] int top = 200)
        {
            var sucursal = idSucursal ?? await _db.ObtenerIdSucursalAsync(User);
            top = Math.Clamp(top, 1, 1000);

            var lista = await _db.MovimientosInventario.AsNoTracking()
                .Where(m => m.Lote != null && m.Lote.IdSucursal == sucursal)
                .OrderByDescending(m => m.Fecha).ThenByDescending(m => m.Id)
                .Take(top)
                .Select(m => new
                {
                    m.Id,
                    m.Fecha,
                    m.Tipo,
                    m.Cantidad,
                    m.Observacion,
                    m.IdVenta,
                    m.IdLote,
                    NumeroLote = m.Lote!.NumeroLote,
                    Medicamento = m.Lote.Medicamento != null ? m.Lote.Medicamento.Nombre : null,
                    Usuario = m.Usuario != null ? m.Usuario.NombreUsuario : null
                })
                .ToListAsync();

            return Ok(lista);
        }

        // ═══════════════ AUXILIARES ═══════════════

        private async Task<string?> ValidarMedicamentoAsync(MedicamentoDto m, int? id)
        {
            if (string.IsNullOrWhiteSpace(m.Nombre)) return "El nombre es obligatorio.";
            if (m.PrecioVenta is null or <= 0m) return "El precio de venta debe ser mayor a 0.";
            if (m.StockMinimo is null or < 0) return "El stock mínimo no puede ser negativo.";
            if (m.IdCategoria != null && !await _db.CategoriasMedicamentos.AnyAsync(c => c.Id == m.IdCategoria))
                return "La categoría seleccionada no existe.";

            var codigo = m.Codigo?.Trim();
            if (!string.IsNullOrEmpty(codigo) && await _db.Medicamentos.AnyAsync(x => x.Codigo == codigo && x.Id != id))
                return "Ya existe un medicamento con ese código.";

            return null;
        }

        private static void AsignarMedicamento(Medicamento med, MedicamentoDto m)
        {
            med.Codigo = string.IsNullOrWhiteSpace(m.Codigo) ? null : m.Codigo.Trim();
            med.Nombre = m.Nombre!.Trim();
            med.Descripcion = string.IsNullOrWhiteSpace(m.Descripcion) ? null : m.Descripcion.Trim();
            med.Marca = string.IsNullOrWhiteSpace(m.Marca) ? null : m.Marca.Trim();
            med.IdCategoria = m.IdCategoria;
            med.PrecioVenta = Math.Round(m.PrecioVenta!.Value, 2);
            med.StockMinimo = m.StockMinimo!.Value;
            med.RequiereReceta = m.RequiereReceta;
            med.ImagenUrl = string.IsNullOrWhiteSpace(m.ImagenUrl) ? null : m.ImagenUrl.Trim();
        }
    }

    public class CategoriaDto
    {
        public string? Nombre { get; set; }
    }

    public class MedicamentoDto
    {
        public string? Codigo { get; set; }
        public string? Nombre { get; set; }
        public string? Descripcion { get; set; }
        public string? Marca { get; set; }
        public int? IdCategoria { get; set; }
        public decimal? PrecioVenta { get; set; }
        public int? StockMinimo { get; set; }
        public bool RequiereReceta { get; set; }
        public string? ImagenUrl { get; set; }
        public bool Activo { get; set; } = true;
    }

    public class LoteDto
    {
        public int? IdMedicamento { get; set; }
        public int? IdSucursal { get; set; }
        public string? NumeroLote { get; set; }
        public DateTime? FechaIngreso { get; set; }
        public DateTime? FechaVencimiento { get; set; }
        public int? Cantidad { get; set; }
    }

    public class LoteEdicionDto
    {
        public string? NumeroLote { get; set; }
        public DateTime? FechaIngreso { get; set; }
        public DateTime? FechaVencimiento { get; set; }
    }

    public class SalidaDto
    {
        public string? Tipo { get; set; }
        public int? Cantidad { get; set; }
        public string? Observacion { get; set; }
    }
}