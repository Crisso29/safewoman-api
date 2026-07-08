using System.Linq.Expressions;
using FluentAssertions;
using Moq;
using SafeWoman.Application.DTOs.DenunciaAnonima;
using SafeWoman.Application.Interfaces;
using SafeWoman.Application.Services;
using SafeWoman.Domain.Entities;
using SafeWoman.Domain.Enums;
using SafeWoman.Domain.Exceptions;
using SafeWoman.Domain.Interfaces;

namespace SafeWoman.UnitTests.Services;

/// <summary>
/// DenunciaAnonimaService recibe denuncias sin identidad — un flujo crítico para
/// testigos que no quieren exponerse. Reglas:
/// - Reutilizar huella si ya existe (para tracking sin revelar identidad).
/// - Bloquear si la huella fue marcada como spam.
/// - Transaccionalidad: rollback + limpieza de archivos si algo falla.
/// </summary>
public class DenunciaAnonimaServiceTests
{
    private readonly Mock<IRepository<DenunciaAnonima>>   _denunciaRepo   = new();
    private readonly Mock<IRepository<DenunciadoAnonima>> _denunciadoRepo = new();
    private readonly Mock<IRepository<EvidenciaAnonima>>  _evidenciaRepo  = new();
    private readonly Mock<IRepository<HuellaDispositivo>> _huellaRepo     = new();
    private readonly Mock<IUnitOfWork>                    _uow            = new();
    private readonly Mock<IFileStorage>                   _storage        = new();
    private readonly Mock<ITransaction>                   _tx             = new();

    private DenunciaAnonimaService CrearSut()
    {
        _uow.Setup(u => u.BeginTransactionAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(_tx.Object);
        return new DenunciaAnonimaService(
            _denunciaRepo.Object, _denunciadoRepo.Object, _evidenciaRepo.Object,
            _huellaRepo.Object, _uow.Object, _storage.Object);
    }

    private static DenunciaAnonimaRequest ReqDeEjemplo(string fingerprint = "device-abc-123") =>
        new(DeviceFingerprint: fingerprint,
            NombreAliasDenunciado: null, RelacionDenunciado: null,
            Departamento: "Ayacucho", Provincia: "Huamanga", Distrito: "Ayacucho",
            ReferenciaUbicacion: "Plaza Mayor",
            Latitud: -13.16m, Longitud: -74.22m,
            FechaHecho: null, HoraHecho: null,
            Descripcion: "Testigo presenció agresión.");

    [Fact]
    public async Task EnviarAsync_con_huella_nueva_debe_crearla_y_persistir_denuncia()
    {
        _huellaRepo
            .Setup(r => r.FindAsync(It.IsAny<Expression<Func<HuellaDispositivo, bool>>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Array.Empty<HuellaDispositivo>());

        var sut = CrearSut();

        await sut.EnviarAsync(ReqDeEjemplo());

        _huellaRepo.Verify(r => r.AddAsync(
            It.Is<HuellaDispositivo>(h => h.DeviceFingerprint == "device-abc-123"),
            It.IsAny<CancellationToken>()), Times.Once);
        _denunciaRepo.Verify(r => r.AddAsync(It.IsAny<DenunciaAnonima>(), It.IsAny<CancellationToken>()), Times.Once);
        _tx.Verify(t => t.CommitAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task EnviarAsync_con_huella_existente_debe_reutilizarla_y_registrar_uso()
    {
        var existente = HuellaDispositivo.Crear("device-abc-123");
        _huellaRepo
            .Setup(r => r.FindAsync(It.IsAny<Expression<Func<HuellaDispositivo, bool>>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new[] { existente });

        var sut = CrearSut();

        await sut.EnviarAsync(ReqDeEjemplo());

        _huellaRepo.Verify(r => r.AddAsync(It.IsAny<HuellaDispositivo>(), It.IsAny<CancellationToken>()),
            Times.Never, "no debe crearse nueva huella si ya existe");
        _huellaRepo.Verify(r => r.Update(existente), Times.Once,
            "debe actualizarse la FechaUltimoUso");
    }

    [Fact]
    public async Task EnviarAsync_con_huella_bloqueada_debe_lanzar_DomainException_y_no_persistir()
    {
        var huella = HuellaDispositivo.Crear("device-spam");
        huella.Bloquear();
        _huellaRepo
            .Setup(r => r.FindAsync(It.IsAny<Expression<Func<HuellaDispositivo, bool>>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new[] { huella });

        var sut = CrearSut();

        var act = () => sut.EnviarAsync(ReqDeEjemplo("device-spam"));

        await act.Should().ThrowAsync<DomainException>()
            .WithMessage("*bloqueado*");
        _denunciaRepo.Verify(r => r.AddAsync(It.IsAny<DenunciaAnonima>(), It.IsAny<CancellationToken>()),
            Times.Never);
        _tx.Verify(t => t.RollbackAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task EnviarAsync_con_evidencias_debe_persistir_archivos_y_registros()
    {
        _huellaRepo
            .Setup(r => r.FindAsync(It.IsAny<Expression<Func<HuellaDispositivo, bool>>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Array.Empty<HuellaDispositivo>());
        _storage
            .Setup(s => s.SaveAsync(It.IsAny<Stream>(), It.IsAny<string>(), "evidencias-anonimas", It.IsAny<CancellationToken>()))
            .ReturnsAsync((Stream _, string nombre, string _, CancellationToken _) => $"/anon/{nombre}");

        var sut = CrearSut();
        using var s1 = new MemoryStream();
        var evidencias = new[] { (s1 as Stream, "foto-agresor.jpg", TipoArchivo.Imagen, 500L) };

        await sut.EnviarAsync(ReqDeEjemplo(), evidencias);

        _storage.Verify(s => s.SaveAsync(It.IsAny<Stream>(), "foto-agresor.jpg", "evidencias-anonimas", It.IsAny<CancellationToken>()), Times.Once);
        _evidenciaRepo.Verify(r => r.AddAsync(
            It.Is<EvidenciaAnonima>(e => e.NombreArchivo == "foto-agresor.jpg"),
            It.IsAny<CancellationToken>()), Times.Once);
    }
}
