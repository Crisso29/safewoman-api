using Microsoft.EntityFrameworkCore;
using SafeWoman.Application.DTOs.Admin;
using SafeWoman.Application.Interfaces;
using SafeWoman.Domain.Entities;
using SafeWoman.Domain.Enums;
using SafeWoman.Domain.Exceptions;
using SafeWoman.Domain.Interfaces;
using SafeWoman.Infrastructure.Persistence;

namespace SafeWoman.Infrastructure.Services.Admin;

public class AdminService : IAdminService
{
    private readonly SafeWomanDbContext        _db;
    private readonly IRepository<LogAuditoria> _logRepo;
    private readonly IUnitOfWork               _uow;

    public AdminService(SafeWomanDbContext db, IRepository<LogAuditoria> logRepo, IUnitOfWork uow)
    {
        _db      = db;
        _logRepo = logRepo;
        _uow     = uow;
    }

    // ── Dashboard ──────────────────────────────────────────────────────────────
    public async Task<AdminDashboardDto> GetDashboardAsync(CancellationToken ct = default)
    {
        var totalVictimas  = await _db.Victimas.IgnoreQueryFilters().CountAsync(ct);
        var alertasActivas = await _db.AlertasSos.CountAsync(a => a.Estado == EstadoAlerta.Activa, ct);
        var inicioDia      = DateTime.UtcNow.Date;
        var finDia         = inicioDia.AddDays(1);
        var denunciasHoy   = await _db.Denuncias.CountAsync(d => d.FechaEnvio >= inicioDia && d.FechaEnvio < finDia, ct)
                           + await _db.DenunciasAnonimas.CountAsync(d => d.FechaEnvio >= inicioDia && d.FechaEnvio < finDia, ct);
        var denunciasPend  = await _db.Denuncias.CountAsync(d => d.Estado == EstadoDenuncia.Pendiente, ct);
        var denuncAnonPend = await _db.DenunciasAnonimas.CountAsync(d => d.Estado == EstadoDenuncia.Pendiente, ct);
        var bloqueadas     = await _db.HuellasDispositivo.CountAsync(h => h.Bloqueada, ct);

        var alertasRecientes = await _db.AlertasSos
            .AsNoTracking()
            .Include(a => a.Victima)
            .OrderByDescending(a => a.TimestampActivacion)
            .Take(8)
            .Select(a => new AdminAlertaDto(
                a.IdAlerta, a.IdVictima,
                a.Victima.NombreCompleto, a.Victima.Telefono,
                a.Latitud, a.Longitud,
                a.TimestampActivacion, a.TimestampCancelacion,
                a.Estado.ToString()))
            .ToListAsync(ct);

        return new AdminDashboardDto(
            totalVictimas, alertasActivas,
            denunciasHoy,
            denunciasPend + denuncAnonPend,
            bloqueadas, alertasRecientes);
    }

    // ── Alertas SOS ───────────────────────────────────────────────────────────
    public async Task<IReadOnlyList<AdminAlertaDto>> ListarAlertasAsync(int page = 1, int pageSize = 50, CancellationToken ct = default)
    {
        var (skip, take) = NormalizePaging(page, pageSize);
        return await _db.AlertasSos
            .AsNoTracking()
            .OrderByDescending(a => a.TimestampActivacion)
            .Skip(skip).Take(take)
            .Select(a => new AdminAlertaDto(
                a.IdAlerta, a.IdVictima,
                a.Victima.NombreCompleto, a.Victima.Telefono,
                a.Latitud, a.Longitud,
                a.TimestampActivacion, a.TimestampCancelacion,
                a.Estado.ToString()))
            .ToListAsync(ct);
    }

    // ── Denuncias formales ────────────────────────────────────────────────────
    public async Task<IReadOnlyList<AdminDenunciaDto>> ListarDenunciasAsync(int page = 1, int pageSize = 50, CancellationToken ct = default)
    {
        var (skip, take) = NormalizePaging(page, pageSize);
        // Proyección directa a DTO en la query: EF traduce las subcolecciones a un
        // único JOIN + agrupación en SQL Server (evita materializar entidades completas).
        return await _db.Denuncias
            .AsNoTracking()
            .IgnoreQueryFilters()
            .OrderByDescending(d => d.FechaEnvio)
            .Skip(skip).Take(take)
            .Select(d => new AdminDenunciaDto(
                d.IdDenuncia, d.IdVictima,
                d.Victima.NombreCompleto, d.Victima.Telefono,
                d.Tipo.ToString(), d.Estado.ToString(), d.FechaEnvio,
                d.Departamento, d.Provincia, d.Distrito,
                d.ReferenciaUbicacion, d.LatHecho, d.LngHecho,
                d.FechaHecho, d.HoraHecho,
                d.Descripcion,
                d.Denunciado != null ? d.Denunciado.NombreAlias : null,
                d.Denunciado != null && d.Denunciado.RelacionVictima != null
                    ? d.Denunciado.RelacionVictima.ToString()
                    : null,
                d.FotoDniRuta,
                d.Evidencias.Select(e => new EvidenciaAdminDto(
                    e.IdEvidencia, e.NombreArchivo,
                    "/" + e.RutaArchivo.Replace('\\', '/'),
                    e.TipoArchivo.ToString(), e.TamanioBytes, e.FechaSubida))
                .ToList()))
            .ToListAsync(ct);
    }

    public async Task CambiarEstadoDenunciaAsync(int idDenuncia, EstadoDenuncia nuevoEstado, int idAdmin, CancellationToken ct = default)
    {
        var denuncia = await _db.Denuncias.FindAsync([idDenuncia], ct)
            ?? throw new DomainException("Denuncia no encontrada.");
        denuncia.CambiarEstado(nuevoEstado);
        await RegistrarLogAsync(idAdmin, AccionAuditoria.CambioEstadoDenuncia,
            "DENUNCIA", idDenuncia, $"Estado → {nuevoEstado}", ct);
        await _uow.SaveChangesAsync(ct);
    }

    public async Task AtenderAlertaAsync(int idAlerta, int idAdmin, CancellationToken ct = default)
    {
        var alerta = await _db.AlertasSos.FindAsync([idAlerta], ct)
            ?? throw new DomainException("Alerta no encontrada.");
        alerta.Atender();
        await RegistrarLogAsync(idAdmin, AccionAuditoria.AtenderAlerta,
            "ALERTA_SOS", idAlerta, "Marcada como atendida por el administrador", ct);
        await _uow.SaveChangesAsync(ct);
    }

    // ── Denuncias anónimas ────────────────────────────────────────────────────
    public async Task<IReadOnlyList<AdminDenunciaAnonimaDto>> ListarDenunciasAnonimasAsync(int page = 1, int pageSize = 50, CancellationToken ct = default)
    {
        var (skip, take) = NormalizePaging(page, pageSize);
        return await _db.DenunciasAnonimas
            .AsNoTracking()
            .OrderByDescending(d => d.FechaEnvio)
            .Skip(skip).Take(take)
            .Select(d => new AdminDenunciaAnonimaDto(
                d.IdDenunciaAnonima, d.IdHuella, d.Estado.ToString(), d.FechaEnvio,
                d.Departamento, d.Provincia, d.Distrito,
                d.ReferenciaUbicacion, d.LatHecho, d.LngHecho,
                d.FechaHecho, d.HoraHecho,
                d.Descripcion,
                d.Denunciado != null ? d.Denunciado.NombreAlias : null,
                d.Denunciado != null && d.Denunciado.Relacion != null
                    ? d.Denunciado.Relacion.ToString()
                    : null,
                !d.HuellaDispositivo.Bloqueada,
                d.Evidencias.Select(e => new EvidenciaAdminDto(
                    e.IdEvidenciaAn, e.NombreArchivo,
                    "/" + e.RutaArchivo.Replace('\\', '/'),
                    e.TipoArchivo.ToString(), e.TamanioBytes, e.FechaSubida))
                .ToList()))
            .ToListAsync(ct);
    }

    public async Task CambiarEstadoDenunciaAnonimaAsync(int id, EstadoDenuncia nuevoEstado, int idAdmin, CancellationToken ct = default)
    {
        var denuncia = await _db.DenunciasAnonimas.FindAsync([id], ct)
            ?? throw new DomainException("Denuncia anónima no encontrada.");
        denuncia.CambiarEstado(nuevoEstado);
        await RegistrarLogAsync(idAdmin, AccionAuditoria.CambioEstadoDenunciaAnonima,
            "DENUNCIA_ANONIMA", id, $"Estado → {nuevoEstado}", ct);
        await _uow.SaveChangesAsync(ct);
    }

    // ── Víctimas ──────────────────────────────────────────────────────────────
    public async Task<IReadOnlyList<AdminVictimaDto>> ListarVictimasAsync(int page = 1, int pageSize = 50, CancellationToken ct = default)
    {
        var (skip, take) = NormalizePaging(page, pageSize);
        // Los Count() se traducen a subqueries agregadas en la misma SELECT (SQL Server los
        // resuelve en una sola pasada usando índices en IdVictima); no es un N+1 en runtime.
        return await _db.Victimas
            .AsNoTracking()
            .IgnoreQueryFilters()
            .OrderByDescending(v => v.FechaRegistro)
            .Skip(skip).Take(take)
            .Select(v => new AdminVictimaDto(
                v.IdVictima, v.NombreCompleto, v.Dni, v.Telefono,
                v.Verificada, v.Activa, v.FechaRegistro,
                _db.AlertasSos.Count(a => a.IdVictima == v.IdVictima),
                _db.Denuncias.Count(d => d.IdVictima == v.IdVictima)))
            .ToListAsync(ct);
    }

    public async Task DesactivarVictimaAsync(int idVictima, int idAdmin, CancellationToken ct = default)
    {
        var victima = await _db.Victimas.IgnoreQueryFilters()
            .FirstOrDefaultAsync(v => v.IdVictima == idVictima, ct)
            ?? throw new DomainException("Víctima no encontrada.");
        victima.Desactivar();
        await RegistrarLogAsync(idAdmin, AccionAuditoria.DesactivarVictima, "VICTIMA", idVictima, victima.NombreCompleto, ct);
        await _uow.SaveChangesAsync(ct);
    }

    public async Task ActivarVictimaAsync(int idVictima, int idAdmin, CancellationToken ct = default)
    {
        var victima = await _db.Victimas.IgnoreQueryFilters()
            .FirstOrDefaultAsync(v => v.IdVictima == idVictima, ct)
            ?? throw new DomainException("Víctima no encontrada.");
        victima.Activar();
        await RegistrarLogAsync(idAdmin, AccionAuditoria.ActivarVictima, "VICTIMA", idVictima, victima.NombreCompleto, ct);
        await _uow.SaveChangesAsync(ct);
    }

    // ── Huellas ───────────────────────────────────────────────────────────────
    public async Task<IReadOnlyList<AdminHuellaDto>> ListarHuellasAsync(int page = 1, int pageSize = 50, CancellationToken ct = default)
    {
        var (skip, take) = NormalizePaging(page, pageSize);
        return await _db.HuellasDispositivo
            .AsNoTracking()
            .OrderByDescending(h => h.FechaUltimoUso)
            .Skip(skip).Take(take)
            .Select(h => new AdminHuellaDto(
                h.IdHuella, h.DeviceFingerprint, h.Bloqueada,
                h.FechaPrimerUso, h.FechaUltimoUso,
                _db.DenunciasAnonimas.Count(d => d.IdHuella == h.IdHuella)))
            .ToListAsync(ct);
    }

    public async Task BloquearHuellaAsync(int idHuella, int idAdmin, CancellationToken ct = default)
    {
        var huella = await _db.HuellasDispositivo.FindAsync([idHuella], ct)
            ?? throw new DomainException("Huella no encontrada.");
        huella.Bloquear();
        await RegistrarLogAsync(idAdmin, AccionAuditoria.BloqueoHuella, "HUELLA_DISPOSITIVO", idHuella, huella.DeviceFingerprint[..8] + "...", ct);
        await _uow.SaveChangesAsync(ct);
    }

    public async Task DesbloquearHuellaAsync(int idHuella, int idAdmin, CancellationToken ct = default)
    {
        var huella = await _db.HuellasDispositivo.FindAsync([idHuella], ct)
            ?? throw new DomainException("Huella no encontrada.");
        huella.Desbloquear();
        await RegistrarLogAsync(idAdmin, AccionAuditoria.DesbloqueoHuella, "HUELLA_DISPOSITIVO", idHuella, huella.DeviceFingerprint[..8] + "...", ct);
        await _uow.SaveChangesAsync(ct);
    }

    // ── Logs ──────────────────────────────────────────────────────────────────
    public async Task<IReadOnlyList<AdminLogDto>> ListarLogsAsync(int page = 1, int pageSize = 100, CancellationToken ct = default)
    {
        var (skip, take) = NormalizePaging(page, pageSize);
        return await _db.LogsAuditoria
            .AsNoTracking()
            .OrderByDescending(l => l.Timestamp)
            .Skip(skip).Take(take)
            .Select(l => new AdminLogDto(
                l.IdLog, l.IdAdmin,
                l.Administrador != null ? l.Administrador.Nombre : "Sistema",
                l.Accion.ToString(), l.EntidadAfectada,
                l.IdEntidadAfectada, l.Descripcion, l.Timestamp))
            .ToListAsync(ct);
    }

    private static (int Skip, int Take) NormalizePaging(int page, int pageSize)
    {
        if (page < 1) page = 1;
        if (pageSize < 1) pageSize = 50;
        if (pageSize > 200) pageSize = 200;
        return ((page - 1) * pageSize, pageSize);
    }

    public async Task RegistrarLogAsync(
        int? idAdmin, AccionAuditoria accion,
        string entidad, int? idEntidad, string? descripcion,
        CancellationToken ct = default)
    {
        var log = LogAuditoria.Registrar(idAdmin, accion, entidad, idEntidad, descripcion);
        await _logRepo.AddAsync(log, ct);
        await _uow.SaveChangesAsync(ct); // ← persiste el log; antes se omitía
    }
}
