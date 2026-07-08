<div align="center">

# 🛡️ SafeWoman

**Aplicación móvil de seguridad para mujeres víctimas de violencia en Ayacucho, Perú.**

[![.NET](https://img.shields.io/badge/.NET-8.0-512BD4?logo=dotnet&logoColor=white)](https://dotnet.microsoft.com/)
[![PostgreSQL](https://img.shields.io/badge/PostgreSQL-16-4169E1?logo=postgresql&logoColor=white)](https://www.postgresql.org/)
[![MAUI](https://img.shields.io/badge/.NET_MAUI-Android-blueviolet?logo=xamarin&logoColor=white)](https://learn.microsoft.com/dotnet/maui/)
[![Render](https://img.shields.io/badge/Deployed-Render.com-46E3B7?logo=render&logoColor=white)](https://safewoman-api.onrender.com)
[![Tests](https://img.shields.io/badge/tests-187%20passing-success)](Tests/)
[![Coverage](https://img.shields.io/badge/coverage-97.8%25-brightgreen)](DOCUMENTACION.md#155-cobertura-de-código)
[![Ley](https://img.shields.io/badge/cumple-Ley%2030364%20%26%2029733-red)](DOCUMENTACION.md#3-marco-legal-peruano)

*Proyecto académico — Universidad Nacional de San Cristóbal de Huamanga (UNSCH)*

</div>

---

## 🎯 ¿Qué es SafeWoman?

SafeWoman permite a las víctimas de violencia:

- 📱 **Denunciar** de forma **formal** o **anónima** desde su teléfono, adjuntando evidencia multimedia.
- 🆘 **Activar una alerta SOS** que notifica a contactos de emergencia con la **ubicación GPS exacta** en segundos.
- 🔒 **Proteger su identidad** — modo anónimo con solo huella del dispositivo.
- 🗺️ **Rastrear el estado** de sus denuncias en tiempo real.

Las autoridades usan un **panel administrativo web** para atender alertas y gestionar denuncias en vivo (SignalR).

## 🌐 Enlaces de producción

| Recurso | URL |
|---|---|
| 🌍 API pública | [safewoman-api.onrender.com](https://safewoman-api.onrender.com) |
| 🖥️ Panel administrativo | [/panel-safewoman/Auth/Login](https://safewoman-api.onrender.com/panel-safewoman/Auth/Login) |
| 📖 Documentación técnica de la API | [/swagger](https://safewoman-api.onrender.com/swagger) |
| 📱 APK Android | Distribuido vía Google Drive |
| 📂 Repositorio | [github.com/Crisso29/safewoman-api](https://github.com/Crisso29/safewoman-api) |

## 🏗️ Arquitectura

Sistema construido con **Clean Architecture** — 4 capas concéntricas donde las dependencias apuntan hacia el dominio:

```
┌─────────────────────────────────────────────────────────────────┐
│                        📱 SafeWoman (MAUI)                      │
│                    App Android para víctimas                    │
└──────────────────────────┬──────────────────────────────────────┘
                           │ HTTPS + JWT
                           ▼
┌─────────────────────────────────────────────────────────────────┐
│                       🌐 SafeWoman.API                          │
│              REST endpoints + Panel Admin + SignalR             │
├─────────────────────────────────────────────────────────────────┤
│                    🔌 SafeWoman.Infrastructure                  │
│          EF Core · Twilio · JWT · Storage · Geocoding           │
├─────────────────────────────────────────────────────────────────┤
│                    ⚙️  SafeWoman.Application                    │
│                     Servicios · DTOs · Validators               │
├─────────────────────────────────────────────────────────────────┤
│                      🎯 SafeWoman.Domain                        │
│                Entidades · Reglas de negocio                    │
└─────────────────────────────────────────────────────────────────┘
        │                    │                       │
        ▼                    ▼                       ▼
   🐘 PostgreSQL       📩 Twilio SMS          🗺️ Nominatim / OSM
   (Neon Cloud)         (OTP + SOS)          (Reverse geocoding)
```

**Stack completo:**

- **Backend**: ASP.NET Core 8, Entity Framework Core 8, PostgreSQL 16, SignalR
- **Frontend móvil**: .NET MAUI 8 (Android), MVVM con CommunityToolkit.Mvvm
- **Seguridad**: JWT (HS256), BCrypt, Rate limiting, HSTS + cabeceras OWASP
- **Servicios externos**: Twilio (SMS), Nominatim (geocoding), CartoDB (mapas)
- **Testing**: xUnit + Moq + FluentAssertions + Testcontainers
- **DevOps**: Docker, GitHub Actions CI/CD, Render.com, UptimeRobot

## 📁 Estructura del proyecto

```
SafeWoman/                          ← Solución completa
│
├── 🎯 SafeWoman.Domain/            ← Capa 1: Núcleo de negocio (sin dependencias)
│   ├── Entities/                       Víctima, Denuncia, AlertaSos, ...
│   ├── Enums/                          TipoDenuncia, EstadoAlerta, ...
│   ├── Exceptions/                     DomainException
│   └── Interfaces/                     IRepository, IUnitOfWork
│
├── ⚙️ SafeWoman.Application/       ← Capa 2: Casos de uso (depende solo de Domain)
│   ├── Services/                       AuthService, AlertaSosService, ...
│   ├── DTOs/                           RegistroRequest, AuthResponse, ...
│   ├── Interfaces/                     Contratos de servicios externos
│   └── Validators/                     FluentValidation
│
├── 🔌 SafeWoman.Infrastructure/    ← Capa 3: Implementaciones concretas
│   ├── Persistence/                    DbContext, Repositorios, Configurations
│   ├── Services/Security/              BCrypt, JWT, OTP generator
│   ├── Services/Sms/                   Twilio, Console (dev)
│   ├── Services/Geocoding/             Nominatim
│   ├── Services/Storage/               LocalFileStorage
│   └── Services/Realtime/              SignalR notifier
│
├── 🌐 SafeWoman.API/               ← Capa 4: Web API + Panel Admin
│   ├── Controllers/                    Endpoints REST (móvil)
│   ├── Areas/Admin/                    MVC del panel administrativo
│   ├── Middleware/                     ExceptionMiddleware
│   ├── Infrastructure/                 DbSeeder, SosHub
│   ├── Program.cs                      Bootstrap ASP.NET Core
│   └── appsettings.json                Config pública
│
├── 📱 SafeWoman/                   ← App móvil MAUI (Android)
│   ├── Views/                          Páginas XAML
│   ├── ViewModels/                     Lógica de presentación (MVVM)
│   ├── Services/                       ApiService, LocationService, ...
│   ├── Platforms/Android/              Código específico Android
│   └── MauiProgram.cs                  Bootstrap MAUI
│
├── 🧪 Tests/                       ← Pirámide de testing (187 tests)
│   ├── SafeWoman.UnitTests/            158 tests · xUnit + Moq
│   ├── SafeWoman.IntegrationTests/     11 tests · Testcontainers PostgreSQL
│   └── SafeWoman.ApiTests/             18 tests · WebApplicationFactory
│
├── 🚀 .github/workflows/           ← CI/CD
│   └── ci.yml                          GitHub Actions (5 jobs)
│
├── 🐳 Dockerfile                   ← Imagen productiva (para Render)
├── 📄 README.md                    ← Este archivo
├── 📚 DOCUMENTACION.md             ← Documentación técnica completa
└── 🚢 DEPLOY.md                    ← Guía de despliegue paso a paso
```

**¿Qué se sube al repositorio y qué NO?** → ver [Estructura de carpetas en DOCUMENTACION.md](DOCUMENTACION.md#19-estructura-de-carpetas-y-git).

## 🚀 Ejecución local

### Requisitos

- [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0)
- [Docker Desktop](https://www.docker.com/products/docker-desktop/) *(solo para tests de integración/API)*
- Un cliente PostgreSQL (opcional — puedes usar Neon o local)

### Pasos

```bash
# 1. Clonar el repositorio
git clone https://github.com/Crisso29/safewoman-api.git
cd safewoman-api

# 2. Configurar los secretos localmente
cp SafeWoman.API/appsettings.json SafeWoman.API/appsettings.Development.json
# Edita appsettings.Development.json con tus credenciales (BD, JWT, Twilio)

# 3. Restaurar y compilar
dotnet restore

# 4. Aplicar migraciones a tu BD local
dotnet ef database update -p SafeWoman.Infrastructure -s SafeWoman.API

# 5. Ejecutar la API
dotnet run --project SafeWoman.API

# La API arranca en https://localhost:7273
```

### Correr los tests

```bash
# Suite completa (187 tests, ~14 seg — Docker Desktop debe estar corriendo)
dotnet test

# Solo tests unitarios (rápido, sin Docker)
dotnet test Tests/SafeWoman.UnitTests/

# Con reporte HTML de cobertura
dotnet test --collect:"XPlat Code Coverage"
reportgenerator -reports:"**/coverage.cobertura.xml" -targetdir:CoverageReport
```

## 🧪 Testing

Pirámide de testing profesional con **187 tests · 97.8% cobertura de líneas**.

| Nivel | Cantidad | Tecnología | Tiempo |
|---|---|---|---|
| 🧪 Unit tests | 158 | xUnit + Moq + FluentAssertions | 5 seg |
| 🐘 Integration tests | 11 | Testcontainers PostgreSQL 16 | 3 seg |
| 🌐 API tests | 18 | WebApplicationFactory + Testcontainers | 5 seg |
| **Total** | **187** | | **~14 seg** |

Los tests corren automáticamente en **[GitHub Actions](https://github.com/Crisso29/safewoman-api/actions)** en cada push y pull request.

Detalles completos en la [sección 15 de DOCUMENTACION.md](DOCUMENTACION.md#15-pruebas-y-verificación).

## 🔒 Seguridad

Defensa en profundidad con **7 capas**:

- 🔐 **HTTPS + HSTS** (Render maneja SSL en el edge)
- 🍪 **Cookies HttpOnly + SameSite=Lax + Secure**
- 🔑 **JWT firmado con HS256** (rotable via env var)
- 🧂 **BCrypt** para contraseñas (work factor 12)
- 🚦 **Rate limiting** — 10 req/min por IP en autenticación
- 🛡️ **Cabeceras OWASP** — X-Frame-Options, X-Content-Type-Options, Referrer-Policy, Permissions-Policy
- 🕵️ **Ofuscación** — panel Admin en `/panel-safewoman/*` (no `/admin`)

Cumplimiento estricto de **Ley N° 30364** (violencia hacia la mujer) y **Ley N° 29733** (protección de datos personales).

## 🚢 Despliegue

Todo el sistema corre en la **capa gratuita**:

- 🌐 **API**: Docker → Render.com (Free Tier + UptimeRobot para no dormir)
- 🐘 **BD**: PostgreSQL 16 en Neon (3 GB gratis)
- 📩 **SMS**: Twilio (pay-per-use, ~$0.08/SMS)
- 📊 **Monitoreo**: UptimeRobot (ping cada 5 min)
- 🔄 **CI/CD**: GitHub Actions (gratis en repos públicos)

Guía paso a paso: **[DEPLOY.md](DEPLOY.md)**.

**Costo mensual estimado**: **~$2 USD** (solo el saldo de Twilio para SMS reales).

## 📚 Documentación

- 📖 **[DOCUMENTACION.md](DOCUMENTACION.md)** — Documentación técnica integral (1500+ líneas)
  - Marco legal, arquitectura, modelo de datos, seguridad, testing, despliegue
- 🚢 **[DEPLOY.md](DEPLOY.md)** — Guía paso a paso de despliegue
- 🌐 **[Swagger](https://safewoman-api.onrender.com/swagger)** — Documentación interactiva de la API en producción

## 🎓 Contexto académico

Proyecto desarrollado como trabajo académico en la **Universidad Nacional de San Cristóbal de Huamanga (UNSCH)** — carrera de Ingeniería de Sistemas, con enfoque interdisciplinario con Derecho.

**Motivación**: Ayacucho es una de las regiones con mayor incidencia de violencia contra la mujer en el Perú. Las víctimas enfrentan barreras significativas para denunciar (distancia, temor al agresor, revictimización). SafeWoman busca **derribar esas barreras** con una herramienta digital gratuita, discreta y accesible desde cualquier teléfono Android.

## 📄 Licencia

Este proyecto es un trabajo académico. Todo su código está bajo licencia MIT y puede ser reutilizado con fines educativos.

---

<div align="center">

**Desarrollado con ❤️ en Ayacucho, Perú**

*Si este proyecto te resulta útil, considera contribuir con feedback, mejoras o traducciones a lenguas originarias (quechua, aymara).*

</div>
