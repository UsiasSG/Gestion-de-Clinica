using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ClinicaBackend.API.Migrations
{
    /// <inheritdoc />
    public partial class CargoMedicamentos : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "id_cargo",
                table: "MovimientosInventario",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "id_medicamento",
                table: "CargosPaciente",
                type: "int",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_MovimientosInventario_id_cargo",
                table: "MovimientosInventario",
                column: "id_cargo");

            migrationBuilder.CreateIndex(
                name: "IX_CargosPaciente_id_medicamento",
                table: "CargosPaciente",
                column: "id_medicamento");

            migrationBuilder.AddForeignKey(
                name: "FK_CargosPaciente_Medicamentos_id_medicamento",
                table: "CargosPaciente",
                column: "id_medicamento",
                principalTable: "Medicamentos",
                principalColumn: "id");

            migrationBuilder.AddForeignKey(
                name: "FK_MovimientosInventario_CargosPaciente_id_cargo",
                table: "MovimientosInventario",
                column: "id_cargo",
                principalTable: "CargosPaciente",
                principalColumn: "id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_CargosPaciente_Medicamentos_id_medicamento",
                table: "CargosPaciente");

            migrationBuilder.DropForeignKey(
                name: "FK_MovimientosInventario_CargosPaciente_id_cargo",
                table: "MovimientosInventario");

            migrationBuilder.DropIndex(
                name: "IX_MovimientosInventario_id_cargo",
                table: "MovimientosInventario");

            migrationBuilder.DropIndex(
                name: "IX_CargosPaciente_id_medicamento",
                table: "CargosPaciente");

            migrationBuilder.DropColumn(
                name: "id_cargo",
                table: "MovimientosInventario");

            migrationBuilder.DropColumn(
                name: "id_medicamento",
                table: "CargosPaciente");
        }
    }
}
