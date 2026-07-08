namespace SafeWoman.Application.DTOs.Victima;

public record VictimaPerfilDto(
    int IdVictima,
    string NombreCompleto,
    string Dni,
    string Telefono,
    bool Verificada,
    DateTime FechaRegistro,
    IReadOnlyList<ContactoEmergenciaDto> Contactos
);
