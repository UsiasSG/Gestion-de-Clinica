using ClinicaBackend.API.Data;
using ClinicaBackend.API.Models;
using Microsoft.EntityFrameworkCore;
using System;
using System.Threading.Tasks;

namespace ClinicaBackend.API.Services
{
    // Descuentos y devoluciones de inventario por uso clínico (cargos a cuenta, insumos de hospitalización)
    public class InventarioClinicoService
    {
        private readonly ClinicaDbContext _db;

        public InventarioClinicoService(ClinicaDbContext db) => _db = db;

        // FEFO: primero el lote que vence antes; nunca lotes vencidos ni de otra sucursal.
        // No llama a SaveChanges: el controlador guarda todo junto para que sea una sola transacción.
        public async Task<string?> DescontarAsync(int idMedicamento, int idSucursal, int cantidad,
            string tipoMovimiento, int? idUsuario, CargoPaciente? cargo, string? observacion)
        {
            var hoy = DateTime.Today;

            var lotes = await _db.LotesMedicamentos
                .Where(l => l.IdMedicamento == idMedicamento && l.IdSucursal == idSucursal
                         && l.CantidadDisponible > 0 && l.FechaVencimiento > hoy)
                .OrderBy(l => l.FechaVencimiento).ThenBy(l => l.Id)
                .ToListAsync();

            var disponible = lotes.Sum(l => l.CantidadDisponible);
            if (disponible < cantidad)
                return $"existencia insuficiente. Disponible (no vencido): {disponible}, solicitado: {cantidad}.";

            var pendiente = cantidad;
            foreach (var lote in lotes)
            {
                if (pendiente == 0) break;

                var tomar = Math.Min(lote.CantidadDisponible, pendiente);
                lote.CantidadDisponible -= tomar;
                pendiente -= tomar;

                _db.MovimientosInventario.Add(new MovimientoInventario
                {
                    IdLote = lote.Id,
                    Tipo = tipoMovimiento,
                    Cantidad = -tomar,
                    Fecha = DateTime.Now,
                    IdUsuario = idUsuario,
                    Cargo = cargo,
                    Observacion = observacion
                });
            }

            return null;
        }

        // Regresa al inventario lo que se descontó para un cargo (al anularlo)
        public async Task DevolverCargoAsync(CargoPaciente cargo, int? idUsuario)
        {
            var salidas = await _db.MovimientosInventario
                .Include(m => m.Lote)
                .Where(m => m.IdCargo == cargo.Id && m.Cantidad < 0)
                .ToListAsync();

            foreach (var salida in salidas)
            {
                if (salida.Lote == null) continue;

                salida.Lote.CantidadDisponible += -salida.Cantidad;

                _db.MovimientosInventario.Add(new MovimientoInventario
                {
                    IdLote = salida.IdLote,
                    Tipo = TiposMovimiento.Devolucion,
                    Cantidad = -salida.Cantidad,
                    Fecha = DateTime.Now,
                    IdUsuario = idUsuario,
                    IdCargo = cargo.Id,
                    Observacion = $"Anulación del cargo #{cargo.Id}"
                });
            }
        }
    }
}