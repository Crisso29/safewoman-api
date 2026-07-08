using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using SafeWoman.Application.Interfaces;
using SafeWoman.Domain.Exceptions;

namespace SafeWoman.Infrastructure.Services.Storage;

public class LocalFileStorage : IFileStorage
{
    // Whitelist de extensiones permitidas para foto de DNI y evidencias
    // (imágenes + PDF + video/audio corto). Cualquier otra se rechaza.
    private static readonly HashSet<string> AllowedExtensions = new(StringComparer.OrdinalIgnoreCase)
    {
        ".jpg", ".jpeg", ".png", ".webp", ".heic", ".pdf",
        ".mp4", ".mov", ".3gp",
        ".mp3", ".m4a", ".ogg", ".wav"
    };

    private readonly string _basePath;
    private readonly string _webRootPath;
    private readonly string _subPath;

    public LocalFileStorage(IWebHostEnvironment env, IConfiguration config)
    {
        _webRootPath = env.WebRootPath ?? Path.Combine(env.ContentRootPath, "wwwroot");
        _subPath     = config["FileStorage:BasePath"] ?? "uploads";
        _basePath    = Path.Combine(_webRootPath, _subPath);
        Directory.CreateDirectory(_basePath);
    }

    public async Task<string> SaveAsync(Stream fileStream, string originalFileName,
        string subFolder, CancellationToken ct = default)
    {
        var ext = Path.GetExtension(originalFileName);
        if (string.IsNullOrWhiteSpace(ext) || !AllowedExtensions.Contains(ext))
            throw new DomainException("Tipo de archivo no permitido.");

        var safeSubFolder = SanitizeSubFolder(subFolder);
        var folder        = Path.Combine(_basePath, safeSubFolder);

        // Blindaje contra path traversal: la ruta final debe seguir bajo _basePath.
        var basePathFull   = Path.GetFullPath(_basePath);
        var folderFull     = Path.GetFullPath(folder);
        if (!folderFull.StartsWith(basePathFull, StringComparison.OrdinalIgnoreCase))
            throw new DomainException("Ruta de destino inválida.");

        Directory.CreateDirectory(folderFull);

        var fileName = $"{Guid.NewGuid():N}{ext.ToLowerInvariant()}";
        var fullPath = Path.Combine(folderFull, fileName);

        await using var fs = File.Create(fullPath);
        await fileStream.CopyToAsync(fs, ct);

        return Path.Combine(_subPath, safeSubFolder, fileName).Replace('\\', '/');
    }

    public void Delete(string filePath)
    {
        // Blindaje contra path traversal en Delete: la ruta debe permanecer bajo wwwroot.
        var full         = Path.GetFullPath(Path.Combine(_webRootPath, filePath));
        var webRootFull  = Path.GetFullPath(_webRootPath);
        if (!full.StartsWith(webRootFull, StringComparison.OrdinalIgnoreCase))
            return;

        if (File.Exists(full))
            File.Delete(full);
    }

    private static string SanitizeSubFolder(string subFolder)
    {
        if (string.IsNullOrWhiteSpace(subFolder))
            return string.Empty;

        // Rechaza caracteres inválidos y segmentos peligrosos; solo permite letras,
        // dígitos, guiones y guiones bajos por segmento.
        var segments = subFolder.Replace('\\', '/').Split('/', StringSplitOptions.RemoveEmptyEntries);
        var clean    = new List<string>(segments.Length);
        foreach (var seg in segments)
        {
            if (seg is "." or "..") continue;
            var trimmed = new string(seg.Where(c => char.IsLetterOrDigit(c) || c is '-' or '_').ToArray());
            if (trimmed.Length > 0) clean.Add(trimmed);
        }
        return string.Join('/', clean);
    }
}
