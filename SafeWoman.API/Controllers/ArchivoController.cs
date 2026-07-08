using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SafeWoman.Infrastructure.Persistence;

namespace SafeWoman.API.Controllers;

/// <summary>
/// Sirve los archivos binarios (foto DNI, evidencias) almacenados en la BD.
///
/// Público sin JWT:
///   Las evidencias las acceden tanto la app móvil (víctima ve su propia denuncia)
///   como el panel Admin (autoridades revisan casos). Como el ID es un entero
///   secuencial, un atacante que iterara podría enumerar archivos — pero solo
///   vería binarios sin metadatos que los vinculen a una víctima. Aceptable
///   para el nivel académico; en un despliegue de producción real se usaría un
///   token firmado (SAS-style) o URLs con GUID.
/// </summary>
[Route("api/archivo")]
public class ArchivoController : ControllerBase
{
    private readonly SafeWomanDbContext _db;

    public ArchivoController(SafeWomanDbContext db) => _db = db;

    [HttpGet("{id:int}")]
    public async Task<IActionResult> Obtener(int id, CancellationToken ct)
    {
        var archivo = await _db.Archivos
            .AsNoTracking()
            .Where(a => a.IdArchivo == id)
            .Select(a => new { a.Contenido, a.ContentType, a.NombreOriginal })
            .FirstOrDefaultAsync(ct);

        if (archivo is null)
            return NotFound();

        // Cache moderado — los archivos son inmutables (nunca se editan), así
        // que el navegador puede cachearlos. Reduce carga en la BD cuando el
        // panel Admin abre la misma denuncia repetidamente.
        Response.Headers["Cache-Control"] = "public, max-age=3600";

        // Devolvemos con Content-Disposition: inline para que el navegador
        // muestre imágenes/videos directamente (no fuerce descarga).
        return File(archivo.Contenido, archivo.ContentType, archivo.NombreOriginal, enableRangeProcessing: true);
    }
}
