using System.Linq.Expressions;
using FluentAssertions;
using Moq;
using SafeWoman.Application.Interfaces;
using SafeWoman.Application.Services;
using SafeWoman.Domain.Entities;
using SafeWoman.Domain.Interfaces;

namespace SafeWoman.UnitTests.Services;

/// <summary>
/// AdminAuthService es la puerta al panel administrativo. Sus reglas:
/// - No revelar si el email existe (retornar null en ambos casos de fallo).
/// - Solo permitir login a admins activos.
/// - Actualizar UltimoAcceso al loguear correctamente.
/// </summary>
public class AdminAuthServiceTests
{
    private readonly Mock<IRepository<Administrador>> _repo   = new();
    private readonly Mock<IUnitOfWork>                _uow    = new();
    private readonly Mock<IPasswordHasher>            _hasher = new();

    private AdminAuthService CrearSut() =>
        new(_repo.Object, _uow.Object, _hasher.Object);

    [Fact]
    public async Task LoginAsync_con_credenciales_correctas_debe_devolver_admin_y_actualizar_ultimo_acceso()
    {
        var admin = Administrador.Crear("María Admin", "maria@safewoman.pe", "hash-bcrypt");
        _repo
            .Setup(r => r.FindAsync(It.IsAny<Expression<Func<Administrador, bool>>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new[] { admin });
        _hasher.Setup(h => h.Verify("pass", "hash-bcrypt")).Returns(true);

        var sut = CrearSut();

        var resultado = await sut.LoginAsync("maria@safewoman.pe", "pass");

        resultado.Should().NotBeNull();
        resultado!.Email.Should().Be("maria@safewoman.pe");
        admin.UltimoAcceso.Should().NotBeNull("el login exitoso debe registrar timestamp");
        _uow.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task LoginAsync_con_email_inexistente_debe_devolver_null_sin_llamar_hasher()
    {
        // Regla: no revelar si un email existe o no — devolver null en ambos casos
        // de fallo, sin distinguir por mensaje al cliente.
        _repo
            .Setup(r => r.FindAsync(It.IsAny<Expression<Func<Administrador, bool>>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Array.Empty<Administrador>());

        var sut = CrearSut();

        var resultado = await sut.LoginAsync("noexiste@safewoman.pe", "pass");

        resultado.Should().BeNull();
        _hasher.Verify(h => h.Verify(It.IsAny<string>(), It.IsAny<string>()), Times.Never);
    }

    [Fact]
    public async Task LoginAsync_con_password_incorrecta_debe_devolver_null()
    {
        var admin = Administrador.Crear("María", "maria@safewoman.pe", "hash");
        _repo
            .Setup(r => r.FindAsync(It.IsAny<Expression<Func<Administrador, bool>>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new[] { admin });
        _hasher.Setup(h => h.Verify(It.IsAny<string>(), It.IsAny<string>())).Returns(false);

        var sut = CrearSut();

        var resultado = await sut.LoginAsync("maria@safewoman.pe", "mala");

        resultado.Should().BeNull();
        _uow.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task ExisteAdminAsync_debe_devolver_true_cuando_hay_al_menos_un_admin()
    {
        var admin = Administrador.Crear("A", "a@b.com", "hash");
        _repo.Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>())).ReturnsAsync(new[] { admin });

        var sut = CrearSut();

        (await sut.ExisteAdminAsync()).Should().BeTrue();
    }

    [Fact]
    public async Task ExisteAdminAsync_debe_devolver_false_cuando_no_hay_admins()
    {
        _repo.Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>())).ReturnsAsync(Array.Empty<Administrador>());

        var sut = CrearSut();

        (await sut.ExisteAdminAsync()).Should().BeFalse();
    }

    [Fact]
    public async Task CrearAdminAsync_debe_hashear_password_y_persistir_admin_activo()
    {
        _hasher.Setup(h => h.Hash("clave-segura")).Returns("hash-bcrypt");

        var sut = CrearSut();

        await sut.CrearAdminAsync("María", "maria@safewoman.pe", "clave-segura");

        _hasher.Verify(h => h.Hash("clave-segura"), Times.Once);
        _repo.Verify(r => r.AddAsync(
            It.Is<Administrador>(a =>
                a.Nombre == "María" &&
                a.Email == "maria@safewoman.pe" &&
                a.PasswordHash == "hash-bcrypt" &&
                a.Activo),
            It.IsAny<CancellationToken>()), Times.Once);
        _uow.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }
}
