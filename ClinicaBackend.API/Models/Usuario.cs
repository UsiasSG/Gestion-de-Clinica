using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ClinicaBackend.API.Models
{
    [Table("Usuarios")]
    public class Usuario
    {
        [Key]
        [Column("id")]
        public int Id { get; set; }

        [Column("id_sucursal")]
        public int? IdSucursal { get; set; }

        [Column("id_rol")]
        public int? IdRol { get; set; }

        [Column("nombre_usuario")]
        public string NombreUsuario { get; set; } = null!;

        [Column("correo")]
        public string Correo { get; set; } = null!;

        // Propiedad Alias/Helper para compatibilidad
        [NotMapped]
        public string Email
        {
            get => Correo;
            set => Correo = value;
        }

        [Column("password_hash")]
        public string PasswordHash { get; set; } = null!;

        [Column("estado")]
        public bool Estado { get; set; } = true;

        // Propiedad Alias/Helper para compatibilidad con "Activo"
        [NotMapped]
        public bool Activo
        {
            get => Estado;
            set => Estado = value;
        }

        [Column("fecha_creacion")]
        public DateTime FechaCreacion { get; set; } = DateTime.Now;

        // Propiedad Alias/Helper para compatibilidad con "FechaRegistro"
        [NotMapped]
        public DateTime FechaRegistro
        {
            get => FechaCreacion;
            set => FechaCreacion = value;
        }

        public virtual Sucursal? Sucursal { get; set; }
        public virtual Rol? Rol { get; set; }
    }
}