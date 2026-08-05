using System.Reflection;
using FluentValidation;
using Icbank.Platform.Application.Auth;
using Icbank.Platform.Application.Common.Behaviors;
using Microsoft.Extensions.DependencyInjection;

namespace Icbank.Platform.Application;

/// <summary>
/// Composition-root extension for the Application layer (R-BE-004: registrations live in one
/// discoverable place, never scattered <c>GetService</c> calls).
/// </summary>
public static class DependencyInjection
{
    private static readonly Assembly ApplicationAssembly = typeof(DependencyInjection).Assembly;

    /// <summary>Registers MediatR handlers, the validation pipeline behaviour, and FluentValidation validators.</summary>
    /// <param name="services">The DI service collection.</param>
    /// <returns>The same <paramref name="services"/> instance, for chaining.</returns>
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.AddMediatR(configuration =>
        {
            configuration.RegisterServicesFromAssembly(ApplicationAssembly);
            configuration.AddOpenBehavior(typeof(ValidationBehavior<,>));
        });

        services.AddValidatorsFromAssembly(ApplicationAssembly);
        services.AddScoped<AuthSessionFactory>();

        return services;
    }
}
