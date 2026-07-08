using SafeWoman.Domain.Enums;

namespace SafeWoman.Application.DTOs.Admin;

public record EvidenciaAdminDto(
    int      Id,
    string   NombreArchivo,
    string   UrlDescarga,
    string   Tipo,
    long?    TamanioBytes,
    DateTime FechaSubida);

public record AdminLoginRequest(string Email, string Password);

public record AdminDashboardDto(
    int TotalVictimas,
    int AlertasActivas,
    int DenunciasHoy,
    int DenunciasPendientes,
    int DispositivosBloqueados,
    IReadOnlyList<AdminAlertaDto> AlertasRecientes);

public record AdminAlertaDto(
    int    IdAlerta,
    int    IdVictima,
    string NombreVictima,
    string TelefonoVictima,
    decimal Latitud,
    decimal Longitud,
    DateTime TimestampActivacion,
    DateTime? TimestampCancelacion,
    string Estado);

public record AdminDenunciaDto(
    int       IdDenuncia,
    int       IdVictima,
    string    NombreVictima,
    string    TelefonoVictima,
    string    Tipo,
    string    Estado,
    DateTime  FechaEnvio,
    string?   Departamento,
    string?   Provincia,
    string?   Distrito,
    string?   ReferenciaUbicacion,
    decimal?  Latitud,
    decimal?  Longitud,
    DateOnly? FechaHecho,
    TimeOnly? HoraHecho,
    string?   Descripcion,
    string?   NombreDenunciado,
    string?   RelacionDenunciado,
    string?   FotoDniRuta,
    IReadOnlyList<EvidenciaAdminDto> Evidencias);

public record AdminDenunciaAnonimaDto(
    int       IdDenunciaAnonima,
    int       IdHuella,
    string    Estado,
    DateTime  FechaEnvio,
    string?   Departamento,
    string?   Provincia,
    string?   Distrito,
    string?   ReferenciaUbicacion,
    decimal?  Latitud,
    decimal?  Longitud,
    DateOnly? FechaHecho,
    TimeOnly? HoraHecho,
    string?   Descripcion,
    string?   NombreDenunciado,
    string?   RelacionDenunciado,
    bool      HuellaActiva,
    IReadOnlyList<EvidenciaAdminDto> Evidencias);

public record AdminVictimaDto(
    int      IdVictima,
    string   NombreCompleto,
    string   Dni,
    string   Telefono,
    bool     Verificada,
    bool     Activa,
    DateTime FechaRegistro,
    int      TotalAlertas,
    int      TotalDenuncias);

public record AdminLogDto(
    int      IdLog,
    int?     IdAdmin,
    string?  NombreAdmin,
    string   Accion,
    string   EntidadAfectada,
    int?     IdEntidadAfectada,
    string?  Descripcion,
    DateTime Timestamp);

public record AdminHuellaDto(
    int      IdHuella,
    string   DeviceFingerprint,
    bool     Bloqueada,
    DateTime FechaPrimerUso,
    DateTime FechaUltimoUso,
    int      TotalDenuncias);
