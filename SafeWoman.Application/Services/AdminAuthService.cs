using SafeWoman.Application.Interfaces;
using SafeWoman.Domain.Entities;
using SafeWoman.Domain.Interfaces;

namespace SafeWoman.Application.Services;

public class AdminAuthService
{
    private readonly IRepository<Administrador> _adminRepo;
    private readonly IUnitOfWork                _uow;
    private readonly IPasswordHasher            _hasher;

    public AdminAuthService(
        IRepository<Administrador> adminRepo,
        IUnitOfWork uow,
        IPasswordHasher hasher)
    {
        _adminRepo = adminRepo;
        _uow       = uow;
        _hasher    = hasher;
    }

    public async Task<Administrador?> LoginAsync(string email, string password, CancellationToken ct = default)
    {
        var admins = await _adminRepo.FindAsync(
            a => a.Email == email.Trim().ToLowerInvariant() && a.Activo, ct);

        var admin = admins.FirstOrDefault();
        if (admin is null)                              return null;
        if (!_hasher.Verify(password, admin.PasswordHash)) return null;

        admin.RegistrarAcceso();
        await _uow.SaveChangesAsync(ct);
        return admin;
    }

    public async Task<bool> ExisteAdminAsync(CancellationToken ct = default)
    {
        var todos = await _adminRepo.GetAllAsync(ct);
        return todos.Any();
    }

    public async Task CrearAdminAsync(string nombre, string email, string password, CancellationToken ct = default)
    {
        var hash  = _hasher.Hash(password);
        var admin = Administrador.Crear(nombre, email, hash);
        await _adminRepo.AddAsync(admin, ct);
        await _uow.SaveChangesAsync(ct);
    }
}
