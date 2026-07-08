using FluentAssertions;
using SafeWoman.Infrastructure.Services.Security;

namespace SafeWoman.UnitTests.Security;

/// <summary>
/// Verifica el correcto hashing de contraseñas con BCrypt.
/// Punto crítico de seguridad: si esto falla, cualquier atacante que
/// robe la BD podría revertir las contraseñas.
/// </summary>
public class BcryptPasswordHasherTests
{
    private readonly BcryptPasswordHasher _sut = new();

    [Fact]
    public void Hash_debe_producir_una_cadena_no_vacia()
    {
        var hash = _sut.Hash("Password123!");

        hash.Should().NotBeNullOrEmpty();
        hash.Should().StartWith("$2"); // firma BCrypt
    }

    [Fact]
    public void Verify_debe_devolver_true_para_password_correcta()
    {
        var password = "MiContrasenaSegura2026$";
        var hash     = _sut.Hash(password);

        _sut.Verify(password, hash).Should().BeTrue();
    }

    [Fact]
    public void Verify_debe_devolver_false_para_password_incorrecta()
    {
        var hash = _sut.Hash("PasswordOriginal");

        _sut.Verify("PasswordEquivocada", hash).Should().BeFalse();
    }

    [Fact]
    public void Hash_debe_producir_hashes_distintos_para_la_misma_password()
    {
        // BCrypt usa un salt aleatorio → dos hashes de la misma password
        // deben ser distintos (defensa contra ataques rainbow table).
        var p = "MismaPassword";

        _sut.Hash(p).Should().NotBe(_sut.Hash(p));
    }

    [Theory]
    [InlineData("")]
    [InlineData("a")]
    [InlineData("password_con_muchos_caracteres_super_largos_para_verificar_soporte_1234567890")]
    public void Hash_debe_funcionar_con_passwords_de_cualquier_longitud(string password)
    {
        var hash = _sut.Hash(password);

        _sut.Verify(password, hash).Should().BeTrue();
    }
}
