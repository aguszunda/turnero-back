# Guia para desarrollar en Turnero Backend

Esta guia explica como trabajar en el backend del sistema de turnos. Esta escrita para alguien que esta comenzando con ASP.NET Core, C# y Entity Framework Core.

## 1. Que estamos construyendo

Turnero Backend es una API HTTP construida con .NET 8, ASP.NET Core Web API, Entity Framework Core 8, PostgreSQL mediante Npgsql, Swagger y xUnit.

El MVP actual permite:

- Crear, listar y actualizar servicios.
- Crear y listar profesionales.
- Asociar servicios a profesionales.
- Definir horarios semanales y descansos.
- Crear turnos validando disponibilidad.
- Evitar solapamientos dentro de una transaccion serializable.

Todavia no estan implementados Identity, clientes registrados, reprogramacion, cancelacion, auditoria ni notificaciones. JWT con roles (Admin/Recepcionista/Profesional/Cliente) esta implementado: una U, registro y login de clientes, y creacion de usuarios internos.

## 2. Antes de comenzar

Verifica estos requisitos:

```bash
dotnet --version
docker --version
dotnet ef --version
```

`dotnet --version` debe mostrar `8.0.425`. `global.json` fija esta version aunque haya otros SDK instalados.

En VS Code se recomienda tener instaladas estas extensiones:

- C# Dev Kit: `ms-dotnettools.csdevkit`.
- C#: `ms-dotnettools.csharp`.
- PostgreSQL: `ckolkman.vscode-postgres`.

## 3. Estructura de la solucion

```text
Turnero.sln
src/
  Turnero.Api/
    Contracts/       DTOs de entrada y salida HTTP.
    Controllers/     Endpoints de la API.
    Program.cs       Registro de dependencias y pipeline HTTP.
  Turnero.Application/
                     Casos de uso y servicios de aplicacion.
  Turnero.Domain/
    Entities/        Entidades y enums del negocio.
  Turnero.Infrastructure/
    Persistence/     DbContext, fabrica y migraciones EF Core.
tests/
  Turnero.Tests/     Pruebas automatizadas.
```

La solucion tiene las cuatro capas preparadas, pero `Turnero.Application` todavia no contiene casos de uso. En el MVP inicial, la validacion de reservas esta temporalmente en `AppointmentsController` para tener un flujo ejecutable. Cuando agreguemos autenticacion, reprogramacion y cancelacion, esa logica debe moverse a un servicio de aplicacion.

El dominio no debe conocer `DbContext` ni ASP.NET Core. No agregues consultas de EF Core directamente a nuevas clases de dominio.

## 4. Como ejecutar el backend

### 4.1. PostgreSQL local

El archivo `.env` contiene valores locales para Docker y no se versiona.

```bash
cp .env.example .env
```

Edita `POSTGRES_PASSWORD` con una clave local y levanta la base:

```bash
docker compose up -d postgres
docker compose ps
```

Para detenerla:

```bash
docker compose down
```

### 4.2. Secretos

Los secretos no se escriben en `appsettings.json`, `.env.example` ni en el codigo fuente. En desarrollo usamos User Secrets:

```bash
dotnet user-secrets set "ConnectionStrings:DefaultConnection" "Host=localhost;Port=5432;Database=turnero;Username=turnero;Password=TU_PASSWORD" --project src/Turnero.Api
dotnet user-secrets set "Jwt:SigningKey" "una-clave-larga-y-aleatoria" --project src/Turnero.Api
```

Tambien se puede definir el admin inicial (solo se crea si la tabla `users` esta vacia y en entorno `Development`):

```bash
dotnet user-secrets set "SeedAdmin:Email" "admin@turnero.local" --project src/Turnero.Api
dotnet user-secrets set "SeedAdmin:Password" "TU-CLAVE-FORTE" --project src/Turnero.Api
```

El `UserSecretsId` esta definido en `src/Turnero.Api/Turnero.Api.csproj`. ASP.NET Core carga esos valores automaticamente en `Development`. Los valores no secretos de JWT (`Issuer`, `Audience`, `ExpireMinutes`) estan en `appsettings.json`.

Para revisar las claves guardadas localmente:

```bash
dotnet user-secrets list --project src/Turnero.Api
```

No compartas la salida si contiene contrasenas o tokens.

### 4.3. Base de datos, build y ejecucion

```bash
dotnet ef database update \
  --project src/Turnero.Infrastructure \
  --startup-project src/Turnero.Api

dotnet restore
dotnet build Turnero.sln
dotnet test Turnero.sln
dotnet run --project src/Turnero.Api
```

Con la API ejecutandose, Swagger queda disponible en `http://localhost:5210/swagger`. El puerto lo fija el perfil `http` en `src/Turnero.Api/Properties/launchSettings.json`.

## 5. Como fluye una request

```text
Cliente HTTP -> Controller -> DTO -> DbContext -> PostgreSQL
```

1. ASP.NET Core recibe el JSON.
2. El controller lo convierte en un DTO.
3. `[ApiController]` ejecuta la validacion basica de Data Annotations.
4. El controller consulta y valida el dominio.
5. EF Core persiste la entidad dentro de una transaccion cuando corresponde.
6. El endpoint devuelve un DTO, nunca una entidad EF directamente.

## 6. Entidades principales

### Service

Representa un servicio de la peluqueria. `DurationMinutes` define la duracion, `Price` el precio actual e `IsActive` permite una baja logica.

### Professional

Representa al profesional. Tiene una relacion N:M con `Service` mediante `ProfessionalService`, horarios mediante `WeeklySchedule` y turnos mediante `Appointment`.

### WeeklySchedule

Representa una jornada semanal. `DayOfWeek` usa la convencion de .NET: domingo `0`, lunes `1`, martes `2`, hasta sabado `6`. `StartTime` y `EndTime` definen la jornada; `BreakStart` y `BreakEnd` el descanso; `MarginMinutes` el tiempo entre turnos.

### Appointment

Representa un turno. `StartsAt` y `EndsAt` son `DateTimeOffset` y se guardan en UTC. `Status` se persiste como texto. Los estados que bloquean disponibilidad son `Pending`, `Reserved` e `InProgress`.

## 7. Endpoints actuales

### Servicios

- `GET /api/services`: lista servicios ordenados por nombre.
- `POST /api/services`: crea un servicio.
- `PUT /api/services/{id}`: actualiza datos y activa o desactiva un servicio.

Ejemplo de `POST /api/services`:

```json
{
  "name": "Corte clasico",
  "description": "Corte con maquina y tijera",
  "durationMinutes": 45,
  "price": 8500,
  "category": "corte"
}
```

### Profesionales

- `GET /api/professionals`: lista profesionales y servicios asociados.
- `POST /api/professionals`: crea un profesional con servicios y horarios.

Ejemplo:

```json
{
  "firstName": "Ana",
  "lastName": "Gomez",
  "phone": "+5491100000000",
  "email": "ana@example.com",
  "specialty": "Color",
  "serviceIds": [1],
  "weeklySchedules": [
    {
      "dayOfWeek": 1,
      "startTime": "09:00:00",
      "endTime": "18:00:00",
      "breakStart": "13:00:00",
      "breakEnd": "14:00:00",
      "marginMinutes": 10
    }
  ]
}
```

### Turnos

- `POST /api/appointments`: crea un turno.

Ejemplo:

```json
{
  "serviceId": 1,
  "professionalId": 1,
  "clientName": "Juan Perez",
  "clientPhone": "+5491100000001",
  "startsAt": "2026-09-21T15:00:00-03:00"
}
```

El backend calcula `EndsAt` usando la duracion del servicio. No envies `endsAt` desde el cliente.

Respuestas importantes:

- `201 Created`: recurso creado.
- `400 Bad Request`: datos invalidos, servicio no prestado o fuera de horario.
- `404 Not Found`: recurso inexistente cuando el endpoint lo contempla.
- `409 Conflict`: existe un turno que se cruza con el horario solicitado.

### Autenticacion y usuarios

Endpoints de auth (sin autenticacion):

- `POST /api/auth/register`: crea un cliente (rol `CLIENTE`) y devuelve `{ token, usuario }`.
- `POST /api/auth/login`: valida credenciales y devuelve el mismo payload.
- `POST /api/users`: crea usuarios internos (admin/recepcionista/profesional). Requiere `Authorization: Bearer <token>` con rol `ADMIN`.

El payload de `usuario` es:

```json
{
  "id": 2,
  "email": "ana@example.com",
  "rol": "CLIENTE",
  "nombre": "Ana",
  "apellido": "Gomez",
  "profesionalId": null,
  "clienteId": 1
}
```

El frontend Angular envia `nombre`, `apellido`, `telefono`, `email` y `password`; el backend los mapea a sus propiedades internas con `[JsonPropertyName]`.

Validaciones de registro:

- `email`: requerido, formato valido, maximo 254 caracteres, unico (409 si ya existe).
- `password`: requerida, minimo 8 caracteres.
- `nombre`/`apellido`: requeridos, sin espacios al inicio/fin.
- `telefono`: requerido, patron `^[+]?[0-9\s()-]{6,}$`.
- El auto-registro siempre crea rol `CLIENTE`; nadie puede auto-asignarse roles elevados.

Respuestas importantes: `201 Created` (usuario creado), `200 OK` (login), `401 Unauthorized` (credenciales invalidas o falta token), `403 Forbidden` (rol insuficiente), `409 Conflict` (email duplicado).

Como autenticarse desde la terminal:

```bash
TOKEN=$(curl -s -X POST http://localhost:5210/api/auth/login \
  -H "Content-Type: application/json" \
  -d '{"email":"TU-EMAIL","password":"TU-CLAVE"}' | python3 -c "import sys,json; print(json.load(sys.stdin)['token']}")
curl -H "Authorization: Bearer $TOKEN" http://localhost:5210/api/users
```

## 8. Reglas de disponibilidad

Al crear un turno, el backend:

1. Comprueba que servicio y profesional existan y esten activos.
2. Comprueba que el profesional preste el servicio.
3. Convierte la fecha a la zona configurada en `Business:TimeZone`.
4. Busca el horario semanal de ese dia.
5. Calcula el final usando `DurationMinutes`.
6. Rechaza turnos fuera de la jornada o que crucen el descanso.
7. Amplia la comparacion con `MarginMinutes`.
8. Busca solapamientos en estados bloqueantes.
9. Guarda el turno en una transaccion serializable.

La zona horaria actual es `America/Argentina/Buenos_Aires` y esta en `appsettings.json`. No uses `DateTime.Now` para reglas del negocio: usa `DateTimeOffset` y conversion explicita.

## 9. Como agregar una funcionalidad nueva

### Paso 1: definir el caso de uso

Escribe quien lo usa, que filtros acepta, que respuesta devuelve, que errores existen y que reglas debe cumplir.

### Paso 2: modificar el dominio si hace falta

Agrega propiedades en `Turnero.Domain` solo si forman parte del negocio. El dominio no debe depender de ASP.NET Core ni de `DbContext`.

### Paso 3: actualizar EF Core

Modifica `TurneroDbContext` para relaciones, indices, precision o nombres de tablas. Luego crea y revisa una migracion:

```bash
dotnet ef migrations add NombreDescriptivo \
  --project src/Turnero.Infrastructure \
  --startup-project src/Turnero.Api \
  --output-dir Persistence/Migrations
```

### Paso 4: crear DTOs

Los DTOs viven en `src/Turnero.Api/Contracts`.

- Usa `sealed record`.
- Usa `Create...Request`, `Update...Request` y `...Response`.
- Usa Data Annotations para validaciones simples.
- Usa `DateTimeOffset` para fechas y horas.
- No devuelvas entidades EF directamente.

### Paso 5: crear el controller

Usa controllers porque es el estilo actual del proyecto. Mantiene las acciones asincronas y acepta `CancellationToken`.

```csharp
[HttpGet]
public async Task<ActionResult<IReadOnlyCollection<ItemResponse>>>
    GetAll(CancellationToken cancellationToken)
{
    var items = await dbContext.Items
        .AsNoTracking()
        .Select(item => new ItemResponse(item.Id, item.Name))
        .ToListAsync(cancellationToken);

    return Ok(items);
}
```

Para consultas de solo lectura usa `AsNoTracking()`. Para crear recursos devuelve `201 Created`. Para recursos inexistentes devuelve `404 NotFound()`. Para conflictos de negocio devuelve `409 Conflict(...)`.

### Paso 6: agregar pruebas

Prueba como minimo el caso valido, datos inexistentes, datos inactivos, reglas incumplidas y conflictos de concurrencia si la funcionalidad modifica disponibilidad.

```bash
dotnet test Turnero.sln
```

## 10. Convenciones de codigo

- Clases y metodos en PascalCase.
- Variables y parametros en camelCase.
- No uses nombres de una sola letra.
- Usa `async` y `await` para acceso a base de datos.
- Pasa siempre el `CancellationToken` a EF Core.
- Usa `decimal` para dinero.
- Usa `DateTimeOffset` para fechas de API y turnos.
- No registres contrasenas, tokens ni cadenas de conexion en logs.
- No agregues `catch (Exception)` para ocultar errores.
- Mantene los metodos pequenos y con una responsabilidad clara.

## 11. Errores frecuentes

### Connection refused en el puerto 5432

PostgreSQL no esta levantado o el puerto es diferente:

```bash
docker compose ps
docker compose logs postgres
```

### `dotnet ef` no encontrado

Agrega la carpeta de herramientas globales al PATH de la sesion:

```bash
export PATH="$PATH:$HOME/.dotnet/tools"
dotnet ef --version
```

### La migracion no refleja un cambio

Verifica que modificaste la entidad o el `DbContext` correcto, que el proyecto es `Turnero.Infrastructure`, que `Turnero.Api` es el startup project y que compilaste antes de generar la migracion.

### El turno se rechaza aunque parece libre

Revisa `dayOfWeek`, zona horaria, duracion del servicio, descanso, margen y asociacion entre profesional y servicio.

## 12. Checklist antes de subir cambios

```bash
dotnet build Turnero.sln
dotnet test Turnero.sln
```

Tambien verifica que no haya secretos en los archivos modificados, que exista una migracion si cambio el esquema, que los endpoints devuelvan codigos HTTP correctos, que las lecturas usen `AsNoTracking()` y que las reglas nuevas tengan pruebas.

## 13. Proximos pasos

1. Profundizar la integracion de clientes registrados en la reserva (vincular `clienteId` al crear turnos).
2. Extraer la reserva a un servicio de aplicacion.
3. Asignar permisos por rol a los endpoints existentes.
4. Incorporar recuperacion de contrasena y refresh tokens.
5. Agregar reprogramacion y cancelacion.
6. Agregar historial de estados.
7. Agregar pruebas de integracion contra PostgreSQL.
8. Agregar manejo global de errores y logging estructurado.