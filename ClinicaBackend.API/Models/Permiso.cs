using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ClinicaBackend.API.Models
{
    [Table("Modulos")]
    public class Modulo
    {
        [Key]
        [Column("id")]
        public int Id { get; set; }

        [Column("nombre")]
        public string Nombre { get; set; } = null!;

        [Column("codigo")]
        public string Codigo { get; set; } = null!;
    }

    [Table("Permisos")]
    public class Permiso
    {
        [Key]
        [Column("id")]
        public int Id { get; set; }

        [Column("id_rol")]
        public int? IdRol { get; set; }

        [Column("id_modulo")]
        public int? IdModulo { get; set; }

        [Column("puede_consultar")]
        public bool PuedeConsultar { get; set; }

        [Column("puede_crear")]
        public bool PuedeCrear { get; set; }

        [Column("puede_modificar")]
        public bool PuedeModificar { get; set; }

        [Column("puede_eliminar")]
        public bool PuedeEliminar { get; set; }

        public virtual Rol? Rol { get; set; }
        public virtual Modulo? Modulo { get; set; }
    }
}