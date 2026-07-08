using SafeWoman.Application.DTOs.DenunciaAnonima;
using SafeWoman.Application.Interfaces;
using SafeWoman.Domain.Entities;
using SafeWoman.Domain.Enums;
using SafeWoman.Domain.Exceptions;
using SafeWoman.Domain.Interfaces;

namespace SafeWoman.Application.Services;

public class DenunciaAnonimaService
{
    private readonly IRepository<DenunciaAnonima>    _denunciaRepo;
    private readonly IRepository<DenunciadoAnonima>  _denunciadoRepo;
    private readonly IRepository<EvidenciaAnonima>   _evidenciaRepo;
    private readonly IRepository<HuellaDispositivo>  _huellaRepo;
    private readonly IUnitOfWork                     _uow;
    private readonly IFileStorage                    _fileStorage;

    public DenunciaAnonimaService(
        IRepository<DenunciaAnonima> denunciaRepo,
        IRepository<DenunciadoAnonima> denunciadoRepo,
        IRepository<EvidenciaAnonima> evidenciaRepo,
        IRepository<HuellaDispositivo> huellaRepo,
        IUnitOfWork uow,
        IFileStorage fileStorage)
    {
        _denunciaRepo   = denunciaRepo;
        _denunciadoRepo = denunciadoRepo;
        _evidenciaRepo  = evidenciaRepo;
        _huellaRepo     = huellaRepo;
        _uow            = uow;
        _fileStorage    = fileStorage;
    }

    public async Task<int> EnviarAsync(
        DenunciaAnonimaRequest req,
        IEnumerable<(Stream stream, string nombre, TipoArchivo tipo, long tamanio)>? evidencias = null,
        CancellationToken ct = default)
    {
        var rutasEvidencias = new List<string>();

        await using var tx = await _uow.BeginTransactionAsync(ct);
        try
        {
            var huella = await ObtenerOCrearHuellaAsync(req.DeviceFingerprint, ct);

            var denuncia = DenunciaAnonima.Crear(
                huella.IdHuella,
                req.Departamento, req.Provincia, req.Distrito,
                req.ReferenciaUbicacion, req.Latitud, req.Longitud,
                req.FechaHecho, req.HoraHecho, req.Descripcion);

            await _denunciaRepo.AddAsync(denuncia, ct);
            await _uow.SaveChangesAsync(ct);

            if (req.NombreAliasDenunciado is not null || req.RelacionDenunciado is not null)
            {
                var denunciado = DenunciadoAnonima.Crear(
                    denuncia.IdDenunciaAnonima, req.NombreAliasDenunciado, req.RelacionDenunciado);
                await _denunciadoRepo.AddAsync(denunciado, ct);
            }

            if (evidencias is not null)
            {
                foreach (var (stream, nombre, tipo, tamanio) in evidencias)
                {
                    var ruta = await _fileStorage.SaveAsync(stream, nombre, "evidencias-anonimas", ct);
                    rutasEvidencias.Add(ruta);
                    await _evidenciaRepo.AddAsync(
                        EvidenciaAnonima.Crear(denuncia.IdDenunciaAnonima, nombre, ruta, tipo, tamanio), ct);
                }
            }

            await _uow.SaveChangesAsync(ct);
            await tx.CommitAsync(ct);

            return denuncia.IdDenunciaAnonima;
        }
        catch
        {
            await tx.RollbackAsync(ct);
            rutasEvidencias.ForEach(_fileStorage.Delete);
            throw;
        }
    }

    private async Task<HuellaDispositivo> ObtenerOCrearHuellaAsync(string fingerprint, CancellationToken ct)
    {
        var huellas = await _huellaRepo.FindAsync(h => h.DeviceFingerprint == fingerprint, ct);

        if (huellas.Any())
        {
            var huella = huellas.First();
            if (huella.Bloqueada)
                throw new DomainException("Este dispositivo ha sido bloqueado para enviar denuncias.");
            huella.RegistrarUso();
            _huellaRepo.Update(huella);
            await _uow.SaveChangesAsync(ct);
            return huella;
        }

        var nueva = HuellaDispositivo.Crear(fingerprint);
        await _huellaRepo.AddAsync(nueva, ct);
        await _uow.SaveChangesAsync(ct);
        return nueva;
    }
}
