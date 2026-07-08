using FluentAssertions;
using SafeWoman.Domain.Entities;

namespace SafeWoman.UnitTests.Domain;

/// <summary>
/// Verifica las reglas de negocio del contacto de emergencia — los teléfonos
/// que reciben el SMS SOS cuando la víctima activa la alerta.
/// </summary>
public class ContactoEmergenciaTests
{
    [Fact]
    public void Crear_debe_inicializar_los_campos_correctamente()
    {
        var contacto = ContactoEmergencia.Crear(
            idVictima: 42,
            nombre: "María Hermana",
            telefono: "+51999888777");

        contacto.IdVictima.Should().Be(42);
        contacto.Nombre.Should().Be("María Hermana");
        contacto.Telefono.Should().Be("+51999888777");
    }

    [Fact]
    public void Crear_debe_recortar_espacios_en_blanco()
    {
        var contacto = ContactoEmergencia.Crear(1, "  María  ", "  +51999888777  ");

        contacto.Nombre.Should().Be("María");
        contacto.Telefono.Should().Be("+51999888777");
    }

    [Fact]
    public void Actualizar_debe_modificar_nombre_y_telefono()
    {
        var contacto = ContactoEmergencia.Crear(1, "María", "+51999888777");

        contacto.Actualizar("María Actualizada", "+51988777666");

        contacto.Nombre.Should().Be("María Actualizada");
        contacto.Telefono.Should().Be("+51988777666");
    }

    [Fact]
    public void Actualizar_debe_recortar_espacios_al_actualizar()
    {
        var contacto = ContactoEmergencia.Crear(1, "María", "+51999888777");

        contacto.Actualizar("  Nueva Marí a  ", "  +51988777666  ");

        contacto.Nombre.Should().Be("Nueva Marí a");
        contacto.Telefono.Should().Be("+51988777666");
    }
}
