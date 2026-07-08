using SafeWoman.Application.DTOs.Victima;
using SafeWoman.Domain.Entities;
using SafeWoman.Domain.Exceptions;
using SafeWoman.Domain.Interfaces;

namespace SafeWoman.Application.Services;

public class ContactoService
{
    private readonly IRepository<ContactoEmergencia> _contactoRepo;
    private readonly IUnitOfWork _uow;

    public ContactoService(IRepository<ContactoEmergencia> contactoRepo, IUnitOfWork uow)
    {
        _contactoRepo = contactoRepo;
        _uow = uow;
    }

    public async Task<IReadOnlyList<ContactoEmergenciaDto>> ListarAsync(int idVictima, CancellationToken ct = default)
    {
        var contactos = await _contactoRepo.FindAsync(c => c.IdVictima == idVictima, ct);
        return contactos.Select(c => new ContactoEmergenciaDto(c.IdContacto, c.Nombre, c.Telefono)).ToList();
    }

    public async Task<ContactoEmergenciaDto> CrearAsync(int idVictima, CrearContactoRequest req, CancellationToken ct = default)
    {
        var existentes = await _contactoRepo.FindAsync(c => c.IdVictima == idVictima, ct);
        if (existentes.Count >= 5)
            throw new DomainException("Máximo 5 contactos de emergencia por cuenta.");

        var contacto = ContactoEmergencia.Crear(idVictima, req.Nombre, req.Telefono);
        await _contactoRepo.AddAsync(contacto, ct);
        await _uow.SaveChangesAsync(ct);

        return new ContactoEmergenciaDto(contacto.IdContacto, contacto.Nombre, contacto.Telefono);
    }

    public async Task ActualizarAsync(int idVictima, int idContacto, ActualizarContactoRequest req, CancellationToken ct = default)
    {
        var contacto = await _contactoRepo.GetByIdAsync(idContacto, ct)
            ?? throw new DomainException("Contacto no encontrado.");

        if (contacto.IdVictima != idVictima)
            throw new DomainException("No autorizado.");

        contacto.Actualizar(req.Nombre, req.Telefono);
        await _uow.SaveChangesAsync(ct);
    }

    public async Task EliminarAsync(int idVictima, int idContacto, CancellationToken ct = default)
    {
        var contacto = await _contactoRepo.GetByIdAsync(idContacto, ct)
            ?? throw new DomainException("Contacto no encontrado.");

        if (contacto.IdVictima != idVictima)
            throw new DomainException("No autorizado.");

        _contactoRepo.Remove(contacto);
        await _uow.SaveChangesAsync(ct);
    }
}
