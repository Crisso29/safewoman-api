using Microsoft.AspNetCore.Mvc;
using SafeWoman.API.DTOs;
using SafeWoman.API.Helpers;
using SafeWoman.Application.DTOs.DenunciaAnonima;
using SafeWoman.Application.Services;

namespace SafeWoman.API.Controllers;

[Route("api/denuncias/anonima")]
public class DenunciaAnonimaController : ApiControllerBase
{
    private readonly DenunciaAnonimaService _service;

    public DenunciaAnonimaController(DenunciaAnonimaService service) => _service = service;

    private const long MaxEvidenciaBytes = 10 * 1024 * 1024; // 10 MB por archivo

    [HttpPost]
    [Consumes("multipart/form-data")]
    public async Task<IActionResult> Enviar([FromForm] DenunciaAnonimaFormRequest form, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(form.DeviceFingerprint))
            return BadRequest(new { error = "DeviceFingerprint requerido." });

        if (form.Evidencias is { Count: > 0 })
        {
            foreach (var f in form.Evidencias)
            {
                if (f.Length > MaxEvidenciaBytes)
                    return BadRequest(new { error = $"El archivo '{f.FileName}' supera el límite de 10 MB." });
                if (!FileTypeHelper.EsArchivoValido(f, out var motivo))
                    return BadRequest(new { error = motivo });
            }
        }

        var req = new DenunciaAnonimaRequest(
            form.DeviceFingerprint,
            form.NombreAliasDenunciado, form.RelacionDenunciado,
            form.Departamento, form.Provincia, form.Distrito,
            form.ReferenciaUbicacion, form.Latitud, form.Longitud,
            form.FechaHecho, form.HoraHecho, form.Descripcion);

        var evidencias = form.Evidencias is { Count: > 0 }
            ? form.Evidencias.Select(f =>
                (f.OpenReadStream(), f.FileName, FileTypeHelper.Inferir(f.ContentType), f.Length))
            : null;

        var id = await _service.EnviarAsync(req, evidencias, ct);
        return Ok(new { idDenunciaAnonima = id, mensaje = "Denuncia anónima enviada correctamente." });
    }

    /// <summary>
    /// Devuelve las denuncias anónimas asociadas al deviceFingerprint provisto.
    /// Endpoint PÚBLICO (sin JWT) porque las denuncias anónimas no vinculan
    /// identidad, pero solo se listan si conoces el fingerprint exacto del
    /// dispositivo que las envió. Cualquier device puede consultar solo lo suyo.
    /// </summary>
    [HttpGet("mis")]
    public async Task<IActionResult> ListarPorDevice(
        [FromQuery] string deviceFingerprint,
        CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(deviceFingerprint))
            return BadRequest(new { error = "deviceFingerprint es obligatorio." });

        var denuncias = await _service.ListarPorDeviceAsync(deviceFingerprint, ct);
        return Ok(denuncias);
    }
}
