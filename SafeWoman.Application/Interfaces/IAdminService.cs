using SafeWoman.Application.DTOs.Admin;
using SafeWoman.Domain.Enums;

namespace SafeWoman.Application.Interfaces;

/// <summary>
/// Contrato del servicio de administración. Implementado en Infrastructure
/// para acceso directo al DbContext (IgnoreQueryFilters, proyecciones complejas).
/// </summary>
public interface IAdminService
{
    Task<AdminDashboardDto>                    GetDashboardAsync(CancellationToken ct = default);

    Task<IReadOnlyList<AdminAlertaDto>>        ListarAlertasAsync(int page = 1, int pageSize = 50, CancellationToken ct = default);
    Task AtenderAlertaAsync(int idAlerta, int idAdmin, CancellationToken ct = default);

    Task<IReadOnlyList<AdminDenunciaDto>>      ListarDenunciasAsync(int page = 1, int pageSize = 50, CancellationToken ct = default);
    Task CambiarEstadoDenunciaAsync(int idDenuncia, EstadoDenuncia nuevoEstado, int idAdmin, CancellationToken ct = default);

    Task<IReadOnlyList<AdminDenunciaAnonimaDto>> ListarDenunciasAnonimasAsync(int page = 1, int pageSize = 50, CancellationToken ct = default);
    Task CambiarEstadoDenunciaAnonimaAsync(int id, EstadoDenuncia nuevoEstado, int idAdmin, CancellationToken ct = default);

    Task<IReadOnlyList<AdminVictimaDto>>       ListarVictimasAsync(int page = 1, int pageSize = 50, CancellationToken ct = default);
    Task ActivarVictimaAsync(int idVictima,   int idAdmin, CancellationToken ct = default);
    Task DesactivarVictimaAsync(int idVictima, int idAdmin, CancellationToken ct = default);

    Task<IReadOnlyList<AdminHuellaDto>>         ListarHuellasAsync(int page = 1, int pageSize = 50, CancellationToken ct = default);
    Task BloquearHuellaAsync(int idHuella,   int idAdmin, CancellationToken ct = default);
    Task DesbloquearHuellaAsync(int idHuella, int idAdmin, CancellationToken ct = default);

    Task<IReadOnlyList<AdminLogDto>>           ListarLogsAsync(int page = 1, int pageSize = 100, CancellationToken ct = default);
    Task RegistrarLogAsync(int? idAdmin, AccionAuditoria accion, string entidad,
                           int? idEntidad, string? descripcion, CancellationToken ct = default);
}
