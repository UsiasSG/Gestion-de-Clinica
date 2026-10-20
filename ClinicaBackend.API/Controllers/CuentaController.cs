using ClinicaBackend.API.Data;
using ClinicaBackend.API.Extensions;
using ClinicaBackend.API.Filters;
using ClinicaBackend.API.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using ClinicaBackend.API.Services;

namespace ClinicaBackend.API.Controllers
{
    [Authorize]
    [ApiController]
    [Route("api/cuenta")]
    public class CuentaController : ControllerBase
    {
        private const decimal TasaIva = 0.12m;
        private static readonly string[] MetodosPago = { "Efectivo", "Tarjeta", "Transferencia" };

        private readonly ClinicaDbContext _db;
        private readonly InventarioClinicoService _inventario;

        public CuentaController(ClinicaDbContext db, InventarioClinicoService inventario)
        {
            _db = db;
            _inventario = inventario;
        }

        // GET: api/cuenta/paciente/5
        [HttpGet("paciente/{idPaciente:int}")]
        [RequierePermiso("COBROS", "Consultar")]
        public async Task<IActionResult> ObtenerCuenta(int idPaciente)
        {
            if (!await _db.Pacientes.AnyAsync(p => p.Id == idPaciente))
                return NotFound(new { mensaje = "Paciente no encontrado." });

            var cargos = await _db.CargosPaciente.AsNoTracking()
                .Where(c => c.IdPaciente == idPaciente)
                .OrderByDescending(c => c.Fecha).ThenByDescending(c => c.Id)
                .Select(c => new
           {
                c.Id,
                c.Tipo,
                c.Descripcion,
                c.Cantidad,
                c.PrecioUnitario,
                c.Subtotal,
                c.Fecha,
                c.Estado,
                c.IdFactura,
                c.IdHospitalizacion,
                CobroAlAlta = c.IdHospitalizacion != null && c.Hospitalizacion != null &&
                (c.Hospitalizacion.Estado == EstadosHospitalizacion.Solicitada || c.Hospitalizacion.Estado == EstadosHospitalizacion.Activa)
           })
           .ToListAsync();

            var facturas = await _db.FacturasPaciente.AsNoTracking()
                .Where(f => f.IdPaciente == idPaciente)
                .OrderByDescending(f => f.Fecha)
                .Select(f => new { f.Id, f.Fecha, f.Tipo, f.Total, f.MetodoPago, f.Estado, Items = f.Cargos.Count })
                .ToListAsync();

            var ventas = await _db.Ventas.AsNoTracking()
                .Where(v => v.IdPaciente == idPaciente)
                .OrderByDescending(v => v.FechaVenta)
                .Select(v => new
                {
                    v.Id,
                    v.FechaVenta,
                    v.Total,
                    v.Estado,
                    Productos = _db.DetalleVentas
                        .Where(d => d.IdVenta == v.Id && d.Lote != null && d.Lote.Medicamento != null)
                        .Select(d => d.Lote!.Medicamento!.Nombre)
                        .ToList()
                })
                .ToListAsync();

            var pendiente = cargos.Where(c => c.Estado == EstadosCargo.Pendiente).Sum(c => c.Subtotal);
            var ivaPendiente = Math.Round(pendiente * TasaIva, 2);

            var resumen = new
            {
                PendienteSubtotal = pendiente,
                PendienteIva = ivaPendiente,
                PendienteTotal = pendiente + ivaPendiente,
                TotalFacturado = facturas.Where(f => f.Estado == "Pagada").Sum(f => f.Total),
                TotalFarmacia = ventas.Sum(v => v.Total)
            };

            // Servicios para agregar cargos manuales (las cirugías se cargan desde Hospitalización)
            var servicios = await _db.ServiciosClinicos.AsNoTracking()
                .Where(s => s.Activo && s.Tipo != TiposServicio.Cirugia)
                .OrderBy(s => s.Tipo).ThenBy(s => s.Nombre)
                .Select(s => new { s.Id, s.Nombre, s.Tipo, s.Precio })
                .ToListAsync();

            var idSucursal = await _db.ObtenerIdSucursalAsync(User);
            var hoy = DateTime.Today;
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

            return Ok(new { cargos, facturas, ventas, resumen, servicios, medicamentos });
        }

        // POST: api/cuenta/cargos  (cargo manual: procedimiento u otro)
        [HttpPost("cargos")]
        [RequierePermiso("COBROS", "Crear")]
        public async Task<IActionResult> AgregarCargo([FromBody] CargoManualDto model)
        {
            if (model.IdPaciente == null || !await _db.Pacientes.AnyAsync(p => p.Id == model.IdPaciente))
                return BadRequest(new { mensaje = "Paciente no válido." });
            if (model.Cantidad is null or < 1 or > 1000)
                return BadRequest(new { mensaje = "La cantidad debe estar entre 1 y 1000." });

            ServicioClinico? servicio = null;
            if (model.IdServicio != null)
            {
                servicio = await _db.ServiciosClinicos.FirstOrDefaultAsync(s => s.Id == model.IdServicio && s.Activo);
                if (servicio == null) return BadRequest(new { mensaje = "El servicio seleccionado no es válido." });
            }

            var descripcion = string.IsNullOrWhiteSpace(model.Descripcion) ? servicio?.Nombre : model.Descripcion.Trim();
            if (string.IsNullOrWhiteSpace(descripcion))
                return BadRequest(new { mensaje = "Indique la descripción del cargo." });

            var precio = model.PrecioUnitario ?? servicio?.Precio;
            if (precio is null or < 0m)
                return BadRequest(new { mensaje = "Indique un precio válido." });

            var cargo = new CargoPaciente
            {
                IdPaciente = model.IdPaciente.Value,
                Tipo = servicio?.Tipo ?? TiposServicio.Otro,
                Descripcion = descripcion,
                Cantidad = model.Cantidad.Value,
                PrecioUnitario = Math.Round(precio.Value, 2),
                Subtotal = Math.Round(precio.Value * model.Cantidad.Value, 2),
                Fecha = DateTime.Now,
                IdServicio = servicio?.Id,
                IdUsuario = User.ObtenerIdUsuario(),
                Estado = EstadosCargo.Pendiente
            };

            _db.CargosPaciente.Add(cargo);
            await _db.SaveChangesAsync();
            return Ok(new { mensaje = $"Cargo agregado: {cargo.Descripcion} (Q{cargo.Subtotal:N2})." });
        }

        // POST: api/cuenta/medicamentos  (medicamento entregado y cargado a la cuenta; descuenta inventario)
        [HttpPost("medicamentos")]
        [RequierePermiso("COBROS", "Crear")]
        public async Task<IActionResult> CargarMedicamento([FromBody] CargoMedicamentoDto model)
        {
            if (model.IdPaciente == null || !await _db.Pacientes.AnyAsync(p => p.Id == model.IdPaciente))
                return BadRequest(new { mensaje = "Paciente no válido." });
            if (model.Cantidad is null or < 1 or > 500)
                return BadRequest(new { mensaje = "La cantidad debe estar entre 1 y 500." });
            var medicamento = await _db.Medicamentos.AsNoTracking()
                .FirstOrDefaultAsync(m => m.Id == model.IdMedicamento && m.Activo);
            if (medicamento == null)
                return BadRequest(new { mensaje = "Seleccione un medicamento activo." });
            var idSucursal = await _db.ObtenerIdSucursalAsync(User);
            if (idSucursal == null)
                return BadRequest(new { mensaje = "Su usuario no tiene una sucursal asignada." });
            var idUsuario = User.ObtenerIdUsuario();
            var cantidad = model.Cantidad.Value;
            var cargo = new CargoPaciente
            {
                IdPaciente = model.IdPaciente.Value,
                Tipo = TiposServicio.Medicamento,
                Descripcion = string.IsNullOrWhiteSpace(medicamento.Marca) ? medicamento.Nombre : $"{medicamento.Nombre} ({medicamento.Marca})",
                Cantidad = cantidad,
                PrecioUnitario = medicamento.PrecioVenta,
                Subtotal = Math.Round(medicamento.PrecioVenta * cantidad, 2),
                Fecha = DateTime.Now,
                IdMedicamento = medicamento.Id,
                IdUsuario = idUsuario,
                Estado = EstadosCargo.Pendiente
            };
            var observacion = string.IsNullOrWhiteSpace(model.Observacion) ? "Cargo a cuenta del paciente" : model.Observacion.Trim();
            var error = await _inventario.DescontarAsync(medicamento.Id, idSucursal.Value, cantidad,
                TiposMovimiento.CargoCuenta, idUsuario, cargo, observacion);
            if (error != null)
                return BadRequest(new { mensaje = $"{medicamento.Nombre}: {error}" });
            _db.CargosPaciente.Add(cargo);
            // Un solo SaveChanges: cargo, descuento de lotes y bitácora se guardan juntos
            await _db.SaveChangesAsync();
            return Ok(new { mensaje = $"Se cargaron {cantidad} × {medicamento.Nombre} (Q{cargo.Subtotal:N2}) a la cuenta. Inventario actualizado." });
        }

        // POST: api/cuenta/cargos/5/anular
        [HttpPost("cargos/{id:int}/anular")]
        [RequierePermiso("COBROS", "Eliminar")]
        public async Task<IActionResult> AnularCargo(int id)
        {
            var cargo = await _db.CargosPaciente.FindAsync(id);
            if (cargo == null) return NotFound(new { mensaje = "Cargo no encontrado." });
            if (cargo.Estado != EstadosCargo.Pendiente)
                return BadRequest(new { mensaje = "Solo se pueden anular cargos pendientes. Si ya fue cobrado, anule la factura." });
            var esMedicamento = cargo.IdMedicamento != null;
            if (esMedicamento)
                await _inventario.DevolverCargoAsync(cargo, User.ObtenerIdUsuario());
            cargo.Estado = EstadosCargo.Anulado;
            await _db.SaveChangesAsync();
            return Ok(new
            {
                mensaje = esMedicamento
                ? "Cargo anulado. El medicamento regresó al inventario."
                : "Cargo anulado."
            });
        }

        // POST: api/cuenta/cobrar  (cobro ambulatorio de los cargos seleccionados)
        [HttpPost("cobrar")]
        [RequierePermiso("COBROS", "Crear")]
        public async Task<IActionResult> Cobrar([FromBody] CobroDto model)
        {
            if (model.IdPaciente == null)
                return BadRequest(new { mensaje = "Paciente no válido." });

            var ids = (model.IdsCargos ?? new()).Distinct().ToList();
            if (ids.Count == 0)
                return BadRequest(new { mensaje = "Seleccione al menos un cargo para cobrar." });

            var cargos = await _db.CargosPaciente.Where(c => ids.Contains(c.Id)).ToListAsync();
            if (cargos.Count != ids.Count || cargos.Any(c => c.IdPaciente != model.IdPaciente))
                return BadRequest(new { mensaje = "Algunos cargos no pertenecen a este paciente." });
            if (cargos.Any(c => c.Estado != EstadosCargo.Pendiente))
                return BadRequest(new { mensaje = "Algunos cargos ya fueron cobrados o anulados. Actualice la pantalla." });

            // Hospitalización: un solo pago, al dar de alta
            var idsHospitalizacion = cargos.Where(c => c.IdHospitalizacion != null).Select(c => c.IdHospitalizacion!.Value).Distinct().ToList();
            if (idsHospitalizacion.Count > 0 && await _db.Hospitalizaciones.AnyAsync(h => idsHospitalizacion.Contains(h.Id) &&
                    (h.Estado == EstadosHospitalizacion.Solicitada || h.Estado == EstadosHospitalizacion.Activa)))
                return BadRequest(new { mensaje = "Los cargos de una hospitalización activa se cobran en un solo pago al dar de alta." });

            var nit = (model.Nit ?? string.Empty).Trim().ToUpperInvariant();
            if (nit == string.Empty) nit = "CF";
            if (nit != "CF" && !Regex.IsMatch(nit, @"^\d+-?[\dK]$"))
                return BadRequest(new { mensaje = "NIT no válido. Use CF o un NIT válido (ej. 1234567-8)." });

            var metodo = MetodosPago.FirstOrDefault(m => m.Equals(model.MetodoPago?.Trim(), StringComparison.OrdinalIgnoreCase));
            if (metodo == null)
                return BadRequest(new { mensaje = "Método de pago no válido." });

            var subtotal = Math.Round(cargos.Sum(c => c.Subtotal), 2);
            var impuesto = Math.Round(subtotal * TasaIva, 2);
            var total = subtotal + impuesto;

            decimal? recibido = null, cambio = null;
            if (metodo == "Efectivo")
            {
                if (model.MontoRecibido is null || model.MontoRecibido < total)
                    return BadRequest(new { mensaje = $"El efectivo recibido debe ser al menos Q{total:N2}." });
                recibido = Math.Round(model.MontoRecibido.Value, 2);
                cambio = recibido - total;
            }
            else if (string.IsNullOrWhiteSpace(model.Referencia))
            {
                return BadRequest(new { mensaje = "Ingrese el número de voucher o referencia bancaria." });
            }

            var factura = new FacturaPaciente
            {
                IdPaciente = model.IdPaciente.Value,
                IdSucursal = await _db.ObtenerIdSucursalAsync(User),
                IdUsuario = User.ObtenerIdUsuario(),
                Fecha = DateTime.Now,
                Tipo = idsHospitalizacion.Count > 0 ? "Hospitalaria" : "Ambulatoria",
                IdHospitalizacion = idsHospitalizacion.Count == 1 ? idsHospitalizacion[0] : null,
                Nit = nit,
                NombreFactura = string.IsNullOrWhiteSpace(model.NombreFactura) ? "Consumidor Final" : model.NombreFactura.Trim(),
                Subtotal = subtotal,
                Impuesto = impuesto,
                Total = total,
                MetodoPago = metodo,
                Referencia = metodo == "Efectivo" ? null : model.Referencia!.Trim(),
                MontoRecibido = recibido,
                Cambio = cambio,
                Estado = "Pagada"
            };

            _db.FacturasPaciente.Add(factura);
            foreach (var c in cargos)
            {
                c.Estado = EstadosCargo.Facturado;
                c.Factura = factura;
            }

            await _db.SaveChangesAsync();

            return Ok(new
            {
                mensaje = $"Cobro registrado. Factura No. {factura.Id}.",
                factura = await DetalleFacturaAsync(factura.Id)
            });
        }

        // GET: api/cuenta/facturas/5
        [HttpGet("facturas/{id:int}")]
        [RequierePermiso("COBROS", "Consultar")]
        public async Task<IActionResult> ObtenerFactura(int id)
        {
            var detalle = await DetalleFacturaAsync(id);
            return detalle == null ? NotFound(new { mensaje = "Factura no encontrada." }) : Ok(detalle);
        }

        // POST: api/cuenta/facturas/5/anular  (los cargos vuelven a quedar pendientes)
        [HttpPost("facturas/{id:int}/anular")]
        [RequierePermiso("COBROS", "Eliminar")]
        public async Task<IActionResult> AnularFactura(int id, [FromBody] AnulacionDto model)
        {
            var factura = await _db.FacturasPaciente.Include(f => f.Cargos).FirstOrDefaultAsync(f => f.Id == id);
            if (factura == null) return NotFound(new { mensaje = "Factura no encontrada." });
            if (factura.Estado == "Anulada") return BadRequest(new { mensaje = "La factura ya está anulada." });
            if (string.IsNullOrWhiteSpace(model.Motivo)) return BadRequest(new { mensaje = "Indique el motivo de la anulación." });

            factura.Estado = "Anulada";
            factura.MotivoAnulacion = model.Motivo.Trim();

            foreach (var c in factura.Cargos.ToList())
            {
                c.Estado = EstadosCargo.Pendiente;
                c.IdFactura = null;
            }

            await _db.SaveChangesAsync();
            return Ok(new { mensaje = "Factura anulada. Sus cargos volvieron a quedar pendientes de cobro." });
        }

        private async Task<object?> DetalleFacturaAsync(int id) =>
            await _db.FacturasPaciente.AsNoTracking()
                .Where(f => f.Id == id)
                .Select(f => new
                {
                    f.Id,
                    f.Fecha,
                    f.Tipo,
                    f.Nit,
                    f.NombreFactura,
                    f.Subtotal,
                    f.Impuesto,
                    f.Total,
                    f.MetodoPago,
                    f.Referencia,
                    f.MontoRecibido,
                    f.Cambio,
                    f.Estado,
                    f.MotivoAnulacion,
                    Paciente = f.Paciente != null ? f.Paciente.Nombres + " " + f.Paciente.Apellidos : null,
                    Sucursal = f.Sucursal != null ? f.Sucursal.Nombre : null,
                    Cajero = f.Usuario != null ? f.Usuario.NombreUsuario : null,
                    Items = f.Cargos
                        .OrderBy(c => c.Tipo).ThenBy(c => c.Fecha)
                        .Select(c => new { c.Tipo, c.Descripcion, c.Cantidad, c.PrecioUnitario, c.Subtotal })
                        .ToList()
                })
                .FirstOrDefaultAsync();
    }

    public class CargoManualDto
    {
        public int? IdPaciente { get; set; }
        public int? IdServicio { get; set; }
        public string? Descripcion { get; set; }
        public int? Cantidad { get; set; } = 1;
        public decimal? PrecioUnitario { get; set; }
    }

    public class CobroDto
    {
        public int? IdPaciente { get; set; }
        public List<int>? IdsCargos { get; set; }
        public string? Nit { get; set; }
        public string? NombreFactura { get; set; }
        public string? MetodoPago { get; set; }
        public decimal? MontoRecibido { get; set; }
        public string? Referencia { get; set; }
    }

    public class AnulacionDto
    {
        public string? Motivo { get; set; }
    }

    public class CargoMedicamentoDto
    {
        public int? IdPaciente { get; set; }
        public int? IdMedicamento { get; set; }
        public int? Cantidad { get; set; } = 1;
        public string? Observacion { get; set; }
    }
}