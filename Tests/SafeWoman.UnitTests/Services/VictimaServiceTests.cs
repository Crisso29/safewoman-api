using System.Linq.Expressions;
using FluentAssertions;
using Moq;
using SafeWoman.Application.Services;
using SafeWoman.Domain.Entities;
using SafeWoman.Domain.Exceptions;
using SafeWoman.Domain.Interfaces;

namespace SafeWoman.UnitTests.Services;

public class VictimaServiceTests
{
    private readonly Mock<IRepository<Victima>>            _victimaRepo  = new();
    private readonly Mock<IRepository<ContactoEmergencia>> _contactoRepo = new();
    private readonly Mock<IUnitOfWork>                     _uow          = new();

    private VictimaService CrearSut() =>
        new(_victimaRepo.Object, _contactoRepo.Object, _uow.Object);

    [Fact]
    public async Task ObtenerPerfilAsync_debe_devolver_los_datos_de_la_victima_y_sus_contactos()
    {
        var victima = Victima.Crear("Ana Prueba", "12345678", "+51987654321", "hash");
        victima.Verificar();
        var contactos = new[]
        {
            ContactoEmergencia.Crear(1, "María", "+51111111111"),
            ContactoEmergencia.Crear(1, "José",  "+51222222222")
        };

        _victimaRepo.Setup(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>())).ReturnsAsync(victima);
        _contactoRepo
            .Setup(r => r.FindAsync(It.IsAny<Expression<Func<ContactoEmergencia, bool>>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(contactos);

        var sut = CrearSut();

        var perfil = await sut.ObtenerPerfilAsync(1);

        perfil.NombreCompleto.Should().Be("Ana Prueba");
        perfil.Dni.Should().Be("12345678");
        perfil.Telefono.Should().Be("+51987654321");
        perfil.Verificada.Should().BeTrue();
        perfil.Contactos.Should().HaveCount(2);
        perfil.Contactos.Select(c => c.Nombre).Should().Contain(["María", "José"]);
    }

    [Fact]
    public async Task ObtenerPerfilAsync_con_victima_inexistente_debe_lanzar_DomainException()
    {
        _victimaRepo.Setup(r => r.GetByIdAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()))
                    .ReturnsAsync((Victima?)null);

        var sut = CrearSut();

        var act = () => sut.ObtenerPerfilAsync(999);

        await act.Should().ThrowAsync<DomainException>().WithMessage("*no encontrada*");
    }

    [Fact]
    public async Task ObtenerPerfilAsync_para_victima_sin_contactos_debe_devolver_lista_vacia()
    {
        var victima = Victima.Crear("Ana", "12345678", "+51987654321", "hash");
        _victimaRepo.Setup(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>())).ReturnsAsync(victima);
        _contactoRepo
            .Setup(r => r.FindAsync(It.IsAny<Expression<Func<ContactoEmergencia, bool>>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Array.Empty<ContactoEmergencia>());

        var sut = CrearSut();

        var perfil = await sut.ObtenerPerfilAsync(1);

        perfil.Contactos.Should().BeEmpty();
    }
}
