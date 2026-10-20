using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ClinicaBackend.API.Models
{
	[Table("Habitaciones")]
	public class Habitacion
	{
		[Key]
		[Column("id")]
		public int Id { get; set; }

		[Column("id_sucursal")]
		public int? IdSucursal { get; set; }

		[Column("numero_habitacion")]
		public string NumeroHabitacion { get; set; } = null!;

		[Column("tipo_habitacion")]
		public string? TipoHabitacion { get; set; }

        [Column("capacidad")]
        public int Capacidad { get; set; } = 1;

        [Column("estado")]
		public string? Estado { get; set; }

		[Column("precio_diario")]
		public decimal? PrecioDiario { get; set; }

		public virtual Sucursal? Sucursal { get; set; }
	}

	[Table("AsignacionHabitaciones")]
	public class AsignacionHabitacion
	{
		[Key]
		[Column("id")]
		public int Id { get; set; }

		[Column("id_habitacion")]
		public int? IdHabitacion { get; set; }

		[Column("id_paciente")]
		public int? IdPaciente { get; set; }

		[Column("fecha_ingreso")]
		public DateTime FechaIngreso { get; set; }

		[Column("fecha_egreso")]
		public DateTime? FechaEgreso { get; set; }

		[Column("observaciones")]
		public string? Observaciones { get; set; }

		[Column("estado")]
		public string? Estado { get; set; }

		public virtual Habitacion? Habitacion { get; set; }
		public virtual Paciente? Paciente { get; set; }
	}
}