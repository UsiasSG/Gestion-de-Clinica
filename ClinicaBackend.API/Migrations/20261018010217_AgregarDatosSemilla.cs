using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace ClinicaBackend.API.Migrations
{
    /// <inheritdoc />
    public partial class AgregarDatosSemilla : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.InsertData(
                table: "Especialidades",
                columns: new[] { "id", "descripcion", "nombre" },
                values: new object[,]
                {
                    { 1, "Atención médica primaria", "Medicina General" },
                    { 2, "Atención a niños y adolescentes", "Pediatría" },
                    { 3, "Salud de la mujer", "Ginecología" }
                });

            migrationBuilder.InsertData(
                table: "Modulos",
                columns: new[] { "id", "codigo", "nombre" },
                values: new object[,]
                {
                    { 1, "PACIENTES", "Gestión de Pacientes" },
                    { 2, "HISTORIAL", "Historial Clínico" },
                    { 3, "POS", "Punto de Venta" },
                    { 4, "PERSONAL", "Gestión de Personal" },
                    { 5, "HABITACIONES", "Habitaciones (SignalR)" }
                });

            migrationBuilder.InsertData(
                table: "Roles",
                columns: new[] { "id", "descripcion", "nombre" },
                values: new object[,]
                {
                    { 1, "Acceso total al sistema", "Administrador" },
                    { 2, "Atención de consultas e historial clínico", "Médico" },
                    { 3, "Operaciones de Punto de Venta / POS", "Cajero" }
                });

            migrationBuilder.InsertData(
                table: "Sucursales",
                columns: new[] { "id", "direccion", "estado", "fecha_registro", "nombre", "telefono" },
                values: new object[] { 1, "Avenida Las Américas 14-20, Zona 14", true, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), "Sucursal Central - Zona 10", "2222-1111" });

            migrationBuilder.InsertData(
                table: "Permisos",
                columns: new[] { "id", "id_modulo", "id_rol", "puede_consultar", "puede_crear", "puede_eliminar", "puede_modificar" },
                values: new object[,]
                {
                    { 1, 1, 1, true, true, true, true },
                    { 2, 2, 1, true, true, true, true },
                    { 3, 3, 1, true, true, true, true },
                    { 4, 4, 1, true, true, true, true },
                    { 5, 5, 1, true, true, true, true }
                });

            migrationBuilder.InsertData(
                table: "Usuarios",
                columns: new[] { "id", "correo", "estado", "fecha_creacion", "id_rol", "id_sucursal", "nombre_usuario", "password_hash" },
                values: new object[] { 1, "admin@clinica.com", true, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), 1, 1, "admin", "admin123" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(
                table: "Especialidades",
                keyColumn: "id",
                keyValue: 1);

            migrationBuilder.DeleteData(
                table: "Especialidades",
                keyColumn: "id",
                keyValue: 2);

            migrationBuilder.DeleteData(
                table: "Especialidades",
                keyColumn: "id",
                keyValue: 3);

            migrationBuilder.DeleteData(
                table: "Permisos",
                keyColumn: "id",
                keyValue: 1);

            migrationBuilder.DeleteData(
                table: "Permisos",
                keyColumn: "id",
                keyValue: 2);

            migrationBuilder.DeleteData(
                table: "Permisos",
                keyColumn: "id",
                keyValue: 3);

            migrationBuilder.DeleteData(
                table: "Permisos",
                keyColumn: "id",
                keyValue: 4);

            migrationBuilder.DeleteData(
                table: "Permisos",
                keyColumn: "id",
                keyValue: 5);

            migrationBuilder.DeleteData(
                table: "Roles",
                keyColumn: "id",
                keyValue: 2);

            migrationBuilder.DeleteData(
                table: "Roles",
                keyColumn: "id",
                keyValue: 3);

            migrationBuilder.DeleteData(
                table: "Usuarios",
                keyColumn: "id",
                keyValue: 1);

            migrationBuilder.DeleteData(
                table: "Modulos",
                keyColumn: "id",
                keyValue: 1);

            migrationBuilder.DeleteData(
                table: "Modulos",
                keyColumn: "id",
                keyValue: 2);

            migrationBuilder.DeleteData(
                table: "Modulos",
                keyColumn: "id",
                keyValue: 3);

            migrationBuilder.DeleteData(
                table: "Modulos",
                keyColumn: "id",
                keyValue: 4);

            migrationBuilder.DeleteData(
                table: "Modulos",
                keyColumn: "id",
                keyValue: 5);

            migrationBuilder.DeleteData(
                table: "Roles",
                keyColumn: "id",
                keyValue: 1);

            migrationBuilder.DeleteData(
                table: "Sucursales",
                keyColumn: "id",
                keyValue: 1);
        }
    }
}
