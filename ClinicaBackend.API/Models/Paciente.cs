using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ClinicaBackend.API.Models
{
    [Table("pacientes")]
    public class Paciente
    {
        [Key]
        [Column("id")]
        public int Id { get; set; }

        [Column("documento_identificacion")]
        public string? DocumentoIdentificacion { get; set; }

        [Column("nombres")]
        public string Nombres { get; set; } = null!;

        [Column("apellidos")]
        public string Apellidos { get; set; } = null!;

        [Column("fecha_nacimiento")]
        public DateTime FechaNacimiento { get; set; }

        [Column("genero")]
        public string Genero { get; set; } = "Femenino";

        [Column("telefono")]
        public string? Telefono { get; set; }

        [Column("direccion")]
        public string? Direccion { get; set; }

        [Column("tipo_sangre")]
        public string? TipoSangre { get; set; }

        [Column("alergias")]
        public string? Alergias { get; set; }

        [Column("es_menor_edad")]
        public bool EsMenorEdad { get; set; }

        [Column("nombre_encargado")]
        public string? NombreEncargado { get; set; }

        [Column("dpi_encargado")]
        public string? DpiEncargado { get; set; }

        [Column("contacto_emergencia")]
        public string? ContactoEmergencia { get; set; }

        [Column("fecha_registro")]
        public DateTime FechaRegistro { get; set; } = DateTime.Now;
    }
}