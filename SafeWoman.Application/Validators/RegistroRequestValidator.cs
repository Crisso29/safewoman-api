using FluentValidation;
using SafeWoman.Application.DTOs.Auth;

namespace SafeWoman.Application.Validators;

public class RegistroRequestValidator : AbstractValidator<RegistroRequest>
{
    public RegistroRequestValidator()
    {
        RuleFor(x => x.NombreCompleto)
            .NotEmpty().WithMessage("El nombre completo es obligatorio.")
            .MaximumLength(200).WithMessage("El nombre no puede superar 200 caracteres.");

        RuleFor(x => x.Dni)
            .NotEmpty().WithMessage("El DNI es obligatorio.")
            .Length(8).WithMessage("El DNI debe tener exactamente 8 dígitos.")
            .Matches(@"^\d{8}$").WithMessage("El DNI solo debe contener dígitos.");

        RuleFor(x => x.Telefono)
            .NotEmpty().WithMessage("El número de celular es obligatorio.")
            .Length(9).WithMessage("El celular debe tener exactamente 9 dígitos.")
            .Matches(@"^\d{9}$").WithMessage("El celular solo debe contener dígitos.");

        RuleFor(x => x.Password)
            .NotEmpty().WithMessage("La contraseña es obligatoria.")
            .MinimumLength(8).WithMessage("La contraseña debe tener al menos 8 caracteres.");
    }
}
