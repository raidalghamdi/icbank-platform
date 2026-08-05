using Icbank.Platform.Application.Common.Interfaces;
using Icbank.Platform.Infrastructure.Persistence;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace Icbank.Platform.IntegrationTests.Auth;

/// <summary>
/// Test host for the full API pipeline (R-BE-081/082), exercising the exact same
/// controllers, handlers and middleware the app runs in production.
/// <para>
/// The database provider is chosen at runtime. When the
/// <c>ICBANK_TEST_SQL_CONNECTION</c> environment variable is set, the suite runs against a
/// real SQL Server instance in a throwaway database created by applying the EF Core
/// migrations, and drops it again on dispose. That is how CI runs, and it is the only
/// configuration that actually exercises relational behaviour: foreign keys, unique
/// indexes, cascade rules, <c>datetimeoffset(3)</c> precision and <c>rowversion</c>
/// optimistic concurrency.
/// </para>
/// <para>
/// When the variable is absent the suite falls back to a uniquely-named EF Core InMemory
/// database so the tests remain runnable on a developer machine with no SQL Server. Be
/// aware that the InMemory provider enforces none of the constraints listed above, so a
/// green local run is weaker evidence than a green CI run.
/// </para>
/// </summary>
public sealed class AuthWebApplicationFactory : WebApplicationFactory<Program>
{
    private static readonly string? SqlServerConnectionTemplate =
        Environment.GetEnvironmentVariable("ICBANK_TEST_SQL_CONNECTION");

    private readonly string relationalDatabaseName = $"IcbankTest_{Guid.NewGuid():N}";
    private bool hostCreated;

    /// <summary>
    /// Gets a value indicating whether the suite is backed by a real SQL Server database
    /// rather than the InMemory provider. Tests that assert relational-only behaviour
    /// (constraint violations, concurrency conflicts) should skip themselves when this is
    /// <see langword="false"/> rather than assert something the provider cannot enforce.
    /// </summary>
    public static bool UsesRelationalDatabase => !string.IsNullOrWhiteSpace(SqlServerConnectionTemplate);

    /// <summary>Gets the unique InMemory database name for this factory instance. Exposed so tests can reset/reseed per-test without cross-test data bleed.</summary>
    public string DatabaseName { get; } = Guid.NewGuid().ToString();

    /// <summary>
    /// Gets the mutable <see cref="IDateTimeProvider"/> test double registered in place of
    /// <c>SystemDateTimeProvider</c> for this factory instance. Tests that need a controllable
    /// Riyadh-day boundary (e.g. cron idempotency) mutate <see cref="FakeDateTimeProvider.FixedUtcNow"/>
    /// between calls instead of depending on the real system clock.
    /// </summary>
    public FakeDateTimeProvider Clock { get; } = new();

    /// <inheritdoc />
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");
        builder.ConfigureAppConfiguration((_, configBuilder) =>
        {
            configBuilder.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ConnectionStrings:Default"] = this.ResolveConnectionString(),
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

            if (UsesRelationalDatabase)
            {
                var connectionString = this.ResolveConnectionString();
                services.AddDbContext<AppDbContext>(options => options.UseSqlServer(connectionString));
            }
            else
            {
                services.AddDbContext<AppDbContext>(options => options.UseInMemoryDatabase(this.DatabaseName));
            }

            ServiceDescriptor? dateTimeDescriptor = services.SingleOrDefault(d => d.ServiceType == typeof(IDateTimeProvider));
            if (dateTimeDescriptor is not null)
            {
                services.Remove(dateTimeDescriptor);
            }

            services.AddSingleton<IDateTimeProvider>(Clock);
        });
    }

    /// <inheritdoc />
    protected override IHost CreateHost(IHostBuilder builder)
    {
        IHost host = base.CreateHost(builder);
        this.hostCreated = true;

        if (UsesRelationalDatabase)
        {
            using IServiceScope scope = host.Services.CreateScope();
            AppDbContext context = scope.ServiceProvider.GetRequiredService<AppDbContext>();

            // Migrate rather than EnsureCreated: this is what validates that the committed
            // migrations actually produce a working schema on real SQL Server.
            context.Database.Migrate();
        }

        return host;
    }

    /// <inheritdoc />
    protected override void Dispose(bool disposing)
    {
        if (disposing && this.hostCreated && UsesRelationalDatabase)
        {
            using IServiceScope scope = this.Services.CreateScope();
            AppDbContext context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            context.Database.EnsureDeleted();
        }

        base.Dispose(disposing);
    }

    private string ResolveConnectionString()
    {
        if (!UsesRelationalDatabase)
        {
            // Never used by the InMemory provider, but the host still binds and validates
            // configuration, so the key must be present and well-formed.
            return "Server=localhost;Database=IcbankPlatformAuthTest;Trusted_Connection=True;TrustServerCertificate=True;";
        }

        return new SqlConnectionStringBuilder(SqlServerConnectionTemplate)
        {
            InitialCatalog = this.relationalDatabaseName,
        }.ConnectionString;
    }
}
