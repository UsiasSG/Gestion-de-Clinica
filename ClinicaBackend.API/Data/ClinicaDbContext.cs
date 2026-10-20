using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;
using ClinicaBackend.API.Models;

namespace ClinicaBackend.API.Data
{
    public class ClinicaDbContext : DbContext
    {
        public ClinicaDbContext(DbContextOptions<ClinicaDbContext> options) : base(options) { }

        // DbSets conectados a las clases en Models/
        public DbSet<Sucursal> Sucursales { get; set; } = null!;
        public DbSet<Rol> Roles { get; set; } = null!;
        public DbSet<Modulo> Modulos { get; set; } = null!;
        public DbSet<Permiso> Permisos { get; set; } = null!;
        public DbSet<Usuario> Usuarios { get; set; } = null!;
        public DbSet<Especialidad> Especialidades { get; set; } = null!;
        public DbSet<Empleado> Empleados { get; set; } = null!;
        public DbSet<Paciente> Pacientes { get; set; } = null!;
        public DbSet<Habitacion> Habitaciones { get; set; } = null!;
        public DbSet<AsignacionHabitacion> AsignacionHabitaciones { get; set; } = null!;
        public DbSet<ConsultaMedica> ConsultasMedicas { get; set; } = null!;
        public DbSet<Diagnostico> Diagnosticos { get; set; } = null!;
        public DbSet<Tratamiento> Tratamientos { get; set; } = null!;
        public DbSet<Examen> Examenes { get; set; } = null!;
        public DbSet<EvolucionPaciente> EvolucionesPaciente { get; set; } = null!;
        public DbSet<CategoriaMedicamento> CategoriasMedicamentos { get; set; } = null!;
        public DbSet<Medicamento> Medicamentos { get; set; } = null!;
        public DbSet<LoteMedicamento> LotesMedicamentos { get; set; } = null!;
        public DbSet<Venta> Ventas { get; set; } = null!;
        public DbSet<DetalleVenta> DetalleVentas { get; set; } = null!;
        
        public DbSet<MovimientoInventario> MovimientosInventario { get; set; } = null!;
        public DbSet<ArchivoClinico> ArchivosClinicos { get; set; } = null!;

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // =================================================================
            // 1. MAPEOS DE PRECISIÓN DECIMAL PARA SQL SERVER (Evita Warnings)
            // =================================================================
            modelBuilder.Entity<DetalleVenta>().Property(d => d.PrecioUnitario).HasPrecision(18, 2);
            modelBuilder.Entity<DetalleVenta>().Property(d => d.Subtotal).HasPrecision(18, 2);
            modelBuilder.Entity<Medicamento>().Property(m => m.PrecioVenta).HasPrecision(18, 2);
            modelBuilder.Entity<Habitacion>().Property(h => h.PrecioDiario).HasPrecision(18, 2);
            modelBuilder.Entity<Venta>().Property(v => v.Subtotal).HasPrecision(18, 2);
            modelBuilder.Entity<Venta>().Property(v => v.Impuesto).HasPrecision(18, 2);
            modelBuilder.Entity<Venta>().Property(v => v.Total).HasPrecision(18, 2);

            // =================================================================
            // 2. CONFIGURACIÓN DE RELACIONES Y CLAVES
            // =================================================================

            // Relaciones de Permisos
            modelBuilder.Entity<Permiso>()
                .HasOne(p => p.Rol)
                .WithMany()
                .HasForeignKey(p => p.IdRol);

            modelBuilder.Entity<Permiso>()
                .HasOne(p => p.Modulo)
                .WithMany()
                .HasForeignKey(p => p.IdModulo);


            // Usuarios
            modelBuilder.Entity<Usuario>()
                .HasOne(u => u.Sucursal)
                .WithMany()
                .HasForeignKey(u => u.IdSucursal);

            modelBuilder.Entity<Usuario>()
                .HasOne(u => u.Rol)
                .WithMany()
                .HasForeignKey(u => u.IdRol);

            // Empleados
            modelBuilder.Entity<Empleado>()
                .HasOne(e => e.Usuario)
                .WithMany()
                .HasForeignKey(e => e.IdUsuario);

            modelBuilder.Entity<Empleado>()
                .HasOne(e => e.Sucursal)
                .WithMany()
                .HasForeignKey(e => e.IdSucursal);

            modelBuilder.Entity<Empleado>()
                .HasOne(e => e.Especialidad)
                .WithMany()
                .HasForeignKey(e => e.IdEspecialidad);

            // Habitaciones
            modelBuilder.Entity<Habitacion>()
                .HasOne(h => h.Sucursal)
                .WithMany()
                .HasForeignKey(h => h.IdSucursal);

            modelBuilder.Entity<AsignacionHabitacion>()
                .HasOne(a => a.Habitacion)
                .WithMany()
                .HasForeignKey(a => a.IdHabitacion);

            modelBuilder.Entity<AsignacionHabitacion>()
                .HasOne(a => a.Paciente)
                .WithMany()
                .HasForeignKey(a => a.IdPaciente);

            // Consultas Médicas
            modelBuilder.Entity<ConsultaMedica>()
                .HasOne(c => c.Paciente)
                .WithMany()
                .HasForeignKey(c => c.IdPaciente);

            modelBuilder.Entity<ConsultaMedica>()
                .HasOne(c => c.Doctor)
                .WithMany()
                .HasForeignKey(c => c.IdDoctor);

            modelBuilder.Entity<ConsultaMedica>()
                .HasOne(c => c.Sucursal)
                .WithMany()
                .HasForeignKey(c => c.IdSucursal);

            // Diagnósticos
            modelBuilder.Entity<Diagnostico>()
                .HasOne(d => d.Consulta)
                .WithMany()
                .HasForeignKey(d => d.IdConsulta);

            modelBuilder.Entity<Diagnostico>()
                .HasOne(d => d.Doctor)
                .WithMany()
                .HasForeignKey(d => d.IdDoctor);

            // Tratamientos
            modelBuilder.Entity<Tratamiento>()
                .HasOne(t => t.Consulta)
                .WithMany()
                .HasForeignKey(t => t.IdConsulta);

            // Exámenes
            modelBuilder.Entity<Examen>()
                .HasOne(e => e.Paciente)
                .WithMany()
                .HasForeignKey(e => e.IdPaciente);

            modelBuilder.Entity<Examen>()
                .HasOne(e => e.Consulta)
                .WithMany()
                .HasForeignKey(e => e.IdConsulta);

            // Evolución Paciente
            modelBuilder.Entity<EvolucionPaciente>()
                .HasOne(ep => ep.Paciente)
                .WithMany()
                .HasForeignKey(ep => ep.IdPaciente);

            modelBuilder.Entity<EvolucionPaciente>()
                .HasOne(ep => ep.Doctor)
                .WithMany()
                .HasForeignKey(ep => ep.IdDoctor);

            // Archivos clínicos
            modelBuilder.Entity<ArchivoClinico>()
                .HasOne(a => a.Paciente).WithMany().HasForeignKey(a => a.IdPaciente).OnDelete(DeleteBehavior.NoAction);

            modelBuilder.Entity<ArchivoClinico>()
                .HasOne(a => a.Examen).WithMany().HasForeignKey(a => a.IdExamen).OnDelete(DeleteBehavior.NoAction);

            modelBuilder.Entity<ArchivoClinico>()
                .HasOne(a => a.Usuario).WithMany().HasForeignKey(a => a.IdUsuario).OnDelete(DeleteBehavior.NoAction);

            modelBuilder.Entity<ArchivoClinico>().HasIndex(a => a.IdPaciente);

            // Medicamentos y Lotes
            modelBuilder.Entity<Medicamento>()
                .HasOne(m => m.Categoria)
                .WithMany()
                .HasForeignKey(m => m.IdCategoria);

            modelBuilder.Entity<LoteMedicamento>()
                .HasOne(l => l.Medicamento)
                .WithMany()
                .HasForeignKey(l => l.IdMedicamento);

            modelBuilder.Entity<LoteMedicamento>()
                .HasOne(l => l.Sucursal)
                .WithMany()
                .HasForeignKey(l => l.IdSucursal);

            // Ventas
            modelBuilder.Entity<Venta>()
                .HasOne(v => v.Sucursal)
                .WithMany()
                .HasForeignKey(v => v.IdSucursal);

            modelBuilder.Entity<Venta>()
                .HasOne(v => v.UsuarioVendedor)
                .WithMany()
                .HasForeignKey(v => v.IdUsuarioVendedor);

            modelBuilder.Entity<Venta>()
                .HasOne(v => v.Paciente)
                .WithMany()
                .HasForeignKey(v => v.IdPaciente);

            modelBuilder.Entity<DetalleVenta>()
                .HasOne(dv => dv.Venta)
                .WithMany()
                .HasForeignKey(dv => dv.IdVenta);

            modelBuilder.Entity<DetalleVenta>()
                .HasOne(dv => dv.Lote)
                .WithMany()
                .HasForeignKey(dv => dv.IdLote);

            // Movimientos de inventario
            modelBuilder.Entity<MovimientoInventario>()
                .HasOne(m => m.Lote)
                .WithMany()
                .HasForeignKey(m => m.IdLote)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<MovimientoInventario>()
                .HasOne(m => m.Usuario)
                .WithMany()
                .HasForeignKey(m => m.IdUsuario)
                .OnDelete(DeleteBehavior.NoAction);

            modelBuilder.Entity<MovimientoInventario>()
                .HasOne(m => m.Venta)
                .WithMany()
                .HasForeignKey(m => m.IdVenta)
                .OnDelete(DeleteBehavior.NoAction);

            modelBuilder.Entity<MovimientoInventario>().HasIndex(m => m.Fecha);

            // Código de medicamento único (los nulos se permiten)
            modelBuilder.Entity<Medicamento>().HasIndex(m => m.Codigo).IsUnique();

            modelBuilder.Entity<Permiso>().HasIndex(p => new { p.IdRol, p.IdModulo }).IsUnique();

            // =================================================================
            // 3. DATOS SEMILLA (Seed Data)
            // =================================================================

            // Sucursal Inicial
            modelBuilder.Entity<Sucursal>().HasData(
                new Sucursal
                {
                    Id = 1,
                    Nombre = "Sucursal Central - Zona 10",
                    Direccion = "Avenida Las Américas 14-20, Zona 14",
                    Telefono = "2222-1111",
                    Estado = true,
                    FechaRegistro = new DateTime(2026, 1, 1)
                }
            );

            // Especialidades Médicas
            modelBuilder.Entity<Especialidad>().HasData(
                new Especialidad { Id = 1, Nombre = "Medicina General", Descripcion = "Atención médica primaria" },
                new Especialidad { Id = 2, Nombre = "Pediatría", Descripcion = "Atención a niños y adolescentes" },
                new Especialidad { Id = 3, Nombre = "Ginecología", Descripcion = "Salud de la mujer" }
            );

            // Roles Base
            modelBuilder.Entity<Rol>().HasData(
                new Rol { Id = 1, Nombre = "Administrador", Descripcion = "Acceso total al sistema" },
                new Rol { Id = 2, Nombre = "Médico", Descripcion = "Atención de consultas e historial clínico" },
                new Rol { Id = 3, Nombre = "Cajero", Descripcion = "Operaciones de Punto de Venta / POS" },
                new Rol { Id = 4, Nombre = "Enfermería", Descripcion = "Exámenes, evolución de pacientes y habitaciones" },
                new Modulo { Id = 8, Nombre = "Roles y Permisos", Codigo = "ROLES" }


            );

            // Módulos
            modelBuilder.Entity<Modulo>().HasData(
                new Modulo { Id = 1, Nombre = "Gestión de Pacientes", Codigo = "PACIENTES" },
                new Modulo { Id = 2, Nombre = "Historial Clínico", Codigo = "HISTORIAL" },
                new Modulo { Id = 3, Nombre = "Punto de Venta", Codigo = "POS" },
                new Modulo { Id = 4, Nombre = "Gestión de Personal", Codigo = "PERSONAL" },
                new Modulo { Id = 5, Nombre = "Habitaciones (SignalR)", Codigo = "HABITACIONES" },
                new Modulo { Id = 6, Nombre = "Inventario de Medicamentos", Codigo = "INVENTARIO" },
                new Modulo { Id = 7, Nombre = "Diagnósticos", Codigo = "DIAGNOSTICOS" },
                new Modulo { Id = 8, Nombre = "Roles y Permisos", Codigo = "ROLES" }

            );

            // Permisos base para Administrador
            modelBuilder.Entity<Permiso>().HasData(
                new Permiso { Id = 1, IdRol = 1, IdModulo = 1, PuedeConsultar = true, PuedeCrear = true, PuedeModificar = true, PuedeEliminar = true },
                new Permiso { Id = 2, IdRol = 1, IdModulo = 2, PuedeConsultar = true, PuedeCrear = true, PuedeModificar = true, PuedeEliminar = true },
                new Permiso { Id = 3, IdRol = 1, IdModulo = 3, PuedeConsultar = true, PuedeCrear = true, PuedeModificar = true, PuedeEliminar = true },
                new Permiso { Id = 4, IdRol = 1, IdModulo = 4, PuedeConsultar = true, PuedeCrear = true, PuedeModificar = true, PuedeEliminar = true },
                new Permiso { Id = 5, IdRol = 1, IdModulo = 5, PuedeConsultar = true, PuedeCrear = true, PuedeModificar = true, PuedeEliminar = true },
                new Permiso { Id = 6, IdRol = 1, IdModulo = 6, PuedeConsultar = true, PuedeCrear = true, PuedeModificar = true, PuedeEliminar = true },
                new Permiso { Id = 7, IdRol = 1, IdModulo = 7, PuedeConsultar = true, PuedeCrear = true, PuedeModificar = true, PuedeEliminar = true },
                new Permiso { Id = 8, IdRol = 2, IdModulo = 1, PuedeConsultar = true, PuedeCrear = true, PuedeModificar = true, PuedeEliminar = false }, // Pacientes
                new Permiso { Id = 9, IdRol = 2, IdModulo = 2, PuedeConsultar = true, PuedeCrear = true, PuedeModificar = true, PuedeEliminar = false }, // Historial
                new Permiso { Id = 10, IdRol = 2, IdModulo = 7, PuedeConsultar = true, PuedeCrear = true, PuedeModificar = false, PuedeEliminar = false }, // Diagnósticos
                new Permiso { Id = 11, IdRol = 2, IdModulo = 5, PuedeConsultar = true, PuedeCrear = false, PuedeModificar = true, PuedeEliminar = false }, // Habitaciones
                                                                                                                                           // Cajero
                new Permiso { Id = 12, IdRol = 3, IdModulo = 3, PuedeConsultar = true, PuedeCrear = true, PuedeModificar = true, PuedeEliminar = true },   // POS
                new Permiso { Id = 13, IdRol = 3, IdModulo = 1, PuedeConsultar = true, PuedeCrear = false, PuedeModificar = false, PuedeEliminar = false }, // Pacientes
                new Permiso { Id = 14, IdRol = 3, IdModulo = 6, PuedeConsultar = true, PuedeCrear = false, PuedeModificar = false, PuedeEliminar = false }, // Inventario
                                                                                                                                            // Enfermería (sin Diagnósticos)
                new Permiso { Id = 15, IdRol = 4, IdModulo = 1, PuedeConsultar = true, PuedeCrear = false, PuedeModificar = false, PuedeEliminar = false }, // Pacientes
                new Permiso { Id = 16, IdRol = 4, IdModulo = 2, PuedeConsultar = true, PuedeCrear = true, PuedeModificar = true, PuedeEliminar = false }, // Historial
                new Permiso { Id = 17, IdRol = 4, IdModulo = 5, PuedeConsultar = true, PuedeCrear = false, PuedeModificar = true, PuedeEliminar = false },  // Habitaciones
                new Permiso { Id = 18, IdRol = 1, IdModulo = 8, PuedeConsultar = true, PuedeCrear = true, PuedeModificar = true, PuedeEliminar = true }

            );

            // Usuario Administrador Inicial
            modelBuilder.Entity<Usuario>().HasData(
                new Usuario
                {
                    Id = 1,
                    IdSucursal = 1,
                    IdRol = 1,
                    NombreUsuario = "admin",
                    Correo = "admin@clinica.com",
                    PasswordHash = "admin123", // Cambiar por el hash si usas BCrypt/SHA256 en tu AuthController
                    Estado = true,
                    FechaCreacion = new DateTime(2026, 1, 1)
                }
            );
        }
    }
}