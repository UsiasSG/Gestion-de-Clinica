using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ClinicaBackend.API.Models
{
    [Table("Especialidades")]
    public class Especialidad
    {
        [Key]
        [Column("id")]
        public int Id { get; set; }

        [Column("nombre")]
        public string Nombre { get; set; } = null!;

        [Column("descripcion")]
        public string? Descripcion { get; set; }
    }
}