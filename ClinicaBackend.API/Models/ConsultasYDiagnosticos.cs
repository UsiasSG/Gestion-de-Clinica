using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ClinicaBackend.API.Models
{
    [Table("ConsultasMedicas")]
    public class ConsultaMedica
    {
        [Key]
        [Column("id")]
        public int Id { get; set; }

        [Column("id_paciente")]
        public int? IdPaciente { get; set; }

        [Column("id_doctor")]
        public int? IdDoctor { get; set; }

        [Column("id_sucursal")]
        public int? IdSucursal { get; set; }

        [Column("fecha_consulta")]
        public DateTime FechaConsulta { get; set; } = DateTime.Now;

        [Column("motivo_consulta")]
        public string? MotivoConsulta { get; set; }

        [Column("sintomatologia")]
        public string? Sintomatologia { get; set; }

        [Column("observaciones")]
        public string? Observaciones { get; set; }

        public virtual Paciente? Paciente { get; set; }
        public virtual Empleado? Doctor { get; set; }
        public virtual Sucursal? Sucursal { get; set; }
    }

    [Table("Diagnosticos")]
    public class Diagnostico
    {
        [Key]
        [Column("id")]
        public int Id { get; set; }

        [Column("id_consulta")]
        public int? IdConsulta { get; set; }

        [Column("id_doctor")]
        public int? IdDoctor { get; set; }

        [Column("codigo_cie")]
        public string? CodigoCie { get; set; }

        [Column("descripcion")]
        public string Descripcion { get; set; } = null!;

        [Column("fecha_diagnostico")]
        public DateTime FechaDiagnostico { get; set; } = DateTime.Now;

        public virtual ConsultaMedica? Consulta { get; set; }
        public virtual Empleado? Doctor { get; set; }
    }

    [Table("Tratamientos")]
    public class Tratamiento
    {
        [Key]
        [Column("id")]
        public int Id { get; set; }

        [Column("id_consulta")]
        public int? IdConsulta { get; set; }

        [Column("id_paciente")]
        public int? IdPaciente { get; set; }

        [Column("id_diagnostico")]
        public int? IdDiagnostico { get; set; }

        [Column("id_doctor")]
        public int? IdDoctor { get; set; }

        [Column("descripcion")]
        public string Descripcion { get; set; } = null!;

        [Column("indicaciones")]
        public string? Indicaciones { get; set; }

        [Column("fecha_inicio")]
        public DateTime? FechaInicio { get; set; }

        [Column("fecha_fin")]
        public DateTime? FechaFin { get; set; }

        [Column("estado")]
        [MaxLength(20)]
        public string? Estado { get; set; } = "Activo";

        [Column("fecha_registro")]
        public DateTime FechaRegistro { get; set; } = DateTime.Now;

        public virtual ConsultaMedica? Consulta { get; set; }
        public virtual Paciente? Paciente { get; set; }
        public virtual Diagnostico? Diagnostico { get; set; }
        public virtual Empleado? Doctor { get; set; }
    }

    [Table("Examenes")]
    public class Examen
    {
        [Key]
        [Column("id")]
        public int Id { get; set; }

        [Column("id_paciente")]
        public int? IdPaciente { get; set; }

        [Column("id_consulta")]
        public int? IdConsulta { get; set; }

        [Column("nombre_examen")]
        public string NombreExamen { get; set; } = null!;

        [Column("resultado")]
        public string? Resultado { get; set; }

        [Column("fecha_examen")]
        public DateTime FechaExamen { get; set; } = DateTime.Now;

        [Column("observaciones")]
        public string? Observaciones { get; set; }

        public virtual Paciente? Paciente { get; set; }
        public virtual ConsultaMedica? Consulta { get; set; }
    }

    [Table("EvolucionPaciente")]
    public class EvolucionPaciente
    {
        [Key]
        [Column("id")]
        public int Id { get; set; }

        [Column("id_paciente")]
        public int? IdPaciente { get; set; }

        [Column("id_doctor")]
        public int? IdDoctor { get; set; }

        [Column("fecha_registro")]
        public DateTime FechaRegistro { get; set; } = DateTime.Now;

        [Column("nota_evolucion")]
        public string NotaEvolucion { get; set; } = null!;

        [Column("estado_salud")]
        public string? EstadoSalud { get; set; }

        public virtual Paciente? Paciente { get; set; }
        public virtual Empleado? Doctor { get; set; }
    }
}