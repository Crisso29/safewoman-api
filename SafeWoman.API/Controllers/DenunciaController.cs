using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SafeWoman.API.DTOs;
using SafeWoman.API.Helpers;
using SafeWoman.Application.DTOs.Denuncia;
using SafeWoman.Application.Services;

namespace SafeWoman.API.Controllers;

[Route("api/denuncias")]
[Authorize(Roles = "Victima")]
public class DenunciaController : ApiControllerBase
{
    private readonly DenunciaService _denunciaService;

    public DenunciaController(DenunciaService denunciaService) => _denunciaService = denunciaService;

    private const long MaxArchivoBytes = 10 * 1024 * 1024; // 10 MB por archivo

    [HttpPost("formal")]
    [Consumes("multipart/form-data")]
    public async Task<IActionResult> CrearFormal([FromForm] DenunciaFormalFormRequest form, CancellationToken ct)
    {
        if (form.FotoDni is null || form.FotoDni.Length == 0)
            return BadRequest(new { error = "La foto del DNI es obligatoria para denuncias formales." });

        if (form.FotoDni.Length > MaxArchivoBytes)
            return BadRequest(new { error = "La foto del DNI no puede superar 10 MB." });

        if (!FileTypeHelper.EsArchivoValido(form.FotoDni, out var motivoDni))
            return BadRequest(new { error = motivoDni });

        if (form.Evidencias is { Count: > 0 })
        {
            foreach (var f in form.Evidencias)
            {
                if (f.Length > MaxArchivoBytes)
                    return BadRequest(new { error = $"El archivo '{f.FileName}' supera el límite de 10 MB." });
                if (!FileTypeHelper.EsArchivoValido(f, out var motivo))
                    return BadRequest(new { error = motivo });
            }
        }

        var req = new DenunciaFormalRequest(
            form.NombreAliasDenunciado, form.RelacionDenunciado,
            form.Departamento, form.Provincia, form.Distrito,
            form.ReferenciaUbicacion, form.Latitud, form.Longitud,
            form.FechaHecho, form.HoraHecho, form.Descripcion ?? string.Empty);

        var evidencias = form.Evidencias is { Count: > 0 }
            ? form.Evidencias.Select(f =>
                (f.OpenReadStream(), f.FileName, FileTypeHelper.Inferir(f.ContentType), f.Length))
            : null;

        var id = await _denunciaService.CrearFormalAsync(
            IdVictima, req,
            form.FotoDni.OpenReadStream(), form.FotoDni.FileName,
            evidencias, ct);

        return Ok(new { idDenuncia = id, mensaje = "Denuncia formal enviada exitosamente." });
    }

    [HttpGet]
    public async Task<IActionResult> Listar(CancellationToken ct)
    {
        var denuncias = await _denunciaService.ListarPorVictimaAsync(IdVictima, ct);
        return Ok(denuncias);
    }

    [HttpGet("{id:int}")]
    public async Task<IActionResult> Obtener(int id, CancellationToken ct)
    {
        var denuncia = await _denunciaService.ObtenerAsync(IdVictima, id, ct);
        return Ok(denuncia);
    }
}
