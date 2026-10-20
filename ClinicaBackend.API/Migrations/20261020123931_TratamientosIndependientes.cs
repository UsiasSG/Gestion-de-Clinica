using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ClinicaBackend.API.Migrations
{
    /// <inheritdoc />
    public partial class TratamientosIndependientes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "estado",
                table: "Tratamientos",
                type: "nvarchar(20)",
                maxLength: 20,
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "fecha_registro",
                table: "Tratamientos",
                type: "datetime2",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AddColumn<int>(
                name: "id_diagnostico",
                table: "Tratamientos",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "id_doctor",
                table: "Tratamientos",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "id_paciente",
                table: "Tratamientos",
                type: "int",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Tratamientos_id_diagnostico",
                table: "Tratamientos",
                column: "id_diagnostico");

            migrationBuilder.CreateIndex(
                name: "IX_Tratamientos_id_doctor",
                table: "Tratamientos",
                column: "id_doctor");

            migrationBuilder.CreateIndex(
                name: "IX_Tratamientos_id_paciente",
                table: "Tratamientos",
                column: "id_paciente");

            migrationBuilder.AddForeignKey(
                name: "FK_Tratamientos_Diagnosticos_id_diagnostico",
                table: "Tratamientos",
                column: "id_diagnostico",
                principalTable: "Diagnosticos",
                principalColumn: "id");

            migrationBuilder.AddForeignKey(
                name: "FK_Tratamientos_Empleados_id_doctor",
                table: "Tratamientos",
                column: "id_doctor",
                principalTable: "Empleados",
                principalColumn: "id");

            migrationBuilder.AddForeignKey(
                name: "FK_Tratamientos_pacientes_id_paciente",
                table: "Tratamientos",
                column: "id_paciente",
                principalTable: "pacientes",
                principalColumn: "id");

            // ───────── Scripts SQL de data backfill ─────────
            migrationBuilder.Sql(@"
                UPDATE t SET t.id_paciente = c.id_paciente, t.id_doctor = c.id_doctor
                FROM Tratamientos t
                INNER JOIN ConsultasMedicas c ON c.id = t.id_consulta
                WHERE t.id_paciente IS NULL;
            ");

            migrationBuilder.Sql("UPDATE Tratamientos SET estado = 'Activo' WHERE estado IS NULL;");

            migrationBuilder.Sql("UPDATE Tratamientos SET fecha_registro = COALESCE(fecha_inicio, SYSDATETIME()) WHERE fecha_registro < '2000-01-01';");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Tratamientos_Diagnosticos_id_diagnostico",
                table: "Tratamientos");

            migrationBuilder.DropForeignKey(
                name: "FK_Tratamientos_Empleados_id_doctor",
                table: "Tratamientos");

            migrationBuilder.DropForeignKey(
                name: "FK_Tratamientos_pacientes_id_paciente",
                table: "Tratamientos");

            migrationBuilder.DropIndex(
                name: "IX_Tratamientos_id_diagnostico",
                table: "Tratamientos");

            migrationBuilder.DropIndex(
                name: "IX_Tratamientos_id_doctor",
                table: "Tratamientos");

            migrationBuilder.DropIndex(
                name: "IX_Tratamientos_id_paciente",
                table: "Tratamientos");

            migrationBuilder.DropColumn(
                name: "estado",
                table: "Tratamientos");

            migrationBuilder.DropColumn(
                name: "fecha_registro",
                table: "Tratamientos");

            migrationBuilder.DropColumn(
                name: "id_diagnostico",
                table: "Tratamientos");

            migrationBuilder.DropColumn(
                name: "id_doctor",
                table: "Tratamientos");

            migrationBuilder.DropColumn(
                name: "id_paciente",
                table: "Tratamientos");
        }
    }
}