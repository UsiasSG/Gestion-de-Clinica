using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace ClinicaBackend.API.Migrations
{
    /// <inheritdoc />
    public partial class HistorialPermisos : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.InsertData(
                table: "Modulos",
                columns: new[] { "id", "codigo", "nombre" },
                values: new object[] { 7, "DIAGNOSTICOS", "Diagnósticos" });

            migrationBuilder.InsertData(
                table: "Permisos",
                columns: new[] { "id", "id_modulo", "id_rol", "puede_consultar", "puede_crear", "puede_eliminar", "puede_modificar" },
                values: new object[,]
                {
                    { 8, 1, 2, true, true, false, true },
                    { 9, 2, 2, true, true, false, true },
                    { 11, 5, 2, true, false, false, true },
                    { 12, 3, 3, true, true, true, true },
                    { 13, 1, 3, true, false, false, false },
                    { 14, 6, 3, true, false, false, false }
                });

            migrationBuilder.InsertData(
                table: "Roles",
                columns: new[] { "id", "descripcion", "nombre" },
                values: new object[] { 4, "Exámenes, evolución de pacientes y habitaciones", "Enfermería" });

            migrationBuilder.InsertData(
                table: "Permisos",
                columns: new[] { "id", "id_modulo", "id_rol", "puede_consultar", "puede_crear", "puede_eliminar", "puede_modificar" },
                values: new object[,]
                {
                    { 7, 7, 1, true, true, true, true },
                    { 10, 7, 2, true, true, false, false },
                    { 15, 1, 4, true, false, false, false },
                    { 16, 2, 4, true, true, false, true },
                    { 17, 5, 4, true, false, false, true }
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(
                table: "Permisos",
                keyColumn: "id",
                keyValue: 7);

            migrationBuilder.DeleteData(
                table: "Permisos",
                keyColumn: "id",
                keyValue: 8);

            migrationBuilder.DeleteData(
                table: "Permisos",
                keyColumn: "id",
                keyValue: 9);

            migrationBuilder.DeleteData(
                table: "Permisos",
                keyColumn: "id",
                keyValue: 10);

            migrationBuilder.DeleteData(
                table: "Permisos",
                keyColumn: "id",
                keyValue: 11);

            migrationBuilder.DeleteData(
                table: "Permisos",
                keyColumn: "id",
                keyValue: 12);

            migrationBuilder.DeleteData(
                table: "Permisos",
                keyColumn: "id",
                keyValue: 13);

            migrationBuilder.DeleteData(
                table: "Permisos",
                keyColumn: "id",
                keyValue: 14);

            migrationBuilder.DeleteData(
                table: "Permisos",
                keyColumn: "id",
                keyValue: 15);

            migrationBuilder.DeleteData(
                table: "Permisos",
                keyColumn: "id",
                keyValue: 16);

            migrationBuilder.DeleteData(
                table: "Permisos",
                keyColumn: "id",
                keyValue: 17);

            migrationBuilder.DeleteData(
                table: "Modulos",
                keyColumn: "id",
                keyValue: 7);

            migrationBuilder.DeleteData(
                table: "Roles",
                keyColumn: "id",
                keyValue: 4);
        }
    }
}
