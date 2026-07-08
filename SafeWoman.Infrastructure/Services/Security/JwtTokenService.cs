using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using SafeWoman.Application.Interfaces;
using SafeWoman.Domain.Entities;

namespace SafeWoman.Infrastructure.Services.Security;

public class JwtTokenService : ITokenService
{
    private readonly IConfiguration _config;

    public JwtTokenService(IConfiguration config) => _config = config;

    // Vida útil por defecto (h) si "Jwt:ExpirationHours" no está configurada.
    // 4 h es un equilibrio razonable entre seguridad y UX para una app de emergencia:
    // corto para limitar el impacto de un token robado, pero largo para no forzar
    // logins innecesarios en el mismo día.
    private const int DefaultExpirationHours = 4;

    public string GenerateVictimaToken(Victima victima)
    {
        var key   = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_config["Jwt:Key"]!));
        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var hours = int.TryParse(_config["Jwt:ExpirationHours"], out var h) && h > 0
            ? h : DefaultExpirationHours;

        var claims = new[]
        {
            new Claim(JwtRegisteredClaimNames.Sub, victima.IdVictima.ToString()),
            new Claim(JwtRegisteredClaimNames.Name, victima.NombreCompleto),
            new Claim("telefono", victima.Telefono),
            new Claim(ClaimTypes.Role, "Victima"),
            new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
        };

        var token = new JwtSecurityToken(
            issuer: _config["Jwt:Issuer"],
            audience: _config["Jwt:Audience"],
            claims: claims,
            expires: DateTime.UtcNow.AddHours(hours),
            signingCredentials: creds);

        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    // GenerateOtpCode() fue extraído a OtpCodeGenerator (SRP: IOtpCodeGenerator)
}
