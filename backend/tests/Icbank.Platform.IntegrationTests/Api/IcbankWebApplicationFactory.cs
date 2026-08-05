using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;

namespace Icbank.Platform.IntegrationTests.Api;

/// <summary>
/// Test host for the API (R-BE-081). Supplies a syntactically valid but never-dialled SQL Server
/// connection string purely so the DI container can construct <c>AppDbContext</c>; no test in this
/// project exercises the database, so no live SQL Server is required to run this suite.
/// </summary>
public sealed class IcbankWebApplicationFactory : WebApplicationFactory<Program>
{
    /// <inheritdoc />
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");
        builder.ConfigureAppConfiguration((_, configBuilder) =>
        {
            configBuilder.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ConnectionStrings:Default"] = "Server=localhost;Database=IcbankPlatformTest;Trusted_Connection=True;TrustServerCertificate=True;",
                ["Jwt:SigningKey"] = "integration-test-signing-key-not-for-production-use-32bytes",
                ["Jwt:Issuer"] = "icbank-platform-tests",
                ["Jwt:Audience"] = "icbank-platform-tests-clients",
                ["Seed:AllowInProduction"] = "false",
                ["Cron:ApiKey"] = "test-cron-key",
            });
        });
    }
}
