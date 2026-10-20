# Sistema de Gestión Clínica Integral

Proyecto Final de Desarrollo – Universidad Mariano Gálvez.

Solución web para el control de una clínica privada multisucursal con farmacia:
pacientes, historial clínico, inventario de medicamentos y lotes, personal,
habitaciones en tiempo real, punto de venta y permisos dinámicos por rol.

## Arquitectura

Frontend Angular → API REST .NET 10 → Entity Framework Core → Azure SQL Database

## Tecnologías

- **Backend:** ASP.NET Core (.NET 10), Entity Framework Core, JWT, SignalR
- **Frontend:** Angular (componentes standalone, signals)
- **Base de datos:** Azure SQL Database
- **Contenedores:** Docker

## Estructura

| Carpeta | Contenido |
|---|---|
| `ClinicaBackend.API/` | API REST, modelos, migraciones y hubs de SignalR |
| `ClinicaFrontend/` | Aplicación web en Angular |

## Ejecución local

**API**
1. Configurar la cadena de conexión: `dotnet user-secrets set "ConnectionStrings:ClinicaDb" "<cadena>"`
2. `dotnet ef database update`
3. `dotnet run`

**Frontend**
1. `npm install`
2. `ng serve`