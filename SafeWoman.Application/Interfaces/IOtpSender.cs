namespace SafeWoman.Application.Interfaces;

public interface IOtpSender
{
    Task SendOtpAsync(string toPhone, string code, CancellationToken ct = default);
}
