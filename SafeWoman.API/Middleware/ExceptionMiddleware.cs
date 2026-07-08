using System.Net;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using SafeWoman.Domain.Exceptions;

namespace SafeWoman.API.Middleware;

public class ExceptionMiddleware
{
    private static readonly JsonSerializerOptions JsonOpts = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull
    };

    private readonly RequestDelegate                _next;
    private readonly ILogger<ExceptionMiddleware>  _logger;
    private readonly IWebHostEnvironment            _env;

    public ExceptionMiddleware(RequestDelegate next,
        ILogger<ExceptionMiddleware> logger, IWebHostEnvironment env)
    {
        _next   = next;
        _logger = logger;
        _env    = env;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (DomainException ex)
        {
            await WriteProblemAsync(context, HttpStatusCode.BadRequest,
                "Solicitud inválida", ex.Message);
        }
        catch (UnauthorizedAccessException ex)
        {
            await WriteProblemAsync(context, HttpStatusCode.Unauthorized,
                "No autorizado", ex.Message);
        }
        catch (OperationCanceledException)
        {
            _logger.LogWarning("Petición cancelada por el cliente.");
            if (!context.Response.HasStarted)
                context.Response.StatusCode = 499; // Client Closed Request
        }
        catch (SqlException ex)
        {
            _logger.LogError(ex, "Error de base de datos");

            var detail = ex.Number == 208
                ? "Tabla no encontrada en la base de datos. Ejecuta 'dotnet ef database update' o reinicia la API para aplicar las migraciones."
                : (_env.IsDevelopment() ? $"SQL Error {ex.Number}: {ex.Message}" : "Error de base de datos.");

            await WriteProblemAsync(context, HttpStatusCode.InternalServerError,
                "Error de base de datos", detail);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error no controlado: {Type} — {Message}", ex.GetType().Name, ex.Message);

            var detail = _env.IsDevelopment()
                ? $"[{ex.GetType().Name}] {ex.Message}"
                : "Error interno del servidor.";

            await WriteProblemAsync(context, HttpStatusCode.InternalServerError,
                "Error interno del servidor", detail);
        }
    }

    private Task WriteProblemAsync(HttpContext ctx, HttpStatusCode status, string title, string detail)
    {
        if (ctx.Response.HasStarted)
            return Task.CompletedTask;

        ctx.Response.ContentType = "application/problem+json";
        ctx.Response.StatusCode  = (int)status;

        var problem = new ProblemDetails
        {
            Type     = MapType(status),
            Title    = title,
            Status   = (int)status,
            Detail   = detail,
            Instance = ctx.Request.Path
        };

        return ctx.Response.WriteAsync(JsonSerializer.Serialize(problem, JsonOpts));
    }

    // URLs de referencia RFC 7231 para los códigos que emitimos. Evita URLs
    // generadas dinámicamente que apunten a secciones inexistentes.
    private static string MapType(HttpStatusCode status) => status switch
    {
        HttpStatusCode.BadRequest          => "https://tools.ietf.org/html/rfc7231#section-6.5.1",
        HttpStatusCode.Unauthorized        => "https://tools.ietf.org/html/rfc7235#section-3.1",
        HttpStatusCode.Forbidden           => "https://tools.ietf.org/html/rfc7231#section-6.5.3",
        HttpStatusCode.NotFound            => "https://tools.ietf.org/html/rfc7231#section-6.5.4",
        HttpStatusCode.Conflict            => "https://tools.ietf.org/html/rfc7231#section-6.5.8",
        HttpStatusCode.InternalServerError => "https://tools.ietf.org/html/rfc7231#section-6.6.1",
        _                                   => $"https://httpstatuses.com/{(int)status}"
    };
}
