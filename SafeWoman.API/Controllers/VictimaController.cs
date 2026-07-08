using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SafeWoman.Application.Services;

namespace SafeWoman.API.Controllers;

[Route("api/victima")]
[Authorize(Roles = "Victima")]
public class VictimaController : ApiControllerBase
{
    private readonly VictimaService _victimaService;

    public VictimaController(VictimaService victimaService) => _victimaService = victimaService;

    [HttpGet("perfil")]
    public async Task<IActionResult> ObtenerPerfil(CancellationToken ct)
    {
        var perfil = await _victimaService.ObtenerPerfilAsync(IdVictima, ct);
        return Ok(perfil);
    }
}
