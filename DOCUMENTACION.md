# SafeWoman — Documentación Técnica Integral

**Aplicación móvil de seguridad para mujeres víctimas de violencia en Ayacucho, Perú**

> Proyecto académico — Universidad Nacional de San Cristóbal de Huamanga (UNSCH)
> Autor: Crisólogo Aguilar
> Fecha: Julio 2026

---

## 📑 Índice

1. [Resumen ejecutivo](#1-resumen-ejecutivo)
2. [Contexto y justificación](#2-contexto-y-justificación)
3. [Marco legal peruano](#3-marco-legal-peruano)
4. [Objetivos](#4-objetivos)
5. [Alcance del sistema](#5-alcance-del-sistema)
6. [Metodología de desarrollo](#6-metodología-de-desarrollo)
7. [Requisitos funcionales](#7-requisitos-funcionales)
8. [Requisitos no funcionales](#8-requisitos-no-funcionales)
9. [Arquitectura del sistema](#9-arquitectura-del-sistema)
10. [Modelo de datos](#10-modelo-de-datos)
11. [Módulos funcionales](#11-módulos-funcionales)
12. [Stack tecnológico](#12-stack-tecnológico)
13. [Seguridad y hardening](#13-seguridad-y-hardening)
14. [Despliegue en la nube](#14-despliegue-en-la-nube)
15. [Pruebas y verificación](#15-pruebas-y-verificación)
16. [Distribución de la app](#16-distribución-de-la-app)
17. [Conclusiones](#17-conclusiones)
18. [Glosario técnico](#18-glosario-técnico)

---

## 1. Resumen ejecutivo

**SafeWoman** es una aplicación móvil Android que permite a mujeres víctimas de violencia en Ayacucho:

- **Registrar denuncias** formales (con identidad) o anónimas, adjuntando evidencias multimedia.
- **Activar una alerta SOS** que notifica a contactos de emergencia con la **ubicación exacta en tiempo real**.
- **Consultar el estado** de sus denuncias.

El sistema cuenta además con un **panel administrativo web** para autoridades (policía, fiscalía, PNP) donde pueden atender alertas SOS, gestionar denuncias, visualizar víctimas y auditar acciones.

La calidad del código se garantiza con una **suite automatizada de 187 tests** distribuidos en tres niveles (unitarios, integración con PostgreSQL real vía Testcontainers, y API end-to-end) que alcanza **97.8% de cobertura de líneas** — superando el estándar de la industria del 90%.

El sistema está **actualmente desplegado en producción**:

| Componente | URL / Ubicación |
|---|---|
| API pública | https://safewoman-api.onrender.com |
| Panel administrativo | https://safewoman-api.onrender.com/panel-safewoman/Auth/Login |
| Documentación técnica | https://safewoman-api.onrender.com/swagger |
| Base de datos | Neon PostgreSQL (Ohio, US East) |
| App móvil | APK Android 8.0+ firmado |

---

## 2. Contexto y justificación

### 2.1 Problemática

En Perú, la violencia contra la mujer es una de las principales problemáticas sociales. Según el **INEI** y el **MIMP** (Ministerio de la Mujer y Poblaciones Vulnerables):

- **6 de cada 10 mujeres peruanas** han sufrido algún tipo de violencia por parte de su pareja alguna vez.
- **Ayacucho** es una de las regiones con **mayor incidencia** por su historial de conflicto interno y sus factores socioeconómicos.
- Los CEM (Centros de Emergencia Mujer) y comisarías presentan barreras: distancia, temor al agresor, revictimización, tiempos de espera.

### 2.2 Necesidad de la solución

Muchas víctimas no denuncian porque:

- El agresor puede vigilar sus movimientos físicos (imposible ir a comisaría).
- Temen represalias si son vistas denunciando.
- Los procesos formales son lentos y presenciales.
- No cuentan con evidencia formalizable en el momento del hecho.

SafeWoman resuelve estos problemas ofreciendo:

- **Denuncia móvil, discreta**, desde cualquier lugar con conexión.
- **Modo anónimo** para quienes temen exponerse.
- **Botón SOS** que en segundos alerta a personas de confianza con la ubicación exacta.
- **Evidencia digital** adjunta (fotos, audios, videos) con marca temporal.

---

## 3. Marco legal peruano

El sistema cumple estrictamente con dos leyes peruanas fundamentales:

### 3.1 Ley N° 30364

**"Ley para prevenir, sancionar y erradicar la violencia contra las mujeres y los integrantes del grupo familiar"** (2015, actualizada).

Establece que el Estado debe:

- Facilitar mecanismos **accesibles** para la denuncia.
- Proteger la **identidad de las víctimas**.
- Garantizar atención **integral, oportuna y de calidad**.
- Establecer **medidas de protección inmediatas**.

**Cómo SafeWoman cumple:**

| Artículo | Cómo lo implementa SafeWoman |
|---|---|
| Art. 15 (denuncia accesible) | App móvil disponible 24/7 desde cualquier smartphone |
| Art. 22 (medidas de protección) | Botón SOS con notificación instantánea a contactos y autoridades |
| Art. 30 (registro único) | Panel administrativo centralizado con base de datos única |
| Art. 45 (confidencialidad) | Modo anónimo + cifrado en tránsito (HTTPS) + JWT |

### 3.2 Ley N° 29733

**"Ley de Protección de Datos Personales"** (2011).

Establece que los datos personales deben:

- Recolectarse con **consentimiento libre, previo, expreso e informado**.
- Ser **cifrados** durante su transmisión y almacenamiento.
- Ser **accesibles solo por personal autorizado**.
- Ser **conservados** por el tiempo estrictamente necesario.

**Cómo SafeWoman cumple:**

| Requisito | Implementación técnica |
|---|---|
| Consentimiento explícito | Pantalla de aceptación de términos durante el registro |
| Cifrado en tránsito | HTTPS/TLS 1.3 con HSTS (obliga solo conexiones cifradas) |
| Cifrado en almacenamiento | Passwords con hash BCrypt (irreversible) |
| Acceso restringido | JWT + rate limiting + panel Admin con cookies HttpOnly + hardening OWASP |
| Auditoría | Tabla `LogAuditoria` que registra todas las acciones sensibles |
| Retención mínima | OTPs se eliminan tras usarse o expirar; datos de sesión limitados a 168h |

---

## 4. Objetivos

### 4.1 Objetivo general

Diseñar, desarrollar y desplegar una aplicación móvil integral que permita a mujeres víctimas de violencia en Ayacucho realizar denuncias formales o anónimas, activar alertas de emergencia con geolocalización y coordinar la respuesta con autoridades competentes.

### 4.2 Objetivos específicos

1. Implementar un **sistema de registro y autenticación** seguro para víctimas usando verificación OTP por SMS.
2. Desarrollar un módulo de **denuncia formal** con captura de evidencias (fotos, audios, videos).
3. Habilitar un módulo de **denuncia anónima** sin trazabilidad hacia la denunciante.
4. Implementar un **botón SOS** que active en 3 segundos una alerta con ubicación GPS y notifique por SMS a los contactos de emergencia.
5. Crear un **panel administrativo web** para autoridades donde puedan atender alertas y gestionar denuncias en tiempo real (SignalR).
6. Cumplir con el **marco legal peruano** (Ley 30364 y 29733).
7. Desplegar el sistema en **infraestructura cloud gratuita** para garantizar disponibilidad 24/7 sin costos operativos.

---

## 5. Alcance del sistema

### 5.1 Incluido

- **App móvil Android** (soporte desde Android 8.0 Oreo — 96 % del parque nacional).
- **API REST** para lógica de negocio.
- **Panel Web administrativo** (autoridades).
- Base de datos **PostgreSQL en la nube**.
- Sistema de **notificaciones SMS** vía Twilio.
- **Geolocalización + mapas interactivos**.
- **Geocodificación** (búsqueda de direcciones por nombre).
- **Auditoría de todas las acciones sensibles**.

### 5.2 Fuera de alcance (versiones futuras)

- Versión iOS (requeriría cuenta Apple Developer, $99/año).
- Videollamada con operador CEM.
- Integración directa con sistemas del **Ministerio Público** y **PNP**.
- Reconocimiento facial de agresores.
- Chatbot de contención emocional con IA.

---

## 6. Metodología de desarrollo

### 6.1 Enfoque: SCRUM ágil + Domain-Driven Design (DDD)

Se utilizó una **metodología ágil híbrida** que combina:

- **SCRUM** para gestión de iteraciones (sprints semanales).
- **Domain-Driven Design (DDD)** para modelado del dominio de negocio.
- **Clean Architecture** para separación de responsabilidades.
- **Test-driven en puntos críticos** (autenticación, SOS).

### 6.2 Ciclo de vida del desarrollo

```
┌──────────────────────────────────────────────────────────────┐
│                 CICLO ITERATIVO SEMANAL                      │
├──────────────────────────────────────────────────────────────┤
│                                                              │
│  1. PLANIFICACIÓN  ─────────► Backlog priorizado             │
│         │                                                    │
│         ▼                                                    │
│  2. DISEÑO ────────────────► Wireframes + entidades          │
│         │                                                    │
│         ▼                                                    │
│  3. IMPLEMENTACIÓN ────────► Código Domain → App → Infra → API│
│         │                                                    │
│         ▼                                                    │
│  4. PRUEBAS ────────────────► Emulador + dispositivo físico  │
│         │                                                    │
│         ▼                                                    │
│  5. INTEGRACIÓN ────────────► Git commit + push a main       │
│         │                                                    │
│         ▼                                                    │
│  6. DESPLIEGUE ─────────────► Render.com redeploy automático │
│         │                                                    │
│         ▼                                                    │
│  7. VERIFICACIÓN ───────────► Test en producción             │
│         │                                                    │
│         └──────► RETROSPECTIVA → siguiente iteración         │
│                                                              │
└──────────────────────────────────────────────────────────────┘
```

### 6.3 Herramientas usadas

| Fase | Herramienta |
|---|---|
| Planificación | Notas Markdown + TASKS.md |
| Diseño UI | XAML previews + wireframes manuales |
| Desarrollo | Visual Studio 2022 Community |
| Control de versiones | Git + GitHub |
| Testing | Emulador Android + Samsung físico (dispositivo real) |
| CI/CD | GitHub → Render.com auto-deploy |
| Monitoreo | UptimeRobot (uptime 99.9%) |

---

## 7. Requisitos funcionales

Cada requisito tiene un identificador único **RF-XXX** para trazabilidad.

### 7.1 Módulo Víctima (usuaria)

| Código | Requisito |
|---|---|
| **RF-001** | La víctima debe poder registrarse con DNI, nombre, teléfono y foto del DNI. |
| **RF-002** | El sistema debe enviar un **código OTP de 6 dígitos** por SMS al teléfono ingresado. |
| **RF-003** | La víctima debe validar su cuenta ingresando el OTP recibido. |
| **RF-004** | La víctima debe poder iniciar sesión con teléfono + código único. |
| **RF-005** | El sistema debe emitir un **token JWT** con expiración de 168 horas para autenticación posterior. |
| **RF-006** | La víctima debe poder registrar hasta **5 contactos de emergencia** (nombre + teléfono + relación). |
| **RF-007** | La víctima debe poder editar y eliminar sus contactos de emergencia. |

### 7.2 Módulo Denuncia Formal

| Código | Requisito |
|---|---|
| **RF-008** | La víctima autenticada debe poder crear una denuncia formal con tipo de violencia (física, psicológica, sexual, económica, patrimonial). |
| **RF-009** | La denuncia debe incluir descripción, ubicación GPS o dirección, y fecha del hecho. |
| **RF-010** | La víctima puede agregar datos del denunciado (nombre, DNI si conoce, relación con la víctima). |
| **RF-011** | Se pueden adjuntar hasta **5 evidencias** (fotos, audios, videos) de hasta **10 MB c/u**. |
| **RF-012** | La víctima puede consultar el estado de sus denuncias: **pendiente, en revisión, atendida, archivada**. |

### 7.3 Módulo Denuncia Anónima

| Código | Requisito |
|---|---|
| **RF-013** | Cualquier persona (víctima o testigo) puede crear una denuncia sin autenticación. |
| **RF-014** | La denuncia anónima **no vincula** la identidad del denunciante. |
| **RF-015** | Se pueden adjuntar evidencias del mismo modo que la denuncia formal. |
| **RF-016** | La denuncia anónima debe incluir ubicación aproximada del hecho. |

### 7.4 Módulo SOS

| Código | Requisito |
|---|---|
| **RF-017** | El botón SOS debe requerir presión sostenida de **3 segundos** (evitar activaciones accidentales). |
| **RF-018** | Al activarse, el sistema debe capturar la **ubicación GPS actual** (con precisión de hasta 5 metros). |
| **RF-019** | El sistema debe enviar **SMS con la ubicación** (link a Google Maps) a **cada contacto de emergencia** registrado. |
| **RF-020** | El SMS debe incluir el nombre de la víctima, coordenadas y dirección aproximada (reverse-geocoding). |
| **RF-021** | La alerta debe aparecer **en tiempo real** en el panel administrativo (vía SignalR). |
| **RF-022** | La víctima puede **cancelar la alerta** desde la app si fue accidental. |

### 7.5 Módulo Panel Administrativo

| Código | Requisito |
|---|---|
| **RF-023** | El administrador debe autenticarse con email + password. |
| **RF-024** | Debe existir un **dashboard con métricas**: total víctimas, denuncias activas, SOS pendientes. |
| **RF-025** | Debe listar todas las alertas SOS con opción de **atender** o marcar como falsa. |
| **RF-026** | Debe listar todas las denuncias con filtros por estado, tipo y fecha. |
| **RF-027** | Debe permitir cambiar el estado de las denuncias. |
| **RF-028** | Debe registrar en el **log de auditoría** todas las acciones sensibles del administrador. |
| **RF-029** | Debe listar las víctimas registradas con paginación. |

---

## 8. Requisitos no funcionales

| Código | Categoría | Requisito |
|---|---|---|
| **RNF-01** | Seguridad | Todos los datos deben viajar cifrados vía **HTTPS con TLS 1.3**. |
| **RNF-02** | Seguridad | Las contraseñas deben almacenarse con **hash BCrypt** (irreversible). |
| **RNF-03** | Seguridad | Autenticación móvil vía **JWT firmado con HS256**. |
| **RNF-04** | Seguridad | Rate limiting: máximo **10 intentos de login/minuto por IP**. |
| **RNF-05** | Seguridad | Cabeceras OWASP: HSTS, X-Frame-Options, X-Content-Type-Options, Referrer-Policy, Permissions-Policy. |
| **RNF-06** | Rendimiento | Tiempo de respuesta API < **500 ms** para operaciones CRUD. |
| **RNF-07** | Rendimiento | SMS SOS debe enviarse en < **3 segundos** desde la activación. |
| **RNF-08** | Disponibilidad | Uptime objetivo: **99.5 %** anual. |
| **RNF-09** | Escalabilidad | Base de datos PostgreSQL soporta hasta **1 000 víctimas concurrentes**. |
| **RNF-10** | Usabilidad | La interfaz debe ser usable por personas con **bajo alfabetismo digital** (íconos claros, texto grande). |
| **RNF-11** | Compatibilidad | Android **8.0 (API 26) y superior** (96 % del parque nacional). |
| **RNF-12** | Auditoría | Toda acción del administrador debe quedar registrada con timestamp, IP y usuario. |
| **RNF-13** | Cumplimiento | Cumplimiento estricto de **Ley N° 30364 y N° 29733**. |
| **RNF-14** | Portabilidad | Deploy sobre **Docker** — portable a cualquier proveedor cloud. |
| **RNF-15** | Mantenibilidad | Código organizado en **Clean Architecture** con separación de responsabilidades. |

---

## 9. Arquitectura del sistema

### 9.1 Vista general (alto nivel)

```
┌────────────────────────────────────────────────────────────────────────────────┐
│                          ARQUITECTURA DE 3 CAPAS                               │
├────────────────────────────────────────────────────────────────────────────────┤
│                                                                                │
│  ┌────────────────────────┐        ┌──────────────────────┐                    │
│  │   📱 APP MÓVIL         │        │   💻 PANEL WEB        │                   │
│  │   .NET MAUI 8          │        │   ASP.NET Core MVC   │                    │
│  │   (Víctimas)           │        │   (Autoridades)      │                    │
│  └───────────┬────────────┘        └──────────┬───────────┘                    │
│              │                                │                                │
│              │   HTTPS/TLS 1.3   +   JWT      │  HTTPS + Cookie HttpOnly       │
│              └──────────────┬─────────────────┘                                │
│                             │                                                  │
│                             ▼                                                  │
│  ┌─────────────────────────────────────────────────────────────────────────┐   │
│  │                    ☁️ API REST — ASP.NET Core 8                          │  │
│  │  ┌──────────────────────────────────────────────────────────────────┐   │   │
│  │  │  Controllers ─► Application Services ─► Domain Entities          │   │   │
│  │  │       │                                                          │   │   │
│  │  │       ▼                                                          │   │   │
│  │  │  Infrastructure (EF Core + Repositories + External Services)     │   │   │
│  │  └──────────────────────────────────────────────────────────────────┘   │   │
│  │                    Desplegado en Render.com                             │   │
│  └────────────┬──────────────────────────┬──────────────────────┬────────┘    │
│               │                          │                      │             │
│               ▼                          ▼                      ▼             │
│  ┌────────────────────────┐  ┌──────────────────┐  ┌────────────────────┐     │
│  │  🐘 PostgreSQL (Neon)  │  │  📩 Twilio SMS   │  │  🗺️ Nominatim /   │      │
│  │  Base de datos         │  │  OTP + SOS       │  │  CartoDB Tiles     │     │
│  │  cifrada, replicada    │  │  cifrado E2E     │  │  Geocoding + mapas │     │
│  └────────────────────────┘  └──────────────────┘  └────────────────────┘     │
│                                                                                │
└────────────────────────────────────────────────────────────────────────────────┘
```

### 9.2 Clean Architecture (arquitectura interna del backend)

El backend se estructura en **4 capas concéntricas** siguiendo Clean Architecture de Robert C. Martin (Uncle Bob):

```
        ┌───────────────────────────────────────────────┐
        │            SafeWoman.API                      │
        │   (Controllers, Middleware, Program.cs,       │
        │       Panel Admin MVC, SignalR Hubs)          │
        │                                               │
        │   ┌───────────────────────────────────────┐   │
        │   │      SafeWoman.Infrastructure         │   │
        │   │    (EF Core, Repos, Twilio, Storage,  │   │
        │   │       BCrypt, JWT, Geocoding)         │   │
        │   │                                       │   │
        │   │   ┌───────────────────────────────┐   │   │
        │   │   │   SafeWoman.Application       │   │   │
        │   │   │  (Services, DTOs, Interfaces, │   │   │
        │   │   │      Casos de uso)            │   │   │
        │   │   │                               │   │   │
        │   │   │   ┌───────────────────────┐   │   │   │
        │   │   │   │  SafeWoman.Domain     │   │   │   │
        │   │   │   │  (Entities, Enums,    │   │   │   │
        │   │   │   │   Value Objects,      │   │   │   │
        │   │   │   │   Exceptions)         │   │   │   │
        │   │   │   └───────────────────────┘   │   │   │
        │   │   └───────────────────────────────┘   │   │
        │   └───────────────────────────────────────┘   │
        └───────────────────────────────────────────────┘

         Regla de oro: las flechas apuntan HACIA ADENTRO.
         Domain no conoce a nadie. API conoce a todos.
```

**Ventajas de esta arquitectura:**

- El **dominio** (entidades de negocio) no depende de frameworks — podrías migrar de EF Core a Dapper sin tocar el Domain.
- Cada capa tiene **una única responsabilidad** (SRP del SOLID).
- Testeable: puedes hacer unit tests del Domain sin necesidad de BD.
- Mantenible: cambios en infraestructura (ej. cambiar SQL Server por PostgreSQL) no afectan a las capas superiores.

### 9.3 Descripción por capa

#### 🎯 SafeWoman.Domain (núcleo)

Contiene la **lógica de negocio pura**. No sabe de bases de datos, HTTP, ni frameworks externos.

**Entidades:** `Victima`, `Administrador`, `Denuncia`, `DenunciaAnonima`, `Denunciado`, `Evidencia`, `AlertaSos`, `ContactoEmergencia`, `HuellaDispositivo`, `OtpVerificacion`, `LogAuditoria`.

**Ejemplo — `Victima.cs`:**

```csharp
public class Victima
{
    public int IdVictima { get; set; }
    public string Dni { get; set; }
    public string Nombre { get; set; }
    public string Telefono { get; set; }
    public string FotoDniUrl { get; set; }
    public bool Activo { get; set; }
    public DateTime FechaRegistro { get; set; }
    // Navegaciones a Denuncias, Contactos, Alertas, OTPs
}
```

#### 📋 SafeWoman.Application

Contiene **casos de uso** — la orquestación de las operaciones que la aplicación puede realizar.

**Servicios de aplicación:**

| Servicio | Responsabilidad |
|---|---|
| `AuthService` | Registro, verificación OTP, login, generación JWT |
| `AdminAuthService` | Login del panel administrativo |
| `VictimaService` | Perfil de la víctima, actualización de datos |
| `ContactoService` | CRUD de contactos de emergencia |
| `DenunciaService` | Crear/listar/gestionar denuncias formales |
| `DenunciaAnonimaService` | Denuncia anónima |
| `AlertaSosService` | Activar/cancelar alertas SOS |
| `AdminService` | Consultas y acciones del panel Admin |

Aquí también viven los **DTOs** (Data Transfer Objects) que separan el modelo de dominio del formato que consume la API pública.

#### 🔌 SafeWoman.Infrastructure

Implementaciones concretas de servicios externos:

- **Persistencia**: `SafeWomanDbContext` (Entity Framework Core 8 con Npgsql para PostgreSQL).
- **Twilio**: `TwilioSmsSender` — envía SMS reales.
- **Console SMS**: `ConsoleSmsSender` — simula SMS en desarrollo (ahorra saldo).
- **BCrypt**: `BcryptPasswordHasher` — hash seguro de contraseñas.
- **JWT**: `JwtTokenService` — emite y valida tokens.
- **Storage**: `LocalFileStorage` — guarda archivos de evidencia.
- **Geocoding**: `NominatimReverseGeocoder` — convierte coordenadas GPS en direcciones legibles.
- **SignalR**: `SignalRSosNotifier` — notifica en tiempo real al panel Admin cuando llega un SOS.

#### 🌐 SafeWoman.API

Punto de entrada del backend. Maneja:

- **Controladores REST** para la app móvil (`Auth`, `Victima`, `Contacto`, `Denuncia`, `DenunciaAnonima`, `AlertaSos`).
- **Panel Admin MVC** (`Areas/Admin/*` con vistas Razor).
- **SignalR Hub** en `/hubs/sos` para tiempo real.
- **Middlewares** (`ExceptionMiddleware`, seguridad, CORS, autenticación).
- **Configuración de dependencias** (DI container).

### 9.4 Front-end móvil (.NET MAUI)

```
📱 SafeWoman (MAUI)
├── App.xaml — configuración global
├── AppShell.xaml — navegación entre páginas
├── MauiProgram.cs — DI + URL de la API (DEV vs RELEASE)
│
├── Views/
│   ├── Auth/       (Welcome, Register, Login, Otp)
│   ├── Home/       (Home con botón SOS, SosActive)
│   ├── Denuncia/   (DenunciaFormal, DenunciaAnonima)
│   ├── Contacto/   (Contactos)
│   └── Perfil/     (Perfil, Configuración)
│
├── ViewModels/     (MVVM — separa lógica de la UI)
├── Services/
│   ├── ApiService              (llamadas HTTP a la API)
│   ├── AuthStateService        (gestión del token JWT)
│   ├── LocationService         (GPS)
│   ├── GeocodingService        (Nominatim)
│   ├── DeviceFingerprintService (huella del dispositivo)
│   └── IAlarmService           (sonido de SOS)
│
└── Platforms/
    └── Android/                (código específico Android)
```

Patrón usado: **MVVM (Model-View-ViewModel)** con `CommunityToolkit.Mvvm`.

---

## 10. Modelo de datos

### 10.1 Diagrama entidad-relación

```
┌──────────────────┐          ┌────────────────────────┐
│    VICTIMA       │◄────────►│  CONTACTO_EMERGENCIA  │
│──────────────────│  1..N    │────────────────────────│
│ id_victima (PK)  │          │ id_contacto (PK)       │
│ dni              │          │ id_victima (FK)        │
│ nombre           │          │ nombre                 │
│ telefono         │          │ telefono               │
│ foto_dni_url     │          │ relacion               │
│ activo           │          └────────────────────────┘
│ fecha_registro   │
└──────┬───────────┘
       │
       │ 1..N
       │
       ├──────────────────┐
       │                  │
       ▼                  ▼
┌──────────────┐   ┌─────────────────┐
│ ALERTA_SOS   │   │    DENUNCIA     │
│──────────────│   │─────────────────│
│ id_alerta    │   │ id_denuncia (PK)│
│ id_victima   │   │ id_victima (FK) │
│ latitud      │   │ tipo_violencia  │
│ longitud     │   │ descripcion     │
│ direccion    │   │ latitud/longitud│
│ estado       │   │ direccion       │
│ fecha        │   │ estado          │
│ atendida_por │   │ fecha_hecho     │
└──────────────┘   │ fecha_registro  │
                   └────────┬────────┘
                            │
                            │ 1..N
                            │
                    ┌───────┴────────┐
                    ▼                ▼
              ┌───────────┐   ┌─────────────┐
              │DENUNCIADO │   │  EVIDENCIA  │
              │───────────│   │─────────────│
              │ id (PK)   │   │ id (PK)     │
              │ id_denunc │   │ id_denunc   │
              │ nombre    │   │ url         │
              │ dni       │   │ tipo        │
              │ relacion  │   │ tamaño      │
              └───────────┘   └─────────────┘

┌───────────────────┐        ┌──────────────────┐
│  ADMINISTRADOR    │        │  OTP_VERIFICACION│
│───────────────────│        │──────────────────│
│ id_admin (PK)     │        │ id_otp (PK)      │
│ email             │        │ id_victima (FK)  │
│ password_hash     │        │ codigo           │
│ nombre            │        │ expiracion       │
│ activo            │        │ usado            │
│ ultimo_acceso     │        └──────────────────┘
└───────────────────┘

┌─────────────────────────┐   ┌─────────────────────┐
│    LOG_AUDITORIA        │   │ HUELLA_DISPOSITIVO  │
│─────────────────────────│   │─────────────────────│
│ id_log (PK)             │   │ id_huella (PK)      │
│ id_admin (FK)           │   │ id_victima (FK)     │
│ accion (enum)           │   │ device_id           │
│ entidad_tipo            │   │ modelo              │
│ entidad_id              │   │ os_version          │
│ descripcion             │   │ activo              │
│ fecha                   │   └─────────────────────┘
└─────────────────────────┘

┌────────────────────────────┐
│    DENUNCIA_ANONIMA        │
│────────────────────────────│
│ id_denuncia_anonima (PK)   │────► DENUNCIADO_ANONIMA (1..N)
│ tipo_violencia             │────► EVIDENCIA_ANONIMA (1..N)
│ descripcion                │
│ latitud/longitud/direccion │
│ fecha_hecho                │
│ fecha_registro             │
└────────────────────────────┘
```

### 10.2 Descripción de tablas principales

| Tabla | Descripción | Registros esperados |
|---|---|---|
| `VICTIMA` | Usuarias registradas | Alto |
| `CONTACTO_EMERGENCIA` | Contactos de cada víctima | 1-5 por víctima |
| `ALERTA_SOS` | Alertas activadas | Medio (crecimiento continuo) |
| `DENUNCIA` | Denuncias formales | Alto |
| `DENUNCIADO` | Personas denunciadas (formal) | 1..N por denuncia |
| `EVIDENCIA` | Fotos/audios/videos | 0..5 por denuncia |
| `DENUNCIA_ANONIMA` | Denuncias sin identidad | Alto |
| `ADMINISTRADOR` | Autoridades del panel | Bajo (~5-20) |
| `LOG_AUDITORIA` | Todas las acciones sensibles | Muy alto |
| `OTP_VERIFICACION` | Códigos temporales | Volátil (se limpian tras uso) |
| `HUELLA_DISPOSITIVO` | Dispositivos vinculados | 1..N por víctima |

### 10.3 Decisiones de diseño

- **Nombres en MAYÚSCULAS**: convención tradicional de SQL. Además Postgres respeta la mayúscula solo si se usan comillas dobles.
- **Soft delete** en `Victima` (campo `activo`) para preservar histórico de denuncias.
- **Enum en base de datos**: los estados (`activa`, `pendiente`, `atendida`, etc.) se guardan como strings — legibles al inspeccionar la BD.
- **Coordenadas GPS**: `decimal(9,7)` para latitud y `decimal(10,7)` para longitud — precisión de ~1 cm.
- **URLs de evidencia**: se guardan rutas relativas, no bytes — mejor rendimiento y respeta límites de columna.

---

## 11. Módulos funcionales

### 11.1 Flujo — Registro de víctima

```
Usuario abre la app
       │
       ▼
Pantalla WELCOME  →  "Registrarme"
       │
       ▼
Formulario de registro
   • DNI
   • Nombre
   • Teléfono (móvil)
   • Foto del DNI (cámara)
       │
       ▼
POST /api/auth/register
       │
       ▼
API guarda víctima en PostgreSQL (activo=false)
API genera OTP de 6 dígitos aleatorio
API envía SMS vía Twilio:
   "Su código SafeWoman es: 384291"
       │
       ▼
Pantalla OTP → víctima ingresa 6 dígitos
       │
       ▼
POST /api/auth/verify-otp
       │
       ▼
API valida OTP, marca víctima como activa=true
API emite JWT (168h) firmado con HS256
       │
       ▼
App guarda JWT en:
   • Memoria (AuthStateService)
   • Preferences (Android SharedPreferences)
   • SecureStorage (KeyStore cifrado)
       │
       ▼
Redirect a HOME
```

### 11.2 Flujo — Denuncia formal

```
Home → tap "Denuncia Formal"
       │
       ▼
Pantalla DenunciaFormal
   • Tipo de violencia (radiobuttons)
   • Descripción detallada
   • Ubicación GPS o dirección (mapa)
   • Datos del denunciado (opcional)
   • Adjuntar evidencias (cámara/galería)
       │
       ▼
Botón "Enviar denuncia"
   → LocationService captura GPS actual
   → Se comprimen las imágenes >2MB
       │
       ▼
POST /api/denuncia (multipart/form-data)
   Headers: Authorization: Bearer <JWT>
   Body: campos + archivos
       │
       ▼
API:
   1. Valida JWT
   2. Extrae id_victima del token
   3. Crea Denuncia en PostgreSQL
   4. Crea Denunciado(s) si aplica
   5. Sube evidencias a storage local
   6. Retorna { id: N, estado: "pendiente" }
       │
       ▼
Confirmación: "Denuncia enviada"
Redirect a Home
```

### 11.3 Flujo — Botón SOS

```
Home → mantener presionado botón SOS 3 segundos
       │
       ▼
LocationService captura ubicación GPS
GeocodingService (Nominatim) → dirección legible
       │
       ▼
POST /api/alertasos/activar
   Body: { latitud, longitud, direccion, victimaId }
       │
       ▼
API:
   1. Crea AlertaSos en PostgreSQL (estado: activa)
   2. Emite notificación SignalR al Hub /hubs/sos
      → panel Admin recibe alerta en tiempo real (< 1 seg)
   3. Por cada ContactoEmergencia de la víctima:
        TwilioSmsSender.SendSosAlertAsync(
          contacto.telefono,
          victima.nombre,
          lat, lng, timestamp, direccion
        )
        → SMS de 1 segmento GSM-7:
        "SOS! Ana necesita ayuda en Av. Ramón Castilla 234,
         Ayacucho. Ver ubicación: maps.google.com/?q=-13.16,-74.22"
       │
       ▼
App móvil:
   • Suena alarma
   • Redirect a SosActivePage con contador
   • Muestra estado a víctima
```

### 11.4 Flujo — Panel Administrativo

```
Autoridad accede a:
https://safewoman-api.onrender.com/panel-safewoman/Auth/Login
       │
       ▼
Formulario email + password
       │
       ▼
POST /panel-safewoman/Auth/Login
   Rate limiting: 10 intentos/min por IP
       │
       ▼
API:
   1. AdminAuthService.LoginAsync(email, password)
   2. Compara password con hash BCrypt en BD
   3. Si válido: crea Cookie de sesión (HttpOnly, SameSite=Lax, 8h)
   4. Registra LogAuditoria (accion: LoginAdmin)
       │
       ▼
Dashboard:
   • KPIs (víctimas, denuncias, alertas)
   • Tab "Alertas SOS" (SignalR realtime)
   • Tab "Denuncias"
   • Tab "Víctimas"
   • Tab "Auditoría"
```

---

## 12. Stack tecnológico

### 12.1 Backend

| Tecnología | Versión | Rol |
|---|---|---|
| **.NET** | 8.0 (LTS) | Runtime |
| **ASP.NET Core** | 8.0 | Web framework |
| **Entity Framework Core** | 8.0.11 | ORM |
| **Npgsql** | 8.0.11 | Driver PostgreSQL |
| **SignalR** | 8.0.11 | WebSockets tiempo real |
| **JWT Bearer** | 8.0 | Autenticación móvil |
| **BCrypt.Net-Next** | 4.0.3 | Hash de contraseñas |
| **Twilio SDK** | 7.3.1 | SMS |
| **Swashbuckle.AspNetCore** | — | Swagger/OpenAPI |

### 12.2 Frontend móvil

| Tecnología | Versión | Rol |
|---|---|---|
| **.NET MAUI** | 8.0 | Framework cross-platform |
| **CommunityToolkit.Mvvm** | 8.x | MVVM helpers |
| **CommunityToolkit.Maui** | 7.x | Componentes extra |
| **Xamarin.AndroidX** | 1.x | Compatibilidad Android |
| **Microsoft.Extensions.Http** | 8.0 | HttpClient factory |
| **System.Text.Json** | 8.0 | Serialización |

### 12.3 Base de datos

- **PostgreSQL 16** hospedado en **Neon** (Ohio, US East).
- **3 GB de storage gratuito** (suficiente para ~30 000 víctimas).
- Conexión SSL obligatoria.
- **Branching**: Neon permite crear ramas de la BD para pruebas sin tocar producción.

### 12.4 Servicios externos

| Servicio | Uso | Costo |
|---|---|---|
| **Twilio** | SMS OTP y SOS | ~$0.08 USD por SMS a Perú |
| **Nominatim (OSM)** | Geocoding y reverse-geocoding | Gratis |
| **CartoDB** | Tiles del mapa | Gratis |
| **Esri World Imagery** | Vista satelital | Gratis |
| **Render.com** | Hosting API (Free tier) | Gratis (con cold start tras 15 min) |
| **Neon** | PostgreSQL managed | Gratis (3 GB) |
| **UptimeRobot** | Monitoreo 24/7 | Gratis (50 monitores) |
| **GitHub** | Repositorio + CI/CD | Gratis |

### 12.5 ¿Por qué estas elecciones?

- **.NET MAUI en vez de Flutter/React Native**: aprovecha conocimiento de C# del autor, integración directa con backend en .NET, un solo lenguaje para todo el stack.
- **PostgreSQL en vez de SQL Server**: gratis en la nube (Neon), estándar de la industria, mejor soporte para JSON y arrays si los necesitáramos en el futuro.
- **JWT en vez de sesiones**: mejor para móviles (stateless), permite escalabilidad horizontal.
- **BCrypt en vez de SHA-256**: BCrypt es adaptable — a mayor hardware, más costoso adivinar contraseñas.
- **SignalR en vez de polling**: notificaciones en tiempo real sin sobrecarga.
- **Twilio en vez de gateways nacionales**: SDK simple, sin trámites, funcionalidad instantánea.

---

## 13. Seguridad y hardening

Este módulo es crítico dado el perfil de los usuarios (mujeres en riesgo). Se aplicó **defensa en profundidad** con múltiples capas:

### 13.1 Capa de transporte

| Medida | Implementación |
|---|---|
| **HTTPS obligatorio** | Certificado SSL emitido por Let's Encrypt (Render lo gestiona automáticamente). |
| **HSTS** | Cabecera `Strict-Transport-Security: max-age=31536000; includeSubDomains` obliga al navegador a usar solo HTTPS por 1 año. |
| **TLS 1.2 mínimo** | Configuración de Kestrel/Render rechaza protocolos antiguos vulnerables. |

### 13.2 Capa de autenticación

**Para la app móvil (víctimas): JWT**

```
Header:  { "alg": "HS256", "typ": "JWT" }
Payload: { "sub": "42", "nombre": "Ana", "exp": 1720000000 }
Firma:   HMAC-SHA256(base64(header) + "." + base64(payload), SECRET_KEY)
```

- Firmado con **clave de 64 caracteres** aleatoria (env var `Jwt__Key`).
- Expiración configurable (168 h para defensa académica, 4-8 h recomendado en producción real).
- Validaciones: `ValidateIssuer`, `ValidateAudience`, `ValidateLifetime`, `ValidateIssuerSigningKey`.
- ClockSkew tolerante de 30 segundos (relojes desincronizados).

**Para el panel Admin: Cookies**

- `HttpOnly=true` — JavaScript no puede acceder → previene XSS.
- `SameSite=Lax` — bloquea CSRF cross-site.
- `Secure=SameAsRequest` — solo se envía por HTTPS.
- Expiración 8 horas, sliding (se renueva con uso).

### 13.3 Capa de almacenamiento

- **BCrypt** con work factor 12 para todos los passwords.
- Salt único e integrado en cada hash.
- SecureStorage nativo de Android para el JWT en el móvil (usa KeyStore, cifrado por hardware).

### 13.4 Capa de aplicación

| Medida | Cómo |
|---|---|
| **Rate limiting** | 10 intentos/min por IP en login (`AddFixedWindowLimiter`). |
| **CSRF** | `[ValidateAntiForgeryToken]` en todos los POST del panel Admin. |
| **CORS** | Solo orígenes explícitos en producción. |
| **Content-Type validation** | Whitelist estricta de tipos MIME en uploads (`image/jpeg`, `image/png`, `audio/*`, `video/mp4`). |
| **Tamaño máximo** | 55 MB por request (5 archivos × 10 MB + overhead). |
| **Fingerprinting** | Cada dispositivo registrado tiene una huella única (`HuellaDispositivo`) para detectar accesos anómalos. |

### 13.5 Cabeceras HTTP OWASP

```
Strict-Transport-Security: max-age=31536000; includeSubDomains
X-Content-Type-Options: nosniff
X-Frame-Options: DENY
Referrer-Policy: strict-origin-when-cross-origin
Permissions-Policy: geolocation=(), microphone=(), camera=()
```

Cada una previene una clase específica de ataque:

- **HSTS** → downgrade attacks.
- **X-Content-Type-Options** → MIME sniffing.
- **X-Frame-Options** → clickjacking.
- **Referrer-Policy** → fuga de URLs a terceros.
- **Permissions-Policy** → el panel no necesita sensores del usuario.

### 13.6 Ofuscación de superficie

- Panel Admin en `/panel-safewoman/*` (no en `/admin` que es predecible).
- Raíz `/` devuelve solo texto neutro, no delata la existencia del panel.
- Scanners automáticos que prueban `/wp-admin`, `/admin`, `/login` reciben 404.

### 13.7 Auditoría

Cada acción sensible del administrador queda registrada en `LOG_AUDITORIA`:

- Login/Logout
- Cambio de estado de una denuncia
- Activación/desactivación de víctimas
- Atención de alertas SOS
- Toggle de huella de dispositivo

Con timestamp, IP y descripción. Cumple con el requerimiento de trazabilidad de la Ley 29733.

---

## 14. Despliegue en la nube

### 14.1 Diagrama de despliegue

```
┌──────────────────────────────────────────────────────────────────────┐
│                         🌎 INTERNET                                   │
└─────────────┬──────────────────────────────────────────────┬─────────┘
              │                                              │
              ▼                                              ▼
   ┌──────────────────────┐                    ┌───────────────────────┐
   │  📱 App SafeWoman    │                    │  💻 Navegador          │
   │  (dispositivos       │                    │  (autoridades)        │
   │   Android físicos)   │                    │                       │
   └──────────┬───────────┘                    └───────────┬───────────┘
              │                                            │
              │  HTTPS/TLS 1.3                             │  HTTPS/TLS 1.3
              │  Bearer JWT                                │  Cookie HttpOnly
              │                                            │
              └────────────────────┬───────────────────────┘
                                   │
                                   ▼
                    ┌──────────────────────────────┐
                    │   🌐 Render.com Edge          │
                    │  (SSL termination + CDN)     │
                    └──────────────┬───────────────┘
                                   │
                                   │  HTTP interno
                                   ▼
                    ┌──────────────────────────────┐
                    │  🐳 Docker container         │
                    │  safewoman-api                │
                    │  .NET 8 ASP.NET Core          │
                    │  Puerto 8080                  │
                    │                              │
                    │  ├─ Autenticación JWT         │
                    │  ├─ Panel Admin MVC          │
                    │  ├─ SignalR Hub              │
                    │  └─ Middlewares OWASP        │
                    └──┬────────────┬──────────────┘
                       │            │
             SQL sobre │            │  HTTPS
             SSL       │            │
                       ▼            ▼
       ┌────────────────────────┐  ┌───────────────────────┐
       │  🐘 Neon PostgreSQL 16 │  │  📱 Twilio API         │
       │  ep-purple-bonus       │  │  api.twilio.com       │
       │  Ohio (US East)        │  │  SMS a Perú (+51)     │
       │  3 GB SSL storage      │  │                       │
       └────────────────────────┘  └───────────────────────┘

              ┌────────────────────────────────────┐
              │  🤖 UptimeRobot (external)         │
              │  ping cada 5 min                   │
              │  → mantiene el servicio despierto  │
              └────────────────────────────────────┘
```

### 14.2 Pipeline CI/CD

```
Developer local                GitHub                    Render.com
┌──────────────┐              ┌────────┐              ┌──────────────┐
│              │              │        │              │              │
│ git commit   │              │  main  │              │  Docker      │
│    │         │  git push    │ branch │  webhook     │  Build       │
│    ▼         │─────────────►│        │─────────────►│    │         │
│ Local test   │              │        │              │    ▼         │
│              │              │        │              │  Restore     │
└──────────────┘              └────────┘              │    │         │
                                                     │    ▼         │
                                                     │  Publish     │
                                                     │    │         │
                                                     │    ▼         │
                                                     │  Deploy      │
                                                     │    │         │
                                                     │    ▼         │
                                                     │  Migrations  │
                                                     │    │         │
                                                     │    ▼         │
                                                     │  🟢 LIVE     │
                                                     └──────────────┘
                                                     Total: 4-8 min
```

### 14.3 Configuración del contenedor Docker

```dockerfile
# Stage 1: build
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src
COPY [global.json, ./]
COPY [Directory.Build.props, ./]
COPY [SafeWoman.API/SafeWoman.API.csproj, SafeWoman.API/]
COPY [SafeWoman.Application/SafeWoman.Application.csproj, SafeWoman.Application/]
COPY [SafeWoman.Domain/SafeWoman.Domain.csproj, SafeWoman.Domain/]
COPY [SafeWoman.Infrastructure/SafeWoman.Infrastructure.csproj, SafeWoman.Infrastructure/]
COPY [SafeWoman.API/packages.lock.json, SafeWoman.API/]
COPY [SafeWoman.Application/packages.lock.json, SafeWoman.Application/]
COPY [SafeWoman.Domain/packages.lock.json, SafeWoman.Domain/]
COPY [SafeWoman.Infrastructure/packages.lock.json, SafeWoman.Infrastructure/]
RUN dotnet restore "SafeWoman.API/SafeWoman.API.csproj"
COPY SafeWoman.API/       SafeWoman.API/
COPY SafeWoman.Application/ SafeWoman.Application/
COPY SafeWoman.Domain/    SafeWoman.Domain/
COPY SafeWoman.Infrastructure/ SafeWoman.Infrastructure/
WORKDIR /src/SafeWoman.API
RUN dotnet publish "SafeWoman.API.csproj" -c Release -o /app/publish /p:UseAppHost=false

# Stage 2: runtime
FROM mcr.microsoft.com/dotnet/aspnet:8.0
WORKDIR /app
COPY --from=build /app/publish .
ENV ASPNETCORE_URLS=http://+:${PORT:-8080}
ENTRYPOINT ["dotnet", "SafeWoman.API.dll"]
```

### 14.4 Variables de entorno de producción

| Variable | Descripción |
|---|---|
| `ASPNETCORE_ENVIRONMENT` | `Production` |
| `ConnectionStrings__DefaultConnection` | Cadena Neon con SSL |
| `Jwt__Key` | Clave secreta (64 chars aleatorios) |
| `Jwt__Issuer` | `SafeWoman.API` |
| `Jwt__Audience` | `SafeWoman.Mobile` |
| `Jwt__ExpirationHours` | `168` |
| `Sms__Provider` | `Twilio` |
| `Twilio__AccountSid` | ID de cuenta Twilio |
| `Twilio__AuthToken` | Token secreto Twilio |
| `Twilio__FromNumber` | Número emisor |
| `AdminSeed__Email` | Email del admin inicial |
| `AdminSeed__Password` | Password fuerte del admin |
| `AdminSeed__Nombre` | Nombre del admin |

Nunca se commitean al repositorio. Se gestionan desde el panel de Render.

### 14.5 Costos operativos

**Todo el sistema corre en la capa gratuita:**

| Servicio | Plan | Límites | Costo mensual |
|---|---|---|---|
| Render (API) | Free | 750 h/mes, cold start tras 15 min | $0 |
| Neon (BD) | Free | 3 GB storage, 0.5 GB RAM | $0 |
| UptimeRobot | Free | 50 monitores, 5 min interval | $0 |
| GitHub | Free (público) | Ilimitado | $0 |
| Twilio | Pay-as-you-go | SMS a $0.08 c/u a Perú | ~$1-2/mes con uso académico |
| **TOTAL** | | | **~$2/mes** |

Con **UptimeRobot pingueando cada 5 min**, la API nunca duerme, resolviendo la única limitación del plan gratuito de Render.

---

## 15. Pruebas y verificación

### 15.1 Estrategia de pruebas — Pirámide de testing

Se implementó la **pirámide de testing** propuesta por Mike Cohn (2009) y adoptada por Google, Uber, Microsoft y toda la industria moderna. Consiste en tres niveles de tests con una **proporción específica**: muchos unitarios (base), menos de integración (medio), pocos end-to-end (cima).

```
                       ▲
                      ╱ ╲
                     ╱API╲                18 tests — 10%
                    ╱─────╲
                   ╱ INTEG ╲               11 tests — 6%
                  ╱─────────╲
                 ╱    UNIT    ╲            158 tests — 84%
                ╱───────────────╲
                   BASE ANCHA
                   
             TOTAL: 187 tests, 14 segundos
```

**Razones detrás de esta forma piramidal:**

- Los **unitarios** son rápidos (milisegundos), aislados y deterministas → son la base.
- Los **de integración** son medios (segundos) y verifican que componentes reales funcionen juntos.
- Los **E2E/API** son lentos y frágiles → se usan solo para flujos críticos.

### 15.2 Nivel 1 — Tests unitarios (158 tests)

**Objetivo**: verificar cada método de lógica de negocio **aislado** de sus dependencias.

**Herramientas empleadas:**

| Herramienta | Rol | Versión |
|---|---|---|
| **xUnit** | Framework de tests | 2.9.2 |
| **FluentAssertions** | Assertions legibles | 6.12.0 |
| **Moq** | Mocking de dependencias | 4.20.70 |
| **Coverlet** | Medición de cobertura | 6.0.2 |
| **ReportGenerator** | HTML de cobertura | 5.5.10 |

**Distribución de tests unitarios:**

| Área | Tests | Cobertura |
|---|---|---|
| Domain (13 entidades) | 40 | 95% promedio |
| Application Services (7 servicios) | 45 | 92% promedio |
| FluentValidation (4 validadores) | 21 | 100% |
| Seguridad (BCrypt, JWT, OTP) | 16 | 100% |
| Otros (entidades auxiliares) | 36 | 90% promedio |

**Ejemplo de test unitario:**

```csharp
[Fact]
public async Task RegistrarAsync_con_DNI_ya_existente_debe_lanzar_DomainException()
{
    var existente = Victima.Crear("Otra", "12345678", "999888777", "hash");
    _victimaRepo.Setup(r => r.FindAsync(...))
        .ReturnsAsync(new[] { existente });

    var sut = CrearSut();
    var req = new RegistroRequest("Ana", "12345678", "987654321", "pass");

    var act = () => sut.RegistrarAsync(req);

    await act.Should().ThrowAsync<DomainException>()
        .WithMessage("*Ya existe una cuenta*");
}
```

### 15.3 Nivel 2 — Tests de integración (11 tests)

**Objetivo**: verificar que la persistencia con **PostgreSQL real** funciona correctamente — atrapa bugs específicos del dialecto SQL que los unitarios ignoran.

**Herramienta clave**: **Testcontainers.PostgreSql** (versión 3.10.0). Cada corrida de tests:

1. Descarga (una sola vez) la imagen `postgres:16-alpine` (~90 MB).
2. Levanta un **contenedor Docker efímero** con PostgreSQL 16.
3. Aplica el esquema del DbContext con `EnsureCreated`.
4. Ejecuta los tests contra este contenedor aislado.
5. Al finalizar, destruye el contenedor automáticamente.

**Ventajas sobre EF Core InMemory:**

- Detecta violaciones de constraints (FK, longitud de columna, NOT NULL).
- Detecta bugs de dialecto (`NOW() AT TIME ZONE 'UTC'` no existe en SQL Server).
- Detecta problemas de precisión decimal.
- Verifica que los `Include` y `Join` funcionan como en producción.

**Bug detectado en la implementación (evidencia del valor de estos tests):**

Durante la primera corrida, 9 de 11 tests fallaron con `value too long for type character varying(9)`. Los tests usaban `+51987654321` (12 chars) pero la columna `telefono` acepta 9 caracteres (formato peruano sin prefijo). **En producción, esto habría causado errores 500 a usuarios reales** — atrapado antes del deploy.

**Ejemplo de test de integración:**

```csharp
[Fact]
public async Task FK_a_victima_inexistente_debe_ser_rechazada_por_PostgreSQL()
{
    using var db = await _fixture.CrearDbContextAsync();

    db.ContactosEmergencia.Add(
        ContactoEmergencia.Crear(idVictima: 99999, "Fantasma", "999999999"));

    var act = () => db.SaveChangesAsync();

    await act.Should().ThrowAsync<DbUpdateException>(
        "PostgreSQL debe rechazar la FK a una víctima inexistente");
}
```

### 15.4 Nivel 3 — Tests de API (18 tests)

**Objetivo**: verificar el **pipeline HTTP completo** — middlewares, controllers, autenticación, autorización — contra un PostgreSQL real.

**Herramienta clave**: `WebApplicationFactory<Program>` de ASP.NET Core (Microsoft.AspNetCore.Mvc.Testing 8.0.11). Arranca la API completa en memoria — mismo código, mismos middlewares que producción, pero con:

- **PostgreSQL efímero** (contenedor Testcontainers).
- **SMS en modo Console** (no consume saldo Twilio).
- **Rate limiting elevado** (10 000 req/min) para no chocar con los tests.
- **JWT con clave conocida** solo por los tests.

**Categorías de tests API:**

| Grupo | Tests | Verifica |
|---|---|---|
| Infraestructura | 5 | Raíz neutra, ruta admin 404, headers OWASP, Swagger |
| Auth flow | 8 | Registro → OTP → Login → JWT completo |
| Endpoints protegidos | 5 | Rechazo 401 sin JWT, aceptación con JWT válido |

**Ejemplo — flujo completo end-to-end:**

```csharp
[Fact]
public async Task Flujo_registro_verificar_login_debe_devolver_JWT_valido()
{
    var client = _factory.CreateClient();

    // 1. Registrar víctima
    await client.PostAsJsonAsync("/api/auth/registro",
        new RegistroRequest("Ana", "22333444", "911222333", "Password123!"));

    // 2. Leer OTP directamente de la BD (SMS es Console en tests)
    var otp = await LeerOtpDirectoDeBd("911222333");

    // 3. Verificar OTP → debe emitir JWT
    var verifyResp = await client.PostAsJsonAsync("/api/auth/verificar-otp",
        new VerificarOtpRequest("911222333", otp!));
    var auth = await verifyResp.Content.ReadFromJsonAsync<AuthResponse>();

    auth!.Token.Should().NotBeNullOrEmpty();
    auth.Verificada.Should().BeTrue();
}
```

### 15.5 Cobertura de código

Se utiliza **Coverlet** para instrumentar el código y **ReportGenerator** para producir reportes HTML visuales.

**Comando de ejecución:**

```bash
dotnet test --collect:"XPlat Code Coverage"
reportgenerator -reports:"**/coverage.cobertura.xml" \
                -targetdir:"CoverageReport" \
                -reporttypes:"Html;TextSummary"
```

**Métricas finales (código con lógica que vale la pena testear):**

| Métrica | Valor | Objetivo |
|---|---|---|
| **Line coverage** | **97.8%** | >90% ✅ |
| **Method coverage** | **95.4%** | >90% ✅ |
| **Branch coverage** | **88.2%** | >85% ✅ |

**Exclusiones estándar** (siguiendo prácticas de SonarQube y Codecov):

- **DTOs** — records sin lógica, solo datos.
- **DependencyInjection.cs** — configuración pura de DI.
- **EF Migrations** — código autogenerado.
- **EF Configurations** — se validan vía integration tests.
- **Program.cs** — bootstrap de la aplicación.
- **Controllers** — se validan vía API tests.
- **Middleware** — se valida vía API tests.
- **Áreas MVC (vistas Razor)** — código de UI.
- **Repositorios / UnitOfWork** — se validan vía integration tests.
- **Integraciones externas** (Twilio, Nominatim, SignalR) — se validan con mocks o con un ambiente de integración dedicado.

### 15.6 Pruebas manuales en dispositivo físico

Complementan la suite automatizada:

- **Samsung Galaxy** (Android físico) — dispositivo principal de pruebas de campo.
- **Emulador Android** (Visual Studio) — para desarrollo rápido.

**Escenarios verificados manualmente:**

- Uso con **Wi-Fi apagado / datos móviles activos** (simula escenario del jurado).
- **Cold start** de Render — primer request tras despertar el servicio.
- **Modo dark/light** de Android — la app fuerza tema claro por consistencia.
- **Teclado con acentos y ñ** — verificado en todos los campos de texto.

### 15.7 Estructura del proyecto de tests

```
Tests/
├── SafeWoman.UnitTests/            (158 tests, 6 seg)
│   ├── Domain/                     (tests de entidades)
│   │   ├── VictimaTests.cs
│   │   ├── AlertaSosTests.cs
│   │   ├── DenunciaTests.cs
│   │   └── ...
│   ├── Services/                   (tests de servicios con Moq)
│   │   ├── AuthServiceTests.cs
│   │   ├── AlertaSosServiceTests.cs
│   │   └── ...
│   └── Security/                   (BCrypt, JWT, OTP)
│
├── SafeWoman.IntegrationTests/     (11 tests, 3 seg)
│   ├── Fixtures/
│   │   └── PostgresContainerFixture.cs  (Testcontainers)
│   └── Persistence/
│       ├── VictimaPersistenceTests.cs
│       ├── ContactoEmergenciaPersistenceTests.cs
│       └── DenunciaPersistenceTests.cs
│
└── SafeWoman.ApiTests/             (18 tests, 5 seg)
    ├── AssemblyInfo.cs             (paralelismo desactivado)
    ├── Fixtures/
    │   └── SafeWomanApiFactory.cs  (WebApplicationFactory + Testcontainers)
    └── Endpoints/
        ├── InfrastructureTests.cs
        ├── AuthEndpointTests.cs
        └── ProtectedEndpointsTests.cs
```

### 15.8 Comandos rápidos

```bash
# Correr toda la suite
dotnet test

# Solo unit tests (rápido)
dotnet test Tests/SafeWoman.UnitTests/

# Solo integration tests (requiere Docker Desktop)
dotnet test Tests/SafeWoman.IntegrationTests/

# Solo API tests (requiere Docker Desktop)
dotnet test Tests/SafeWoman.ApiTests/

# Generar reporte HTML de cobertura
dotnet test --collect:"XPlat Code Coverage"
reportgenerator -reports:"**/coverage.cobertura.xml" \
                -targetdir:"CoverageReport" \
                -reporttypes:Html
start CoverageReport/index.html
```

---

## 16. Distribución de la app

### 16.1 Firma del APK

El APK está firmado con **debug key** para facilitar la distribución académica. Para un lanzamiento comercial se debería:

1. Generar keystore de producción.
2. Registrarse en **Google Play Console** ($25 USD única vez).
3. Subir el APK firmado a Play Store.
4. Pasar el proceso de revisión de Google (~1-2 días).

### 16.2 Distribución académica

**Método usado**: Google Drive con link público + código QR.

**Ventajas:**

- Instantáneo — un link sirve para 5, 50 o 500 usuarios.
- Sin costo — no requiere cuenta de desarrollador.
- Compatible con cualquier Android 8.0+.

**Advertencia esperada al instalar:**

- "Play Protect no reconoce esta app" — normal, APK no distribuido por Play Store.
- Usuario debe **"Instalar de todas formas"**.

### 16.3 Compatibilidad

- **Android 8.0 Oreo (API 26) mínimo** — cubre el 96% del parque nacional peruano.
- **Arquitecturas soportadas**: ARM64, ARMv7, x86_64 (single APK universal).
- **Tamaño del APK**: ~35 MB.

---

## 17. Conclusiones

### 17.1 Logros del proyecto

- Se desarrolló un **sistema integral funcional** que cubre todas las necesidades identificadas en la problemática.
- El sistema está **desplegado en producción** con disponibilidad 24/7.
- Cumple estrictamente con la **Ley N° 30364 y N° 29733**.
- Implementa **defensa en profundidad** con múltiples capas de seguridad.
- Utiliza **arquitectura escalable** que puede crecer a miles de usuarios.
- Todo el sistema opera **a costo casi cero** (~$2 USD/mes), sostenible para una entidad como el CEM Ayacucho.

### 17.2 Aportes técnicos y sociales

**Técnicos:**

- Aplicación pionera del stack **.NET MAUI + PostgreSQL + Render** para un TFG en la UNSCH.
- Implementación de **Clean Architecture** en el contexto académico local.
- Documentación detallada del proceso, útil para futuros proyectos.

**Sociales:**

- Herramienta gratuita para víctimas de violencia en Ayacucho.
- Reduce barreras de denuncia (movilidad, tiempo, temor).
- Facilita coordinación con autoridades gracias al panel administrativo.

### 17.3 Trabajo futuro

- Versión **iOS** para cubrir el ~10% de smartphones peruanos con iPhone.
- Integración directa con **RENIEC** para validación automática de DNI.
- **Integración con Fiscalía** para envío automático de denuncias al Ministerio Público.
- **Chatbot con IA** para contención emocional inmediata.
- **Modo offline** con sincronización posterior para zonas sin cobertura.
- Aplicación en **otras regiones** de Perú.

---

## 18. Glosario técnico

| Término | Definición |
|---|---|
| **API** | Application Programming Interface. Punto de comunicación entre la app móvil y el servidor. |
| **JWT** | JSON Web Token. Cadena firmada digitalmente que autentica al usuario en cada request. |
| **BCrypt** | Algoritmo de hash de contraseñas resistente a ataques de fuerza bruta. |
| **OTP** | One-Time Password. Código de 6 dígitos enviado por SMS que expira en 5 minutos. |
| **SignalR** | Biblioteca de Microsoft para comunicación en tiempo real (WebSockets). |
| **REST** | Representational State Transfer. Estilo de arquitectura para APIs web. |
| **HTTPS** | HTTP sobre TLS/SSL. Cifra la comunicación entre cliente y servidor. |
| **HSTS** | HTTP Strict Transport Security. Cabecera que obliga al navegador a usar solo HTTPS. |
| **CORS** | Cross-Origin Resource Sharing. Política de qué dominios pueden llamar a la API. |
| **DTO** | Data Transfer Object. Objeto plano que transporta datos entre capas. |
| **ORM** | Object-Relational Mapping. Mapea objetos C# a tablas de base de datos (Entity Framework). |
| **DI** | Dependency Injection. Patrón donde las dependencias se pasan al constructor. |
| **MVVM** | Model-View-ViewModel. Patrón que separa lógica de UI en apps móviles. |
| **MVC** | Model-View-Controller. Patrón usado en el panel administrativo web. |
| **MAUI** | Multi-platform App UI. Framework de Microsoft para apps cross-platform. |
| **Docker** | Plataforma que empaqueta la app en un contenedor portable. |
| **CI/CD** | Continuous Integration / Continuous Deployment. Automatización del despliegue. |
| **OWASP** | Open Web Application Security Project. Estándar de seguridad web. |
| **Nominatim** | Servicio gratuito de geocoding basado en OpenStreetMap. |
| **Twilio** | Plataforma cloud de comunicaciones (SMS, voz, video). |
| **Neon** | PostgreSQL serverless en la nube. |
| **Render.com** | Plataforma de hosting con auto-deploy desde Git. |
| **Clean Architecture** | Estilo arquitectónico donde las dependencias apuntan hacia el dominio. |
| **DDD** | Domain-Driven Design. Modela el software siguiendo el lenguaje del negocio. |
| **SDLC** | Software Development Life Cycle. Ciclo de vida de desarrollo del software. |
| **xUnit** | Framework de tests estándar en .NET moderno. |
| **Moq** | Librería de mocking — simula dependencias en tests unitarios. |
| **FluentAssertions** | Librería de assertions con sintaxis legible (`.Should().Be(...)`). |
| **Testcontainers** | Levanta contenedores Docker efímeros para tests de integración. |
| **WebApplicationFactory** | Arranca la API ASP.NET Core en memoria para tests end-to-end. |
| **Code Coverage** | Porcentaje del código productivo ejecutado por al menos un test. |
| **Coverlet** | Instrumentador de código para medir cobertura en .NET. |
| **ReportGenerator** | Convierte reportes de cobertura crudos en HTML navegable. |
| **Testing Pyramid** | Modelo de proporción de tests: muchos unitarios, menos integración, pocos E2E. |
| **AAA Pattern** | Estructura de tests: Arrange (preparar), Act (ejecutar), Assert (verificar). |
| **Mock** | Objeto falso que imita una dependencia para tests aislados. |
| **Test Fixture** | Contexto compartido entre múltiples tests (ej. contenedor Docker). |

---

## 📎 Anexo — URLs y credenciales de producción

**URLs públicas:**

- API base: `https://safewoman-api.onrender.com/api/`
- Panel Admin: `https://safewoman-api.onrender.com/panel-safewoman/Auth/Login`
- Swagger: `https://safewoman-api.onrender.com/swagger`
- Raíz: `https://safewoman-api.onrender.com/`

**Credenciales de panel Admin (para defensa):**

- Email: `admin@safewoman.pe`
- Password: `S4feW0m4n#P4n3l-Ay4cucho-2026$`

**Infraestructura:**

- Repositorio: GitHub `Crisso29/safewoman-api`
- Base de datos: Neon PostgreSQL, región Ohio
- Hosting: Render.com Free Tier
- Monitoreo: UptimeRobot (ping cada 5 min)
- SMS: Twilio (número emisor US)

---

*Documento generado el 8 de julio de 2026 para la defensa académica del proyecto SafeWoman en la Universidad Nacional de San Cristóbal de Huamanga.*
