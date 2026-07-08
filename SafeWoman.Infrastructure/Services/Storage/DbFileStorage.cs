using SafeWoman.Application.Interfaces;
using SafeWoman.Domain.Entities;
using SafeWoman.Domain.Exceptions;
using SafeWoman.Infrastructure.Persistence;

namespace SafeWoman.Infrastructure.Services.Storage;

/// <summary>
/// Implementación de IFileStorage que persiste los archivos en PostgreSQL como bytea.
///
/// Por qué en la BD y no en el filesystem:
///   Los PaaS gratuitos (Render Free, Fly.io free, Heroku hobby) usan filesystem
///   efímero — cada redeploy destruye los archivos. Guardar en la BD garantiza
///   que las evidencias sobreviven mientras Neon mantenga los datos.
///
/// Ruta devuelta:
///   Devuelve "archivo/{id}" (ej. "archivo/42"). El panel Admin usa la ruta como
///   href relativo, y el endpoint público /api/archivo/{id} sirve el binario.
/// </summary>
public class DbFileStorage : IFileStorage
{
    private static readonly HashSet<string> AllowedExtensions = new(StringComparer.OrdinalIgnoreCase)
    {
        ".jpg", ".jpeg", ".png", ".webp", ".heic", ".pdf",
        ".mp4", ".mov", ".3gp",
        ".mp3", ".m4a", ".ogg", ".wav"
    };

    private readonly SafeWomanDbContext _db;

    public DbFileStorage(SafeWomanDbContext db) => _db = db;

    public async Task<string> SaveAsync(Stream fileStream, string originalFileName,
        string subFolder, CancellationToken ct = default)
    {
        var ext = Path.GetExtension(originalFileName);
        if (string.IsNullOrWhiteSpace(ext) || !AllowedExtensions.Contains(ext))
            throw new DomainException("Tipo de archivo no permitido.");

        // Copiar el stream a un array — necesitamos byte[] para EF Core.
        using var ms = new MemoryStream();
        await fileStream.CopyToAsync(ms, ct);
        var contenido = ms.ToArray();

        // ContentType inferido desde la extensión (más confiable que el ContentType
        // que envía Android, a veces vacío o incorrecto).
        var contentType = InferirContentType(ext);

        var archivo = ArchivoAlmacenado.Crear(
            contenido: contenido,
            contentType: contentType,
            nombreOriginal: originalFileName,
            categoria: SanitizarCategoria(subFolder));

        await _db.Archivos.AddAsync(archivo, ct);
        await _db.SaveChangesAsync(ct);

        // Ruta relativa — el panel Admin la concatena a la URL base del sitio.
        // El endpoint /api/archivo/{id} resuelve el binario.
        return $"archivo/{archivo.IdArchivo}";
    }

    public void Delete(string filePath)
    {
        // filePath viene con formato "archivo/{id}".
        var partes = filePath.Split('/', StringSplitOptions.RemoveEmptyEntries);
        if (partes.Length != 2 || !int.TryParse(partes[1], out var id))
            return;

        var archivo = _db.Archivos.Find(id);
        if (archivo is not null)
        {
            _db.Archivos.Remove(archivo);
            _db.SaveChanges();
        }
    }

    // ContentType por extensión — evita depender del que envía el cliente
    // (MediaPicker de Android a veces envía "" o "application/octet-stream").
    private static string InferirContentType(string extension) => extension.ToLowerInvariant() switch
    {
        ".jpg" or ".jpeg" => "image/jpeg",
        ".png"            => "image/png",
        ".webp"           => "image/webp",
        ".heic"           => "image/heic",
        ".pdf"            => "application/pdf",
        ".mp4"            => "video/mp4",
        ".mov"            => "video/quicktime",
        ".3gp"            => "video/3gpp",
        ".mp3"            => "audio/mpeg",
        ".m4a"            => "audio/mp4",
        ".ogg"            => "audio/ogg",
        ".wav"            => "audio/wav",
        _                 => "application/octet-stream"
    };

    private static string SanitizarCategoria(string subFolder)
    {
        if (string.IsNullOrWhiteSpace(subFolder))
            return "general";
        var clean = new string(subFolder.Where(c => char.IsLetterOrDigit(c) || c is '-' or '_').ToArray());
        return clean.Length > 0 ? clean : "general";
    }
}
