using System.Linq.Expressions;
using FluentAssertions;
using Moq;
using SafeWoman.Application.DTOs.Denuncia;
using SafeWoman.Application.Interfaces;
using SafeWoman.Application.Services;
using SafeWoman.Domain.Entities;
using SafeWoman.Domain.Enums;
using SafeWoman.Domain.Exceptions;
using SafeWoman.Domain.Interfaces;

namespace SafeWoman.UnitTests.Services;

/// <summary>
/// DenunciaService coordina la creación de denuncias formales con archivos
/// (foto DNI + evidencias). Reglas críticas:
/// - Atomicidad: si algo falla, archivos y registros se limpian.
/// - Autorización: una víctima no puede consultar denuncias de otra.
/// - Orden: la lista devuelve las más recientes primero.
/// </summary>
public class DenunciaServiceTests
{
    private readonly Mock<IRepository<Denuncia>>   _denunciaRepo   = new();
    private readonly Mock<IRepository<Denunciado>> _denunciadoRepo = new();
    private readonly Mock<IRepository<Evidencia>>  _evidenciaRepo  = new();
    private readonly Mock<IUnitOfWork>             _uow            = new();
    private readonly Mock<IFileStorage>            _storage        = new();
    private readonly Mock<ITransaction>            _tx             = new();

    private DenunciaService CrearSut()
    {
        _uow.Setup(u => u.BeginTransactionAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(_tx.Object);
        return new DenunciaService(
            _denunciaRepo.Object, _denunciadoRepo.Object, _evidenciaRepo.Object,
            _uow.Object, _storage.Object);
    }

    private static DenunciaFormalRequest ReqDeEjemplo(string? aliasDenunciado = null,
                                                      RelacionVictima? relacion = null) =>
        new(NombreAliasDenunciado: aliasDenunciado,
            RelacionDenunciado: relacion,
            Departamento: "Ayacucho",
            Provincia: "Huamanga",
            Distrito: "Ayacucho",
            ReferenciaUbicacion: "Av. Ramón Castilla 234",
            Latitud: -13.16m,
            Longitud: -74.22m,
            FechaHecho: new DateOnly(2026, 7, 1),
            HoraHecho: new TimeOnly(20, 0),
            Descripcion: "Agresión física por parte de la pareja.");

    // ── CREACIÓN ──────────────────────────────────────────────────────────────

    [Fact]
    public async Task CrearFormalAsync_debe_persistir_denuncia_y_hacer_commit()
    {
        _storage.Setup(s => s.SaveAsync(It.IsAny<Stream>(), "dni.jpg", "dni", It.IsAny<CancellationToken>()))
                .ReturnsAsync("/uploads/dni/abc.jpg");

        var sut = CrearSut();
        using var fotoStream = new MemoryStream(new byte[] { 1, 2, 3 });

        await sut.CrearFormalAsync(idVictima: 42, ReqDeEjemplo(), fotoStream, "dni.jpg");

        _denunciaRepo.Verify(r => r.AddAsync(
            It.Is<Denuncia>(d =>
                d.IdVictima == 42 &&
                d.Estado == EstadoDenuncia.Pendiente &&
                d.Tipo == TipoDenuncia.Formal &&
                d.FotoDniRuta == "/uploads/dni/abc.jpg"),
            It.IsAny<CancellationToken>()), Times.Once);
        _tx.Verify(t => t.CommitAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task CrearFormalAsync_con_denunciado_debe_crear_registro_de_denunciado()
    {
        _storage.Setup(s => s.SaveAsync(It.IsAny<Stream>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync("/uploads/dni.jpg");

        var sut = CrearSut();
        using var foto = new MemoryStream();

        await sut.CrearFormalAsync(42,
            ReqDeEjemplo(aliasDenunciado: "Juan Pérez", relacion: RelacionVictima.Pareja),
            foto, "dni.jpg");

        _denunciadoRepo.Verify(r => r.AddAsync(
            It.Is<Denunciado>(d => d.NombreAlias == "Juan Pérez" && d.RelacionVictima == RelacionVictima.Pareja),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task CrearFormalAsync_sin_datos_de_denunciado_no_debe_crear_registro_denunciado()
    {
        _storage.Setup(s => s.SaveAsync(It.IsAny<Stream>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync("/uploads/dni.jpg");

        var sut = CrearSut();
        using var foto = new MemoryStream();

        await sut.CrearFormalAsync(42, ReqDeEjemplo(), foto, "dni.jpg");

        _denunciadoRepo.Verify(r => r.AddAsync(It.IsAny<Denunciado>(), It.IsAny<CancellationToken>()),
            Times.Never, "sin datos de denunciado no debe crearse registro");
    }

    [Fact]
    public async Task CrearFormalAsync_con_evidencias_debe_persistirlas_todas()
    {
        _storage
            .Setup(s => s.SaveAsync(It.IsAny<Stream>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Stream _, string nombre, string _, CancellationToken _) => $"/uploads/{nombre}");

        var sut = CrearSut();
        using var foto  = new MemoryStream();
        using var ev1   = new MemoryStream();
        using var ev2   = new MemoryStream();
        var evidencias = new[]
        {
            (ev1 as Stream, "foto1.jpg", TipoArchivo.Imagen, 100L),
            (ev2 as Stream, "video1.mp4", TipoArchivo.Video, 200L)
        };

        await sut.CrearFormalAsync(42, ReqDeEjemplo(), foto, "dni.jpg", evidencias);

        _evidenciaRepo.Verify(r => r.AddAsync(
            It.Is<Evidencia>(e => e.NombreArchivo == "foto1.jpg" && e.TipoArchivo == TipoArchivo.Imagen),
            It.IsAny<CancellationToken>()), Times.Once);
        _evidenciaRepo.Verify(r => r.AddAsync(
            It.Is<Evidencia>(e => e.NombreArchivo == "video1.mp4" && e.TipoArchivo == TipoArchivo.Video),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task CrearFormalAsync_si_falla_persistencia_debe_hacer_rollback_y_borrar_archivos_subidos()
    {
        _storage.Setup(s => s.SaveAsync(It.IsAny<Stream>(), "dni.jpg", "dni", It.IsAny<CancellationToken>()))
                .ReturnsAsync("/uploads/dni/xyz.jpg");
        _uow.Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("BD caída"));

        var sut = CrearSut();
        using var foto = new MemoryStream();

        var act = () => sut.CrearFormalAsync(42, ReqDeEjemplo(), foto, "dni.jpg");

        await act.Should().ThrowAsync<InvalidOperationException>();
        _tx.Verify(t => t.RollbackAsync(It.IsAny<CancellationToken>()), Times.Once);
        _storage.Verify(s => s.Delete("/uploads/dni/xyz.jpg"), Times.Once,
            "los archivos huérfanos deben limpiarse tras un rollback");
    }

    // ── LISTADO / OBTENCIÓN ──────────────────────────────────────────────────

    [Fact]
    public async Task ListarPorVictimaAsync_debe_ordenar_las_denuncias_por_fecha_descendente()
    {
        var d1 = Denuncia.CrearFormal(1, "/dni.jpg", null, null, null, null, null, null, null, null, "Vieja");
        Thread.Sleep(5);
        var d2 = Denuncia.CrearFormal(1, "/dni.jpg", null, null, null, null, null, null, null, null, "Nueva");
        _denunciaRepo
            .Setup(r => r.FindAsync(It.IsAny<Expression<Func<Denuncia, bool>>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new[] { d1, d2 });

        var sut = CrearSut();

        var lista = await sut.ListarPorVictimaAsync(1);

        lista.Should().HaveCount(2);
        lista[0].Descripcion.Should().Be("Nueva", "la más reciente debe salir primero");
    }

    [Fact]
    public async Task ObtenerAsync_debe_devolver_denuncia_propia_de_la_victima()
    {
        var denuncia = Denuncia.CrearFormal(idVictima: 42, "/dni.jpg",
            "Ayacucho", "Huamanga", "Ayacucho", null, null, null, null, null, "Descripcion.");
        _denunciaRepo.Setup(r => r.GetByIdAsync(10, It.IsAny<CancellationToken>())).ReturnsAsync(denuncia);

        var sut = CrearSut();

        var dto = await sut.ObtenerAsync(idVictima: 42, idDenuncia: 10);

        dto.Departamento.Should().Be("Ayacucho");
        dto.Descripcion.Should().Be("Descripcion.");
    }

    [Fact]
    public async Task ObtenerAsync_no_debe_permitir_consultar_denuncias_de_otra_victima()
    {
        var denuncia = Denuncia.CrearFormal(idVictima: 999, "/dni.jpg",
            null, null, null, null, null, null, null, null, "Denuncia ajena.");
        _denunciaRepo.Setup(r => r.GetByIdAsync(10, It.IsAny<CancellationToken>())).ReturnsAsync(denuncia);

        var sut = CrearSut();

        var act = () => sut.ObtenerAsync(idVictima: 1, idDenuncia: 10);

        await act.Should().ThrowAsync<DomainException>().WithMessage("*No autorizado*");
    }

    [Fact]
    public async Task ObtenerAsync_con_denuncia_inexistente_debe_lanzar_DomainException()
    {
        _denunciaRepo.Setup(r => r.GetByIdAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()))
                     .ReturnsAsync((Denuncia?)null);

        var sut = CrearSut();

        var act = () => sut.ObtenerAsync(1, 999);

        await act.Should().ThrowAsync<DomainException>().WithMessage("*no encontrada*");
    }
}
