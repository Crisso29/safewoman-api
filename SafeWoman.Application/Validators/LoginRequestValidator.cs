using FluentValidation;
using SafeWoman.Application.DTOs.Auth;

namespace SafeWoman.Application.Validators;

public class LoginRequestValidator : AbstractValidator<LoginRequest>
{
    public LoginRequestValidator()
    {
        // Cascade.Stop: si NotEmpty falla, no ejecutar las reglas siguientes.
        // Sin esto, .Must(v => v.Length == ...) explota con NRE cuando el body es {} y v es null.
        RuleFor(x => x.Identificador)
            .Cascade(CascadeMode.Stop)
            .NotEmpty().WithMessage("Ingrese su teléfono o DNI.")
            .Must(v => v.Length == 8 || v.Length == 9)
                .WithMessage("El identificador debe ser DNI (8 dígitos) o teléfono (9 dígitos).")
            .Matches(@"^\d+$").WithMessage("Solo debe contener dígitos.");

        RuleFor(x => x.Password)
            .NotEmpty().WithMessage("La contraseña es obligatoria.");
    }
}
