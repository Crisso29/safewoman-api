# SafeWoman

App móvil de seguridad para mujeres víctimas de violencia en Ayacucho, Perú.
Proyecto académico UNSCH (Ingeniería de Sistemas + Derecho).
Cumple Ley N°30364 y N°29733.

## Stack

- **API**: ASP.NET Core 8 + Entity Framework Core 8
- **BD**: SQL Server 2022
- **App móvil**: .NET MAUI 8 (Android)
- **Panel Admin**: ASP.NET Core MVC + SignalR
- **SMS**: Twilio (con modo Console para desarrollo sin costo)
- **Autenticación**: JWT con expiración configurable

## Arquitectura

Clean Architecture + MVVM en el cliente móvil.

- `SafeWoman.Domain` — Entidades, Enums, Interfaces base
- `SafeWoman.Application` — Casos de uso, DTOs, contratos de servicios externos
- `SafeWoman.Infrastructure` — EF Core, Twilio, JWT, SignalR, LocalFileStorage
- `SafeWoman.API` — Web API + Panel Admin + SignalR Hub
- `SafeWoman` — App MAUI

## Ejecución local

```bash
dotnet restore
dotnet ef database update -p SafeWoman.Infrastructure -s SafeWoman.API
dotnet run --project SafeWoman.API --launch-profile phone
```

## Variables de entorno para producción

| Variable | Ejemplo | Descripción |
|---|---|---|
| `ConnectionStrings__DefaultConnection` | `Server=...;Database=...;User Id=...;Password=...;` | Connection string SQL Server |
| `Jwt__Key` | `una-clave-muy-larga-de-al-menos-32-chars` | Clave para firmar JWT |
| `Jwt__Issuer` | `SafeWoman.API` | Emisor del JWT |
| `Jwt__Audience` | `SafeWoman.Mobile` | Audiencia del JWT |
| `Jwt__ExpirationHours` | `168` | Duración del token (168h = 1 semana) |
| `Sms__Provider` | `Twilio` o `Console` | Proveedor SMS |
| `Twilio__AccountSid` | `AC...` | Twilio account SID (si Provider=Twilio) |
| `Twilio__AuthToken` | `...` | Twilio auth token |
| `Twilio__FromNumber` | `+1...` | Número Twilio de origen |

## Deploy en Render.com

Ver [DEPLOY.md](DEPLOY.md) para la guía paso a paso.
