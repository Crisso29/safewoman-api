using FluentAssertions;
using SafeWoman.Domain.Entities;
using SafeWoman.Domain.Enums;

namespace SafeWoman.UnitTests.Domain;

public class DenunciadoTests
{
    [Fact]
    public void Crear_debe_asociar_denunciado_a_la_denuncia_correcta()
    {
        var d = Denunciado.Crear(idDenuncia: 99, "Juan Pérez", RelacionVictima.Pareja);

        d.IdDenuncia.Should().Be(99);
        d.NombreAlias.Should().Be("Juan Pérez");
        d.RelacionVictima.Should().Be(RelacionVictima.Pareja);
    }

    [Fact]
    public void Crear_debe_recortar_espacios_del_nombre()
    {
        var d = Denunciado.Crear(1, "  Juan Pérez  ", RelacionVictima.Familiar);

        d.NombreAlias.Should().Be("Juan Pérez");
    }

    [Fact]
    public void Crear_debe_aceptar_denunciado_sin_nombre_ni_relacion()
    {
        // Escenario real: la víctima solo describe el hecho sin identificar al agresor.
        var d = Denunciado.Crear(1, null, null);

        d.NombreAlias.Should().BeNull();
        d.RelacionVictima.Should().BeNull();
    }
}

public class DenunciadoAnonimaTests
{
    [Fact]
    public void Crear_debe_asociar_denunciado_anonimo_a_la_denuncia()
    {
        var d = DenunciadoAnonima.Crear(idDenunciaAnonima: 42, "Sujeto sin identificar", RelacionVictima.Desconocido);

        d.IdDenunciaAnonima.Should().Be(42);
        d.NombreAlias.Should().Be("Sujeto sin identificar");
        d.Relacion.Should().Be(RelacionVictima.Desconocido);
    }

    [Fact]
    public void Crear_debe_recortar_espacios_y_permitir_nulls()
    {
        var d = DenunciadoAnonima.Crear(1, "  Alguien  ", null);

        d.NombreAlias.Should().Be("Alguien");
        d.Relacion.Should().BeNull();
    }
}
