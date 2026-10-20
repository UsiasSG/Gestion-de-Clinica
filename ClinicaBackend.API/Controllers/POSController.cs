using ClinicaBackend.API.Data;
using ClinicaBackend.API.Extensions;
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
    public class POSController : ControllerBase
    {
        private const decimal TasaIva = 0.12m;
        private readonly ClinicaDbContext _db;

        public POSController(ClinicaDbContext db) => _db = db;

        // GET: api/POS/productos  (existencia vigente de la sucursal del usuario)
        [HttpGet("productos")]
        [RequierePermiso("POS", "Consultar")]
        public async Task<IActionResult> ObtenerProductos()
        {
            var idSucursal = await _db.ObtenerIdSucursalAsync(User);
            var hoy = DateTime.Today;

            var productos = await _db.Medicamentos.AsNoTracking()
                .Where(m => m.Activo)
                .OrderBy(m => m.Nombre)
                .Select(m => new
                {
                    m.Id,
                    Codigo = m.Codigo ?? "MED-" + m.Id.ToString(),
                    m.Nombre,
                    m.Marca,
                    m.Descripcion,
                    Categoria = m.Categoria != null ? m.Categoria.Nombre : "General",
                    Precio = m.PrecioVenta,
                    Stock = _db.LotesMedicamentos
                        .Where(l => l.IdMedicamento == m.Id && l.IdSucursal == idSucursal && l.FechaVencimiento > hoy)
                        .Sum(l => (int?)l.CantidadDisponible) ?? 0,
                    m.ImagenUrl,
                    m.RequiereReceta,
                    m.StockMinimo
                })
                .ToListAsync();

            return Ok(productos);
        }

        // GET: api/POS/pacientes?query=juan
        [HttpGet("pacientes")]
        [RequierePermiso("POS", "Consultar")]
        public async Task<IActionResult> BuscarPacientes([FromQuery] string? query)
        {
            var consulta = _db.Pacientes.AsNoTracking().AsQueryable();

            if (!string.IsNullOrWhiteSpace(query))
            {
                var q = query.Trim();
                consulta = consulta.Where(p =>
                    (p.Nombres + " " + p.Apellidos).Contains(q) ||
                    (p.Telefono != null && p.Telefono.Contains(q)) ||
                    (p.DocumentoIdentificacion != null && p.DocumentoIdentificacion.Contains(q)));
            }

            var lista = await consulta
                .OrderBy(p => p.Nombres)
                .Take(10)
                .Select(p => new
                {
                    p.Id,
                    NombreCompleto = p.Nombres + " " + p.Apellidos,
                    p.Telefono,
                    Nit = "CF",
                    Dpi = p.DocumentoIdentificacion
                })
                .ToListAsync();

            return Ok(lista);
        }

        // POST: api/POS/ventas
        [HttpPost("ventas")]
        [RequierePermiso("POS", "Crear")]
        public async Task<IActionResult> RegistrarVenta([FromBody] VentaRequestDto request)
        {
            if (request?.Detalles == null || request.Detalles.Count == 0)
                return BadRequest(new { mensaje = "La venta debe contener al menos un producto." });
            if (request.Detalles.Any(d => d.Cantidad <= 0))
                return BadRequest(new { mensaje = "Todas las cantidades deben ser mayores a 0." });

            var idUsuario = User.ObtenerIdUsuario();
            if (idUsuario == null)
                return Unauthorized(new { mensaje = "No se pudo identificar al usuario de la sesión." });

            var idSucursal = await _db.ObtenerIdSucursalAsync(User);
            if (idSucursal == null)
                return BadRequest(new { mensaje = "Su usuario no tiene una sucursal asignada." });

            if (request.IdPaciente != null && !await _db.Pacientes.AnyAsync(p => p.Id == request.IdPaciente))
                return BadRequest(new { mensaje = "El paciente seleccionado no existe." });

            // Agrupar por producto, por si viene repetido en el carrito
            var items = request.Detalles
                .GroupBy(d => d.ProductoId)
                .Select(g => new { ProductoId = g.Key, Cantidad = g.Sum(x => x.Cantidad) })
                .ToList();

            var ids = items.Select(i => i.ProductoId).ToList();
            var medicamentos = await _db.Medicamentos.AsNoTracking()
                .Where(m => ids.Contains(m.Id) && m.Activo)
                .ToDictionaryAsync(m => m.Id);

            var hoy = DateTime.Today;
            var estrategia = _db.Database.CreateExecutionStrategy();

            try
            {
                return await estrategia.ExecuteAsync<IActionResult>(async () =>
                {
                    await using var transaccion = await _db.Database.BeginTransactionAsync();

                    var venta = new Venta
                    {
                        IdSucursal = idSucursal,
                        IdUsuarioVendedor = idUsuario,
                        IdPaciente = request.IdPaciente,
                        FechaVenta = DateTime.Now,
                        Estado = "Completada"
                    };

                    decimal subtotal = 0;

                    foreach (var item in items)
                    {
                        if (!medicamentos.TryGetValue(item.ProductoId, out var med))
                            return BadRequest(new { mensaje = $"El producto con ID {item.ProductoId} no existe o está inactivo." });

                        // FEFO: primero el lote que vence antes; nunca vencidos ni de otra sucursal
                        var lotes = await _db.LotesMedicamentos
                            .Where(l => l.IdMedicamento == med.Id && l.IdSucursal == idSucursal
                                     && l.CantidadDisponible > 0 && l.FechaVencimiento > hoy)
                            .OrderBy(l => l.FechaVencimiento).ThenBy(l => l.Id)
                            .ToListAsync();

                        var disponible = lotes.Sum(l => l.CantidadDisponible);
                        if (disponible < item.Cantidad)
                            return BadRequest(new { mensaje = $"Stock insuficiente de \"{med.Nombre}\". Disponible (no vencido): {disponible}, solicitado: {item.Cantidad}." });

                        var pendiente = item.Cantidad;
                        foreach (var lote in lotes)
                        {
                            if (pendiente == 0) break;

                            var tomar = Math.Min(lote.CantidadDisponible, pendiente);
                            lote.CantidadDisponible -= tomar;
                            pendiente -= tomar;

                            var subtotalLinea = tomar * med.PrecioVenta;
                            subtotal += subtotalLinea;

                            _db.DetalleVentas.Add(new DetalleVenta
                            {
                                Venta = venta,
                                IdLote = lote.Id,
                                Cantidad = tomar,
                                PrecioUnitario = med.PrecioVenta,
                                Subtotal = subtotalLinea
                            });

                            _db.MovimientosInventario.Add(new MovimientoInventario
                            {
                                IdLote = lote.Id,
                                Tipo = TiposMovimiento.Venta,
                                Cantidad = -tomar,
                                Fecha = DateTime.Now,
                                IdUsuario = idUsuario,
                                Venta = venta
                            });
                        }
                    }

                    venta.Subtotal = Math.Round(subtotal, 2);
                    venta.Impuesto = Math.Round(subtotal * TasaIva, 2);
                    venta.Total = venta.Subtotal + venta.Impuesto;

                    _db.Ventas.Add(venta);
                    await _db.SaveChangesAsync();
                    await transaccion.CommitAsync();

                    return Ok(new
                    {
                        mensaje = "Venta registrada con éxito.",
                        ventaId = venta.Id,
                        idVenta = venta.Id,
                        venta.Subtotal,
                        venta.Impuesto,
                        venta.Total
                    });
                });
            }
            catch (DbUpdateException ex)
            {
                return StatusCode(500, new { mensaje = "Error al guardar la venta.", detalle = ex.InnerException?.Message ?? ex.Message });
            }
        }
    }

    // Sucursal, vendedor y precios NO se reciben del navegador: se toman del token y de la BD
    public class VentaRequestDto
    {
        public int? IdPaciente { get; set; }
        public List<DetalleVentaRequestDto> Detalles { get; set; } = new();
    }

    public class DetalleVentaRequestDto
    {
        public int ProductoId { get; set; }
        public int Cantidad { get; set; }
    }
}