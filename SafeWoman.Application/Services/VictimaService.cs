using SafeWoman.Application.DTOs.Victima;
using SafeWoman.Domain.Entities;
using SafeWoman.Domain.Exceptions;
using SafeWoman.Domain.Interfaces;

namespace SafeWoman.Application.Services;

public class VictimaService
{
    private readonly IRepository<Victima> _victimaRepo;
    private readonly IRepository<ContactoEmergencia> _contactoRepo;
    private readonly IUnitOfWork _uow;

    public VictimaService(
        IRepository<Victima> victimaRepo,
        IRepository<ContactoEmergencia> contactoRepo,
        IUnitOfWork uow)
    {
        _victimaRepo = victimaRepo;
        _contactoRepo = contactoRepo;
        _uow = uow;
    }

    public async Task<VictimaPerfilDto> ObtenerPerfilAsync(int idVictima, CancellationToken ct = default)
    {
        var victima = await _victimaRepo.GetByIdAsync(idVictima, ct)
            ?? throw new DomainException("Víctima no encontrada.");

        var contactos = await _contactoRepo.FindAsync(c => c.IdVictima == idVictima, ct);

        return new VictimaPerfilDto(
            victima.IdVictima,
            victima.NombreCompleto,
            victima.Dni,
            victima.Telefono,
            victima.Verificada,
            victima.FechaRegistro,
            contactos.Select(c => new ContactoEmergenciaDto(c.IdContacto, c.Nombre, c.Telefono)).ToList());
    }
}
