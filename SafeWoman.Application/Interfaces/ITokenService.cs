using SafeWoman.Domain.Entities;

namespace SafeWoman.Application.Interfaces;

/// <summary>
/// Emite JWT para víctimas autenticadas.
/// </summary>
public interface ITokenService
{
    string GenerateVictimaToken(Victima victima);
}
