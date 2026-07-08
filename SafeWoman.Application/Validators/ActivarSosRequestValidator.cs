using FluentValidation;
using SafeWoman.Application.DTOs.AlertaSos;

namespace SafeWoman.Application.Validators;

public class ActivarSosRequestValidator : AbstractValidator<ActivarSosRequest>
{
    public ActivarSosRequestValidator()
    {
        RuleFor(x => x.Latitud)
            .InclusiveBetween(-90m, 90m)
            .WithMessage("La latitud debe estar entre -90 y 90.");

        RuleFor(x => x.Longitud)
            .InclusiveBetween(-180m, 180m)
            .WithMessage("La longitud debe estar entre -180 y 180.");
    }
}
