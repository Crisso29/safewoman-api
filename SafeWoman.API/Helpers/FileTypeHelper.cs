using Microsoft.AspNetCore.Http;
using SafeWoman.Domain.Enums;

namespace SafeWoman.API.Helpers;

public static class FileTypeHelper
{
    // Whitelist de familias aceptadas por la API. Debe estar alineada con
    // LocalFileStorage.AllowedExtensions para que la validación coincida
    // en ambas capas (defensa en profundidad).
    private static readonly HashSet<string> AllowedExtensions = new(StringComparer.OrdinalIgnoreCase)
    {
        ".jpg", ".jpeg", ".png", ".webp", ".heic", ".pdf",
        ".mp4", ".mov", ".3gp",
        ".mp3", ".m4a", ".ogg", ".wav"
    };

    // Prefijos de content-type aceptados. Se comparan con StartsWith porque
    // los navegadores/plataformas pueden añadir parámetros (charset, codec, etc.).
    private static readonly string[] AllowedContentTypePrefixes =
        ["image/", "video/", "audio/", "application/pdf",
         "application/octet-stream"]; // Android MediaPicker a veces envía este genérico

    // Mapa de extensión → content-type esperado. Usado cuando el cliente no envía
    // el ContentType o envía uno genérico. La extensión es la fuente de verdad.
    private static readonly Dictionary<string, string> ExtensionToContentType =
        new(StringComparer.OrdinalIgnoreCase)
        {
            [".jpg"]  = "image/jpeg",  [".jpeg"] = "image/jpeg",
            [".png"]  = "image/png",   [".webp"] = "image/webp",
            [".heic"] = "image/heic",  [".pdf"]  = "application/pdf",
            [".mp4"]  = "video/mp4",   [".mov"]  = "video/quicktime",
            [".3gp"]  = "video/3gpp",  [".mp3"]  = "audio/mpeg",
            [".m4a"]  = "audio/mp4",   [".ogg"]  = "audio/ogg",
            [".wav"]  = "audio/wav"
        };

    public static TipoArchivo Inferir(string? contentType) => (contentType ?? string.Empty) switch
    {
        var ct when ct.StartsWith("image/", StringComparison.OrdinalIgnoreCase)  => TipoArchivo.Imagen,
        var ct when ct.StartsWith("video/", StringComparison.OrdinalIgnoreCase)  => TipoArchivo.Video,
        var ct when ct.StartsWith("audio/", StringComparison.OrdinalIgnoreCase)  => TipoArchivo.Documento,
        "application/pdf"                                                        => TipoArchivo.Pdf,
        var ct when ct.Contains("pdf", StringComparison.OrdinalIgnoreCase)       => TipoArchivo.Pdf,
        _                                                                        => TipoArchivo.Documento
    };

    /// Valida que el archivo tenga extensión permitida. La extensión es la fuente de
    /// verdad — es lo que usaremos para elegir dónde guardarlo. El ContentType es un
    /// hint secundario del cliente: si viene vacío o genérico (Android MediaPicker),
    /// aceptamos igual porque la extensión ya está whitelisted.
    public static bool EsArchivoValido(IFormFile file, out string motivo)
    {
        var ext = Path.GetExtension(file.FileName);
        if (string.IsNullOrWhiteSpace(ext) || !AllowedExtensions.Contains(ext))
        {
            motivo = $"El archivo '{file.FileName}' tiene una extensión no permitida ({ext}).";
            return false;
        }

        var ct = (file.ContentType ?? string.Empty).Trim();

        // Si el ContentType viene vacío O es genérico octet-stream, aceptamos:
        // la extensión ya está en la whitelist y el LocalFileStorage la revalidará.
        if (ct.Length == 0
            || ct.Equals("application/octet-stream", StringComparison.OrdinalIgnoreCase))
        {
            motivo = string.Empty;
            return true;
        }

        // Si viene un ContentType real, exigimos que coincida con familia aceptada.
        var contentTypeOk = AllowedContentTypePrefixes
            .Any(prefix => ct.StartsWith(prefix, StringComparison.OrdinalIgnoreCase));
        if (!contentTypeOk)
        {
            motivo = $"El archivo '{file.FileName}' declara un tipo de contenido no permitido ('{ct}').";
            return false;
        }

        motivo = string.Empty;
        return true;
    }

    /// Devuelve el content-type correcto para una extensión conocida.
    /// Usado por el cliente para siempre enviar un content-type válido.
    public static string ContentTypeFromExtension(string fileName)
    {
        var ext = Path.GetExtension(fileName);
        return ExtensionToContentType.TryGetValue(ext, out var ct)
            ? ct
            : "application/octet-stream";
    }
}
