using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ClinicaBackend.API.Models
{
	[Table("Sucursales")]
	public class Sucursal
	{
		[Key]
		[Column("id")]
		public int Id { get; set; }

		[Column("nombre")]
		public string Nombre { get; set; } = null!;

		[Column("direccion")]
		public string? Direccion { get; set; }

		[Column("telefono")]
		public string? Telefono { get; set; }

		[Column("estado")]
		public bool Estado { get; set; } = true;

		[Column("fecha_registro")]
		public DateTime FechaRegistro { get; set; } = DateTime.Now;
	}
}