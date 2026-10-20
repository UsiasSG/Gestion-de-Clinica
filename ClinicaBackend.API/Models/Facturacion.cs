using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ClinicaBackend.API.Models
{
    public static class TiposServicio
    {
        public const string Consulta = "Consulta";
        public const string Examen = "Examen";
        public const string Procedimiento = "Procedimiento";
        public const string Cirugia = "Cirugía";
        public const string Otro = "Otro";
        public const string Medicamento = "Medicamento";
        public const string Insumo = "Insumo";
        public const string Habitacion = "Habitación";
        public static readonly string[] Todos = { Consulta, Examen, Procedimiento, Cirugia, Otro };
    }

    public static class EstadosCargo
    {
        public const string Pendiente = "Pendiente";
        public const string Facturado = "Facturado";
        public const string Anulado = "Anulado";
    }

    [Table("ServiciosClinicos")]
    public class ServicioClinico
    {
        [Key]
        [Column("id")]
        public int Id { get; set; }

        [Column("codigo")]
        [MaxLength(30)]
        public string? Codigo { get; set; }

        [Column("nombre")]
        [MaxLength(150)]
        public string Nombre { get; set; } = null!;

        [Column("tipo")]
        [MaxLength(30)]
        public string Tipo { get; set; } = null!;

        [Column("descripcion")]
        [MaxLength(300)]
        public string? Descripcion { get; set; }

        [Column("precio")]
        public decimal Precio { get; set; }

        [Column("activo")]
        public bool Activo { get; set; } = true;
    }

    [Table("CargosPaciente")]
    public class CargoPaciente
    {
        [Key]
        [Column("id")]
        public int Id { get; set; }

        [Column("id_paciente")]
        public int IdPaciente { get; set; }

        [Column("tipo")]
        [MaxLength(30)]
        public string Tipo { get; set; } = null!;

        [Column("descripcion")]
        [MaxLength(250)]
        public string Descripcion { get; set; } = null!;

        [Column("cantidad")]
        public int Cantidad { get; set; } = 1;

        [Column("precio_unitario")]
        public decimal PrecioUnitario { get; set; }

        [Column("subtotal")]
        public decimal Subtotal { get; set; }

        [Column("fecha")]
        public DateTime Fecha { get; set; } = DateTime.Now;

        [Column("id_servicio")]
        public int? IdServicio { get; set; }

        [Column("id_consulta")]
        public int? IdConsulta { get; set; }

        [Column("id_examen")]
        public int? IdExamen { get; set; }

        [Column("id_medicamento")]
        public int? IdMedicamento { get; set; }


        [Column("id_usuario")]
        public int? IdUsuario { get; set; }

        [Column("estado")]
        [MaxLength(20)]
        public string Estado { get; set; } = EstadosCargo.Pendiente;

        [Column("id_factura")]
        public int? IdFactura { get; set; }

        [Column("id_hospitalizacion")]
        public int? IdHospitalizacion { get; set; }

        [Column("id_cirugia")]
        public int? IdCirugia { get; set; }

        public virtual Hospitalizacion? Hospitalizacion { get; set; }
        public virtual Cirugia? Cirugia { get; set; }
        public virtual Medicamento? Medicamento { get; set; }
        public virtual Paciente? Paciente { get; set; }
        public virtual ServicioClinico? Servicio { get; set; }
        public virtual ConsultaMedica? Consulta { get; set; }
        public virtual Examen? Examen { get; set; }
        public virtual Usuario? Usuario { get; set; }
        public virtual FacturaPaciente? Factura { get; set; }
    }

    [Table("FacturasPaciente")]
    public class FacturaPaciente
    {
        [Key]
        [Column("id")]
        public int Id { get; set; }

        [Column("id_paciente")]
        public int IdPaciente { get; set; }

        [Column("id_sucursal")]
        public int? IdSucursal { get; set; }

        [Column("id_usuario")]
        public int? IdUsuario { get; set; }

        [Column("fecha")]
        public DateTime Fecha { get; set; } = DateTime.Now;

        [Column("tipo")]
        [MaxLength(20)]
        public string Tipo { get; set; } = "Ambulatoria";

        [Column("nit")]
        [MaxLength(20)]
        public string Nit { get; set; } = "CF";

        [Column("nombre_factura")]
        [MaxLength(150)]
        public string NombreFactura { get; set; } = "Consumidor Final";

        [Column("subtotal")]
        public decimal Subtotal { get; set; }

        [Column("impuesto")]
        public decimal Impuesto { get; set; }

        [Column("total")]
        public decimal Total { get; set; }

        [Column("metodo_pago")]
        [MaxLength(20)]
        public string MetodoPago { get; set; } = "Efectivo";

        [Column("referencia")]
        [MaxLength(60)]
        public string? Referencia { get; set; }

        [Column("monto_recibido")]
        public decimal? MontoRecibido { get; set; }

        [Column("cambio")]
        public decimal? Cambio { get; set; }

        [Column("estado")]
        [MaxLength(20)]
        public string Estado { get; set; } = "Pagada";

        [Column("motivo_anulacion")]
        [MaxLength(250)]
        public string? MotivoAnulacion { get; set; }

        [Column("id_hospitalizacion")]
        public int? IdHospitalizacion { get; set; }

        public virtual Hospitalizacion? Hospitalizacion { get; set; }
        public virtual Paciente? Paciente { get; set; }
        public virtual Sucursal? Sucursal { get; set; }
        public virtual Usuario? Usuario { get; set; }
        public virtual ICollection<CargoPaciente> Cargos { get; set; } = new List<CargoPaciente>();
    }
}