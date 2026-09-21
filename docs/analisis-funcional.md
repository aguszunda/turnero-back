# Análisis Funcional — Sistema de Gestión de Turnos para Peluquería

**Versión:** 1.0
**Fecha:** 20/09/2026
**Autor:** Analista Funcional
**Estado:** Borrador para validación

---

## 1. Objetivo del proyecto

Desarrollar una aplicación web para la gestión de turnos de una peluquería que brinda múltiples servicios y cuenta con varios profesionales. El sistema debe permitir **asignar turnos por profesional y por servicio**, optimizando la agenda, evitando superposiciones y brindando trazabilidad de la operación comercial.

### Alcance
- Gestión de profesionales, servicios y horarios de atención.
- Reserva de turnos por servicio y profesional.
- Validación de disponibilidad en tiempo real.
- Administración de horarios laborales y franjas de atención.
- Consulta y gestión de agenda (por día, profesional y servicio).
- Notificación/estado del turno (confirmado, en curso, completado, cancelado, no asistió).

### Fuera de alcance (fase inicial)
- Pagos en línea / facturación electrónica.
- Módulo de venta de productos.
- Aplicación móvil nativa (la plataforma será responsive).
- Integración con pasarelas de pagos.
- Multisucursal (se contempla como extensión futura).

---

## 2. Usuarios y roles

| Rol | Descripción | Permisos principales |
|-----|-------------|----------------------|
| **Administrador** | Dueño/gerente de la peluquería | ABM de profesionales, servicios, horarios, usuarios; consulta de métricas; gestión de todos los turnos. |
| **Recepcionista** | Personal de mostrador | Asignar, reprogramar y cancelar turnos; visualizar agenda completa. |
| **Profesional** | Peluquero/barbero | Visualizar su agenda; marcar turno en curso / completado; declarar disponibilidad. |
| **Cliente** | Usuario final (registrado) | Reservar, reprogramar y cancelar turnos propios; ver historial. |

---

## 3. Reglas de negocio

1. **Un turno pertenece a un único profesional y a un único servicio.**
2. Un turno tiene **una fecha y hora de inicio**, y su **duración derivada del servicio** (configurable por profesional si aplica).
3. **No puede existir superposición de turnos** para el mismo profesional (validación de cruce de franjas horarias).
4. Un profesional puede tener **más de un servicio** asociado y habilita/deshabilita servicios prestados.
5. Los turnos solo se pueden reservar dentro de los **horarios laborales** del profesional y su **franja de atención** (ej.: almuerzo/descanso no reservable).
6. El **tiempo de margen** entre turnos es configurable (ej.: 10 minutos de limpieza/desinfección).
7. Política de cancelación:
   - **Cliente:** puede cancelar hasta X horas antes del turno (configurable, ej.: 4 h).
   - **Recepcionista/Admin:** cancelación sin restricción, con obligación de registrar motivo (opcional).
8. `capacidadMaxima` (overbooking) = 0 para empezar; no se permite doble reserva.
9. Cada turno transita por un **estado** (ver apartado 6).
10. Un servicio no puede ser asignado a un turno si el profesional no lo presta.
11. Al **completar** un turno se puede registrar observaciones (ej.: producto usado, preferencias del cliente).
12. El horario semanal se define con **plantillas por día** (lunes a domingo, hasta 2 franjas/día) por profesional.

---

## 4. Requerimientos funcionales

### 4.1. Gestión de servicios (RF-01)
| ID | Requerimiento | Prioridad |
|----|---------------|-----------|
| RF-01.1 | ABM de servicios: nombre, descripción, duración (minutos), precio, categoría (corte, color, tratamiento, barbería, etc.). | Alta |
| RF-01.2 | Alta/Baja lógica de servicios (no se eliminan si hay turnos históricos). | Alta |
| RF-01.3 | Activar/inactivar un servicio para no ser ofrecido en reserva. | Media |

**Atributos de Servicio:**
- `idServicio`, `nombre`, `descripcion`, `duracionMin`, `precio`, `categoria`, `activo`, `createdAt`, `updatedAt`.

### 4.2. Gestión de profesionales (RF-02)
| ID | Requerimiento | Prioridad |
|----|---------------|-----------|
| RF-02.1 | ABM de profesionales: nombre, apellido, teléfono, email, especialidad, foto (opcional). | Alta |
| RF-02.2 | Asociar profesionales con los servicios que prestan (relación N:M). | Alta |
| RF-02.3 | Definir horario laboral semanal por profesional (plantilla). | Alta |
| RF-02.4 | Definir franjas no laborales / bloqueos de agenda (ej.: vacaciones, descanso). | Media |
| RF-02.5 | Activar/inactivar profesional (no lista en reservas). | Alta |

**Atributos de Profesional:** `idProfesional`, `nombre`, `apellido`, `telefono`, `email`, `especialidad`, `activo`, lista de `Servicio`.

**Horario laboral (plantilla por día):** `idHorario`, `idProfesional`, `diaSemana (0–6)`, `horaInicio`, `horaFin`, `horaInicioDescanso`, `horaFinDescanso`, `margenMin`.

**Bloqueo:** `idBloqueo`, `idProfesional`, `fechaDesde`, `fechaHasta`, `motivo`, `todoElDia`.

### 4.3. Reserva de turnos (RF-03)
| ID | Requerimiento | Prioridad |
|----|---------------|-----------|
| RF-03.1 | El formulario de reserva solicita: **servicio**, **profesional** (opcional "primero disponible") y **fecha/hora**. | Alta |
| RF-03.2 | Mostrar **solo horarios disponibles** según profesional, horario laboral, franjas bloqueadas y turnos existentes. | Alta |
| RF-03.3 | Validación de disponibilidad en el backend (evita doble reserva por concurrencia — transacción + check de overlap). | Alta |
| RF-03.4 | Generar `código de turno` único (ej.: TN-20260920-0001). | Alta |
| RF-03.5 | Turno creado por cliente queda en estado **Pendiente de confirmación** o **Reservado**, según config (RF-03.6). | Media |
| RF-03.6 | Flag configurable: `confirmacionAutomatica = true/false` para confirmar turnos al crearse. | Media |
| RF-03.7 | Notificación por email/WhatsApp al cliente y al profesional (mock Fase 1, integración Fase 2). | Baja |

**Atributos de Turno:** `idTurno`, `codigo`, `idCliente`, `idProfesional`, `idServicio`, `fechaHoraInicio`, `fechaHoraFin`, `estado`, `precioAplicado`, `observaciones`, `motivoCancelacion`, `creadoPor`, `createdAt`, `updatedAt`.

### 4.4. Reprogramación y cancelación (RF-04)
| ID | Requerimiento | Prioridad |
|----|---------------|-----------|
| RF-04.1 | Reprogramar turno validando nuevamente disponibilidad. | Alta |
| RF-04.2 | Cancelación con registro de motivo y rol/usuario que cancela. | Alta |
| RF-04.3 | El cupo liberado queda disponible de inmediato. | Alta |
| RF-04.4 | Si se cancela dentro de la ventana prohibida (ver RN.7), registrar como cancelación tardía (configurable si aplica penalidad). | Media |

### 4.5. Agenda y consultas (RF-05)
| ID | Requerimiento | Prioridad |
|----|---------------|-----------|
| RF-05.1 | Vista de agenda **por día/semana**, filtrable por profesional y servicio. | Alta |
| RF-05.2 | Vista de agenda del **profesional** (solo sus turnos). | Alta |
| RF-05.3 | Búsqueda de turnos por cliente, código, fecha o estado. | Media |
| RF-05.4 | Exportar agenda del día a PDF/CSV. | Baja |

### 4.6. Estado y seguimiento del turno (RF-06)
| ID | Requerimiento | Prioridad |
|----|---------------|-----------|
| RF-06.1 | El profesional puede cambiar estado a **En curso**, **Completado** o **No asistió**. | Alta |
| RF-06.2 | Al completar, registrar observaciones y recaudación efectivizada. | Media |
| RF-06.3 | Historial de estados del turno (auditoría). | Media |

### 4.7. Usuarios y autenticación (RF-07)
> Estado: **en curso** — implementado: registro/login de clientes con JWT, creación de usuarios internos por Admin, roles. Pendiente: recupero de contraseña, perfil.

| ID | Requerimiento | Prioridad |
|----|---------------|-----------|
| RF-07.1 | Registro e inicio de sesión de clientes (JWT). | Alta |
| RF-07.2 | Login de usuarios internos (admin/recepcionista/profesional) por rol. | Alta |
| RF-07.3 | Recupero de contraseña (email). | Media |
| RF-07.4 | Perfil de cliente: datos personales, historial de turnos. | Media |

### 4.8. Reportes (RF-08) — Fase 2
| ID | Requerimiento | Prioridad |
|----|---------------|-----------|
| RF-08.1 | Turnos por profesional / servicio / período. | Media |
| RF-08.2 | Recaudación estimada vs. efectivizada. | Media |
| RF-08.3 | Ausentismo (no asistió), cancelaciones y reprogramaciones. | Baja |

---

## 5. Modelo de datos (PostgreSQL)

### 5.1. Diagrama de entidades (resumen)

```
Usuario 1──N Cliente  ──┐
                        ├─── N:M (a través de Turno)
Profesional N──M Servicio (ProfesionalServicio)
HorarioSemanal 1──N Profesional
Bloqueo        1──N Profesional
Turno  N──1 Profesional
Turno  N──1 Servicio
Turno  N──1 Cliente
TurnoHistorial 1──N Turno
```

### 5.2. Tablas principales

```sql
-- Usuarios / autenticación
usuarios(id SERIAL PK, email UNIQUE NOT NULL, passwordHash, rol,
         activo BOOL DEFAULT true, createdAt, updatedAt);

clientes(id SERIAL PK, usuarioId FK, nombre, apellido, telefono,
         fechaNacimiento, notas, createdAt, updatedAt);

profesionales(id SERIAL PK, usuarioId FK, nombre, apellido, telefono,
              especialidad, bio, activo BOOL DEFAULT true, createdAt, updatedAt);

servicios(id SERIAL PK, nombre, descripcion, duracionMin INT,
          precio DECIMAL(10,2), categoria, activo BOOL, createdAt, updatedAt);

profesionales_servicios(profesionalId FK, servicioId FK, PRIMARY KEY(profesionalId, servicioId));

horarios_semanales(id SERIAL PK, profesionalId FK, diaSemana INT CHECK(0..6),
                   horaInicio TIME, horaFin TIME,
                   inicioDescanso TIME NULL, finDescanso TIME NULL,
                   margenMin INT DEFAULT 0);

bloqueos(id SERIAL PK, profesionalId FK, fechaDesde DATE, fechaHasta DATE,
        todoElDia BOOL, motivo);

turnos(id SERIAL PK, codigo UNIQUE, clienteId FK, profesionalId FK, servicioId FK,
       fechaHoraInicio TIMESTAMPTZ, fechaHoraFin TIMESTAMPTZ,
       estado VARCHAR(20), precioAplicado DECIMAL(10,2),
       observaciones TEXT, motivoCancelacion TEXT, creadoPor VARCHAR(20),
       createdAt TIMESTAMPTZ DEFAULT now(), updatedAt TIMESTAMPTZ,
       CONSTRAINT chk_estado CHECK (estado IN
          ('pendiente','reservado','en_curso','completado','cancelado','no_asistio')));

turnos_historial(id SERIAL PK, turnoId FK, estadoAnterior, estadoNuevo,
                 usuarioId FK, fechaCambio TIMESTAMPTZ DEFAULT now(),
                 detalle TEXT);
```

**Índices recomendados:**
- `turnos(idProfesional, fechaHoraInicio)` — rápida validación de disponibilidad.
- `turnos(idCliente)` — historial del cliente.
- `turnos(fechaHoraInicio)` — vista de agenda del día.
- `turnos(estado)` — reportes.

**Validación de disponibilidad (anti-overlap) — consulta clave:**
```sql
SELECT COUNT(*) FROM turnos t
WHERE t.profesionalId = @prof
  AND t.estado IN ('pendiente','reservado','en_curso')
  AND t.fechaHoraInicio < @finNuevo
  AND t.fechaHoraFin > @iniNuevo;
-- Debe dar 0 para permitir la reserva. Ejecutar dentro de una transacción
-- con lock de fila en el horario del profesional.
```

---

## 6. Catálogo de estados del turno

```
                        ┌─────────────────────────────┐
                        v                             │
pendiente ──> reservado ──> en_curso ──> completado
                  │          │               ▲
                  │          └─── no_asistio ┘
                  └──────> cancelado (motivo)
```

| Estado | Significado |
|--------|-------------|
| `pendiente` | Creado por autoservicio, aguardando confirmación. |
| `reservado` | Turno confirmado/agendado. |
| `en_curso` | El profesional inició la atención. |
| `completado` | Servicio finalizado (con observaciones opcionales). |
| `cancelado` | Cancelado por cliente o staff, con motivo. |
| `no_asistio` | El cliente no se presentó. |

---

## 7. Arquitectura tecnológica

```
┌─────────────────┐     HTTPS      ┌──────────────────┐      ┌──────────────┐
│  Angular (SPA)  │ ──────────────►│  Backend C# /    │ ────►│  PostgreSQL  │
│  Angular CLI 17 │    JSON/JWT    │  ASP.NET Core    │  EF  │              │
│  Standalone     │                │  Web API (.NET 8)│ Core │              │
└─────────────────┘                └──────────────────┘      └──────────────┘
```

### Capas del backend (C#)
1. **Controllers** (API REST) — `TurnosController`, `ProfesionalesController`, `ServiciosController`, `ClientesController`, `AuthController`, `AgendaController`.
2. **Services** — lógica de negocio: `DisponibilidadService`, `TurnoService`, `AgendaService`.
3. **Repositories / EF Core DbContext** — acceso a datos con PostgreSQL (Npgsql).
4. **DTOs** — contratos de entrada/salida (`CreateTurnoDto`, `TurnoDto`, etc.).
5. **Seguridad** — JWT (Identity o JWT bearer), roles, RefreshToken.

### Estructura sugerida del proyecto front (Angular)
```
src/app/
├── core/            (auth, guards, interceptores, servicios HTTP)
├── features/
│   ├── auth/        (login, registro)
│   ├── reservas/    (nueva reserva, mis turnos)
│   ├── agenda/      (agenda por día/semana, filtros)
│   ├── profesionales/ (ABM)
│   ├── servicios/   (ABM)
│   └── admin/       (usuarios, config, reportes)
└── shared/          (componentes y pipes reutilizables)
```

### Puntos críticos de diseño
- **Concurrencia de reserva**: usar transacción + `SELECT ... FOR UPDATE` sobre `horarios_semanales` o bloqueo optimista (columna `rowversion`) para evitar dobles reservas.
- **Zonas horarias**: manejar todo en `TIMESTAMPTZ`; el front normaliza a la zona local.
- **Caché de disponibilidad**: opcional en Fase 2 (Redis) para reducir carga de consultas de agenda.
- **Validación de duración**: `fechaHoraFin = fechaHoraInicio + duracionMin + margenMin`.

---

## 8. Historias de usuario (HU) — priorizadas

| ID | Historia de usuario | Criterios de aceptación | Prioridad |
|----|---------------------|-------------------------|-----------|
| HU-01 | Como administrador quiero dar de alta servicios para ofrecerlos en reserva. | Se crea servicio con duración y precio; aparece en el catálogo de reserva. | Alta |
| HU-02 | Como administrador quiero dar de alta profesionales y asignarles servicios. | Profesional creado; relación Servicio–Profesional persistida; no aparece en reserva un servicio que no presta. | Alta |
| HU-03 | Como administrador quiero definir el horario semanal por profesional. | El sistema solo ofrece turnos dentro del horario y fuera del descanso definido. | Alta |
| HU-04 | Como recepcionista quiero asignar un turno a un cliente indicando servicio, profesional, fecha y hora. | Solo se muestran horarios disponibles; el backend rechaza solapamientos; se genera código de turno. | Alta |
| HU-05 | Como cliente quiero reservar en línea eligiendo servicio y profesional. | Reserva validada con disponibilidad; recibo código/confirmación; mi turno figura en "Mis turnos". | Alta |
| HU-06 | Como profesional quiero ver solo mi agenda del día y marcar estados. | Lista mis turnos ordenados; puedo pasar a En curso/Completado/No asistió. | Alta |
| HU-07 | Como recepcionista quiero reprogramar o cancelar turnos. | Reprogramación revalida disponibilidad; cancelación registra motivo y libera el cupo. | Alta |
| HU-08 | Como administrador quiero ver reportes de turnos por profesional/servicio. | Reporte filtrable por rango de fechas, con recuento y totales. | Media |

---

## 9. Requerimientos no funcionales (RNF)

| RNF | Descripción |
|-----|-------------|
| Rendimiento | Respuesta de consulta de disponibilidad < 500 ms con agenda razonable; consultas indexadas. |
| Disponibilidad | Sistema web disponible en horario comercial (objetivo 99%). |
| Seguridad | Autenticación JWT; roles; BCrypt/Argon2 para contraseñas; HTTPS obligatorio; validación de permisos por endpoint. |
| Concurrencia | 30+ usuarios simultáneos; reserva sin conflictos (transacciones aisladas). |
| Auditoría | Historial de cambios de estado y de ABM (campos `createdAt`/`updatedAt` + `turnos_historial`). |
| Compatibilidad | Responsive (escritorio, tablet, móvil); Chrome, Safari, Edge. |
| Escalabilidad | Separación de capas que permita migrar a microservicios o agregar Redis/mensajería. |
| Accesibilidad | Navegación por teclado, contraste AA, ARIA en formularios clave. |
| Idioma | Interfaz en español (diseñar i18n para futuras traducciones). |

---

## 10. Plan de implementación por fases

| Fase | Entregables | Incluye |
|------|-------------|---------|
| **Fase 1 — MVP** | Reserva + agenda básica | ABM servicios, profesionales, horarios; reserva con disponibilidad; validación anti-solape; agenda por día; estados básicos; auth con roles; turnos de clientes. |
| **Fase 2** | Autoservicio de clientes | Reserva web del cliente, reprogramar/cancelar propio, historial, confirmación por email. |
| **Fase 3** | Operación avanzada | Bloqueos de agenda, margen configurable, exportar agenda (PDF/CSV), notificaciones (email/WhatsApp), auditoría de estados. |
| **Fase 4** | Reportes y métricas | Recaudación, ausentismo, top servicios/profesionales, dashboard administrativo. |

---

## 11. Casos de uso críticos (probar en QA)

1. **Doble reserva concurrente**: 2 clientes intentan reservar el mismo horario de un profesional → solo 1 debe triunfar.
2. **Turno que cruza el descanso** del profesional → rechazado.
3. **Servicio que no presta el profesional** → bloqueado en la UI y validado en API.
4. **Cancelación que libera el cupo** y queda visible para nueva reserva.
5. **Reprogramar a un horario ya ocupado** → rechazado con mensaje claro.
6. **Cambio de estado fuera de secuencia** (ej.: completado sin haber estado en curso) → control por regla de negocio.
7. **Horario de fin de día** (23:50 con duración 30 min) → supera límite, se rechaza.
8. **Zona horaria / cambio de hora** (DST) → los turnos no se desplazan incorrectamente.

---

## 12. Glosario

| Término | Definición |
|---------|------------|
| Turno | Reserva de fecha+horario de un servicio con un profesional y cliente. |
| Solapamiento | Dos turnos del mismo profesional cuyas franjas horarias se cruzan. |
| Franja de descanso | Rango horario no reservable dentro de la jornada laboral. |
| Bloqueo | Período (días) donde el profesional no atiende (vacaciones, permiso). |
| Margen | Tiempo de buffer entre turnos (limpieza, preparación). |
| Overbooking | Permitir más reservas que capacidad disponible (se deshabilita en v1). |
| ABM | Alta, Baja y Modificación (CRUD). |

---

## 13. Preguntas abiertas para el cliente

1. ¿Los profesionales cobran porcentaje por servicio o salario fijo? (impacta reportes).
2. ¿Se necesita gestión de pagos anticipados/señas al reservar?
3. ¿Ventana máxima de reserva (con cuánta anticipación puedo reservar)?
4. ¿Confirman turnos manualmente (recepcionista) o autoconfirmados?
5. ¿Es necesario multiusuario con user/pass para clientes, o alcanza con datos de contacto?
6. ¿Necesitan imprimir comprobante de turno?
7. ¿Cada profesional puede cambiar su duración por servicio o es fija?

---

## 14. Próximos pasos

1. Validar este documento con el cliente y resolver preguntas abiertas.
2. Definir prototipo de baja fidelidad (wireframes) de reserva y agenda.
3. Generar modelo Entidad–Relación definitivo y script de migración inicial (EF Core).
4. Aprobar fases y priorización de HU.
5. Iniciar Fase 1 (MVP) con configuración del proyecto Angular + .NET 8 + PostgreSQL.