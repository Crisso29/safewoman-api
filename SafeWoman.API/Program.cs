using System.Text;
using System.Threading.RateLimiting;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using SafeWoman.API.Infrastructure;
using SafeWoman.API.Middleware;
using SafeWoman.Application;
using SafeWoman.Infrastructure;
using SafeWoman.Infrastructure.Persistence;
using SafeWoman.Infrastructure.Services.Realtime;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddApplication();
builder.Services.AddInfrastructure(builder.Configuration);

// ── JWT ──────────────────────────────────────────────────────────────────────
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddCookie("AdminCookies", opt =>
    {
        opt.LoginPath         = "/panel-safewoman/Auth/Login";
        opt.AccessDeniedPath  = "/panel-safewoman/Auth/Denegado";
        opt.Cookie.Name       = "SafeWoman.Admin";
        opt.Cookie.HttpOnly   = true;
        opt.Cookie.SameSite   = SameSiteMode.Lax;
        opt.Cookie.SecurePolicy = CookieSecurePolicy.SameAsRequest;
        opt.ExpireTimeSpan    = TimeSpan.FromHours(8);
        opt.SlidingExpiration = true;
    })
    .AddJwtBearer(options =>
    {
        var key = Encoding.UTF8.GetBytes(builder.Configuration["Jwt:Key"]!);
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer            = true,
            ValidateAudience          = true,
            ValidateLifetime          = true,
            ValidateIssuerSigningKey  = true,
            ValidIssuer               = builder.Configuration["Jwt:Issuer"],
            ValidAudience             = builder.Configuration["Jwt:Audience"],
            IssuerSigningKey          = new SymmetricSecurityKey(key),
            // Tolerancia de 30 s: los relojes de un celular y del servidor pueden
            // diferir por unos segundos. Zero era demasiado estricto y causaba 401
            // por diferencia horaria del dispositivo.
            ClockSkew                 = TimeSpan.FromSeconds(30)
        };

        // En Development mostramos el detalle exacto del fallo — clave para
        // saber si es firma, expiración, issuer o audience.
        options.IncludeErrorDetails = builder.Environment.IsDevelopment();

        options.Events = new JwtBearerEvents
        {
            OnMessageReceived = ctx =>
            {
                // Permite JWT desde query string para SignalR
                var token = ctx.Request.Query["access_token"];
                if (!string.IsNullOrEmpty(token) && ctx.Request.Path.StartsWithSegments("/hubs"))
                    ctx.Token = token;
                return Task.CompletedTask;
            },
            OnAuthenticationFailed = ctx =>
            {
                // Log detallado por qué rechazamos el token — evita el 401 críptico.
                var logger = ctx.HttpContext.RequestServices
                    .GetRequiredService<ILogger<Program>>();
                logger.LogWarning(
                    "JWT rechazado en {Path}: {ExceptionType} — {Message}",
                    ctx.Request.Path,
                    ctx.Exception.GetType().Name,
                    ctx.Exception.Message);
                return Task.CompletedTask;
            },
            OnChallenge = ctx =>
            {
                // Se dispara cuando no hay token o el token es rechazado.
                var logger = ctx.HttpContext.RequestServices
                    .GetRequiredService<ILogger<Program>>();
                var motivo = string.IsNullOrEmpty(ctx.Error) ? "sin token" : ctx.Error;
                logger.LogWarning(
                    "JWT challenge en {Path}: {Motivo} — {Description}",
                    ctx.Request.Path, motivo, ctx.ErrorDescription ?? "-");
                return Task.CompletedTask;
            }
        };
    });

builder.Services.AddAuthorization();

// Límite del body: 5 archivos × 10 MB + overhead de formulario
builder.WebHost.ConfigureKestrel(opts =>
    opts.Limits.MaxRequestBodySize = 55 * 1024 * 1024); // 55 MB

builder.Services.AddControllersWithViews()
    .AddJsonOptions(opt =>
        // Serializa enums como string ("activa", "cancelada") en lugar de número (0, 1)
        opt.JsonSerializerOptions.Converters.Add(
            new System.Text.Json.Serialization.JsonStringEnumConverter(
                System.Text.Json.JsonNamingPolicy.CamelCase)));
builder.Services.AddRazorPages();

// ── Swagger ───────────────────────────────────────────────────────────────────
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new() { Title = "SafeWoman API", Version = "v1" });
    c.AddSecurityDefinition("Bearer", new Microsoft.OpenApi.Models.OpenApiSecurityScheme
    {
        Name        = "Authorization",
        Type        = Microsoft.OpenApi.Models.SecuritySchemeType.Http,
        Scheme      = "bearer",
        BearerFormat = "JWT",
        In          = Microsoft.OpenApi.Models.ParameterLocation.Header
    });
    c.AddSecurityRequirement(new Microsoft.OpenApi.Models.OpenApiSecurityRequirement
    {
        {
            new Microsoft.OpenApi.Models.OpenApiSecurityScheme
            {
                Reference = new Microsoft.OpenApi.Models.OpenApiReference
                    { Type = Microsoft.OpenApi.Models.ReferenceType.SecurityScheme, Id = "Bearer" }
            },
            Array.Empty<string>()
        }
    });
});

// ── CORS ──────────────────────────────────────────────────────────────────────
// En Development se permite cualquier origen (emulador, Swagger, etc.)
// En Production solo los orígenes declarados en Cors:AllowedOrigins
builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
    {
        if (builder.Environment.IsDevelopment())
        {
            policy.AllowAnyOrigin().AllowAnyHeader().AllowAnyMethod();
        }
        else
        {
            var origins = builder.Configuration
                .GetSection("Cors:AllowedOrigins")
                .Get<string[]>() ?? [];

            policy.WithOrigins(origins)
                  .AllowAnyHeader()
                  .AllowAnyMethod()
                  .AllowCredentials();
        }
    });
});

// ── Rate Limiting ─────────────────────────────────────────────────────────────
// Máximo 10 peticiones por minuto por IP en los endpoints de autenticación
builder.Services.AddRateLimiter(options =>
{
    options.AddFixedWindowLimiter("auth", limiter =>
    {
        limiter.PermitLimit           = 10;
        limiter.Window                = TimeSpan.FromMinutes(1);
        limiter.QueueProcessingOrder  = QueueProcessingOrder.OldestFirst;
        limiter.QueueLimit            = 0;
    });

    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
    options.OnRejected = async (ctx, ct) =>
    {
        ctx.HttpContext.Response.ContentType = "application/json";
        await ctx.HttpContext.Response.WriteAsync(
            "{\"error\":\"Demasiadas solicitudes. Espere un momento e intente de nuevo.\"}", ct);
    };
});

// ─────────────────────────────────────────────────────────────────────────────
var app = builder.Build();

// Aplica migraciones pendientes automáticamente — envuelto en try/catch para no crashear el proceso
{
    using var scope  = app.Services.CreateScope();
    var startLogger  = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();
    try
    {
        var db = scope.ServiceProvider.GetRequiredService<SafeWomanDbContext>();
        db.Database.Migrate();
        startLogger.LogInformation("✔ Migraciones aplicadas correctamente.");
    }
    catch (Exception ex)
    {
        startLogger.LogError(ex,
            "⚠ No se pudieron aplicar migraciones: {Message}. " +
            "Verifique que SQL Server esté activo y la cadena de conexión sea correcta.",
            ex.Message);
        // No relanzamos — la API arranca aunque las migraciones fallen,
        // mostrando errores detallados en cada endpoint que use la BD.
    }
}

app.UseMiddleware<ExceptionMiddleware>();

// ── Cabeceras de seguridad ────────────────────────────────────────────────────
// Defensa en profundidad estándar OWASP. Ninguna cabecera cambia la funcionalidad,
// pero endurecen la superficie frente a ataques de red y de navegador.
app.Use(async (ctx, next) =>
{
    var h = ctx.Response.Headers;
    h["X-Content-Type-Options"]  = "nosniff";                                // bloquea MIME-sniffing
    h["X-Frame-Options"]         = "DENY";                                   // bloquea clickjacking
    h["Referrer-Policy"]         = "strict-origin-when-cross-origin";        // no filtrar URLs a terceros
    h["Permissions-Policy"]      = "geolocation=(), microphone=(), camera=()"; // el panel no necesita sensores
    // HSTS obliga al navegador a usar solo HTTPS. Se aplica únicamente cuando la
    // conexión ya es HTTPS (evita romper pruebas locales por HTTP).
    if (ctx.Request.IsHttps)
        h["Strict-Transport-Security"] = "max-age=31536000; includeSubDomains";
    await next();
});

// ── Swagger protegido con Basic Auth ──────────────────────────────────────────
// El catálogo de endpoints es información sensible para un atacante. Lo dejamos
// disponible para la defensa académica pero pedimos credenciales antes de servir
// tanto el HTML del UI como el JSON del schema.
app.UseWhen(
    ctx => ctx.Request.Path.StartsWithSegments("/swagger"),
    swaggerApp =>
    {
        swaggerApp.Use(async (ctx, next) =>
        {
            var user = builder.Configuration["Swagger:User"];
            var pass = builder.Configuration["Swagger:Password"];

            // Si no hay credenciales configuradas, bloquea por defecto —
            // "fail closed" para no exponer Swagger por olvido en producción.
            if (string.IsNullOrEmpty(user) || string.IsNullOrEmpty(pass))
            {
                ctx.Response.StatusCode = StatusCodes.Status404NotFound;
                return;
            }

            var auth = ctx.Request.Headers.Authorization.ToString();
            if (auth.StartsWith("Basic ", StringComparison.OrdinalIgnoreCase))
            {
                var raw = Encoding.UTF8.GetString(Convert.FromBase64String(auth[6..]));
                var parts = raw.Split(':', 2);
                if (parts.Length == 2 && parts[0] == user && parts[1] == pass)
                {
                    await next();
                    return;
                }
            }

            ctx.Response.Headers.WWWAuthenticate = "Basic realm=\"SafeWoman API — Documentación\"";
            ctx.Response.StatusCode = StatusCodes.Status401Unauthorized;
        });
        swaggerApp.UseSwagger();
        swaggerApp.UseSwaggerUI(c =>
        {
            c.SwaggerEndpoint("/swagger/v1/swagger.json", "SafeWoman API v1");
            c.DocumentTitle = "SafeWoman API — Documentación";
        });
    });

// UseHttpsRedirection NO se activa nunca dentro del contenedor:
//   - En Development trabajamos con HTTP directo (LAN interna).
//   - En Production (Render, Azure App Service, etc.), el reverse proxy termina
//     el SSL en el edge y pasa HTTP al contenedor. Si activáramos redirection
//     aquí, el server intentaría redirigir 307 → HTTPS y HttpClient descartaría
//     el header Authorization en el redirect (defensa contra fuga de token).
// La política HTTPS-only ya la aplica Render en su edge.

app.UseStaticFiles();
app.UseRateLimiter();
app.UseCors();
app.UseAuthentication();
app.UseAuthorization();

// ── Rutas MVC para el panel Admin (ANTES de MapControllers) ──────────────────
// El area interna se sigue llamando "Admin" (los controllers viven en Areas/Admin),
// pero la URL pública es /panel-safewoman/... para no delatar la existencia de un
// panel administrativo a scanners automáticos.
app.MapAreaControllerRoute(
    name:      "PanelSafeWoman",
    areaName:  "Admin",
    pattern:   "panel-safewoman/{controller=Dashboard}/{action=Index}/{id?}");

app.MapControllers();
app.MapHub<SosHub>("/hubs/sos");

// La raíz no revela la existencia del panel admin — solo devuelve un mensaje
// neutro. Cualquiera que quiera entrar al panel debe conocer la ruta exacta.
app.MapGet("/", () => Results.Text(
    "SafeWoman API — servicio en línea.",
    contentType: "text/plain; charset=utf-8"));

// ── Seed del primer administrador ────────────────────────────────────────────
await DbSeeder.SeedAsync(app.Services);

app.Run();
