using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ClinicaBackend.API.Migrations
{
    /// <inheritdoc />
    public partial class ArchivosClinicos : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ArchivosClinicos",
                columns: table => new
                {
                    id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    id_paciente = table.Column<int>(type: "int", nullable: false),
                    id_examen = table.Column<int>(type: "int", nullable: true),
                    nombre_archivo = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    tipo_contenido = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    tamano_bytes = table.Column<long>(type: "bigint", nullable: false),
                    contenido = table.Column<byte[]>(type: "varbinary(max)", nullable: false),
                    categoria = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    descripcion = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: true),
                    fecha_subida = table.Column<DateTime>(type: "datetime2", nullable: false),
                    id_usuario = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ArchivosClinicos", x => x.id);
                    table.ForeignKey(
                        name: "FK_ArchivosClinicos_Examenes_id_examen",
                        column: x => x.id_examen,
                        principalTable: "Examenes",
                        principalColumn: "id");
                    table.ForeignKey(
                        name: "FK_ArchivosClinicos_Usuarios_id_usuario",
                        column: x => x.id_usuario,
                        principalTable: "Usuarios",
                        principalColumn: "id");
                    table.ForeignKey(
                        name: "FK_ArchivosClinicos_pacientes_id_paciente",
                        column: x => x.id_paciente,
                        principalTable: "pacientes",
                        principalColumn: "id");
                });

            migrationBuilder.CreateIndex(
                name: "IX_ArchivosClinicos_id_examen",
                table: "ArchivosClinicos",
                column: "id_examen");

            migrationBuilder.CreateIndex(
                name: "IX_ArchivosClinicos_id_paciente",
                table: "ArchivosClinicos",
                column: "id_paciente");

            migrationBuilder.CreateIndex(
                name: "IX_ArchivosClinicos_id_usuario",
                table: "ArchivosClinicos",
                column: "id_usuario");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ArchivosClinicos");
        }
    }
}
