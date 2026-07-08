using System.Security.Cryptography;
using SafeWoman.Application.Interfaces;

namespace SafeWoman.Infrastructure.Services.Security;

public class OtpCodeGenerator : IOtpCodeGenerator
{
    public string Generate()
    {
        using var rng = RandomNumberGenerator.Create();
        var bytes = new byte[4];
        rng.GetBytes(bytes);
        var value = BitConverter.ToUInt32(bytes, 0) % 1_000_000;
        return value.ToString("D6");
    }
}
