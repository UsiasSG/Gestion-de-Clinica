using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ClinicaBackend.API.Migrations
{
    /// <inheritdoc />
    public partial class EmpleadoPuesto : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "puesto",
                table: "Empleados",
                type: "nvarchar(max)",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "puesto",
                table: "Empleados");
        }
    }
}
