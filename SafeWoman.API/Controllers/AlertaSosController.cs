using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SafeWoman.Application.DTOs.AlertaSos;
using SafeWoman.Application.Services;

namespace SafeWoman.API.Controllers;

[Route("api/sos")]
[Authorize(Roles = "Victima")]
public class AlertaSosController : ApiControllerBase
{
    private readonly AlertaSosService _sosService;

    public AlertaSosController(AlertaSosService sosService) => _sosService = sosService;

    [HttpPost("activar")]
    public async Task<IActionResult> Activar([FromBody] ActivarSosRequest req, CancellationToken ct)
    {
        var alerta = await _sosService.ActivarAsync(IdVictima, req, ct);
        return Ok(alerta);
    }

    [HttpPost("{idAlerta:int}/cancelar")]
    public async Task<IActionResult> Cancelar(int idAlerta, CancellationToken ct)
    {
        var alerta = await _sosService.CancelarAsync(IdVictima, idAlerta, ct);
        return Ok(alerta);
    }

    [HttpGet]
    public async Task<IActionResult> Listar(CancellationToken ct)
    {
        var alertas = await _sosService.ListarPorVictimaAsync(IdVictima, ct);
        return Ok(alertas);
    }
}
