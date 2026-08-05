using Icbank.Platform.Infrastructure.Persistence;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Icbank.Platform.IntegrationTests.Auth;

/// <summary>
/// Test host for the full auth/RBAC pipeline (R-BE-081/082). Swaps <c>AppDbContext</c>'s
/// provider from SQL Server to a uniquely-named EF Core InMemory database per factory instance so
/// every end-to-end auth test (login, lockout, refresh rotation, SEC-01 escalation, SSO redirect
/// validation, audit log) runs without a live SQL Server, while still exercising the exact same
/// controllers/handlers/middleware pipeline the app runs in production.
/// </summary>
public sealed class AuthWebApplicationFactory : WebApplicationFactory<Program>
{
    /// <summary>Gets the unique InMemory database name for this factory instance. Exposed so tests can reset/reseed per-test without cross-test data bleed.</summary>
    public string DatabaseName { get; } = Guid.NewGuid().ToString();

    /// <inheritdoc />
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");
        builder.ConfigureAppConfiguration((_, configBuilder) =>
        {
            configBuilder.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ConnectionStrings:Default"] = "Server=localhost;Database=IcbankPlatformAuthTest;Trusted_Connection=True;TrustServerCertificate=True;",
                ["Jwt:SigningKey"] = "integration-test-signing-key-not-for-production-use-32bytes",
                ["Jwt:Issuer"] = "icbank-platform-tests",
                ["Jwt:Audience"] = "icbank-platform-tests-clients",
                ["Jwt:AccessTokenMinutes"] = "15",
                ["Jwt:RefreshTokenHours"] = "8",
                ["AzureAd:Enabled"] = "true",
                ["AzureAd:AllowedRedirectTargets:0"] = "/dashboard",
                ["Seed:AllowInProduction"] = "false",
                ["Cron:ApiKey"] = "test-cron-key",
            });
        });

        builder.ConfigureServices(services =>
        {
            ServiceDescriptor? descriptor = services.SingleOrDefault(
                d => d.ServiceType == typeof(DbContextOptions<AppDbContext>));
            if (descriptor is not null)
            {
                services.Remove(descriptor);
            }

            services.AddDbContext<AppDbContext>(options => options.UseInMemoryDatabase(DatabaseName));
        });
    }
}
