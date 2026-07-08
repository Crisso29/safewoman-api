using FluentAssertions;
using SafeWoman.Domain.Entities;
using SafeWoman.Domain.Enums;

namespace SafeWoman.UnitTests.Domain;

public class EvidenciaTests
{
    [Fact]
    public void Crear_debe_asociar_evidencia_con_su_denuncia_y_registrar_metadatos()
    {
        var evidencia = Evidencia.Crear(
            idDenuncia: 100,
            nombreArchivo: "foto-agresion.jpg",
            rutaArchivo: "/uploads/2026/07/foto-agresion.jpg",
            tipo: TipoArchivo.Imagen,
            tamanioBytes: 2_048_576);

        evidencia.IdDenuncia.Should().Be(100);
        evidencia.NombreArchivo.Should().Be("foto-agresion.jpg");
        evidencia.RutaArchivo.Should().Be("/uploads/2026/07/foto-agresion.jpg");
        evidencia.TipoArchivo.Should().Be(TipoArchivo.Imagen);
        evidencia.TamanioBytes.Should().Be(2_048_576);
        evidencia.FechaSubida.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));
    }

    [Fact]
    public void Crear_debe_soportar_tamanio_desconocido()
    {
        // MediaPicker de Android a veces no reporta el tamaño hasta subir el archivo.
        var evidencia = Evidencia.Crear(1, "video.mp4", "/uploads/video.mp4", TipoArchivo.Video, tamanioBytes: null);

        evidencia.TamanioBytes.Should().BeNull();
    }

    [Theory]
    [InlineData(TipoArchivo.Imagen)]
    [InlineData(TipoArchivo.Video)]
    [InlineData(TipoArchivo.Documento)]
    [InlineData(TipoArchivo.Pdf)]
    public void Crear_debe_aceptar_todos_los_tipos_de_archivo(TipoArchivo tipo)
    {
        var evidencia = Evidencia.Crear(1, $"archivo.{tipo}", $"/uploads/archivo", tipo, 1000);

        evidencia.TipoArchivo.Should().Be(tipo);
    }
}

public class EvidenciaAnonimaTests
{
    [Fact]
    public void Crear_debe_asociar_evidencia_anonima_con_denuncia_anonima()
    {
        var evidencia = EvidenciaAnonima.Crear(
            idDenunciaAnonima: 55,
            nombreArchivo: "video.mp4",
            rutaArchivo: "/uploads/anon/video.mp4",
            tipo: TipoArchivo.Video,
            tamanioBytes: 500_000);

        evidencia.IdDenunciaAnonima.Should().Be(55);
        evidencia.NombreArchivo.Should().Be("video.mp4");
        evidencia.TipoArchivo.Should().Be(TipoArchivo.Video);
        evidencia.FechaSubida.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));
    }
}
