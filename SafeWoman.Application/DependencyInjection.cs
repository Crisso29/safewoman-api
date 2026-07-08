using FluentValidation;
using FluentValidation.AspNetCore;
using Microsoft.Extensions.DependencyInjection;
using SafeWoman.Application.Services;
using SafeWoman.Application.Validators;

namespace SafeWoman.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.AddScoped<AuthService>();
        services.AddScoped<AlertaSosService>();
        services.AddScoped<DenunciaService>();
        services.AddScoped<DenunciaAnonimaService>();
        services.AddScoped<ContactoService>();
        services.AddScoped<VictimaService>();
        services.AddScoped<AdminAuthService>();

        services.AddValidatorsFromAssemblyContaining<RegistroRequestValidator>(ServiceLifetime.Scoped);
        services.AddFluentValidationAutoValidation();

        return services;
    }
}
