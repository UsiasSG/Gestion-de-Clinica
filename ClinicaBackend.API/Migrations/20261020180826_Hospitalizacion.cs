using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace ClinicaBackend.API.Migrations
{
    /// <inheritdoc />
    public partial class Hospitalizacion : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "id_hospitalizacion",
                table: "FacturasPaciente",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "id_cirugia",
                table: "CargosPaciente",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "id_hospitalizacion",
                table: "CargosPaciente",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "id_hospitalizacion",
                table: "AsignacionHabitaciones",
                type: "int",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "Hospitalizaciones",
                columns: table => new
                {
                    id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    id_paciente = table.Column<int>(type: "int", nullable: false),
                    id_sucursal = table.Column<int>(type: "int", nullable: true),
                    id_diagnostico = table.Column<int>(type: "int", nullable: true),
                    id_medico_responsable = table.Column<int>(type: "int", nullable: true),
                    tipo = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    motivo = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    estado = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    fecha_solicitud = table.Column<DateTime>(type: "datetime2", nullable: false),
                    fecha_ingreso = table.Column<DateTime>(type: "datetime2", nullable: true),
                    fecha_alta = table.Column<DateTime>(type: "datetime2", nullable: true),
                    observaciones_alta = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    id_usuario_registro = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Hospitalizaciones", x => x.id);
                    table.ForeignKey(
                        name: "FK_Hospitalizaciones_Diagnosticos_id_diagnostico",
                        column: x => x.id_diagnostico,
                        principalTable: "Diagnosticos",
                        principalColumn: "id");
                    table.ForeignKey(
                        name: "FK_Hospitalizaciones_Empleados_id_medico_responsable",
                        column: x => x.id_medico_responsable,
                        principalTable: "Empleados",
                        principalColumn: "id");
                    table.ForeignKey(
                        name: "FK_Hospitalizaciones_Sucursales_id_sucursal",
                        column: x => x.id_sucursal,
                        principalTable: "Sucursales",
                        principalColumn: "id");
                    table.ForeignKey(
                        name: "FK_Hospitalizaciones_Usuarios_id_usuario_registro",
                        column: x => x.id_usuario_registro,
                        principalTable: "Usuarios",
                        principalColumn: "id");
                    table.ForeignKey(
                        name: "FK_Hospitalizaciones_pacientes_id_paciente",
                        column: x => x.id_paciente,
                        principalTable: "pacientes",
                        principalColumn: "id");
                });

            migrationBuilder.CreateTable(
                name: "Cirugias",
                columns: table => new
                {
                    id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    id_hospitalizacion = table.Column<int>(type: "int", nullable: false),
                    id_servicio = table.Column<int>(type: "int", nullable: true),
                    procedimiento = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: false),
                    id_cirujano = table.Column<int>(type: "int", nullable: true),
                    fecha_programada = table.Column<DateTime>(type: "datetime2", nullable: false),
                    fecha_realizada = table.Column<DateTime>(type: "datetime2", nullable: true),
                    estado = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    costo_base = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    costo_final = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    observaciones = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Cirugias", x => x.id);
                    table.ForeignKey(
                        name: "FK_Cirugias_Empleados_id_cirujano",
                        column: x => x.id_cirujano,
                        principalTable: "Empleados",
                        principalColumn: "id");
                    table.ForeignKey(
                        name: "FK_Cirugias_Hospitalizaciones_id_hospitalizacion",
                        column: x => x.id_hospitalizacion,
                        principalTable: "Hospitalizaciones",
                        principalColumn: "id");
                    table.ForeignKey(
                        name: "FK_Cirugias_ServiciosClinicos_id_servicio",
                        column: x => x.id_servicio,
                        principalTable: "ServiciosClinicos",
                        principalColumn: "id");
                });

            migrationBuilder.InsertData(
                table: "Modulos",
                columns: new[] { "id", "codigo", "nombre" },
                values: new object[] { 11, "HOSPITALIZACION", "Hospitalización y Cirugías" });

            migrationBuilder.InsertData(
                table: "Permisos",
                columns: new[] { "id", "id_modulo", "id_rol", "puede_consultar", "puede_crear", "puede_eliminar", "puede_modificar" },
                values: new object[,]
                {
                    { 23, 11, 1, true, true, true, true },
                    { 24, 11, 2, true, true, false, true },
                    { 25, 11, 4, true, false, false, true }
                });

            migrationBuilder.CreateIndex(
                name: "IX_FacturasPaciente_id_hospitalizacion",
                table: "FacturasPaciente",
                column: "id_hospitalizacion");

            migrationBuilder.CreateIndex(
                name: "IX_CargosPaciente_id_cirugia",
                table: "CargosPaciente",
                column: "id_cirugia");

            migrationBuilder.CreateIndex(
                name: "IX_CargosPaciente_id_hospitalizacion",
                table: "CargosPaciente",
                column: "id_hospitalizacion");

            migrationBuilder.CreateIndex(
                name: "IX_AsignacionHabitaciones_id_hospitalizacion",
                table: "AsignacionHabitaciones",
                column: "id_hospitalizacion");

            migrationBuilder.CreateIndex(
                name: "IX_Cirugias_id_cirujano",
                table: "Cirugias",
                column: "id_cirujano");

            migrationBuilder.CreateIndex(
                name: "IX_Cirugias_id_hospitalizacion",
                table: "Cirugias",
                column: "id_hospitalizacion");

            migrationBuilder.CreateIndex(
                name: "IX_Cirugias_id_servicio",
                table: "Cirugias",
                column: "id_servicio");

            migrationBuilder.CreateIndex(
                name: "IX_Hospitalizaciones_id_diagnostico",
                table: "Hospitalizaciones",
                column: "id_diagnostico");

            migrationBuilder.CreateIndex(
                name: "IX_Hospitalizaciones_id_medico_responsable",
                table: "Hospitalizaciones",
                column: "id_medico_responsable");

            migrationBuilder.CreateIndex(
                name: "IX_Hospitalizaciones_id_paciente",
                table: "Hospitalizaciones",
                column: "id_paciente");

            migrationBuilder.CreateIndex(
                name: "IX_Hospitalizaciones_id_sucursal",
                table: "Hospitalizaciones",
                column: "id_sucursal");

            migrationBuilder.CreateIndex(
                name: "IX_Hospitalizaciones_id_usuario_registro",
                table: "Hospitalizaciones",
                column: "id_usuario_registro");

            migrationBuilder.AddForeignKey(
                name: "FK_AsignacionHabitaciones_Hospitalizaciones_id_hospitalizacion",
                table: "AsignacionHabitaciones",
                column: "id_hospitalizacion",
                principalTable: "Hospitalizaciones",
                principalColumn: "id");

            migrationBuilder.AddForeignKey(
                name: "FK_CargosPaciente_Cirugias_id_cirugia",
                table: "CargosPaciente",
                column: "id_cirugia",
                principalTable: "Cirugias",
                principalColumn: "id");

            migrationBuilder.AddForeignKey(
                name: "FK_CargosPaciente_Hospitalizaciones_id_hospitalizacion",
                table: "CargosPaciente",
                column: "id_hospitalizacion",
                principalTable: "Hospitalizaciones",
                principalColumn: "id");

            migrationBuilder.AddForeignKey(
                name: "FK_FacturasPaciente_Hospitalizaciones_id_hospitalizacion",
                table: "FacturasPaciente",
                column: "id_hospitalizacion",
                principalTable: "Hospitalizaciones",
                principalColumn: "id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_AsignacionHabitaciones_Hospitalizaciones_id_hospitalizacion",
                table: "AsignacionHabitaciones");

            migrationBuilder.DropForeignKey(
                name: "FK_CargosPaciente_Cirugias_id_cirugia",
                table: "CargosPaciente");

            migrationBuilder.DropForeignKey(
                name: "FK_CargosPaciente_Hospitalizaciones_id_hospitalizacion",
                table: "CargosPaciente");

            migrationBuilder.DropForeignKey(
                name: "FK_FacturasPaciente_Hospitalizaciones_id_hospitalizacion",
                table: "FacturasPaciente");

            migrationBuilder.DropTable(
                name: "Cirugias");

            migrationBuilder.DropTable(
                name: "Hospitalizaciones");

            migrationBuilder.DropIndex(
                name: "IX_FacturasPaciente_id_hospitalizacion",
                table: "FacturasPaciente");

            migrationBuilder.DropIndex(
                name: "IX_CargosPaciente_id_cirugia",
                table: "CargosPaciente");

            migrationBuilder.DropIndex(
                name: "IX_CargosPaciente_id_hospitalizacion",
                table: "CargosPaciente");

            migrationBuilder.DropIndex(
                name: "IX_AsignacionHabitaciones_id_hospitalizacion",
                table: "AsignacionHabitaciones");

            migrationBuilder.DeleteData(
                table: "Permisos",
                keyColumn: "id",
                keyValue: 23);

            migrationBuilder.DeleteData(
                table: "Permisos",
                keyColumn: "id",
                keyValue: 24);

            migrationBuilder.DeleteData(
                table: "Permisos",
                keyColumn: "id",
                keyValue: 25);

            migrationBuilder.DeleteData(
                table: "Modulos",
                keyColumn: "id",
                keyValue: 11);

            migrationBuilder.DropColumn(
                name: "id_hospitalizacion",
                table: "FacturasPaciente");

            migrationBuilder.DropColumn(
                name: "id_cirugia",
                table: "CargosPaciente");

            migrationBuilder.DropColumn(
                name: "id_hospitalizacion",
                table: "CargosPaciente");

            migrationBuilder.DropColumn(
                name: "id_hospitalizacion",
                table: "AsignacionHabitaciones");
        }
    }
}
