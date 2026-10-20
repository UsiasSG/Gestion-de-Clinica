using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ClinicaBackend.API.Models
{
    public static class EstadosHospitalizacion
    {
        public const string Solicitada = "Solicitada";
        public const string Activa = "Activa";
        public const string Alta = "Alta";
        public const string Cancelada = "Cancelada";
    }

    public static class TiposHospitalizacion
    {
        public const string Vigilancia = "Vigilancia";
        public const string Cirugia = "Cirugía";

        public static readonly string[] Todos = { Vigilancia, Cirugia };
    }

    public static class EstadosCirugia
    {
        public const string Programada = "Programada";
        public const string Realizada = "Realizada";
        public const string Cancelada = "Cancelada";
    }

    [Table("Hospitalizaciones")]
    public class Hospitalizacion
    {
        [Key]
        [Column("id")]
        public int Id { get; set; }

        [Column("id_paciente")]
        public int IdPaciente { get; set; }

        [Column("id_sucursal")]
        public int? IdSucursal { get; set; }

        [Column("id_diagnostico")]
        public int? IdDiagnostico { get; set; }

        [Column("id_medico_responsable")]
        public int? IdMedicoResponsable { get; set; }

        [Column("tipo")]
        [MaxLength(20)]
        public string Tipo { get; set; } = TiposHospitalizacion.Vigilancia;

        [Column("motivo")]
        [MaxLength(500)]
        public string Motivo { get; set; } = null!;

        [Column("estado")]
        [MaxLength(20)]
        public string Estado { get; set; } = EstadosHospitalizacion.Solicitada;

        [Column("fecha_solicitud")]
        public DateTime FechaSolicitud { get; set; } = DateTime.Now;

        [Column("fecha_ingreso")]
        public DateTime? FechaIngreso { get; set; }

        [Column("fecha_alta")]
        public DateTime? FechaAlta { get; set; }

        [Column("observaciones_alta")]
        [MaxLength(500)]
        public string? ObservacionesAlta { get; set; }

        [Column("id_usuario_registro")]
        public int? IdUsuarioRegistro { get; set; }

        public virtual Paciente? Paciente { get; set; }
        public virtual Sucursal? Sucursal { get; set; }
        public virtual Diagnostico? Diagnostico { get; set; }
        public virtual Empleado? MedicoResponsable { get; set; }
        public virtual Usuario? UsuarioRegistro { get; set; }
    }

    [Table("Cirugias")]
    public class Cirugia
    {
        [Key]
        [Column("id")]
        public int Id { get; set; }

        [Column("id_hospitalizacion")]
        public int IdHospitalizacion { get; set; }

        [Column("id_servicio")]
        public int? IdServicio { get; set; }

        [Column("procedimiento")]
        [MaxLength(150)]
        public string Procedimiento { get; set; } = null!;

        [Column("id_cirujano")]
        public int? IdCirujano { get; set; }

        [Column("fecha_programada")]
        public DateTime FechaProgramada { get; set; }

        [Column("fecha_realizada")]
        public DateTime? FechaRealizada { get; set; }

        [Column("estado")]
        [MaxLength(20)]
        public string Estado { get; set; } = EstadosCirugia.Programada;

        [Column("costo_base")]
        public decimal CostoBase { get; set; }

        [Column("costo_final")]
        public decimal CostoFinal { get; set; }

        [Column("observaciones")]
        [MaxLength(500)]
        public string? Observaciones { get; set; }

        public virtual Hospitalizacion? Hospitalizacion { get; set; }
        public virtual ServicioClinico? Servicio { get; set; }
        public virtual Empleado? Cirujano { get; set; }
    }
}