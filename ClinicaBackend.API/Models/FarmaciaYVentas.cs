using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ClinicaBackend.API.Models
{
    [Table("CategoriasMedicamentos")]
    public class CategoriaMedicamento
    {
        [Key]
        [Column("id")]
        public int Id { get; set; }

        [Column("nombre")]
        public string Nombre { get; set; } = null!;
    }

    [Table("Medicamentos")]
    public class Medicamento
    {
        [Key]
        [Column("id")]
        public int Id { get; set; }

        [Column("id_categoria")]
        public int? IdCategoria { get; set; }

        [Column("nombre")]
        public string Nombre { get; set; } = null!;

        [Column("descripcion")]
        public string? Descripcion { get; set; }

        [Column("marca")]
        public string? Marca { get; set; }

        [Column("precio_venta")]
        public decimal PrecioVenta { get; set; }

        [Column("requiere_receta")]
        public bool RequiereReceta { get; set; }

        [Column("imagen_url")]
        public string? ImagenUrl { get; set; }

        [Column("codigo")]
        [MaxLength(30)]
        public string? Codigo { get; set; }

        [Column("stock_minimo")]
        public int StockMinimo { get; set; } = 10;

        [Column("activo")]
        public bool Activo { get; set; } = true;

        public virtual CategoriaMedicamento? Categoria { get; set; }
    }

    [Table("LotesMedicamentos")]
    public class LoteMedicamento
    {
        [Key]
        [Column("id")]
        public int Id { get; set; }

        [Column("id_medicamento")]
        public int? IdMedicamento { get; set; }

        [Column("id_sucursal")]
        public int? IdSucursal { get; set; }

        [Column("numero_lote")]
        public string NumeroLote { get; set; } = null!;

        [Column("fecha_ingreso")]
        public DateTime FechaIngreso { get; set; }

        [Column("fecha_vencimiento")]
        public DateTime FechaVencimiento { get; set; }

        [Column("cantidad_inicial")]
        public int CantidadInicial { get; set; }

        [Column("cantidad_disponible")]
        public int CantidadDisponible { get; set; }

        public virtual Medicamento? Medicamento { get; set; }
        public virtual Sucursal? Sucursal { get; set; }
    }

    [Table("Ventas")]
    public class Venta
    {
        [Key]
        [Column("id")]
        public int Id { get; set; }

        [Column("id_sucursal")]
        public int? IdSucursal { get; set; }

        [Column("id_usuario_vendedor")]
        public int? IdUsuarioVendedor { get; set; }

        [Column("id_paciente")]
        public int? IdPaciente { get; set; }

        [Column("fecha_venta")]
        public DateTime FechaVenta { get; set; } = DateTime.Now;

        [Column("subtotal")]
        public decimal Subtotal { get; set; }

        [Column("impuesto")]
        public decimal Impuesto { get; set; }

        [Column("total")]
        public decimal Total { get; set; }

        [Column("estado")]
        public string? Estado { get; set; } = "Completada";

        public virtual Sucursal? Sucursal { get; set; }
        public virtual Usuario? UsuarioVendedor { get; set; }
        public virtual Paciente? Paciente { get; set; }
    }

    [Table("DetalleVentas")]
    public class DetalleVenta
    {
        [Key]
        [Column("id")]
        public int Id { get; set; }

        [Column("id_venta")]
        public int? IdVenta { get; set; }

        [Column("id_lote")]
        public int? IdLote { get; set; }

        [Column("cantidad")]
        public int Cantidad { get; set; }

        [Column("precio_unitario")]
        public decimal PrecioUnitario { get; set; }

        [Column("subtotal")]
        public decimal Subtotal { get; set; }

        public virtual Venta? Venta { get; set; }
        public virtual LoteMedicamento? Lote { get; set; }
    }

    [Table("MovimientosInventario")]
    public class MovimientoInventario
    {
        [Key]
        [Column("id")]
        public int Id { get; set; }

        [Column("id_lote")]
        public int IdLote { get; set; }

        [Column("tipo")]
        [MaxLength(30)]
        public string Tipo { get; set; } = null!;

        // Positiva = entrada, negativa = salida
        [Column("cantidad")]
        public int Cantidad { get; set; }

        [Column("fecha")]
        public DateTime Fecha { get; set; } = DateTime.Now;

        [Column("id_usuario")]
        public int? IdUsuario { get; set; }

        [Column("id_venta")]
        public int? IdVenta { get; set; }

        [Column("observacion")]
        [MaxLength(250)]
        public string? Observacion { get; set; }

        public virtual LoteMedicamento? Lote { get; set; }
        public virtual Usuario? Usuario { get; set; }
        public virtual Venta? Venta { get; set; }
    }

    public static class TiposMovimiento
    {
        public const string Entrada = "Entrada";
        public const string Venta = "Venta";
        public const string UsoClinico = "Uso clínico";
        public const string Merma = "Merma";
        public const string BajaVencimiento = "Baja por vencimiento";

        public static readonly string[] SalidasManuales = { UsoClinico, Merma, BajaVencimiento };
    }
}