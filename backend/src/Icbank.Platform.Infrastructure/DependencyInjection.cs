using Icbank.Platform.Application.Auth;
using Icbank.Platform.Application.Common.Interfaces;
using Icbank.Platform.Infrastructure.Http;
using Icbank.Platform.Infrastructure.Identity;
using Icbank.Platform.Infrastructure.Persistence;
using Icbank.Platform.Infrastructure.Persistence.Interceptors;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Polly;

namespace Icbank.Platform.Infrastructure;

/// <summary>
/// Composition-root extension for the Infrastructure layer (R-BE-004). Wires EF Core, the audit
/// interceptor, the current-user port, and outbound HTTP resilience policies.
/// </summary>
public static class DependencyInjection
{
    private const int MaxRetryAttempts = 3; // R-BE-095 — named, not a bare "3".
    private const double CircuitBreakerFailureRatio = 0.5;
    private static readonly TimeSpan AttemptTimeout = TimeSpan.FromSeconds(5);

    /// <summary>Registers persistence, identity, and outbound-HTTP infrastructure services.</summary>
    /// <param name="services">The DI service collection.</param>
    /// <param name="configuration">The application configuration, used for the SQL Server connection string.</param>
    /// <returns>The same <paramref name="services"/> instance, for chaining.</returns>
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddHttpContextAccessor();
        services.AddScoped<ICurrentUserService, CurrentUserService>();
        services.AddScoped<IRequestContext, HttpRequestContext>();

        // Why: identity/auth ports registered here per R-BE-004 (composition root only) —
        // Application/Api consume these purely through the interfaces defined in Application.
        services.AddSingleton<IPasswordHasher, PasswordHasher>();
        services.AddSingleton<ITokenService, JwtTokenService>();
        services.AddScoped<IRefreshTokenService, RefreshTokenService>();
        services.AddScoped<IPermissionResolver, PermissionResolver>();
        services.AddScoped<IAuditLogService, AuditLogService>();
        services.AddScoped<Icbank.Platform.Application.Common.Interfaces.IAsyncQueryExecutor, Persistence.EfAsyncQueryExecutor>();
        services.AddScoped<IResourceAuthorizationService, Security.ResourceAuthorizationService>();
        services.AddSingleton<ISafeStoragePathValidator, Security.SafeStoragePathValidator>();
        services.AddSingleton<ITemporaryPasswordGenerator, TemporaryPasswordGenerator>();

        services.AddOptions<JwtOptions>()
            .Bind(configuration.GetSection(JwtOptions.SectionName))
            .Validate(options => !string.IsNullOrWhiteSpace(options.SigningKey), "Jwt:SigningKey must be configured.")
            .ValidateOnStart();

        // Why: Azure AD SSO (BUSINESS-RULES.md §11.2/§11.3) — the distributed cache is a
        // single-instance in-memory implementation by default; swap AddDistributedMemoryCache()
        // for a Redis-backed registration to support horizontal scaling (see AUTH-PORT-NOTES.md).
        services.AddDistributedMemoryCache();
        services.AddOptions<AzureAdOptions>().Bind(configuration.GetSection(AzureAdOptions.SectionName));
        services.AddHttpClient("idp");
        services.AddScoped<IAzureAdClient, AzureAdClient>();
        services.AddSingleton<ISsoStateStore, DistributedSsoStateStore>();
        services.AddScoped<ISsoOptionsProvider, AzureAdOptionsProvider>();

        services.AddOptions<Seeding.SeedOptions>().Bind(configuration.GetSection(Seeding.SeedOptions.SectionName));
        services.AddScoped<Seeding.DatabaseSeeder>();

        // Why: AuditInterceptor depends on the scoped ICurrentUserService, so it must itself be
        // scoped (a singleton cannot consume a scoped dependency — ASP.NET Core's DI validator
        // rejects this at startup with ValidateScopes/ValidateOnBuild).
        services.AddScoped<AuditInterceptor>();

        var connectionString = configuration.GetConnectionString("Default")
            ?? throw new InvalidOperationException("Connection string 'Default' is not configured.");

        services.AddDbContext<AppDbContext>((serviceProvider, options) => options
            .UseSqlServer(connectionString)
            .AddInterceptors(serviceProvider.GetRequiredService<AuditInterceptor>()));

        // Why: Application depends only on the narrow IApplicationDbContext port (R-BE-002);
        // this resolves it to the same scoped AppDbContext instance EF Core already manages.
        services.AddScoped<Icbank.Platform.Application.Common.Interfaces.IApplicationDbContext>(
            sp => sp.GetRequiredService<AppDbContext>());

        AddResilientHttpClients(services);

        return services;
    }

    /// <summary>Registers the named <c>HttpClient</c> instances used for outbound calls, wrapped in a standard resilience pipeline (Polly v8 via <c>Microsoft.Extensions.Http.Resilience</c>).</summary>
    /// <param name="services">The DI service collection.</param>
    private static void AddResilientHttpClients(IServiceCollection services)
    {
        services.AddHttpClient("downstream")
            .AddHttpMessageHandler(() => new CorrelationPropagationHandler())
            .AddStandardResilienceHandler(options =>
            {
                options.Retry.MaxRetryAttempts = MaxRetryAttempts;
                options.Retry.BackoffType = DelayBackoffType.Exponential;
                options.AttemptTimeout.Timeout = AttemptTimeout;
                options.CircuitBreaker.FailureRatio = CircuitBreakerFailureRatio;
            });
    }
}
