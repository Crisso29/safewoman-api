# 🚢 Guía de despliegue — SafeWoman

**Tiempo total estimado: ~45 minutos**
**Costo: $0/mes** (solo pagas los SMS de Twilio si los usas — ~$0.08 c/u)

---

## 📋 Prerrequisitos

Necesitas tener creadas estas cuentas (todas gratis):

| Servicio | Para qué | Link |
|---|---|---|
| **GitHub** | Repositorio + CI/CD | [github.com](https://github.com/signup) |
| **Neon** | Base de datos PostgreSQL 16 | [neon.tech](https://neon.tech) |
| **Render** | Hosting de la API en Docker | [render.com](https://render.com) |
| **UptimeRobot** | Monitoreo — evita el "cold start" | [uptimerobot.com](https://uptimerobot.com) |
| **Twilio** | SMS reales (opcional, hay modo Console) | [twilio.com](https://twilio.com) |

---

## 🐘 Paso 1 — Crear la base de datos (Neon, 5 min)

### 1.1 Crear el proyecto

1. Entra a [console.neon.tech](https://console.neon.tech) → **New Project**.
2. Configuración:
   - **Project name**: `safewoman`
   - **Postgres version**: `16`
   - **Region**: `AWS US East (Ohio)` — mismo continente que Render
   - **Database name**: `neondb` (por defecto)
3. Clic **Create Project**.

### 1.2 Copiar la connection string

En el dashboard del proyecto, sección **Connection Details** → copia la **connection string** completa. Tiene este formato:

```
postgresql://neondb_owner:XXXX@ep-XXXXX.c-3.us-east-2.aws.neon.tech/neondb?sslmode=require
```

**Guárdala** — la necesitas en el Paso 3.

> 💡 **Neon Branch para tests de QA**: crea una segunda rama de tu BD desde el dashboard (Branches → New branch → nombrarla `qa`). Sirve para probar cambios sin afectar producción — te da otra connection string distinta.

---

## 📤 Paso 2 — Subir el código a GitHub (5 min)

Si tu repo ya existe en GitHub, ignora este paso.

```powershell
# Desde la raíz de la solución
cd C:\Users\Intel\Downloads\SafeWoman\SafeWoman

git init
git branch -M main
git add .
git commit -m "Initial commit"

# Crea un repo nuevo en github.com/new (privado o público)
# Luego:
git remote add origin https://github.com/TU-USUARIO/safewoman-api.git
git push -u origin main
```

### ⚠️ Verifica que estos archivos NO se hayan subido:

- `appsettings.Development.json` (contiene passwords)
- `bin/`, `obj/`, `TestResults/`, `CoverageReport/`
- `SafeWoman/Platforms/Android/Resources/values/strings.xml` (Google Maps key)
- `PasswordAdmin.txt`, `*.apk`, `*.keystore`

Si alguno aparece en el repo → ya están en el `.gitignore`, pero si se coló antes, quítalo:
```bash
git rm --cached appsettings.Development.json
git commit -m "Remove secrets from repo"
git push
```

---

## 🌐 Paso 3 — Desplegar la API en Render (15 min)

### 3.1 Crear el servicio web

1. [render.com](https://render.com) → **New +** → **Web Service**.
2. **Connect a repository** → autoriza GitHub → elige `safewoman-api`.
3. Configuración inicial:
   - **Name**: `safewoman-api`
   - **Region**: **Ohio (US East)** — misma que Neon
   - **Branch**: `main`
   - **Runtime**: **Docker** (detecta tu `Dockerfile` automáticamente)
   - **Instance Type**: **Free**

**⚠️ NO hagas clic en "Create Web Service" todavía** — falta configurar las variables de entorno.

### 3.2 Variables de entorno (críticas)

Baja hasta **Environment Variables** → agrega estas **13 variables**:

| Key | Value | Notas |
|---|---|---|
| `ASPNETCORE_ENVIRONMENT` | `Production` | Modo de ejecución |
| `ConnectionStrings__DefaultConnection` | `Host=ep-XXX.neon.tech;Database=neondb;Username=neondb_owner;Password=XXX;SSL Mode=Require;Trust Server Certificate=true` | ⚠️ Convierte la Neon URL al formato `Host=...;Database=...` |
| `Jwt__Key` | Genera 64 caracteres aleatorios | `openssl rand -base64 48` |
| `Jwt__Issuer` | `SafeWoman.API` | Emisor |
| `Jwt__Audience` | `SafeWoman.Mobile` | Audiencia |
| `Jwt__ExpirationHours` | `168` | 7 días (para demo académica) |
| `Sms__Provider` | `Twilio` | O `Console` para no gastar saldo |
| `Twilio__AccountSid` | `ACXXXXXXX...` | De [console.twilio.com](https://console.twilio.com) |
| `Twilio__AuthToken` | `XXXXXXXXX` | Del mismo lugar |
| `Twilio__FromNumber` | `+17XXXXXXXXX` | Tu número Twilio |
| `AdminSeed__Email` | `admin@safewoman.pe` | Admin inicial |
| `AdminSeed__Password` | Password fuerte, mínimo 12 chars | Guárdala en un manager |
| `AdminSeed__Nombre` | `Administrador SafeWoman` | Nombre visible en el panel |

**⚠️ Notas importantes:**

- Los `__` (doble guion bajo) son la convención de ASP.NET Core que se convierte a `:` en `IConfiguration`. Si pones solo un guion, no funciona.
- Si no configuras Twilio, la app arranca igual pero los SMS no se envían de verdad (a menos que uses `Sms__Provider=Console`).

### 3.3 Deploy

Ahora sí, clic en **Create Web Service**. Render empieza el build.

**Tiempos esperados:**

| Fase | Duración |
|---|---|
| Descarga del SDK .NET 8 (primera vez) | 2-3 min |
| `dotnet restore` | 1-2 min |
| `dotnet publish -c Release` | 1-2 min |
| Subir la imagen y arrancar el contenedor | 30 seg |
| **Total primera vez** | **~5-8 min** |
| **Deploys siguientes (con cache)** | **~2-4 min** |

Cuando termine, verás **"Your service is live 🎉"** y tu URL:

```
https://safewoman-api.onrender.com
```

### 3.4 Verificar el deploy

Abre estas URLs — todas deben responder:

- ✅ https://safewoman-api.onrender.com → `SafeWoman API — servicio en línea.`
- ✅ https://safewoman-api.onrender.com/panel-safewoman/Auth/Login → formulario de login
- ✅ https://safewoman-api.onrender.com/swagger → documentación OpenAPI

**Entra al panel Admin** con las credenciales de `AdminSeed`. Si logras acceder al dashboard, ¡la BD está bien conectada!

---

## ⏰ Paso 4 — Evitar el "cold start" con UptimeRobot (5 min)

El plan Free de Render **duerme el contenedor tras 15 min sin tráfico**. La primera petición tras dormir tarda **30-50 segundos**. Solución: pingear la API cada 5 minutos para mantenerla despierta.

### 4.1 Crear el monitor

1. [uptimerobot.com](https://uptimerobot.com) → **Sign Up** (gratis).
2. Confirma tu email.
3. Dashboard → **+ New Monitor**:
   - **Monitor Type**: `HTTP(s)`
   - **Friendly Name**: `SafeWoman API`
   - **URL**: `https://safewoman-api.onrender.com/panel-safewoman/Auth/Login`
   - **Monitoring Interval**: `5 minutes` (mínimo del plan free)
   - **How will we notify you**: deja solo `E-mail` (marca el checkbox)
4. Clic **Create Monitor**.

En ~1 minuto verás el monitor en estado **Up** (verde). A partir de ahí, la API nunca duerme.

---

## 📱 Paso 5 — Compilar y distribuir el APK Android (15 min)

### 5.1 Actualizar la URL de producción

Abre `SafeWoman/MauiProgram.cs` y verifica que en la rama Release apunte a producción:

```csharp
#else
    // Producción — API desplegada en Render.com con SSL válido.
    private const string ApiBaseUrl = "https://safewoman-api.onrender.com/api/";
#endif
```

### 5.2 Compilar el APK

Desde PowerShell en la raíz del proyecto:

```powershell
dotnet publish SafeWoman/SafeWoman.csproj `
  -f net8.0-android `
  -c Release `
  -p:AndroidPackageFormat=apk
```

Tarda **~5-10 minutos** la primera vez. El APK se genera en:

```
SafeWoman\SafeWoman\bin\Release\net8.0-android\publish\com.companyname.safewoman-Signed.apk
```

### 5.3 Distribuir el APK

**Opción A — Google Drive (recomendado para academia)**:
1. Sube el `.apk` a tu Drive.
2. **Clic derecho → Compartir → "Cualquier persona con el vínculo" → Lector**.
3. Copia el link. Los testers lo abren, descargan e instalan.

**Opción B — QR code** (impacto en la defensa):
1. Genera un QR de la URL del Drive en [qr-code-generator.com](https://qr-code-generator.com).
2. Muestra el QR en tu presentación → el jurado escanea e instala en vivo.

**Opción C — Firebase App Distribution** (más profesional, requiere crear proyecto Firebase):
1. [console.firebase.google.com](https://console.firebase.google.com) → **Add project**.
2. **App Distribution** → sube el APK → agrega correos de testers.
3. Cada tester recibe email con link de instalación.

**⚠️ Al instalar**: Android mostrará "Play Protect no reconoce esta app" — es normal, el APK no está firmado con clave de Play Store. Los usuarios deben tocar **"Instalar de todas formas"**.

---

## 🚀 Paso 6 — CI/CD con GitHub Actions (ya configurado)

El proyecto incluye **`.github/workflows/ci.yml`** que corre automáticamente:

- ✅ En cada **push a `main`** o `qa`
- ✅ En cada **pull request hacia `main`**

Los 5 jobs del pipeline:

```
1. 🔨 Build              → compila los 4 proyectos + tests
2. 🧪 Unit tests         → 158 tests (sin infra)
3. 🐘 Integration tests  → 11 tests (Testcontainers PostgreSQL)
4. 🌐 API tests          → 18 tests (WebApplicationFactory)
5. 📊 Coverage report    → HTML descargable como artifact
```

Ver resultados en: `https://github.com/TU-USUARIO/safewoman-api/actions`.

**No requiere configuración adicional** — funciona con solo hacer push al repo. Si algún test falla, el pipeline se marca en rojo y bloquea el merge del PR.

---

## 🌿 Flujo de ramas recomendado

```
                 ┌───────────────┐
                 │  main         │  ← Producción (Render escucha aquí)
                 └───────┬───────┘
                         │
              merge tras │ tests verdes en CI
                         │
                 ┌───────┴───────┐
                 │  qa           │  ← Integración / staging
                 └───────┬───────┘
                         │
              merge tras │ code review
                         │
             ┌───────────┴───────────┐
             │  feature/nueva-cosa   │  ← Tu trabajo diario
             └───────────────────────┘
```

**Reglas de oro:**
- **Nunca hagas push directo a `main`** — usa Pull Requests.
- **CI/CD debe estar verde** antes de mergear.
- Render solo despliega desde `main` — todo lo que empujes a otras ramas no toca producción.

---

## 🔥 Rollback si algo falla en producción

Render mantiene **todos los deploys anteriores**. En emergencia:

1. Dashboard de Render → tu servicio → **Events**.
2. Encuentra el último deploy que funcionaba.
3. Clic en él → **"Rollback to this deploy"**.
4. En ~30 segundos, la versión anterior vuelve a estar activa.

Alternativamente, desde Git:

```bash
git revert HEAD                    # revierte el último commit
git push origin main                # Render redespliega la versión "revertida"
```

---

## 💰 Costos reales

| Servicio | Plan | Uso mensual esperado | Costo |
|---|---|---|---|
| Render (Web Service) | Free | 750 horas/mes | $0 |
| Neon (PostgreSQL) | Free | 3 GB storage, 0.5 GB RAM | $0 |
| GitHub | Free (público) | Ilimitado | $0 |
| GitHub Actions | Free (público) | 2000 min/mes | $0 |
| UptimeRobot | Free | 50 monitores | $0 |
| Twilio SMS | Pay-as-you-go | ~5-30 SMS de prueba | $0.40 – $2.40 |
| **TOTAL** | | | **~$2/mes** |

*Los planes gratuitos son suficientes para demostración académica y prototipos con hasta ~1000 usuarios.*

---

## ✅ Checklist antes de la defensa

- [ ] Panel Admin en producción responde: https://safewoman-api.onrender.com/panel-safewoman/Auth/Login
- [ ] Swagger accesible: https://safewoman-api.onrender.com/swagger
- [ ] UptimeRobot en **"Up"** (verde)
- [ ] GitHub Actions verde en `main`
- [ ] APK Android instalado en tu teléfono, funcionando con datos móviles
- [ ] Al menos 1 víctima registrada y 1 contacto de emergencia en la BD
- [ ] Twilio con saldo mínimo ($0.50 = ~6 SMS de demo)
- [ ] Password del admin **guardada en un lugar seguro** (no en el código)

---

## 🆘 Troubleshooting común

| Síntoma | Causa probable | Solución |
|---|---|---|
| Login del panel "credenciales incorrectas" | Admin no se creó al inicio | Ver logs de Render, buscar "Admin semilla creado" |
| API devuelve 500 en /swagger | Basic Auth mal configurado | Comentar el middleware o dejar Swagger público |
| `429 Too Many Requests` en /api/auth/* | Rate limiter | Espera 1 min o aumenta `RateLimit__AuthPerMinute` |
| APK dice "no conexión al servidor" | URL antigua en el APK | Recompila con la URL de Render |
| Cold start de 50 seg | UptimeRobot no está pingeando | Verifica que el monitor esté "Up" |
| PostgreSQL error "database does not exist" | Migraciones no aplicadas | El seeder las aplica al arrancar; revisa logs |

---

**¿Todo listo?** Vuelve al [README](README.md) o consulta la [documentación técnica completa](DOCUMENTACION.md).
