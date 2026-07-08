using System.Linq.Expressions;
using FluentAssertions;
using Moq;
using SafeWoman.Application.DTOs.Auth;
using SafeWoman.Application.Interfaces;
using SafeWoman.Application.Services;
using SafeWoman.Domain.Entities;
using SafeWoman.Domain.Exceptions;
using SafeWoman.Domain.Interfaces;

namespace SafeWoman.UnitTests.Services;

/// <summary>
/// AuthService coordina el registro, verificación OTP y login de víctimas.
/// Es el punto de entrada crítico — bugs aquí abren la puerta a cuentas fraudulentas.
///
/// Los tests usan Moq para simular las dependencias (repositorios, hasher, sender, etc.)
/// y verifican solo la lógica de negocio del servicio.
/// </summary>
public class AuthServiceTests
{
    private readonly Mock<IRepository<Victima>>         _victimaRepo = new();
    private readonly Mock<IRepository<OtpVerificacion>> _otpRepo     = new();
    private readonly Mock<IUnitOfWork>                  _uow         = new();
    private readonly Mock<IPasswordHasher>              _hasher      = new();
    private readonly Mock<ITokenService>                _tokenSvc    = new();
    private readonly Mock<IOtpCodeGenerator>            _otpGen      = new();
    private readonly Mock<IOtpSender>                   _otpSender   = new();
    private readonly Mock<ITransaction>                 _tx          = new();

    private AuthService CrearSut()
    {
        _uow.Setup(x => x.BeginTransactionAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(_tx.Object);
        return new AuthService(
            _victimaRepo.Object, _otpRepo.Object, _uow.Object,
            _hasher.Object, _tokenSvc.Object, _otpGen.Object, _otpSender.Object);
    }

    // ── REGISTRO ──────────────────────────────────────────────────────────────

    [Fact]
    public async Task RegistrarAsync_debe_crear_victima_persistir_OTP_y_enviar_SMS()
    {
        _victimaRepo
            .Setup(r => r.FindAsync(It.IsAny<Expression<Func<Victima, bool>>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Array.Empty<Victima>());
        _hasher.Setup(h => h.Hash("mi-pass")).Returns("hash-bcrypt");
        _otpGen.Setup(g => g.Generate()).Returns("123456");

        var sut = CrearSut();
        var req = new RegistroRequest("Ana Prueba", "12345678", "+51987654321", "mi-pass");

        await sut.RegistrarAsync(req);

        // Se creó la víctima
        _victimaRepo.Verify(r => r.AddAsync(
            It.Is<Victima>(v => v.Dni == "12345678" && v.Telefono == "+51987654321"),
            It.IsAny<CancellationToken>()), Times.Once);
        // Se guardó el OTP
        _otpRepo.Verify(r => r.AddAsync(
            It.Is<OtpVerificacion>(o => o.Codigo == "123456"),
            It.IsAny<CancellationToken>()), Times.Once);
        // Se envió el SMS
        _otpSender.Verify(s => s.SendOtpAsync("+51987654321", "123456", It.IsAny<CancellationToken>()), Times.Once);
        // Se hizo commit
        _tx.Verify(t => t.CommitAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task RegistrarAsync_con_DNI_ya_existente_debe_lanzar_DomainException()
    {
        var existente = Victima.Crear("Otra", "12345678", "+51999888777", "hash");
        _victimaRepo
            .Setup(r => r.FindAsync(It.IsAny<Expression<Func<Victima, bool>>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new[] { existente });

        var sut = CrearSut();
        var req = new RegistroRequest("Ana", "12345678", "+51987654321", "pass");

        var act = () => sut.RegistrarAsync(req);

        await act.Should().ThrowAsync<DomainException>()
            .WithMessage("*Ya existe una cuenta*");
    }

    [Fact]
    public async Task RegistrarAsync_si_falla_persistencia_debe_hacer_rollback_y_no_enviar_SMS()
    {
        _victimaRepo
            .Setup(r => r.FindAsync(It.IsAny<Expression<Func<Victima, bool>>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Array.Empty<Victima>());
        _hasher.Setup(h => h.Hash(It.IsAny<string>())).Returns("hash");
        _otpGen.Setup(g => g.Generate()).Returns("123456");
        _uow.SetupSequence(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1)
            .ThrowsAsync(new InvalidOperationException("BD caída"));

        var sut = CrearSut();
        var req = new RegistroRequest("Ana", "12345678", "+51987654321", "pass");

        var act = () => sut.RegistrarAsync(req);

        await act.Should().ThrowAsync<InvalidOperationException>();

        _tx.Verify(t => t.RollbackAsync(It.IsAny<CancellationToken>()), Times.Once,
            "una excepción durante la persistencia debe disparar rollback");
        _otpSender.Verify(s => s.SendOtpAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()),
            Times.Never, "no debe enviar SMS si el registro falló");
    }

    // ── VERIFICACIÓN OTP ──────────────────────────────────────────────────────

    [Fact]
    public async Task VerificarOtpAsync_con_codigo_correcto_debe_marcar_verificada_y_emitir_token()
    {
        var victima = Victima.Crear("Ana", "12345678", "+51987654321", "hash");
        var otp     = OtpVerificacion.Crear(victima.IdVictima, "123456");

        _victimaRepo
            .Setup(r => r.FindAsync(It.IsAny<Expression<Func<Victima, bool>>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new[] { victima });
        _otpRepo
            .Setup(r => r.FindAsync(It.IsAny<Expression<Func<OtpVerificacion, bool>>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new[] { otp });
        _tokenSvc.Setup(t => t.GenerateVictimaToken(victima)).Returns("jwt.token.signed");

        var sut = CrearSut();
        var req = new VerificarOtpRequest("+51987654321", "123456");

        var respuesta = await sut.VerificarOtpAsync(req);

        respuesta.Token.Should().Be("jwt.token.signed");
        respuesta.Verificada.Should().BeTrue();
        victima.Verificada.Should().BeTrue();
        otp.Usado.Should().BeTrue();
    }

    [Fact]
    public async Task VerificarOtpAsync_con_telefono_no_registrado_debe_lanzar_DomainException()
    {
        _victimaRepo
            .Setup(r => r.FindAsync(It.IsAny<Expression<Func<Victima, bool>>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Array.Empty<Victima>());

        var sut = CrearSut();
        var act = () => sut.VerificarOtpAsync(new VerificarOtpRequest("+51000000000", "123456"));

        await act.Should().ThrowAsync<DomainException>()
            .WithMessage("*no encontrado*");
    }

    [Fact]
    public async Task VerificarOtpAsync_con_codigo_incorrecto_debe_lanzar_DomainException()
    {
        var victima = Victima.Crear("Ana", "12345678", "+51987654321", "hash");
        var otp     = OtpVerificacion.Crear(victima.IdVictima, "999999");

        _victimaRepo
            .Setup(r => r.FindAsync(It.IsAny<Expression<Func<Victima, bool>>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new[] { victima });
        _otpRepo
            .Setup(r => r.FindAsync(It.IsAny<Expression<Func<OtpVerificacion, bool>>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new[] { otp });

        var sut = CrearSut();
        var act = () => sut.VerificarOtpAsync(new VerificarOtpRequest("+51987654321", "111111"));

        await act.Should().ThrowAsync<DomainException>()
            .WithMessage("*inválido o expirado*");
    }

    [Fact]
    public async Task VerificarOtpAsync_sin_OTP_pendiente_debe_lanzar_DomainException()
    {
        var victima = Victima.Crear("Ana", "12345678", "+51987654321", "hash");

        _victimaRepo
            .Setup(r => r.FindAsync(It.IsAny<Expression<Func<Victima, bool>>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new[] { victima });
        _otpRepo
            .Setup(r => r.FindAsync(It.IsAny<Expression<Func<OtpVerificacion, bool>>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Array.Empty<OtpVerificacion>());

        var sut = CrearSut();
        var act = () => sut.VerificarOtpAsync(new VerificarOtpRequest("+51987654321", "123456"));

        await act.Should().ThrowAsync<DomainException>()
            .WithMessage("*No hay código OTP pendiente*");
    }

    // ── LOGIN ─────────────────────────────────────────────────────────────────

    [Fact]
    public async Task LoginAsync_con_credenciales_correctas_debe_emitir_token()
    {
        var victima = Victima.Crear("Ana", "12345678", "+51987654321", "hash-bcrypt");
        victima.Verificar();

        _victimaRepo
            .Setup(r => r.FindAsync(It.IsAny<Expression<Func<Victima, bool>>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new[] { victima });
        _hasher.Setup(h => h.Verify("password", "hash-bcrypt")).Returns(true);
        _tokenSvc.Setup(t => t.GenerateVictimaToken(victima)).Returns("jwt.abc");

        var sut = CrearSut();

        var resp = await sut.LoginAsync(new LoginRequest("+51987654321", "password"));

        resp.Token.Should().Be("jwt.abc");
        resp.NombreCompleto.Should().Be("Ana");
        resp.Verificada.Should().BeTrue();
    }

    [Fact]
    public async Task LoginAsync_con_password_incorrecta_debe_lanzar_DomainException()
    {
        var victima = Victima.Crear("Ana", "12345678", "+51987654321", "hash");
        victima.Verificar();

        _victimaRepo
            .Setup(r => r.FindAsync(It.IsAny<Expression<Func<Victima, bool>>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new[] { victima });
        _hasher.Setup(h => h.Verify(It.IsAny<string>(), It.IsAny<string>())).Returns(false);

        var sut = CrearSut();

        var act = () => sut.LoginAsync(new LoginRequest("+51987654321", "mala"));

        await act.Should().ThrowAsync<DomainException>()
            .WithMessage("*Credenciales incorrectas*");
    }

    [Fact]
    public async Task LoginAsync_con_cuenta_no_verificada_debe_lanzar_DomainException()
    {
        var victima = Victima.Crear("Ana", "12345678", "+51987654321", "hash");
        // NO llamamos victima.Verificar() → sigue sin verificar

        _victimaRepo
            .Setup(r => r.FindAsync(It.IsAny<Expression<Func<Victima, bool>>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new[] { victima });

        var sut = CrearSut();

        var act = () => sut.LoginAsync(new LoginRequest("+51987654321", "pass"));

        await act.Should().ThrowAsync<DomainException>()
            .WithMessage("*no está verificada*");
    }

    [Fact]
    public async Task LoginAsync_con_identificador_no_registrado_debe_lanzar_DomainException()
    {
        _victimaRepo
            .Setup(r => r.FindAsync(It.IsAny<Expression<Func<Victima, bool>>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Array.Empty<Victima>());

        var sut = CrearSut();
        var act = () => sut.LoginAsync(new LoginRequest("+51000000000", "pass"));

        await act.Should().ThrowAsync<DomainException>()
            .WithMessage("*Credenciales incorrectas*");
    }

    // ── REENVIAR OTP ──────────────────────────────────────────────────────────

    [Fact]
    public async Task ReenviarOtpAsync_debe_generar_nuevo_codigo_y_enviarlo_por_SMS()
    {
        var victima = Victima.Crear("Ana", "12345678", "+51987654321", "hash");

        _victimaRepo
            .Setup(r => r.FindAsync(It.IsAny<Expression<Func<Victima, bool>>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new[] { victima });
        _otpGen.Setup(g => g.Generate()).Returns("654321");

        var sut = CrearSut();

        await sut.ReenviarOtpAsync("+51987654321");

        _otpRepo.Verify(r => r.AddAsync(
            It.Is<OtpVerificacion>(o => o.Codigo == "654321"),
            It.IsAny<CancellationToken>()), Times.Once);
        _otpSender.Verify(s => s.SendOtpAsync("+51987654321", "654321", It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task ReenviarOtpAsync_con_telefono_no_registrado_debe_lanzar_DomainException()
    {
        _victimaRepo
            .Setup(r => r.FindAsync(It.IsAny<Expression<Func<Victima, bool>>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Array.Empty<Victima>());

        var sut = CrearSut();

        var act = () => sut.ReenviarOtpAsync("+51000000000");

        await act.Should().ThrowAsync<DomainException>()
            .WithMessage("*Teléfono no registrado*");
    }
}
