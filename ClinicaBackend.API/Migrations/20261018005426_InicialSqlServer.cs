using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ClinicaBackend.API.Migrations
{
    /// <inheritdoc />
    public partial class InicialSqlServer : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "CategoriasMedicamentos",
                columns: table => new
                {
                    id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    nombre = table.Column<string>(type: "nvarchar(max)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CategoriasMedicamentos", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "Especialidades",
                columns: table => new
                {
                    id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    nombre = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    descripcion = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Especialidades", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "Modulos",
                columns: table => new
                {
                    id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    nombre = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    codigo = table.Column<string>(type: "nvarchar(max)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Modulos", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "pacientes",
                columns: table => new
                {
                    id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    documento_identificacion = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    nombres = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    apellidos = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    fecha_nacimiento = table.Column<DateTime>(type: "datetime2", nullable: false),
                    genero = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    telefono = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    direccion = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    tipo_sangre = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    alergias = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    es_menor_edad = table.Column<bool>(type: "bit", nullable: false),
                    nombre_encargado = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    dpi_encargado = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    contacto_emergencia = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    fecha_registro = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_pacientes", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "Roles",
                columns: table => new
                {
                    id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    nombre = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    descripcion = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Roles", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "Sucursales",
                columns: table => new
                {
                    id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    nombre = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    direccion = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    telefono = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    estado = table.Column<bool>(type: "bit", nullable: false),
                    fecha_registro = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Sucursales", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "Medicamentos",
                columns: table => new
                {
                    id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    id_categoria = table.Column<int>(type: "int", nullable: true),
                    nombre = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    descripcion = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    marca = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    precio_venta = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    requiere_receta = table.Column<bool>(type: "bit", nullable: false),
                    imagen_url = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Medicamentos", x => x.id);
                    table.ForeignKey(
                        name: "FK_Medicamentos_CategoriasMedicamentos_id_categoria",
                        column: x => x.id_categoria,
                        principalTable: "CategoriasMedicamentos",
                        principalColumn: "id");
                });

            migrationBuilder.CreateTable(
                name: "Permisos",
                columns: table => new
                {
                    id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    id_rol = table.Column<int>(type: "int", nullable: true),
                    id_modulo = table.Column<int>(type: "int", nullable: true),
                    puede_consultar = table.Column<bool>(type: "bit", nullable: false),
                    puede_crear = table.Column<bool>(type: "bit", nullable: false),
                    puede_modificar = table.Column<bool>(type: "bit", nullable: false),
                    puede_eliminar = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Permisos", x => x.id);
                    table.ForeignKey(
                        name: "FK_Permisos_Modulos_id_modulo",
                        column: x => x.id_modulo,
                        principalTable: "Modulos",
                        principalColumn: "id");
                    table.ForeignKey(
                        name: "FK_Permisos_Roles_id_rol",
                        column: x => x.id_rol,
                        principalTable: "Roles",
                        principalColumn: "id");
                });

            migrationBuilder.CreateTable(
                name: "Habitaciones",
                columns: table => new
                {
                    id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    id_sucursal = table.Column<int>(type: "int", nullable: true),
                    numero_habitacion = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    tipo_habitacion = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    estado = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    precio_diario = table.Column<decimal>(type: "decimal(18,2)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Habitaciones", x => x.id);
                    table.ForeignKey(
                        name: "FK_Habitaciones_Sucursales_id_sucursal",
                        column: x => x.id_sucursal,
                        principalTable: "Sucursales",
                        principalColumn: "id");
                });

            migrationBuilder.CreateTable(
                name: "Usuarios",
                columns: table => new
                {
                    id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    id_sucursal = table.Column<int>(type: "int", nullable: true),
                    id_rol = table.Column<int>(type: "int", nullable: true),
                    nombre_usuario = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    correo = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    password_hash = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    estado = table.Column<bool>(type: "bit", nullable: false),
                    fecha_creacion = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Usuarios", x => x.id);
                    table.ForeignKey(
                        name: "FK_Usuarios_Roles_id_rol",
                        column: x => x.id_rol,
                        principalTable: "Roles",
                        principalColumn: "id");
                    table.ForeignKey(
                        name: "FK_Usuarios_Sucursales_id_sucursal",
                        column: x => x.id_sucursal,
                        principalTable: "Sucursales",
                        principalColumn: "id");
                });

            migrationBuilder.CreateTable(
                name: "LotesMedicamentos",
                columns: table => new
                {
                    id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    id_medicamento = table.Column<int>(type: "int", nullable: true),
                    id_sucursal = table.Column<int>(type: "int", nullable: true),
                    numero_lote = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    fecha_ingreso = table.Column<DateTime>(type: "datetime2", nullable: false),
                    fecha_vencimiento = table.Column<DateTime>(type: "datetime2", nullable: false),
                    cantidad_inicial = table.Column<int>(type: "int", nullable: false),
                    cantidad_disponible = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LotesMedicamentos", x => x.id);
                    table.ForeignKey(
                        name: "FK_LotesMedicamentos_Medicamentos_id_medicamento",
                        column: x => x.id_medicamento,
                        principalTable: "Medicamentos",
                        principalColumn: "id");
                    table.ForeignKey(
                        name: "FK_LotesMedicamentos_Sucursales_id_sucursal",
                        column: x => x.id_sucursal,
                        principalTable: "Sucursales",
                        principalColumn: "id");
                });

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

            migrationBuilder.CreateTable(
                name: "AsignacionHabitaciones",
                columns: table => new
                {
                    id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    id_habitacion = table.Column<int>(type: "int", nullable: true),
                    id_paciente = table.Column<int>(type: "int", nullable: true),
                    fecha_ingreso = table.Column<DateTime>(type: "datetime2", nullable: false),
                    fecha_egreso = table.Column<DateTime>(type: "datetime2", nullable: true),
                    observaciones = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    estado = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AsignacionHabitaciones", x => x.id);
                    table.ForeignKey(
                        name: "FK_AsignacionHabitaciones_Habitaciones_id_habitacion",
                        column: x => x.id_habitacion,
                        principalTable: "Habitaciones",
                        principalColumn: "id");
                    table.ForeignKey(
                        name: "FK_AsignacionHabitaciones_pacientes_id_paciente",
                        column: x => x.id_paciente,
                        principalTable: "pacientes",
                        principalColumn: "id");
                });

            migrationBuilder.CreateTable(
                name: "Empleados",
                columns: table => new
                {
                    id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    id_usuario = table.Column<int>(type: "int", nullable: true),
                    id_sucursal = table.Column<int>(type: "int", nullable: true),
                    id_especialidad = table.Column<int>(type: "int", nullable: true),
                    nombres = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    apellidos = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    tipo_empleado = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    telefono = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    correo = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Empleados", x => x.id);
                    table.ForeignKey(
                        name: "FK_Empleados_Especialidades_id_especialidad",
                        column: x => x.id_especialidad,
                        principalTable: "Especialidades",
                        principalColumn: "id");
                    table.ForeignKey(
                        name: "FK_Empleados_Sucursales_id_sucursal",
                        column: x => x.id_sucursal,
                        principalTable: "Sucursales",
                        principalColumn: "id");
                    table.ForeignKey(
                        name: "FK_Empleados_Usuarios_id_usuario",
                        column: x => x.id_usuario,
                        principalTable: "Usuarios",
                        principalColumn: "id");
                });

            migrationBuilder.CreateTable(
                name: "Ventas",
                columns: table => new
                {
                    id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    id_sucursal = table.Column<int>(type: "int", nullable: true),
                    id_usuario_vendedor = table.Column<int>(type: "int", nullable: true),
                    id_paciente = table.Column<int>(type: "int", nullable: true),
                    fecha_venta = table.Column<DateTime>(type: "datetime2", nullable: false),
                    subtotal = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    impuesto = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    total = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    estado = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Ventas", x => x.id);
                    table.ForeignKey(
                        name: "FK_Ventas_Sucursales_id_sucursal",
                        column: x => x.id_sucursal,
                        principalTable: "Sucursales",
                        principalColumn: "id");
                    table.ForeignKey(
                        name: "FK_Ventas_Usuarios_id_usuario_vendedor",
                        column: x => x.id_usuario_vendedor,
                        principalTable: "Usuarios",
                        principalColumn: "id");
                    table.ForeignKey(
                        name: "FK_Ventas_pacientes_id_paciente",
                        column: x => x.id_paciente,
                        principalTable: "pacientes",
                        principalColumn: "id");
                });

            migrationBuilder.CreateTable(
                name: "ConsultasMedicas",
                columns: table => new
                {
                    id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    id_paciente = table.Column<int>(type: "int", nullable: true),
                    id_doctor = table.Column<int>(type: "int", nullable: true),
                    id_sucursal = table.Column<int>(type: "int", nullable: true),
                    fecha_consulta = table.Column<DateTime>(type: "datetime2", nullable: false),
                    motivo_consulta = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    sintomatologia = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    observaciones = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ConsultasMedicas", x => x.id);
                    table.ForeignKey(
                        name: "FK_ConsultasMedicas_Empleados_id_doctor",
                        column: x => x.id_doctor,
                        principalTable: "Empleados",
                        principalColumn: "id");
                    table.ForeignKey(
                        name: "FK_ConsultasMedicas_Sucursales_id_sucursal",
                        column: x => x.id_sucursal,
                        principalTable: "Sucursales",
                        principalColumn: "id");
                    table.ForeignKey(
                        name: "FK_ConsultasMedicas_pacientes_id_paciente",
                        column: x => x.id_paciente,
                        principalTable: "pacientes",
                        principalColumn: "id");
                });

            migrationBuilder.CreateTable(
                name: "EvolucionPaciente",
                columns: table => new
                {
                    id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    id_paciente = table.Column<int>(type: "int", nullable: true),
                    id_doctor = table.Column<int>(type: "int", nullable: true),
                    fecha_registro = table.Column<DateTime>(type: "datetime2", nullable: false),
                    nota_evolucion = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    estado_salud = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EvolucionPaciente", x => x.id);
                    table.ForeignKey(
                        name: "FK_EvolucionPaciente_Empleados_id_doctor",
                        column: x => x.id_doctor,
                        principalTable: "Empleados",
                        principalColumn: "id");
                    table.ForeignKey(
                        name: "FK_EvolucionPaciente_pacientes_id_paciente",
                        column: x => x.id_paciente,
                        principalTable: "pacientes",
                        principalColumn: "id");
                });

            migrationBuilder.CreateTable(
                name: "DetalleVentas",
                columns: table => new
                {
                    id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    id_venta = table.Column<int>(type: "int", nullable: true),
                    id_lote = table.Column<int>(type: "int", nullable: true),
                    cantidad = table.Column<int>(type: "int", nullable: false),
                    precio_unitario = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    subtotal = table.Column<decimal>(type: "decimal(18,2)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DetalleVentas", x => x.id);
                    table.ForeignKey(
                        name: "FK_DetalleVentas_LotesMedicamentos_id_lote",
                        column: x => x.id_lote,
                        principalTable: "LotesMedicamentos",
                        principalColumn: "id");
                    table.ForeignKey(
                        name: "FK_DetalleVentas_Ventas_id_venta",
                        column: x => x.id_venta,
                        principalTable: "Ventas",
                        principalColumn: "id");
                });

            migrationBuilder.CreateTable(
                name: "Diagnosticos",
                columns: table => new
                {
                    id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    id_consulta = table.Column<int>(type: "int", nullable: true),
                    id_doctor = table.Column<int>(type: "int", nullable: true),
                    codigo_cie = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    descripcion = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    fecha_diagnostico = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Diagnosticos", x => x.id);
                    table.ForeignKey(
                        name: "FK_Diagnosticos_ConsultasMedicas_id_consulta",
                        column: x => x.id_consulta,
                        principalTable: "ConsultasMedicas",
                        principalColumn: "id");
                    table.ForeignKey(
                        name: "FK_Diagnosticos_Empleados_id_doctor",
                        column: x => x.id_doctor,
                        principalTable: "Empleados",
                        principalColumn: "id");
                });

            migrationBuilder.CreateTable(
                name: "Examenes",
                columns: table => new
                {
                    id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    id_paciente = table.Column<int>(type: "int", nullable: true),
                    id_consulta = table.Column<int>(type: "int", nullable: true),
                    nombre_examen = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    resultado = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    fecha_examen = table.Column<DateTime>(type: "datetime2", nullable: false),
                    observaciones = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Examenes", x => x.id);
                    table.ForeignKey(
                        name: "FK_Examenes_ConsultasMedicas_id_consulta",
                        column: x => x.id_consulta,
                        principalTable: "ConsultasMedicas",
                        principalColumn: "id");
                    table.ForeignKey(
                        name: "FK_Examenes_pacientes_id_paciente",
                        column: x => x.id_paciente,
                        principalTable: "pacientes",
                        principalColumn: "id");
                });

            migrationBuilder.CreateTable(
                name: "Tratamientos",
                columns: table => new
                {
                    id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    id_consulta = table.Column<int>(type: "int", nullable: true),
                    descripcion = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    indicaciones = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    fecha_inicio = table.Column<DateTime>(type: "datetime2", nullable: true),
                    fecha_fin = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Tratamientos", x => x.id);
                    table.ForeignKey(
                        name: "FK_Tratamientos_ConsultasMedicas_id_consulta",
                        column: x => x.id_consulta,
                        principalTable: "ConsultasMedicas",
                        principalColumn: "id");
                });

            migrationBuilder.CreateIndex(
                name: "IX_AsignacionHabitaciones_id_habitacion",
                table: "AsignacionHabitaciones",
                column: "id_habitacion");

            migrationBuilder.CreateIndex(
                name: "IX_AsignacionHabitaciones_id_paciente",
                table: "AsignacionHabitaciones",
                column: "id_paciente");

            migrationBuilder.CreateIndex(
                name: "IX_ConsultasMedicas_id_doctor",
                table: "ConsultasMedicas",
                column: "id_doctor");

            migrationBuilder.CreateIndex(
                name: "IX_ConsultasMedicas_id_paciente",
                table: "ConsultasMedicas",
                column: "id_paciente");

            migrationBuilder.CreateIndex(
                name: "IX_ConsultasMedicas_id_sucursal",
                table: "ConsultasMedicas",
                column: "id_sucursal");

            migrationBuilder.CreateIndex(
                name: "IX_DetalleVentas_id_lote",
                table: "DetalleVentas",
                column: "id_lote");

            migrationBuilder.CreateIndex(
                name: "IX_DetalleVentas_id_venta",
                table: "DetalleVentas",
                column: "id_venta");

            migrationBuilder.CreateIndex(
                name: "IX_Diagnosticos_id_consulta",
                table: "Diagnosticos",
                column: "id_consulta");

            migrationBuilder.CreateIndex(
                name: "IX_Diagnosticos_id_doctor",
                table: "Diagnosticos",
                column: "id_doctor");

            migrationBuilder.CreateIndex(
                name: "IX_Empleados_id_especialidad",
                table: "Empleados",
                column: "id_especialidad");

            migrationBuilder.CreateIndex(
                name: "IX_Empleados_id_sucursal",
                table: "Empleados",
                column: "id_sucursal");

            migrationBuilder.CreateIndex(
                name: "IX_Empleados_id_usuario",
                table: "Empleados",
                column: "id_usuario");

            migrationBuilder.CreateIndex(
                name: "IX_EvolucionPaciente_id_doctor",
                table: "EvolucionPaciente",
                column: "id_doctor");

            migrationBuilder.CreateIndex(
                name: "IX_EvolucionPaciente_id_paciente",
                table: "EvolucionPaciente",
                column: "id_paciente");

            migrationBuilder.CreateIndex(
                name: "IX_Examenes_id_consulta",
                table: "Examenes",
                column: "id_consulta");

            migrationBuilder.CreateIndex(
                name: "IX_Examenes_id_paciente",
                table: "Examenes",
                column: "id_paciente");

            migrationBuilder.CreateIndex(
                name: "IX_Habitaciones_id_sucursal",
                table: "Habitaciones",
                column: "id_sucursal");

            migrationBuilder.CreateIndex(
                name: "IX_LotesMedicamentos_id_medicamento",
                table: "LotesMedicamentos",
                column: "id_medicamento");

            migrationBuilder.CreateIndex(
                name: "IX_LotesMedicamentos_id_sucursal",
                table: "LotesMedicamentos",
                column: "id_sucursal");

            migrationBuilder.CreateIndex(
                name: "IX_Medicamentos_id_categoria",
                table: "Medicamentos",
                column: "id_categoria");

            migrationBuilder.CreateIndex(
                name: "IX_Permisos_id_modulo",
                table: "Permisos",
                column: "id_modulo");

            migrationBuilder.CreateIndex(
                name: "IX_Permisos_id_rol",
                table: "Permisos",
                column: "id_rol");

            migrationBuilder.CreateIndex(
                name: "IX_RolPermisos_id_permiso",
                table: "RolPermisos",
                column: "id_permiso");

            migrationBuilder.CreateIndex(
                name: "IX_Tratamientos_id_consulta",
                table: "Tratamientos",
                column: "id_consulta");

            migrationBuilder.CreateIndex(
                name: "IX_Usuarios_id_rol",
                table: "Usuarios",
                column: "id_rol");

            migrationBuilder.CreateIndex(
                name: "IX_Usuarios_id_sucursal",
                table: "Usuarios",
                column: "id_sucursal");

            migrationBuilder.CreateIndex(
                name: "IX_Ventas_id_paciente",
                table: "Ventas",
                column: "id_paciente");

            migrationBuilder.CreateIndex(
                name: "IX_Ventas_id_sucursal",
                table: "Ventas",
                column: "id_sucursal");

            migrationBuilder.CreateIndex(
                name: "IX_Ventas_id_usuario_vendedor",
                table: "Ventas",
                column: "id_usuario_vendedor");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AsignacionHabitaciones");

            migrationBuilder.DropTable(
                name: "DetalleVentas");

            migrationBuilder.DropTable(
                name: "Diagnosticos");

            migrationBuilder.DropTable(
                name: "EvolucionPaciente");

            migrationBuilder.DropTable(
                name: "Examenes");

            migrationBuilder.DropTable(
                name: "RolPermisos");

            migrationBuilder.DropTable(
                name: "Tratamientos");

            migrationBuilder.DropTable(
                name: "Habitaciones");

            migrationBuilder.DropTable(
                name: "LotesMedicamentos");

            migrationBuilder.DropTable(
                name: "Ventas");

            migrationBuilder.DropTable(
                name: "Permisos");

            migrationBuilder.DropTable(
                name: "ConsultasMedicas");

            migrationBuilder.DropTable(
                name: "Medicamentos");

            migrationBuilder.DropTable(
                name: "Modulos");

            migrationBuilder.DropTable(
                name: "Empleados");

            migrationBuilder.DropTable(
                name: "pacientes");

            migrationBuilder.DropTable(
                name: "CategoriasMedicamentos");

            migrationBuilder.DropTable(
                name: "Especialidades");

            migrationBuilder.DropTable(
                name: "Usuarios");

            migrationBuilder.DropTable(
                name: "Roles");

            migrationBuilder.DropTable(
                name: "Sucursales");
        }
    }
}
