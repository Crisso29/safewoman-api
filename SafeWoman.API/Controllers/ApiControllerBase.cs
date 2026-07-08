using System.Security.Claims;
using Microsoft.AspNetCore.Mvc;

namespace SafeWoman.API.Controllers;

[ApiController]
public abstract class ApiControllerBase : ControllerBase
{
    protected int IdVictima =>
        int.TryParse(
            User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirstValue("sub"),
            out var id)
            ? id
            : throw new UnauthorizedAccessException("Token inválido o sin identificador de usuario.");
}
