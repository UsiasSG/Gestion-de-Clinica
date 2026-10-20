# ───────── Etapa 1: compilación ─────────
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# Primero solo el .csproj: así Docker reutiliza la caché de paquetes si no cambian
COPY ClinicaBackend.API.csproj ./
RUN dotnet restore ClinicaBackend.API.csproj

# Luego el resto del código y la publicación
COPY . .
RUN dotnet publish ClinicaBackend.API.csproj -c Release -o /app/publish /p:UseAppHost=false

# ───────── Etapa 2: imagen final (solo el runtime) ─────────
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final
WORKDIR /app

ENV ASPNETCORE_URLS=http://+:8080 \
    TZ=America/Guatemala

EXPOSE 8080
COPY --from=build /app/publish .

# Ejecuta la app con un usuario sin privilegios (buena práctica de seguridad)
USER $APP_UID

ENTRYPOINT ["dotnet", "ClinicaBackend.API.dll"]