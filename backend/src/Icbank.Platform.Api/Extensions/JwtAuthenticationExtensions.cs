using System.Text;
using Icbank.Platform.Infrastructure.Identity;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;

namespace Icbank.Platform.Api.Extensions;

/// <summary>
/// Registers JWT bearer authentication against the same signing key
/// <see cref="JwtTokenService"/> issues tokens with (DOTNET-CONVENTIONS.md §5.1). The access
/// token is bearer-only; the refresh token never appears here — it's read directly from the
/// httpOnly cookie by <c>AuthController</c>, never through the authentication middleware.
/// </summary>
public static class JwtAuthenticationExtensions
{
    /// <summary>Adds JWT bearer authentication bound to the <c>Jwt</c> configuration section.</summary>
    /// <param name="services">The DI service collection.</param>
    /// <param name="configuration">The application configuration.</param>
    /// <returns>The same <paramref name="services"/> instance, for chaining.</returns>
    public static IServiceCollection AddIcbankJwtAuthentication(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddOptions<JwtOptions>().Bind(configuration.GetSection(JwtOptions.SectionName));

        // Why: the signing key must be read lazily from IOptions at token-validation time, not
        // eagerly from IConfiguration when this extension runs — eager reads captured a
        // snapshot of configuration that predates any ConfigureAppConfiguration override applied
        // by a test host (WebApplicationFactory), causing a spurious "key length is zero" failure
        // that never reproduces outside the test harness.
        services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
            .AddJwtBearer();
        services.AddOptions<JwtBearerOptions>(JwtBearerDefaults.AuthenticationScheme)
            .Configure<Microsoft.Extensions.Options.IOptions<JwtOptions>>((options, jwtOptions) =>
            {
                JwtOptions jwt = jwtOptions.Value;
                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuer = true,
                    ValidIssuer = jwt.Issuer,
                    ValidateAudience = true,
                    ValidAudience = jwt.Audience,
                    ValidateLifetime = true,
                    ValidateIssuerSigningKey = true,
                    IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(
                        string.IsNullOrEmpty(jwt.SigningKey) ? Guid.NewGuid().ToString() : jwt.SigningKey)),
                    ClockSkew = TimeSpan.Zero,
                };
            });

        return services;
    }
}
