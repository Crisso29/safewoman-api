using FluentAssertions;
using SafeWoman.Domain.Entities;

namespace SafeWoman.UnitTests.Domain;

public class AdministradorTests
{
    [Fact]
    public void Crear_debe_inicializar_administrador_activo_con_email_normalizado()
    {
        var admin = Administrador.Crear(
            nombre: "  María Admin  ",
            email: "  Maria.Admin@SafeWoman.PE  ",
            passwordHash: "$2a$12$hashSeguro");

        admin.Nombre.Should().Be("María Admin");
        admin.Email.Should().Be("maria.admin@safewoman.pe",
            "el email debe normalizarse a minúsculas para evitar duplicados");
        admin.PasswordHash.Should().Be("$2a$12$hashSeguro");
        admin.Activo.Should().BeTrue();
        admin.UltimoAcceso.Should().BeNull("un admin recién creado nunca accedió");
        admin.FechaRegistro.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));
    }

    [Fact]
    public void RegistrarAcceso_debe_actualizar_UltimoAcceso_a_la_hora_actual()
    {
        var admin = Administrador.Crear("Admin", "admin@safewoman.pe", "hash");
        admin.UltimoAcceso.Should().BeNull();

        admin.RegistrarAcceso();

        admin.UltimoAcceso.Should().NotBeNull()
            .And.Subject.Value.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));
    }

    [Fact]
    public void Desactivar_debe_marcar_admin_como_no_activo()
    {
        var admin = Administrador.Crear("Admin", "admin@safewoman.pe", "hash");
        admin.Activo.Should().BeTrue();

        admin.Desactivar();

        admin.Activo.Should().BeFalse();
    }
}
