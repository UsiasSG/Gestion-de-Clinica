using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ClinicaBackend.API.Migrations
{
    /// <inheritdoc />
    public partial class RolesPermisosDinamicos : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "RolPermisos");

            migrationBuilder.DropIndex(
                name: "IX_Permisos_id_rol",
                table: "Permisos");

            migrationBuilder.InsertData(
                table: "Modulos",
                columns: new[] { "id", "codigo", "nombre" },
                values: new object[] { 8, "ROLES", "Roles y Permisos" });

            migrationBuilder.InsertData(
                table: "Roles",
                columns: new[] { "id", "descripcion", "nombre" },
                values: new object[] { 8, null, "Roles y Permisos" });

            migrationBuilder.InsertData(
                table: "Permisos",
                columns: new[] { "id", "id_modulo", "id_rol", "puede_consultar", "puede_crear", "puede_eliminar", "puede_modificar" },
                values: new object[] { 18, 8, 1, true, true, true, true });

            migrationBuilder.CreateIndex(
                name: "IX_Permisos_id_rol_id_modulo",
                table: "Permisos",
                columns: new[] { "id_rol", "id_modulo" },
                unique: true,
                filter: "[id_rol] IS NOT NULL AND [id_modulo] IS NOT NULL");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Permisos_id_rol_id_modulo",
                table: "Permisos");

            migrationBuilder.DeleteData(
                table: "Permisos",
                keyColumn: "id",
                keyValue: 18);

            migrationBuilder.DeleteData(
                table: "Roles",
                keyColumn: "id",
                keyValue: 8);

            migrationBuilder.DeleteData(
                table: "Modulos",
                keyColumn: "id",
                keyValue: 8);

            migrationBuilder.CreateTable(
                name: "RolPermisos",
                columns: table => new
                {
                    id_rol = table.Column<int>(type: "int", nullable: false),
                    id_permiso = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RolPermisos", x => new { x.id_rol, x.id_permiso });
                    table.ForeignKey(
                        name: "FK_RolPermisos_Permisos_id_permiso",
                        column: x => x.id_permiso,
                        principalTable: "Permisos",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_RolPermisos_Roles_id_rol",
                        column: x => x.id_rol,
                        principalTable: "Roles",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Permisos_id_rol",
                table: "Permisos",
                column: "id_rol");

            migrationBuilder.CreateIndex(
                name: "IX_RolPermisos_id_permiso",
                table: "RolPermisos",
                column: "id_permiso");
        }
    }
}
