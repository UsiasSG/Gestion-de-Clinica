using System.Text;
using ClinicaBackend.API.Data;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAngular", policy =>
    {
        policy.WithOrigins("http://localhost:4200")
              .AllowAnyHeader()
              .AllowAnyMethod()
              .AllowCredentials(); // Importante para SignalR y WebSockets
    });
});

// 1. Configuración de Base de Datos Azure SQL Server
// Se remueve EnableRetryOnFailure para permitir transacciones explícitas con DbContext
var connectionString = builder.Configuration.GetConnectionString("ClinicaDb");
builder.Services.AddDbContext<ClinicaDbContext>(options =>
    options.UseSqlServer(connectionString));

// 2. Configuración de Autenticación JWT con soporte para WebSockets/SignalR
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

    // Requisito indispensable para que SignalR valide el JWT mediante WebSockets
    options.Events = new JwtBearerEvents
    {
        OnMessageReceived = context =>
        {
            var accessToken = context.Request.Query["access_token"];
            var path = context.HttpContext.Request.Path;
            if (!string.IsNullOrEmpty(accessToken) && path.StartsWithSegments("/hubs"))
            {
                context.Token = accessToken;
            }
            return Task.CompletedTask;
        }
    };
});

// 3. Controladores y SignalR
builder.Services.AddControllers();
builder.Services.AddScoped<ClinicaBackend.API.Services.PermisosService>();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSignalR();

// 4. Configuración de Swagger
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
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference
                {
                    Type = ReferenceType.SecurityScheme,
                    Id = "Bearer"
                }
            },
            Array.Empty<string>()
        }
    });
});

var app = builder.Build();

// 5. Middleware Pipeline
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(c =>
    {
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "Clínica API v1");
    });
}

app.UseCors("AllowAngular");
app.UseRouting();
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();
app.MapHub<ClinicaBackend.API.Hubs.HabitacionesHub>("/hubs/habitaciones");
app.MapHub<ClinicaBackend.API.Hubs.NotificacionesHub>("/hubs/notificaciones");

app.Run();