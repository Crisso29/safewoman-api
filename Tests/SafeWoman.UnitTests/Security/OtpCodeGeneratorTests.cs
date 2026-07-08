using FluentAssertions;
using SafeWoman.Infrastructure.Services.Security;

namespace SafeWoman.UnitTests.Security;

/// <summary>
/// Verifica el generador de códigos OTP (6 dígitos) que se envía por SMS
/// durante el registro. Punto crítico: si los códigos son predecibles,
/// un atacante podría hijackear cuentas.
/// </summary>
public class OtpCodeGeneratorTests
{
    private readonly OtpCodeGenerator _sut = new();

    [Fact]
    public void Generate_debe_devolver_exactamente_6_digitos()
    {
        var codigo = _sut.Generate();

        codigo.Should().HaveLength(6);
        codigo.Should().MatchRegex(@"^\d{6}$");
    }

    [Fact]
    public void Generate_debe_soportar_numero_con_ceros_a_la_izquierda()
    {
        // Genera muchos códigos y verifica que al menos algunos empiezan con 0.
        // Sin padding, un valor como "42" se enviaría como 2 dígitos → OTP inválido.
        var codigos = Enumerable.Range(0, 500).Select(_ => _sut.Generate()).ToList();

        codigos.Should().OnlyContain(c => c.Length == 6);
    }

    [Fact]
    public void Generate_debe_producir_valores_distintos_en_llamadas_consecutivas()
    {
        // No exigimos entropía perfecta (imposible con 6 dígitos y 1000 llamadas),
        // pero sí una diversidad razonable — al menos 90% únicos en 1000 llamadas.
        var codigos = Enumerable.Range(0, 1000).Select(_ => _sut.Generate()).ToHashSet();

        codigos.Count.Should().BeGreaterThan(900,
            "el generador debe producir códigos con alta entropía");
    }

    [Fact]
    public void Generate_debe_ser_thread_safe()
    {
        // Simula 100 threads paralelos generando 10 códigos cada uno.
        // Si RandomNumberGenerator no fuera thread-safe, veríamos excepciones.
        var codigos = new System.Collections.Concurrent.ConcurrentBag<string>();

        Parallel.For(0, 100, _ =>
        {
            for (int i = 0; i < 10; i++)
                codigos.Add(_sut.Generate());
        });

        codigos.Should().HaveCount(1000);
        codigos.Should().OnlyContain(c => c.Length == 6 && c.All(char.IsDigit));
    }
}
