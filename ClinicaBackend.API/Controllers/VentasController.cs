using ClinicaBackend.API.Data;
using ClinicaBackend.API.Filters;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ClinicaBackend.API.Models;


namespace ClinicaBackend.API.Controllers
{
    [Authorize]
    [ApiController]
    [Route("api/[controller]")]
    public class VentasController : ControllerBase
    {
        private readonly ClinicaDbContext _db;

        public VentasController(ClinicaDbContext db)
        {
            _db = db;
        }

        [HttpPost]
        [RequierePermiso("FARMACIA", "Crear")]
        public async Task<IActionResult> RegistrarVenta([FromBody] VentaCreateDto request)
        {
            var strategy = _db.Database.CreateExecutionStrategy();

            try
            {
                var ventaProcesada = await strategy.ExecuteAsync(async () =>
                {
                    // Abrimos la transacción mediante la estrategia
                    using var transaction = await _db.Database.BeginTransactionAsync();

                    var venta = new Venta
                    {
                        IdSucursal = request.IdSucursal,
                        IdUsuarioVendedor = request.IdUsuarioVendedor,
                        IdPaciente = request.IdPaciente,
                        FechaVenta = DateTime.UtcNow,
                        Subtotal = 0,
                        Impuesto = 0,
                        Total = 0
                    };

                    _db.Ventas.Add(venta);
                    await _db.SaveChangesAsync();

                    decimal subtotalAcumulado = 0;

                    foreach (var item in request.Detalles)
                    {
                        var lote = await _db.LotesMedicamentos.FindAsync(item.IdLote);

                        if (lote == null)
                            throw new Exception($"El lote con ID {item.IdLote} no existe.");

                        if (lote.FechaVencimiento.Date < DateTime.Today)
                            throw new Exception($"El lote {lote.NumeroLote} se encuentra vencido ({lote.FechaVencimiento:dd/MM/yyyy}).");

                        if (lote.CantidadDisponible < item.Cantidad)
                            throw new Exception($"Stock insuficiente en el lote {lote.NumeroLote}. Disponible: {lote.CantidadDisponible}");

                        lote.CantidadDisponible -= item.Cantidad;

                        decimal subtotalDetalle = item.Cantidad * item.PrecioUnitario;
                        subtotalAcumulado += subtotalDetalle;

                        var detalle = new DetalleVenta
                        {
                            IdVenta = venta.Id,
                            IdLote = item.IdLote,
                            Cantidad = item.Cantidad,
                            PrecioUnitario = item.PrecioUnitario,
                            Subtotal = subtotalDetalle
                        };

                        _db.DetalleVentas.Add(detalle);
                    }

                    venta.Subtotal = subtotalAcumulado;
                    venta.Impuesto = Math.Round(subtotalAcumulado * 0.12m, 2);
                    venta.Total = venta.Subtotal + venta.Impuesto;

                    await _db.SaveChangesAsync();
                    await transaction.CommitAsync();

                    return venta;
                });

                return Ok(new { mensaje = "Venta registrada exitosamente.", ventaId = ventaProcesada.Id, total = ventaProcesada.Total });
            }
            catch (Exception ex)
            {
                return BadRequest(new { mensaje = ex.Message });
            }
        }
    }

    public class VentaCreateDto
    {
        public int IdSucursal { get; set; }
        public int IdUsuarioVendedor { get; set; }
        public int? IdPaciente { get; set; }
        public List<DetalleVentaCreateDto> Detalles { get; set; } = new();
    }

    public class DetalleVentaCreateDto
    {
        public int IdLote { get; set; }
        public int Cantidad { get; set; }
        public decimal PrecioUnitario { get; set; }
    }
}