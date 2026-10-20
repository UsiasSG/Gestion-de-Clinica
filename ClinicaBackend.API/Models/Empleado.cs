using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ClinicaBackend.API.Models
{
    [Table("Empleados")]
    public class Empleado
    {
        [Key]
        [Column("id")]
        public int Id { get; set; }

        [Column("id_usuario")]
        public int? IdUsuario { get; set; }

        [Column("id_sucursal")]
        public int? IdSucursal { get; set; }

        [Column("id_especialidad")]
        public int? IdEspecialidad { get; set; }

        [Column("nombres")]
        public string Nombres { get; set; } = null!;

        [Column("apellidos")]
        public string Apellidos { get; set; } = null!;

        [Column("tipo_empleado")]
        public string? TipoEmpleado { get; set; }

        [Column("puesto")]
        public string? Puesto { get; set; } 

        [Column("telefono")]
        public string? Telefono { get; set; }

        [Column("correo")]
        public string? Correo { get; set; }

        public virtual Usuario? Usuario { get; set; }
        public virtual Sucursal? Sucursal { get; set; }
        public virtual Especialidad? Especialidad { get; set; }
    }
}