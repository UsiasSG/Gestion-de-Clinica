using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ClinicaBackend.API.Migrations
{
    /// <inheritdoc />
    public partial class HabitacionCapacidad : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "capacidad",
                table: "Habitaciones",
                type: "int",
                nullable: false,
                defaultValue: 1);


            migrationBuilder.Sql("UPDATE Habitaciones SET capacidad = 2 WHERE LOWER(tipo_habitacion) = 'doble'");
            migrationBuilder.Sql("UPDATE Habitaciones SET estado = 'Disponible' WHERE estado IS NULL OR LOWER(estado) IN ('libre', 'disponible')");
            migrationBuilder.Sql("UPDATE Habitaciones SET estado = 'Limpieza' WHERE LOWER(estado) LIKE '%limp%'");
            migrationBuilder.Sql("UPDATE Habitaciones SET estado = 'Mantenimiento' WHERE LOWER(estado) LIKE '%mantenim%'");
        }



        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "capacidad",
                table: "Habitaciones");
        }
    }
}
