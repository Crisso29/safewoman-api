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
        opt.LoginPath         = "/Admin/Auth/Login";
        opt.AccessDeniedPath  = "/Admin/Auth/Denegado";
        opt.Cookie.Name       = "SafeWoman.Admin";
        opt.Cookie.HttpOnly   = true;
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

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

// UseHttpsRedirection en Development ROMPE el flujo cuando el cliente MAUI usa HTTP:
// el server responde 307 → HTTPS, HttpClient sigue el redirect pero descarta el
// header Authorization por seguridad (defensa contra fuga de token). Resultado:
// el server ve "sin token" y responde 401. En Development trabajamos con HTTP
// directo (LAN interna). En producción HTTPS lo maneja el reverse proxy.
if (!app.Environment.IsDevelopment())
    app.UseHttpsRedirection();

app.UseStaticFiles();
app.UseRateLimiter();
app.UseCors();
app.UseAuthentication();
app.UseAuthorization();

// ── Rutas MVC para el panel Admin (ANTES de MapControllers) ──────────────────
app.MapControllerRoute(
    name: "Admin",
    pattern: "{area:exists}/{controller=Dashboard}/{action=Index}/{id?}");

app.MapControllers();
app.MapHub<SosHub>("/hubs/sos");

// Redirect raíz → panel admin
app.MapGet("/", ctx => { ctx.Response.Redirect("/Admin/Auth/Login"); return Task.CompletedTask; });

// ── Seed del primer administrador ────────────────────────────────────────────
await DbSeeder.SeedAsync(app.Services);

app.Run();
