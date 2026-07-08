using FluentValidation;
using SafeWoman.Application.DTOs.Auth;

namespace SafeWoman.Application.Validators;

public class VerificarOtpRequestValidator : AbstractValidator<VerificarOtpRequest>
{
    public VerificarOtpRequestValidator()
    {
        RuleFor(x => x.Telefono)
            .NotEmpty().WithMessage("El número de celular es obligatorio.")
            .Length(9).WithMessage("El celular debe tener exactamente 9 dígitos.")
            .Matches(@"^\d{9}$").WithMessage("El celular solo debe contener dígitos.");

        RuleFor(x => x.Codigo)
            .NotEmpty().WithMessage("El código OTP es obligatorio.")
            .Length(6).WithMessage("El código OTP debe tener exactamente 6 dígitos.")
            .Matches(@"^\d{6}$").WithMessage("El código OTP solo debe contener dígitos.");
    }
}
