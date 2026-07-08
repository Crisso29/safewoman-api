# Guía de despliegue — SafeWoman

Tiempo estimado: **60-90 minutos** en total.

## Prerrequisitos

- Cuenta GitHub (tienes).
- Cuenta Azure (crear en portal.azure.com — pide tarjeta pero no cobra Free Tier).
- Cuenta Render.com (crear con GitHub, sin tarjeta).

---

## Paso 1 — Subir el proyecto a GitHub (10 min)

Desde la raíz de la solución (`C:\Users\Intel\Downloads\SafeWoman\SafeWoman`):

```bash
git init
git add .
git commit -m "Initial commit — SafeWoman API + Panel Admin"

# En github.com crea un repo nuevo llamado "safewoman-api" (privado o público).
# Después:
git remote add origin https://github.com/TU-USUARIO/safewoman-api.git
git branch -M main
git push -u origin main
```

**Verifica** que en el repo no aparezcan estos archivos (deberían estar en `.gitignore`):
- `appsettings.Development.json` (contiene secretos)
- `bin/`, `obj/`
- `strings.xml` de Android (contiene Google Maps key)

---

## Paso 2 — Crear la BD en Azure SQL (30 min)

### 2.1 Crear el server
1. [portal.azure.com](https://portal.azure.com) → **Crear un recurso** → **SQL Database**.
2. **Suscripción**: la tuya.
3. **Grupo de recursos**: crear nuevo, `safewoman-rg`.
4. **Database name**: `safewomandb`.
5. **Server**: crear nuevo:
   - Server name: `safewoman-sql-servername` (único, minúsculas).
   - Location: **East US 2** (Free Tier disponible aquí).
   - Authentication: **SQL Authentication**.
   - Admin login: `sqladmin`.
   - Password: **algo largo con mayúsculas, minúsculas, números y símbolo**. Guárdalo.
6. **Compute + Storage** → **Configure database**:
   - Service tier: **General Purpose Serverless**.
   - **Marca la casilla "Apply free offer"** ← MUY IMPORTANTE.
   - Vcores: 0.5, max 2.
   - Auto-pause delay: 1 hora.
7. **Backup storage redundancy**: Locally-redundant (más barato).
8. **Review + Create** → **Create**.

### 2.2 Configurar firewall
1. Cuando el server esté listo, ve a **SQL server** (no la database, sino el server).
2. **Networking** → **Public access** → **Selected networks**.
3. **Firewall rules**:
   - Add your IP: **Add your client IPv4 address** (para conectarte desde tu PC).
   - **Add rule for Azure services**: `0.0.0.0` a `0.0.0.0` con nombre `AllowAzure`.
   - Add rule para Render (rango dinámico): agrega `0.0.0.0` a `255.255.255.255` con nombre `AllowAll` **SOLO POR AHORA**.
4. **Save**.

> ⚠️ Después del deploy exitoso, restringe `AllowAll` a los rangos IP de Render (los publican en su docs).

### 2.3 Aplicar las migraciones EF Core
Desde tu PC:

```bash
$env:AZURE_CONN = "Server=tcp:TU-SERVER.database.windows.net,1433;Database=safewomandb;User Id=sqladmin;Password=TU-PASSWORD;Encrypt=true;TrustServerCertificate=false;Connection Timeout=30;"

# En Windows PowerShell:
$env:ConnectionStrings__DefaultConnection=$env:AZURE_CONN

# Aplicar migraciones
dotnet ef database update -p SafeWoman.Infrastructure -s SafeWoman.API
```

Si te da error de conexión: verifica que tu IP esté en el firewall.

### 2.4 Obtener la connection string
En Azure: **SQL databases** → tu `safewomandb` → **Connection strings** → copia la de **ADO.NET (SQL authentication)**.

Reemplaza `{your_password}` por la contraseña real.

**Guárdala** — la necesitas en Paso 3.

---

## Paso 3 — Desplegar la API en Render (20 min)

### 3.1 Crear el servicio
1. [render.com](https://render.com) → **New +** → **Web Service**.
2. **Connect a repository** → autoriza GitHub → elige `safewoman-api`.
3. Configuración:
   - **Name**: `safewoman-api`.
   - **Region**: **Ohio** (US East).
   - **Branch**: `main`.
   - **Runtime**: **Docker** (detecta tu Dockerfile automáticamente).
   - **Instance Type**: **Free**.

### 3.2 Variables de entorno
En la sección **Environment**, agrega:

| Key | Value |
|---|---|
| `ConnectionStrings__DefaultConnection` | (la connection string de Azure SQL del paso 2.4) |
| `Jwt__Key` | genera una key aleatoria de 40+ caracteres |
| `Jwt__Issuer` | `SafeWoman.API` |
| `Jwt__Audience` | `SafeWoman.Mobile` |
| `Jwt__ExpirationHours` | `168` |
| `Sms__Provider` | `Twilio` |
| `Twilio__AccountSid` | tu AccountSid |
| `Twilio__AuthToken` | tu AuthToken |
| `Twilio__FromNumber` | `+17752566118` |
| `AdminSeed__Email` | `admin@safewoman.pe` |
| `AdminSeed__Password` | password fuerte de admin |
| `AdminSeed__Nombre` | `Administrador SafeWoman` |

### 3.3 Deploy
1. **Create Web Service**.
2. Render empieza a construir la imagen Docker.
3. El primer build tarda 5-10 min.
4. Al terminar, obtienes URL: `https://safewoman-api.onrender.com`.
5. Verifica que funcione visitando: `https://safewoman-api.onrender.com/swagger`.

### 3.4 Verifica que la BD tenga el admin creado
Visita `https://safewoman-api.onrender.com/Admin/Auth/Login` e ingresa con las credenciales de `AdminSeed`. Si entras, la BD está OK.

---

## Paso 4 — Compilar y distribuir el APK (15 min)

### 4.1 Actualizar la URL de la API
En `SafeWoman/MauiProgram.cs`:

```csharp
#if DEBUG && ANDROID
    private const string ApiBaseUrl = "http://" + LanIp + ":5015/api/";
#elif DEBUG
    private const string ApiBaseUrl = "https://localhost:7273/api/";
#else
    private const string ApiBaseUrl = "https://safewoman-api.onrender.com/api/";
#endif
```

Y elimina el `#error` de la rama Release.

### 4.2 En Visual Studio
1. Cambia la configuración a **Release**.
2. Menú **Compilación** → **Publicar SafeWoman** (el proyecto MAUI).
3. Elige **Android** → **Ad-hoc** (para APK).
4. Firma con un keystore de prueba (Visual Studio lo genera si no tienes).
5. Se genera un archivo `.apk` en:
   `SafeWoman/bin/Release/net8.0-android/publish/com.companyname.safewoman-Signed.apk`.

### 4.3 Distribuir el APK
Opciones:

**A. WhatsApp / Correo**: envías el `.apk` directo. Los usuarios lo abren y deben permitir "Instalar apps de fuentes desconocidas". Simple pero manual.

**B. Firebase App Distribution** (recomendado):
1. [console.firebase.google.com](https://console.firebase.google.com) → **Add project** → gratis.
2. Registrar app Android con package `com.companyname.safewoman`.
3. **App Distribution** → sube el APK → agrega correo de testers.
4. Los testers reciben email con link para instalar.

---

## Paso 5 — Mantener el servicio despierto (UptimeRobot)

Render Free "duerme" el servicio después de 15 min sin uso. Primer request tras dormir tarda 30-50 seg.

**Solución**: [uptimerobot.com](https://uptimerobot.com) → cuenta gratis → **Add New Monitor**:
- Type: HTTP(s)
- URL: `https://safewoman-api.onrender.com/swagger`
- Monitoring Interval: 5 minutes

Así el servicio nunca duerme.

---

## Paso 6 — Antes de la defensa

1. **Verificar el sitio**: visita `https://safewoman-api.onrender.com/Admin/Auth/Login` desde tu celular por datos móviles (no wifi del PC). Debe cargar.
2. **Probar el APK** en el celular con datos móviles: registro + login + SOS.
3. **Pre-calentar** el servicio 1 hora antes: entra al panel Admin.
4. Ten a mano el **plan B**: laptop encendida con VS por si algo falla.

---

## Rollback si algo falla en producción

Render mantiene versiones anteriores. **Dashboard → Deploys → Rollback** te devuelve al último deploy que funcionó.

## Costo real

| Servicio | Free Tier | Costo si excedes |
|---|---|---|
| Azure SQL Serverless | 32 vCore-hours/mes | ~$0.50/hora extra |
| Render Web Service | 750 horas/mes | $7/mes plan Starter |
| GitHub | Ilimitado repos privados | $0 |
| UptimeRobot | 50 monitores | $0 |
| Firebase App Distribution | Ilimitado | $0 |
| Twilio | Con tu crédito trial | $0.08/SMS después |

**Total esperado durante tesis/demo: $0.00**.
