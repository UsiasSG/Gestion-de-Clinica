using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ClinicaBackend.API.Migrations
{
    /// <inheritdoc />
    public partial class InventarioMovimientos : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "activo",
                table: "Medicamentos",
                type: "bit",
                nullable: false,
                defaultValue: true);

            migrationBuilder.AddColumn<string>(
                name: "codigo",
                table: "Medicamentos",
                type: "nvarchar(30)",
                maxLength: 30,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "stock_minimo",
                table: "Medicamentos",
                type: "int",
                nullable: false,
                defaultValue: 10);

            migrationBuilder.CreateTable(
                name: "MovimientosInventario",
                columns: table => new
                {
                    id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    id_lote = table.Column<int>(type: "int", nullable: false),
                    tipo = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    cantidad = table.Column<int>(type: "int", nullable: false),
                    fecha = table.Column<DateTime>(type: "datetime2", nullable: false),
                    id_usuario = table.Column<int>(type: "int", nullable: true),
                    id_venta = table.Column<int>(type: "int", nullable: true),
                    observacion = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MovimientosInventario", x => x.id);
                    table.ForeignKey(
                        name: "FK_MovimientosInventario_LotesMedicamentos_id_lote",
                        column: x => x.id_lote,
                        principalTable: "LotesMedicamentos",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_MovimientosInventario_Usuarios_id_usuario",
                        column: x => x.id_usuario,
                        principalTable: "Usuarios",
                        principalColumn: "id");
                    table.ForeignKey(
                        name: "FK_MovimientosInventario_Ventas_id_venta",
                        column: x => x.id_venta,
                        principalTable: "Ventas",
                        principalColumn: "id");
                });

            migrationBuilder.InsertData(
                table: "Modulos",
                columns: new[] { "id", "codigo", "nombre" },
                values: new object[] { 6, "INVENTARIO", "Inventario de Medicamentos" });

            migrationBuilder.InsertData(
                table: "Permisos",
                columns: new[] { "id", "id_modulo", "id_rol", "puede_consultar", "puede_crear", "puede_eliminar", "puede_modificar" },
                values: new object[] { 6, 6, 1, true, true, true, true });

            migrationBuilder.CreateIndex(
                name: "IX_Medicamentos_codigo",
                table: "Medicamentos",
                column: "codigo",
                unique: true,
                filter: "[codigo] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_MovimientosInventario_fecha",
                table: "MovimientosInventario",
                column: "fecha");

            migrationBuilder.CreateIndex(
                name: "IX_MovimientosInventario_id_lote",
                table: "MovimientosInventario",
                column: "id_lote");

            migrationBuilder.CreateIndex(
                name: "IX_MovimientosInventario_id_usuario",
                table: "MovimientosInventario",
                column: "id_usuario");

            migrationBuilder.CreateIndex(
                name: "IX_MovimientosInventario_id_venta",
                table: "MovimientosInventario",
                column: "id_venta");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "MovimientosInventario");

            migrationBuilder.DropIndex(
                name: "IX_Medicamentos_codigo",
                table: "Medicamentos");

            migrationBuilder.DeleteData(
                table: "Permisos",
                keyColumn: "id",
                keyValue: 6);

            migrationBuilder.DeleteData(
                table: "Modulos",
                keyColumn: "id",
                keyValue: 6);

            migrationBuilder.DropColumn(
                name: "activo",
                table: "Medicamentos");

            migrationBuilder.DropColumn(
                name: "codigo",
                table: "Medicamentos");

            migrationBuilder.DropColumn(
                name: "stock_minimo",
                table: "Medicamentos");
        }
    }
}
