using FluentAssertions;
using SafeWoman.Domain.Entities;

namespace SafeWoman.UnitTests.Domain;

/// <summary>
/// Verifica las invariantes de la entidad Victima — el corazón del dominio.
/// Las reglas de negocio codificadas aquí NO deben romperse por accidente.
/// </summary>
public class VictimaTests
{
    [Fact]
    public void Crear_debe_inicializar_una_victima_no_verificada_y_activa()
    {
        var victima = Victima.Crear(
            nombreCompleto: "Ana García",
            dni: "12345678",
            telefono: "+51987654321",
            passwordHash: "$2a$12$hashSeguro");

        victima.NombreCompleto.Should().Be("Ana García");
        victima.Dni.Should().Be("12345678");
        victima.Telefono.Should().Be("+51987654321");
        victima.PasswordHash.Should().Be("$2a$12$hashSeguro");
        victima.Verificada.Should().BeFalse("una víctima recién creada NO debe estar verificada");
        victima.Activa.Should().BeTrue("una víctima recién creada debe estar activa");
        victima.FechaRegistro.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));
    }

    [Fact]
    public void Crear_debe_limpiar_espacios_en_blanco_de_nombre_dni_y_telefono()
    {
        var victima = Victima.Crear("  Ana García  ", "  12345678 ", " +51987654321 ", "hash");

        victima.NombreCompleto.Should().Be("Ana García");
        victima.Dni.Should().Be("12345678");
        victima.Telefono.Should().Be("+51987654321");
    }

    [Fact]
    public void Verificar_debe_marcar_la_victima_como_verificada()
    {
        var victima = Victima.Crear("Ana", "12345678", "+51987654321", "hash");
        victima.Verificada.Should().BeFalse();

        victima.Verificar();

        victima.Verificada.Should().BeTrue();
    }

    [Fact]
    public void ActualizarPassword_debe_cambiar_el_hash()
    {
        var victima = Victima.Crear("Ana", "12345678", "+51987654321", "hash_original");

        victima.ActualizarPassword("hash_nuevo");

        victima.PasswordHash.Should().Be("hash_nuevo");
    }

    [Fact]
    public void Desactivar_debe_permitir_soft_delete_sin_borrar_historial()
    {
        var victima = Victima.Crear("Ana", "12345678", "+51987654321", "hash");
        victima.Activa.Should().BeTrue();

        victima.Desactivar();

        victima.Activa.Should().BeFalse("desactivar preserva el historial de denuncias");
    }

    [Fact]
    public void Activar_debe_reactivar_una_victima_desactivada()
    {
        var victima = Victima.Crear("Ana", "12345678", "+51987654321", "hash");
        victima.Desactivar();

        victima.Activar();

        victima.Activa.Should().BeTrue();
    }

    [Fact]
    public void Colecciones_navegacion_deben_estar_inicializadas_vacias()
    {
        var victima = Victima.Crear("Ana", "12345678", "+51987654321", "hash");

        victima.ContactosEmergencia.Should().NotBeNull().And.BeEmpty();
        victima.AlertasSos.Should().NotBeNull().And.BeEmpty();
        victima.Denuncias.Should().NotBeNull().And.BeEmpty();
    }
}
