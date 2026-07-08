using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;

namespace SafeWoman.Infrastructure.Services.Realtime;

// Solo víctimas autenticadas (JWT vía ?access_token=) o administradores con
// sesión activa (AdminCookies) pueden conectarse. El hub no acepta mensajes
// de clientes: solo recibe broadcasts emitidos por los controllers de la API
// tras validar el JWT en el pipeline. Restringimos la CONEXIÓN para evitar
// que un tercero anónimo se suscriba y escuche alertas SOS en curso.
[Authorize(AuthenticationSchemes = "Bearer,AdminCookies")]
public class SosHub : Hub
{
    public override Task OnConnectedAsync()  => base.OnConnectedAsync();
    public override Task OnDisconnectedAsync(Exception? exception) => base.OnDisconnectedAsync(exception);
}
