using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ClinicaBackend.API.Models
{
    [Table("ArchivosClinicos")]
    public class ArchivoClinico
    {
        [Key]
        [Column("id")]
        public int Id { get; set; }

        [Column("id_paciente")]
        public int IdPaciente { get; set; }

        [Column("id_examen")]
        public int? IdExamen { get; set; }

        [Column("nombre_archivo")]
        [MaxLength(200)]
        public string NombreArchivo { get; set; } = null!;

        [Column("tipo_contenido")]
        [MaxLength(100)]
        public string TipoContenido { get; set; } = null!;

        [Column("tamano_bytes")]
        public long TamanoBytes { get; set; }

        [Column("contenido")]
        public byte[] Contenido { get; set; } = null!;

        [Column("categoria")]
        [MaxLength(50)]
        public string Categoria { get; set; } = null!;

        [Column("descripcion")]
        [MaxLength(250)]
        public string? Descripcion { get; set; }

        [Column("fecha_subida")]
        public DateTime FechaSubida { get; set; } = DateTime.Now;

        [Column("id_usuario")]
        public int? IdUsuario { get; set; }

        public virtual Paciente? Paciente { get; set; }
        public virtual Examen? Examen { get; set; }
        public virtual Usuario? Usuario { get; set; }
    }
}