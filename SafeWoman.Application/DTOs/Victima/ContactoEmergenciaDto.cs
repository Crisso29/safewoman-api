namespace SafeWoman.Application.DTOs.Victima;

public record ContactoEmergenciaDto(int IdContacto, string Nombre, string Telefono);

public record CrearContactoRequest(string Nombre, string Telefono);

public record ActualizarContactoRequest(string Nombre, string Telefono);
