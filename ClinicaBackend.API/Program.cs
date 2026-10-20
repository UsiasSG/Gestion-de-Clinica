using System.Text;
using ClinicaBackend.API.Data;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;

var builder = WebApplication.CreateBuilder(args);

// 1. CORS: los orígenes permitidos vienen de la configuración (Cors:Origenes)
var origenes = builder.Configuration.GetSection("Cors:Origenes").Get<string[]>()
               ?? new[] { "http://localhost:4200" };

builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAngular", policy =>
    {
        policy.WithOrigins(origenes)
              .AllowAnyHeader()
              .AllowAnyMethod()
              .AllowCredentials(); // Necesario para SignalR
    });
});

// 2. Base de datos Azure SQL con reintentos (la BD serverless tarda en despertar).
//    Las transacciones explícitas usan CreateExecutionStrategy, compatible con esto.
var connectionString = builder.Configuration.GetConnectionString("ClinicaDb");
if (string.IsNullOrWhiteSpace(connectionString))
    throw new InvalidOperationException("Falta la cadena de conexión 'ConnectionStrings:ClinicaDb'.");

builder.Services.AddDbContext<ClinicaDbContext>(options =>
    options.UseSqlServer(connectionString, sql =>
        sql.EnableRetryOnFailure(maxRetryCount: 5, maxRetryDelay: TimeSpan.FromSeconds(10), errorNumbersToAdd: null)));

// 3. Autenticación JWT con soporte para SignalR
var jwtKey = builder.Configuration["Jwt:Key"] ?? "ClaveSecretaSuperSeguraParaJWTDeMiClinica2026!";

builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(options =>
{
    options.RequireHttpsMetadata = false;
    options.SaveToken = true;
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuerSigningKey = true,
        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey)),
        ValidateIssuer = true,
        ValidIssuer = builder.Configuration["Jwt:Issuer"] ?? "ClinicaBackendAPI",
        ValidateAudience = true,
        ValidAudience = builder.Configuration["Jwt:Audience"] ?? "ClinicaFrontendClient",
        ClockSkew = TimeSpan.Zero
    };

    // SignalR envía el token por query string en los WebSockets
    options.Events = new JwtBearerEvents
    {
        OnMessageReceived = context =>
        {
            var accessToken = context.Request.Query["access_token"];
            if (!string.IsNullOrEmpty(accessToken) && context.HttpContext.Request.Path.StartsWithSegments("/hubs"))
                context.Token = accessToken;
            return Task.CompletedTask;
        }
    };
});

// 4. Servicios
builder.Services.AddControllers();
builder.Services.AddScoped<ClinicaBackend.API.Services.PermisosService>();
builder.Services.AddScoped<ClinicaBackend.API.Services.InventarioClinicoService>();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSignalR();

builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo { Title = "Clínica Backend API", Version = "v1" });

    c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Name = "Authorization",
        Description = "Ingresa el token JWT en el formato: Bearer {tu_token}",
        In = ParameterLocation.Header,
        Type = SecuritySchemeType.ApiKey,
        Scheme = "Bearer"
    });

    c.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme { Reference = new OpenApiReference { Type = ReferenceType.SecurityScheme, Id = "Bearer" } },
            Array.Empty<string>()
        }
    });
});

var app = builder.Build();

// 5. Pipeline. Swagger: siempre en desarrollo; en Azure solo si Swagger:Habilitado = true
if (app.Environment.IsDevelopment() || builder.Configuration.GetValue<bool>("Swagger:Habilitado"))
{
    app.UseSwagger();
    app.UseSwaggerUI(c => c.SwaggerEndpoint("/swagger/v1/swagger.json", "Clínica API v1"));
}

app.UseCors("AllowAngular");
app.UseRouting();
app.UseAuthentication();
app.UseAuthorization();

app.MapGet("/salud", () => Results.Ok(new { estado = "ok", hora = DateTime.Now })); // prueba rápida de despliegue
app.MapControllers();
app.MapHub<ClinicaBackend.API.Hubs.HabitacionesHub>("/hubs/habitaciones");
app.MapHub<ClinicaBackend.API.Hubs.NotificacionesHub>("/hubs/notificaciones");

app.Run();