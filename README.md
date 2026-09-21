# Turnero Backend

Backend del sistema de gestion de turnos, construido con ASP.NET Core 8 y preparado para PostgreSQL.

## Requisitos

- .NET SDK 8.0.425
- PostgreSQL 16 o superior
- VS Code con C# Dev Kit

El SDK requerido queda fijado en `global.json` para evitar compilar accidentalmente con otra version.

## Estructura

- `src/Turnero.Api`: HTTP, controllers y configuracion de la aplicacion.
- `src/Turnero.Application`: casos de uso, DTOs y validaciones.
- `src/Turnero.Domain`: entidades y reglas de negocio.
- `src/Turnero.Infrastructure`: persistencia y servicios externos.
- `tests/Turnero.Tests`: pruebas unitarias y de integracion.

## Secretos locales

Los secretos de desarrollo se almacenan fuera del repositorio mediante User Secrets:

```bash
dotnet user-secrets set "ConnectionStrings:DefaultConnection" "Host=localhost;Port=5432;Database=turnero;Username=turnero;Password=change-me" --project src/Turnero.Api
dotnet user-secrets set "Jwt:SigningKey" "reemplazar-por-una-clave-larga-y-aleatoria" --project src/Turnero.Api
```

Para levantar PostgreSQL localmente:

```bash
cp .env.example .env
docker compose up -d postgres
dotnet user-secrets set "ConnectionStrings:DefaultConnection" "Host=localhost;Port=5432;Database=turnero;Username=turnero;Password=el-valor-de-POSTGRES_PASSWORD" --project src/Turnero.Api
```

En produccion se deben inyectar mediante variables de entorno o un gestor de secretos como Azure Key Vault. Nunca se deben guardar contrasenas, claves JWT o tokens en `appsettings.json` ni en Git.

## Comandos principales

```bash
dotnet restore
dotnet build
dotnet test
dotnet run --project src/Turnero.Api
```

Las migraciones se crean desde la raiz del repositorio:

```bash
dotnet ef migrations add InitialCreate --project src/Turnero.Infrastructure --startup-project src/Turnero.Api --output-dir Persistence/Migrations
dotnet ef database update --project src/Turnero.Infrastructure --startup-project src/Turnero.Api
```