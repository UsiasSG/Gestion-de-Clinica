using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace ClinicaBackend.API.Migrations
{
    /// <inheritdoc />
    public partial class Facturacion : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "id_servicio",
                table: "Examenes",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "id_servicio",
                table: "ConsultasMedicas",
                type: "int",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "FacturasPaciente",
                columns: table => new
                {
                    id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    id_paciente = table.Column<int>(type: "int", nullable: false),
                    id_sucursal = table.Column<int>(type: "int", nullable: true),
                    id_usuario = table.Column<int>(type: "int", nullable: true),
                    fecha = table.Column<DateTime>(type: "datetime2", nullable: false),
                    tipo = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    nit = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    nombre_factura = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: false),
                    subtotal = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    impuesto = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    total = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    metodo_pago = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    referencia = table.Column<string>(type: "nvarchar(60)", maxLength: 60, nullable: true),
                    monto_recibido = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: true),
                    cambio = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: true),
                    estado = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    motivo_anulacion = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FacturasPaciente", x => x.id);
                    table.ForeignKey(
                        name: "FK_FacturasPaciente_Sucursales_id_sucursal",
                        column: x => x.id_sucursal,
                        principalTable: "Sucursales",
                        principalColumn: "id");
                    table.ForeignKey(
                        name: "FK_FacturasPaciente_Usuarios_id_usuario",
                        column: x => x.id_usuario,
                        principalTable: "Usuarios",
                        principalColumn: "id");
                    table.ForeignKey(
                        name: "FK_FacturasPaciente_pacientes_id_paciente",
                        column: x => x.id_paciente,
                        principalTable: "pacientes",
                        principalColumn: "id");
                });

            migrationBuilder.CreateTable(
                name: "ServiciosClinicos",
                columns: table => new
                {
                    id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    codigo = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: true),
                    nombre = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: false),
                    tipo = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    descripcion = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: true),
                    precio = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    activo = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ServiciosClinicos", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "CargosPaciente",
                columns: table => new
                {
                    id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    id_paciente = table.Column<int>(type: "int", nullable: false),
                    tipo = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    descripcion = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: false),
                    cantidad = table.Column<int>(type: "int", nullable: false),
                    precio_unitario = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    subtotal = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    fecha = table.Column<DateTime>(type: "datetime2", nullable: false),
                    id_servicio = table.Column<int>(type: "int", nullable: true),
                    id_consulta = table.Column<int>(type: "int", nullable: true),
                    id_examen = table.Column<int>(type: "int", nullable: true),
                    id_usuario = table.Column<int>(type: "int", nullable: true),
                    estado = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    id_factura = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CargosPaciente", x => x.id);
                    table.ForeignKey(
                        name: "FK_CargosPaciente_ConsultasMedicas_id_consulta",
                        column: x => x.id_consulta,
                        principalTable: "ConsultasMedicas",
                        principalColumn: "id");
                    table.ForeignKey(
                        name: "FK_CargosPaciente_Examenes_id_examen",
                        column: x => x.id_examen,
                        principalTable: "Examenes",
                        principalColumn: "id");
                    table.ForeignKey(
                        name: "FK_CargosPaciente_FacturasPaciente_id_factura",
                        column: x => x.id_factura,
                        principalTable: "FacturasPaciente",
                        principalColumn: "id");
                    table.ForeignKey(
                        name: "FK_CargosPaciente_ServiciosClinicos_id_servicio",
                        column: x => x.id_servicio,
                        principalTable: "ServiciosClinicos",
                        principalColumn: "id");
                    table.ForeignKey(
                        name: "FK_CargosPaciente_Usuarios_id_usuario",
                        column: x => x.id_usuario,
                        principalTable: "Usuarios",
                        principalColumn: "id");
                    table.ForeignKey(
                        name: "FK_CargosPaciente_pacientes_id_paciente",
                        column: x => x.id_paciente,
                        principalTable: "pacientes",
                        principalColumn: "id");
                });

            migrationBuilder.InsertData(
                table: "Modulos",
                columns: new[] { "id", "codigo", "nombre" },
                values: new object[,]
                {
                    { 9, "TARIFARIO", "Tarifario de Servicios" },
                    { 10, "COBROS", "Caja y Cobros" }
                });

            migrationBuilder.InsertData(
                table: "ServiciosClinicos",
                columns: new[] { "id", "activo", "codigo", "descripcion", "nombre", "precio", "tipo" },
                values: new object[,]
                {
                    { 1, true, "CON-GEN", null, "Consulta general", 150m, "Consulta" },
                    { 2, true, "CON-ESP", null, "Consulta de especialidad", 250m, "Consulta" },
                    { 3, true, "CON-EMG", null, "Consulta de emergencia", 350m, "Consulta" },
                    { 4, true, "EX-HEM", null, "Hemograma completo", 75m, "Examen" },
                    { 5, true, "EX-QS", null, "Química sanguínea", 120m, "Examen" },
                    { 6, true, "EX-ORI", null, "Examen general de orina", 50m, "Examen" },
                    { 7, true, "EX-RXT", null, "Radiografía de tórax", 200m, "Examen" },
                    { 8, true, "EX-USG", null, "Ultrasonido abdominal", 350m, "Examen" },
                    { 9, true, "PRO-CUR", null, "Curación de herida", 100m, "Procedimiento" },
                    { 10, true, "PRO-SUT", null, "Sutura", 175m, "Procedimiento" },
                    { 11, true, "PRO-NEB", null, "Nebulización", 60m, "Procedimiento" },
                    { 12, true, "CIR-APE", null, "Apendicectomía", 8500m, "Cirugía" },
                    { 13, true, "CIR-COL", null, "Colecistectomía laparoscópica", 12000m, "Cirugía" },
                    { 14, true, "CIR-HER", null, "Hernioplastia", 7000m, "Cirugía" },
                    { 15, true, "CIR-CES", null, "Cesárea", 9500m, "Cirugía" }
                });

            migrationBuilder.InsertData(
                table: "Permisos",
                columns: new[] { "id", "id_modulo", "id_rol", "puede_consultar", "puede_crear", "puede_eliminar", "puede_modificar" },
                values: new object[,]
                {
                    { 19, 9, 1, true, true, true, true },
                    { 20, 10, 1, true, true, true, true },
                    { 21, 10, 3, true, true, false, false },
                    { 22, 9, 3, true, false, false, false }
                });

            migrationBuilder.CreateIndex(
                name: "IX_Examenes_id_servicio",
                table: "Examenes",
                column: "id_servicio");

            migrationBuilder.CreateIndex(
                name: "IX_ConsultasMedicas_id_servicio",
                table: "ConsultasMedicas",
                column: "id_servicio");

            migrationBuilder.CreateIndex(
                name: "IX_CargosPaciente_id_consulta",
                table: "CargosPaciente",
                column: "id_consulta");

            migrationBuilder.CreateIndex(
                name: "IX_CargosPaciente_id_examen",
                table: "CargosPaciente",
                column: "id_examen");

            migrationBuilder.CreateIndex(
                name: "IX_CargosPaciente_id_factura",
                table: "CargosPaciente",
                column: "id_factura");

            migrationBuilder.CreateIndex(
                name: "IX_CargosPaciente_id_paciente",
                table: "CargosPaciente",
                column: "id_paciente");

            migrationBuilder.CreateIndex(
                name: "IX_CargosPaciente_id_servicio",
                table: "CargosPaciente",
                column: "id_servicio");

            migrationBuilder.CreateIndex(
                name: "IX_CargosPaciente_id_usuario",
                table: "CargosPaciente",
                column: "id_usuario");

            migrationBuilder.CreateIndex(
                name: "IX_FacturasPaciente_id_paciente",
                table: "FacturasPaciente",
                column: "id_paciente");

            migrationBuilder.CreateIndex(
                name: "IX_FacturasPaciente_id_sucursal",
                table: "FacturasPaciente",
                column: "id_sucursal");

            migrationBuilder.CreateIndex(
                name: "IX_FacturasPaciente_id_usuario",
                table: "FacturasPaciente",
                column: "id_usuario");

            migrationBuilder.CreateIndex(
                name: "IX_ServiciosClinicos_codigo",
                table: "ServiciosClinicos",
                column: "codigo",
                unique: true,
                filter: "[codigo] IS NOT NULL");

            migrationBuilder.AddForeignKey(
                name: "FK_ConsultasMedicas_ServiciosClinicos_id_servicio",
                table: "ConsultasMedicas",
                column: "id_servicio",
                principalTable: "ServiciosClinicos",
                principalColumn: "id");

            migrationBuilder.AddForeignKey(
                name: "FK_Examenes_ServiciosClinicos_id_servicio",
                table: "Examenes",
                column: "id_servicio",
                principalTable: "ServiciosClinicos",
                principalColumn: "id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_ConsultasMedicas_ServiciosClinicos_id_servicio",
                table: "ConsultasMedicas");

            migrationBuilder.DropForeignKey(
                name: "FK_Examenes_ServiciosClinicos_id_servicio",
                table: "Examenes");

            migrationBuilder.DropTable(
                name: "CargosPaciente");

            migrationBuilder.DropTable(
                name: "FacturasPaciente");

            migrationBuilder.DropTable(
                name: "ServiciosClinicos");

            migrationBuilder.DropIndex(
                name: "IX_Examenes_id_servicio",
                table: "Examenes");

            migrationBuilder.DropIndex(
                name: "IX_ConsultasMedicas_id_servicio",
                table: "ConsultasMedicas");

            migrationBuilder.DeleteData(
                table: "Permisos",
                keyColumn: "id",
                keyValue: 19);

            migrationBuilder.DeleteData(
                table: "Permisos",
                keyColumn: "id",
                keyValue: 20);

            migrationBuilder.DeleteData(
                table: "Permisos",
                keyColumn: "id",
                keyValue: 21);

            migrationBuilder.DeleteData(
                table: "Permisos",
                keyColumn: "id",
                keyValue: 22);

            migrationBuilder.DeleteData(
                table: "Modulos",
                keyColumn: "id",
                keyValue: 9);

            migrationBuilder.DeleteData(
                table: "Modulos",
                keyColumn: "id",
                keyValue: 10);

            migrationBuilder.DropColumn(
                name: "id_servicio",
                table: "Examenes");

            migrationBuilder.DropColumn(
                name: "id_servicio",
                table: "ConsultasMedicas");
        }
    }
}
