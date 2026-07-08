using SafeWoman.Application.DTOs.Denuncia;
using SafeWoman.Application.Interfaces;
using SafeWoman.Domain.Entities;
using SafeWoman.Domain.Enums;
using SafeWoman.Domain.Exceptions;
using SafeWoman.Domain.Interfaces;

namespace SafeWoman.Application.Services;

public class DenunciaService
{
    private readonly IRepository<Denuncia>   _denunciaRepo;
    private readonly IRepository<Denunciado> _denunciadoRepo;
    private readonly IRepository<Evidencia>  _evidenciaRepo;
    private readonly IUnitOfWork             _uow;
    private readonly IFileStorage            _fileStorage;

    public DenunciaService(
        IRepository<Denuncia> denunciaRepo,
        IRepository<Denunciado> denunciadoRepo,
        IRepository<Evidencia> evidenciaRepo,
        IUnitOfWork uow,
        IFileStorage fileStorage)
    {
        _denunciaRepo   = denunciaRepo;
        _denunciadoRepo = denunciadoRepo;
        _evidenciaRepo  = evidenciaRepo;
        _uow            = uow;
        _fileStorage    = fileStorage;
    }

    public async Task<int> CrearFormalAsync(
        int idVictima,
        DenunciaFormalRequest req,
        Stream fotoDni,
        string fotoDniNombre,
        IEnumerable<(Stream stream, string nombre, TipoArchivo tipo, long tamanio)>? evidencias = null,
        CancellationToken ct = default)
    {
        // Los archivos se guardan primero (fuera de la transacción BD).
        // Si la transacción falla, se limpian en el catch.
        var fotoDniRuta     = await _fileStorage.SaveAsync(fotoDni, fotoDniNombre, "dni", ct);
        var rutasEvidencias = new List<string>();

        await using var tx = await _uow.BeginTransactionAsync(ct);
        try
        {
            var denuncia = Denuncia.CrearFormal(
                idVictima, fotoDniRuta,
                req.Departamento, req.Provincia, req.Distrito,
                req.ReferenciaUbicacion, req.Latitud, req.Longitud,
                req.FechaHecho, req.HoraHecho, req.Descripcion);

            await _denunciaRepo.AddAsync(denuncia, ct);
            await _uow.SaveChangesAsync(ct);

            if (req.NombreAliasDenunciado is not null || req.RelacionDenunciado is not null)
            {
                var denunciado = Denunciado.Crear(
                    denuncia.IdDenuncia, req.NombreAliasDenunciado, req.RelacionDenunciado);
                await _denunciadoRepo.AddAsync(denunciado, ct);
            }

            if (evidencias is not null)
            {
                foreach (var (stream, nombre, tipo, tamanio) in evidencias)
                {
                    var ruta = await _fileStorage.SaveAsync(stream, nombre, "evidencias", ct);
                    rutasEvidencias.Add(ruta);
                    await _evidenciaRepo.AddAsync(
                        Evidencia.Crear(denuncia.IdDenuncia, nombre, ruta, tipo, tamanio), ct);
                }
            }

            await _uow.SaveChangesAsync(ct);
            await tx.CommitAsync(ct);

            return denuncia.IdDenuncia;
        }
        catch
        {
            await tx.RollbackAsync(ct);
            _fileStorage.Delete(fotoDniRuta);
            rutasEvidencias.ForEach(_fileStorage.Delete);
            throw;
        }
    }

    public async Task<IReadOnlyList<DenunciaDto>> ListarPorVictimaAsync(int idVictima, CancellationToken ct = default)
    {
        var denuncias = await _denunciaRepo.FindAsync(d => d.IdVictima == idVictima, ct);
        return denuncias.OrderByDescending(d => d.FechaEnvio).Select(MapToDto).ToList();
    }

    public async Task<DenunciaDto> ObtenerAsync(int idVictima, int idDenuncia, CancellationToken ct = default)
    {
        var denuncia = await _denunciaRepo.GetByIdAsync(idDenuncia, ct)
            ?? throw new DomainException("Denuncia no encontrada.");

        if (denuncia.IdVictima != idVictima)
            throw new DomainException("No autorizado.");

        return MapToDto(denuncia);
    }

    private static DenunciaDto MapToDto(Denuncia d) =>
        new(d.IdDenuncia, d.Tipo, d.Estado, d.FechaEnvio,
            d.Departamento, d.Provincia, d.Distrito, d.Descripcion,
            d.Denunciado?.NombreAlias,
            d.Denunciado?.RelacionVictima?.ToString(),  // ?.ToString() evita string vacío cuando la relación es null
            d.Evidencias.Select(e => new EvidenciaDto(
                e.IdEvidencia, e.NombreArchivo, e.TipoArchivo, e.TamanioBytes, e.FechaSubida)).ToList());
}
