using System.Linq.Expressions;
using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using SafeWoman.Application.DTOs.AlertaSos;
using SafeWoman.Application.Interfaces;
using SafeWoman.Application.Services;
using SafeWoman.Domain.Entities;
using SafeWoman.Domain.Enums;
using SafeWoman.Domain.Exceptions;
using SafeWoman.Domain.Interfaces;

namespace SafeWoman.UnitTests.Services;

/// <summary>
/// AlertaSosService es el corazón del feature SOS. Cualquier bug aquí es directamente
/// una vida en riesgo. Los tests verifican:
/// - Que se persista la alerta antes de intentar SMS (SMS falla ≠ alerta perdida).
/// - Que se envíe SMS a TODOS los contactos.
/// - Que un fallo de Twilio no bloquee el SignalR ni la respuesta al cliente.
/// - Que solo la víctima dueña pueda cancelar sus alertas.
/// </summary>
public class AlertaSosServiceTests
{
    private readonly Mock<IRepository<AlertaSos>>          _alertaRepo   = new();
    private readonly Mock<IRepository<Victima>>            _victimaRepo  = new();
    private readonly Mock<IRepository<ContactoEmergencia>> _contactoRepo = new();
    private readonly Mock<IUnitOfWork>                     _uow          = new();
    private readonly Mock<ISosSmsNotifier>                 _sms          = new();
    private readonly Mock<ISosNotifier>                    _hub          = new();
    private readonly Mock<IReverseGeocoder>                _geo          = new();

    private AlertaSosService CrearSut() => new(
        _alertaRepo.Object, _victimaRepo.Object, _contactoRepo.Object,
        _uow.Object, _sms.Object, _hub.Object, _geo.Object,
        NullLogger<AlertaSosService>.Instance);

    // ── ACTIVAR ───────────────────────────────────────────────────────────────

    [Fact]
    public async Task ActivarAsync_debe_persistir_alerta_notificar_SignalR_y_enviar_SMS_a_todos_los_contactos()
    {
        var victima = Victima.Crear("Ana Prueba", "12345678", "+51987654321", "hash");
        var contactos = new[]
        {
            ContactoEmergencia.Crear(1, "María", "+51111111111"),
            ContactoEmergencia.Crear(1, "José",  "+51222222222"),
            ContactoEmergencia.Crear(1, "Lucía", "+51333333333")
        };
        _victimaRepo.Setup(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>())).ReturnsAsync(victima);
        _contactoRepo
            .Setup(r => r.FindAsync(It.IsAny<Expression<Func<ContactoEmergencia, bool>>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(contactos);
        _geo.Setup(g => g.LookupAsync(It.IsAny<decimal>(), It.IsAny<decimal>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync("Av. Ramón Castilla 234, Ayacucho");

        var sut = CrearSut();
        var req = new ActivarSosRequest(-13.16m, -74.22m);

        var resultado = await sut.ActivarAsync(idVictima: 1, req);

        // Alerta persistida
        _alertaRepo.Verify(r => r.AddAsync(
            It.Is<AlertaSos>(a => a.IdVictima == 1 && a.Estado == EstadoAlerta.Activa),
            It.IsAny<CancellationToken>()), Times.Once);
        _uow.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);

        // SMS a cada uno de los 3 contactos
        _sms.Verify(s => s.SendSosAlertAsync("+51111111111", "Ana Prueba", -13.16m, -74.22m,
            It.IsAny<DateTime>(), "Av. Ramón Castilla 234, Ayacucho", It.IsAny<CancellationToken>()), Times.Once);
        _sms.Verify(s => s.SendSosAlertAsync("+51222222222", "Ana Prueba", -13.16m, -74.22m,
            It.IsAny<DateTime>(), "Av. Ramón Castilla 234, Ayacucho", It.IsAny<CancellationToken>()), Times.Once);
        _sms.Verify(s => s.SendSosAlertAsync("+51333333333", "Ana Prueba", -13.16m, -74.22m,
            It.IsAny<DateTime>(), "Av. Ramón Castilla 234, Ayacucho", It.IsAny<CancellationToken>()), Times.Once);

        // Notificación SignalR al panel Admin
        _hub.Verify(h => h.NotifyNewAlertAsync(
            It.IsAny<int>(), "Ana Prueba", "+51987654321", -13.16m, -74.22m,
            It.IsAny<DateTime>(), It.IsAny<CancellationToken>()), Times.Once);

        resultado.Estado.Should().Be(EstadoAlerta.Activa);
        resultado.NombreVictima.Should().Be("Ana Prueba");
    }

    [Fact]
    public async Task ActivarAsync_sin_contactos_de_emergencia_debe_lanzar_DomainException()
    {
        var victima = Victima.Crear("Ana", "12345678", "+51987654321", "hash");
        _victimaRepo.Setup(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>())).ReturnsAsync(victima);
        _contactoRepo
            .Setup(r => r.FindAsync(It.IsAny<Expression<Func<ContactoEmergencia, bool>>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Array.Empty<ContactoEmergencia>());

        var sut = CrearSut();

        var act = () => sut.ActivarAsync(1, new ActivarSosRequest(-13.16m, -74.22m));

        await act.Should().ThrowAsync<DomainException>()
            .WithMessage("*Necesita al menos un contacto de emergencia*");
        _alertaRepo.Verify(r => r.AddAsync(It.IsAny<AlertaSos>(), It.IsAny<CancellationToken>()),
            Times.Never, "no debe registrar alerta si no hay contactos");
    }

    [Fact]
    public async Task ActivarAsync_con_victima_inexistente_debe_lanzar_DomainException()
    {
        _victimaRepo.Setup(r => r.GetByIdAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()))
                    .ReturnsAsync((Victima?)null);

        var sut = CrearSut();
        var act = () => sut.ActivarAsync(999, new ActivarSosRequest(-13.16m, -74.22m));

        await act.Should().ThrowAsync<DomainException>().WithMessage("*no encontrada*");
    }

    [Fact]
    public async Task ActivarAsync_si_Twilio_falla_debe_igual_persistir_la_alerta()
    {
        // Escenario: Twilio caído — no queremos perder la alerta.
        var victima = Victima.Crear("Ana", "12345678", "+51987654321", "hash");
        var contactos = new[] { ContactoEmergencia.Crear(1, "María", "+51111111111") };
        _victimaRepo.Setup(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>())).ReturnsAsync(victima);
        _contactoRepo
            .Setup(r => r.FindAsync(It.IsAny<Expression<Func<ContactoEmergencia, bool>>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(contactos);
        _sms.Setup(s => s.SendSosAlertAsync(It.IsAny<string>(), It.IsAny<string>(),
            It.IsAny<decimal>(), It.IsAny<decimal>(), It.IsAny<DateTime>(), It.IsAny<string?>(),
            It.IsAny<CancellationToken>())).ThrowsAsync(new Exception("Twilio caído"));

        var sut = CrearSut();

        var resultado = await sut.ActivarAsync(1, new ActivarSosRequest(-13.16m, -74.22m));

        resultado.Should().NotBeNull(
            "la alerta debe registrarse aunque Twilio falle — la vida de la víctima no depende del SMS");
        _alertaRepo.Verify(r => r.AddAsync(It.IsAny<AlertaSos>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task ActivarAsync_si_reverse_geocoder_falla_debe_seguir_con_SMS_sin_direccion()
    {
        // Nominatim puede fallar por rate limit. El SMS debe salir igual con solo coordenadas.
        var victima = Victima.Crear("Ana", "12345678", "+51987654321", "hash");
        var contactos = new[] { ContactoEmergencia.Crear(1, "María", "+51111111111") };
        _victimaRepo.Setup(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>())).ReturnsAsync(victima);
        _contactoRepo
            .Setup(r => r.FindAsync(It.IsAny<Expression<Func<ContactoEmergencia, bool>>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(contactos);
        _geo.Setup(g => g.LookupAsync(It.IsAny<decimal>(), It.IsAny<decimal>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((string?)null);

        var sut = CrearSut();

        await sut.ActivarAsync(1, new ActivarSosRequest(-13.16m, -74.22m));

        _sms.Verify(s => s.SendSosAlertAsync(
            "+51111111111", "Ana", -13.16m, -74.22m,
            It.IsAny<DateTime>(), null, It.IsAny<CancellationToken>()), Times.Once);
    }

    // ── CANCELAR ──────────────────────────────────────────────────────────────

    [Fact]
    public async Task CancelarAsync_debe_cambiar_alerta_a_Cancelada_y_notificar_a_los_contactos()
    {
        var alerta  = AlertaSos.Activar(idVictima: 1, latitud: -13.16m, longitud: -74.22m);
        var victima = Victima.Crear("Ana", "12345678", "+51987654321", "hash");
        var contactos = new[] { ContactoEmergencia.Crear(1, "María", "+51111111111") };

        _alertaRepo.Setup(r => r.GetByIdAsync(10, It.IsAny<CancellationToken>())).ReturnsAsync(alerta);
        _victimaRepo.Setup(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>())).ReturnsAsync(victima);
        _contactoRepo
            .Setup(r => r.FindAsync(It.IsAny<Expression<Func<ContactoEmergencia, bool>>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(contactos);

        var sut = CrearSut();

        await sut.CancelarAsync(idVictima: 1, idAlerta: 10);

        alerta.Estado.Should().Be(EstadoAlerta.Cancelada);
        _sms.Verify(s => s.SendCancelacionSosAsync("+51111111111", "Ana", It.IsAny<CancellationToken>()), Times.Once);
        _hub.Verify(h => h.NotifyAlertCancelledAsync(10, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task CancelarAsync_no_debe_permitir_cancelar_alertas_de_otra_victima()
    {
        var alerta = AlertaSos.Activar(idVictima: 999, latitud: -13.16m, longitud: -74.22m);
        _alertaRepo.Setup(r => r.GetByIdAsync(10, It.IsAny<CancellationToken>())).ReturnsAsync(alerta);

        var sut = CrearSut();

        var act = () => sut.CancelarAsync(idVictima: 1, idAlerta: 10);

        await act.Should().ThrowAsync<DomainException>().WithMessage("*No autorizado*");
        alerta.Estado.Should().Be(EstadoAlerta.Activa, "no debe haberse modificado");
    }

    [Fact]
    public async Task CancelarAsync_de_una_alerta_inexistente_debe_lanzar_DomainException()
    {
        _alertaRepo.Setup(r => r.GetByIdAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()))
                   .ReturnsAsync((AlertaSos?)null);

        var sut = CrearSut();
        var act = () => sut.CancelarAsync(1, 999);

        await act.Should().ThrowAsync<DomainException>().WithMessage("*no encontrada*");
    }

    // ── LISTAR ────────────────────────────────────────────────────────────────

    [Fact]
    public async Task ListarPorVictimaAsync_debe_devolver_alertas_ordenadas_por_fecha_descendente()
    {
        var victima = Victima.Crear("Ana", "12345678", "+51987654321", "hash");
        _victimaRepo.Setup(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>())).ReturnsAsync(victima);

        var a1 = AlertaSos.Activar(1, -13.16m, -74.22m);
        Thread.Sleep(5);
        var a2 = AlertaSos.Activar(1, -13.17m, -74.23m);
        _alertaRepo
            .Setup(r => r.FindAsync(It.IsAny<Expression<Func<AlertaSos, bool>>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new[] { a1, a2 });

        var sut = CrearSut();

        var lista = await sut.ListarPorVictimaAsync(1);

        lista.Should().HaveCount(2);
        lista[0].TimestampActivacion.Should().BeAfter(lista[1].TimestampActivacion,
            "la más reciente debe salir primero");
    }
}
