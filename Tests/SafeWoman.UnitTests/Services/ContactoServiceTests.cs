using System.Linq.Expressions;
using FluentAssertions;
using Moq;
using SafeWoman.Application.DTOs.Victima;
using SafeWoman.Application.Services;
using SafeWoman.Domain.Entities;
using SafeWoman.Domain.Exceptions;
using SafeWoman.Domain.Interfaces;

namespace SafeWoman.UnitTests.Services;

/// <summary>
/// ContactoService gestiona los contactos de emergencia — los teléfonos que
/// reciben el SMS cuando la víctima activa el SOS. Reglas críticas:
/// máximo 5 contactos por víctima y ninguno puede acceder a los de otra.
/// </summary>
public class ContactoServiceTests
{
    private readonly Mock<IRepository<ContactoEmergencia>> _repo = new();
    private readonly Mock<IUnitOfWork> _uow = new();

    private ContactoService CrearSut() => new(_repo.Object, _uow.Object);

    // ── LISTAR ────────────────────────────────────────────────────────────────

    [Fact]
    public async Task ListarAsync_debe_devolver_solo_contactos_de_la_victima_solicitante()
    {
        var contactos = new[]
        {
            ContactoEmergencia.Crear(idVictima: 1, "María", "+51111111111"),
            ContactoEmergencia.Crear(idVictima: 1, "José",  "+51222222222")
        };
        _repo.Setup(r => r.FindAsync(It.IsAny<Expression<Func<ContactoEmergencia, bool>>>(), It.IsAny<CancellationToken>()))
             .ReturnsAsync(contactos);

        var sut = CrearSut();

        var resultado = await sut.ListarAsync(idVictima: 1);

        resultado.Should().HaveCount(2);
        resultado.Select(c => c.Nombre).Should().Contain(["María", "José"]);
    }

    [Fact]
    public async Task ListarAsync_para_victima_sin_contactos_debe_devolver_lista_vacia()
    {
        _repo.Setup(r => r.FindAsync(It.IsAny<Expression<Func<ContactoEmergencia, bool>>>(), It.IsAny<CancellationToken>()))
             .ReturnsAsync(Array.Empty<ContactoEmergencia>());

        var sut = CrearSut();

        var resultado = await sut.ListarAsync(idVictima: 1);

        resultado.Should().BeEmpty();
    }

    // ── CREAR ─────────────────────────────────────────────────────────────────

    [Fact]
    public async Task CrearAsync_debe_persistir_contacto_y_devolver_DTO()
    {
        _repo.Setup(r => r.FindAsync(It.IsAny<Expression<Func<ContactoEmergencia, bool>>>(), It.IsAny<CancellationToken>()))
             .ReturnsAsync(Array.Empty<ContactoEmergencia>());

        var sut = CrearSut();
        var req = new CrearContactoRequest("María Hermana", "+51999888777");

        var resultado = await sut.CrearAsync(idVictima: 42, req);

        resultado.Nombre.Should().Be("María Hermana");
        resultado.Telefono.Should().Be("+51999888777");
        _repo.Verify(r => r.AddAsync(
            It.Is<ContactoEmergencia>(c =>
                c.IdVictima == 42 && c.Nombre == "María Hermana" && c.Telefono == "+51999888777"),
            It.IsAny<CancellationToken>()), Times.Once);
        _uow.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task CrearAsync_al_intentar_agregar_un_6to_contacto_debe_lanzar_DomainException()
    {
        var cincoExistentes = Enumerable.Range(1, 5)
            .Select(i => ContactoEmergencia.Crear(1, $"Contacto{i}", $"+5199999{i:D4}"))
            .ToArray();
        _repo.Setup(r => r.FindAsync(It.IsAny<Expression<Func<ContactoEmergencia, bool>>>(), It.IsAny<CancellationToken>()))
             .ReturnsAsync(cincoExistentes);

        var sut = CrearSut();
        var req = new CrearContactoRequest("Sexto", "+51999999999");

        var act = () => sut.CrearAsync(idVictima: 1, req);

        await act.Should().ThrowAsync<DomainException>()
            .WithMessage("*Máximo 5 contactos*");
        _repo.Verify(r => r.AddAsync(It.IsAny<ContactoEmergencia>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    // ── ACTUALIZAR ────────────────────────────────────────────────────────────

    [Fact]
    public async Task ActualizarAsync_debe_modificar_contacto_de_la_misma_victima()
    {
        var contacto = ContactoEmergencia.Crear(idVictima: 1, "María", "+51999888777");
        _repo.Setup(r => r.GetByIdAsync(10, It.IsAny<CancellationToken>()))
             .ReturnsAsync(contacto);

        var sut = CrearSut();
        var req = new ActualizarContactoRequest("María Actualizada", "+51988777666");

        await sut.ActualizarAsync(idVictima: 1, idContacto: 10, req);

        contacto.Nombre.Should().Be("María Actualizada");
        contacto.Telefono.Should().Be("+51988777666");
        _uow.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task ActualizarAsync_no_debe_permitir_modificar_contactos_de_otra_victima()
    {
        // Regla crítica de seguridad: si la víctima 1 intenta modificar un contacto
        // de la víctima 999, debe recibir "No autorizado" para no exponer datos ajenos.
        var contacto = ContactoEmergencia.Crear(idVictima: 999, "OtroContacto", "+51000000000");
        _repo.Setup(r => r.GetByIdAsync(10, It.IsAny<CancellationToken>()))
             .ReturnsAsync(contacto);

        var sut = CrearSut();
        var req = new ActualizarContactoRequest("Hackeado", "+51000000000");

        var act = () => sut.ActualizarAsync(idVictima: 1, idContacto: 10, req);

        await act.Should().ThrowAsync<DomainException>()
            .WithMessage("*No autorizado*");
        contacto.Nombre.Should().Be("OtroContacto", "el contacto no debe haberse tocado");
    }

    [Fact]
    public async Task ActualizarAsync_contacto_inexistente_debe_lanzar_DomainException()
    {
        _repo.Setup(r => r.GetByIdAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()))
             .ReturnsAsync((ContactoEmergencia?)null);

        var sut = CrearSut();
        var act = () => sut.ActualizarAsync(1, 999, new ActualizarContactoRequest("X", "+51000000000"));

        await act.Should().ThrowAsync<DomainException>()
            .WithMessage("*no encontrado*");
    }

    // ── ELIMINAR ──────────────────────────────────────────────────────────────

    [Fact]
    public async Task EliminarAsync_debe_borrar_contacto_de_la_misma_victima()
    {
        var contacto = ContactoEmergencia.Crear(idVictima: 1, "María", "+51999888777");
        _repo.Setup(r => r.GetByIdAsync(10, It.IsAny<CancellationToken>()))
             .ReturnsAsync(contacto);

        var sut = CrearSut();

        await sut.EliminarAsync(idVictima: 1, idContacto: 10);

        _repo.Verify(r => r.Remove(contacto), Times.Once);
        _uow.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task EliminarAsync_no_debe_permitir_borrar_contactos_de_otra_victima()
    {
        var contacto = ContactoEmergencia.Crear(idVictima: 999, "Ajeno", "+51000000000");
        _repo.Setup(r => r.GetByIdAsync(10, It.IsAny<CancellationToken>()))
             .ReturnsAsync(contacto);

        var sut = CrearSut();

        var act = () => sut.EliminarAsync(idVictima: 1, idContacto: 10);

        await act.Should().ThrowAsync<DomainException>()
            .WithMessage("*No autorizado*");
        _repo.Verify(r => r.Remove(It.IsAny<ContactoEmergencia>()), Times.Never);
    }
}
