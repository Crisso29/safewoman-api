using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using SafeWoman.Application.DTOs.Auth;
using SafeWoman.Application.Services;

namespace SafeWoman.API.Controllers;

[Route("api/auth")]
[EnableRateLimiting("auth")]
public class AuthController : ApiControllerBase
{
    private readonly AuthService _authService;

    public AuthController(AuthService authService) => _authService = authService;

    [HttpPost("registro")]
    public async Task<IActionResult> Registro([FromBody] RegistroRequest? req, CancellationToken ct)
    {
        if (req is null) return BadRequest(new { error = "El cuerpo de la petición es obligatorio." });
        var idVictima = await _authService.RegistrarAsync(req, ct);
        return Ok(new { idVictima, mensaje = "Cuenta creada. Verifique el SMS enviado a su teléfono." });
    }

    [HttpPost("verificar-otp")]
    public async Task<IActionResult> VerificarOtp([FromBody] VerificarOtpRequest? req, CancellationToken ct)
    {
        if (req is null) return BadRequest(new { error = "El cuerpo de la petición es obligatorio." });
        var response = await _authService.VerificarOtpAsync(req, ct);
        return Ok(response);
    }

    [HttpPost("login")]
    public async Task<IActionResult> Login([FromBody] LoginRequest? req, CancellationToken ct)
    {
        if (req is null) return BadRequest(new { error = "El cuerpo de la petición es obligatorio." });
        var response = await _authService.LoginAsync(req, ct);
        return Ok(response);
    }

    [HttpPost("reenviar-otp")]
    public async Task<IActionResult> ReenviarOtp([FromBody] ReenviarOtpRequest? req, CancellationToken ct)
    {
        if (req is null || string.IsNullOrWhiteSpace(req.Telefono))
            return BadRequest(new { error = "El teléfono es obligatorio." });
        await _authService.ReenviarOtpAsync(req.Telefono, ct);
        return Ok(new { mensaje = "Código reenviado." });
    }
}
