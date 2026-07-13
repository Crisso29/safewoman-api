using SafeWoman.Application.DTOs.Auth;
using SafeWoman.Application.Interfaces;
using SafeWoman.Domain.Entities;
using SafeWoman.Domain.Exceptions;
using SafeWoman.Domain.Interfaces;

namespace SafeWoman.Application.Services;

public class AuthService
{
    private readonly IRepository<Victima>         _victimaRepo;
    private readonly IRepository<OtpVerificacion> _otpRepo;
    private readonly IUnitOfWork                  _uow;
    private readonly IPasswordHasher              _hasher;
    private readonly ITokenService                _tokenService;
    private readonly IOtpCodeGenerator            _otpGenerator;
    private readonly IOtpSender                   _otpSender;

    public AuthService(
        IRepository<Victima> victimaRepo,
        IRepository<OtpVerificacion> otpRepo,
        IUnitOfWork uow,
        IPasswordHasher hasher,
        ITokenService tokenService,
        IOtpCodeGenerator otpGenerator,
        IOtpSender otpSender)
    {
        _victimaRepo  = victimaRepo;
        _otpRepo      = otpRepo;
        _uow          = uow;
        _hasher       = hasher;
        _tokenService = tokenService;
        _otpGenerator = otpGenerator;
        _otpSender    = otpSender;
    }

    public async Task<int> RegistrarAsync(RegistroRequest req, CancellationToken ct = default)
    {
        // Buscamos cualquier cuenta con el mismo DNI o teléfono (índices únicos en BD).
        var existentes = await _victimaRepo.FindAsync(
            v => v.Dni == req.Dni || v.Telefono == req.Telefono, ct);
        var existente = existentes.FirstOrDefault();

        if (existente is not null)
        {
            // Ya verificada → bloqueamos y guiamos al login.
            if (existente.Verificada)
                throw new DomainException(
                    ErrorCodes.ACCOUNT_ALREADY_VERIFIED,
                    "Ya existe una cuenta verificada con ese DNI o teléfono. Inicia sesión.");

            // No verificada → la usuaria abandonó el flujo, se equivocó de teléfono
            // o nunca recibió el SMS. Eliminamos la cuenta vieja para que pueda
            // registrarse limpio sin quedar atrapada por los índices únicos.
            // La cascade en OtpVerificacion elimina también los códigos pendientes.
            _victimaRepo.Remove(existente);
            await _uow.SaveChangesAsync(ct);
        }

        var hash    = _hasher.Hash(req.Password);
        var victima = Victima.Crear(req.NombreCompleto, req.Dni, req.Telefono, hash);
        var codigo  = _otpGenerator.Generate();

        // Registro atómico: Víctima + OTP en una sola transacción para evitar
        // que la cuenta quede huérfana sin código si el segundo insert falla.
        await using var tx = await _uow.BeginTransactionAsync(ct);
        try
        {
            await _victimaRepo.AddAsync(victima, ct);
            await _uow.SaveChangesAsync(ct);

            var otp = OtpVerificacion.Crear(victima.IdVictima, codigo);
            await _otpRepo.AddAsync(otp, ct);
            await _uow.SaveChangesAsync(ct);

            await tx.CommitAsync(ct);
        }
        catch
        {
            await tx.RollbackAsync(ct);
            throw;
        }

        // El envío de SMS se hace fuera de la transacción: si Twilio falla,
        // el usuario puede reintentar con ReenviarOtpAsync sin duplicar el registro.
        await _otpSender.SendOtpAsync(victima.Telefono, codigo, ct);

        return victima.IdVictima;
    }

    public async Task<AuthResponse> VerificarOtpAsync(VerificarOtpRequest req, CancellationToken ct = default)
    {
        var victimas = await _victimaRepo.FindAsync(v => v.Telefono == req.Telefono, ct);
        var victima  = victimas.FirstOrDefault()
            ?? throw new DomainException(ErrorCodes.PHONE_NOT_FOUND, "Número de teléfono no encontrado.");

        var otps = await _otpRepo.FindAsync(o => o.IdVictima == victima.IdVictima && !o.Usado, ct);
        var otp  = otps.OrderByDescending(o => o.FechaGeneracion).FirstOrDefault()
            ?? throw new DomainException(ErrorCodes.OTP_NOT_FOUND, "No hay código OTP pendiente.");

        if (!otp.EsValido(req.Codigo))
            throw new DomainException(ErrorCodes.OTP_INVALID, "Código inválido o expirado.");

        otp.Consumir();
        victima.Verificar();

        // FindAsync usa AsNoTracking → entidades desconectadas.
        // Update() las reatacha en estado Modified para que SaveChanges las persista.
        _otpRepo.Update(otp);
        _victimaRepo.Update(victima);
        await _uow.SaveChangesAsync(ct);

        return new AuthResponse(
            _tokenService.GenerateVictimaToken(victima),
            victima.IdVictima,
            victima.NombreCompleto,
            victima.Dni,
            victima.Telefono,
            victima.Verificada);
    }

    public async Task<AuthResponse> LoginAsync(LoginRequest req, CancellationToken ct = default)
    {
        // RF-04: el identificador puede ser teléfono (9 dígitos) o DNI (8 dígitos)
        // v.Activa está cubierto por el HasQueryFilter global en VictimaConfiguration
        var victimas = await _victimaRepo.FindAsync(
            v => v.Telefono == req.Identificador || v.Dni == req.Identificador, ct);
        var victima  = victimas.FirstOrDefault()
            ?? throw new DomainException(ErrorCodes.INVALID_CREDENTIALS, "Credenciales incorrectas.");

        if (!victima.Verificada)
            throw new DomainException(
                ErrorCodes.ACCOUNT_NOT_VERIFIED,
                "La cuenta aún no está verificada. Revise su SMS.");

        if (!_hasher.Verify(req.Password, victima.PasswordHash))
            throw new DomainException(ErrorCodes.INVALID_CREDENTIALS, "Credenciales incorrectas.");

        return new AuthResponse(
            _tokenService.GenerateVictimaToken(victima),
            victima.IdVictima,
            victima.NombreCompleto,
            victima.Dni,
            victima.Telefono,
            victima.Verificada);
    }

    public async Task ReenviarOtpAsync(string telefono, CancellationToken ct = default)
    {
        var victimas = await _victimaRepo.FindAsync(v => v.Telefono == telefono, ct);
        var victima  = victimas.FirstOrDefault()
            ?? throw new DomainException(ErrorCodes.PHONE_NOT_FOUND, "Teléfono no registrado.");

        var codigo = _otpGenerator.Generate();
        var otp    = OtpVerificacion.Crear(victima.IdVictima, codigo);
        await _otpRepo.AddAsync(otp, ct);
        await _uow.SaveChangesAsync(ct);

        await _otpSender.SendOtpAsync(victima.Telefono, codigo, ct);
    }
}
