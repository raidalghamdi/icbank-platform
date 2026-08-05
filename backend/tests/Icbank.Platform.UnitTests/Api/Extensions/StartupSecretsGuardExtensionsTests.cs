using FluentAssertions;
using Icbank.Platform.Api.Extensions;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;

namespace Icbank.Platform.UnitTests.Api.Extensions;

/// <summary>
/// Verifies <see cref="StartupSecretsGuardExtensions.AddIcbankStartupSecretsGuard"/> fails fast
/// with a clear, key-naming error in a production-like environment when a required secret is
/// missing or blank, is skipped in Development/Testing, and -- the specific case called out by
/// R-BE-043 -- never allows the app to boot with an empty JWT signing key.
/// </summary>
public sealed class StartupSecretsGuardExtensionsTests
{
    private static readonly IReadOnlyDictionary<string, string?> AllSecretsPresent = new Dictionary<string, string?>
    {
        ["ConnectionStrings:Default"] = "Server=tcp:example;Database=icbank;",
        ["Jwt:SigningKey"] = "a-sufficiently-long-signing-key-value",
        ["Cron:ApiKey"] = "cron-api-key-value",
    };

    [Fact]
    public void AddIcbankStartupSecretsGuard_AllSecretsPresentInProduction_DoesNotThrow()
    {
        WebApplicationBuilder builder = CreateBuilder("Production", AllSecretsPresent);

        Action act = () => builder.AddIcbankStartupSecretsGuard();

        act.Should().NotThrow();
    }

    [Fact]
    public void AddIcbankStartupSecretsGuard_MissingConnectionString_ThrowsNamingTheKey()
    {
        var config = new Dictionary<string, string?>(AllSecretsPresent)
        {
            ["ConnectionStrings:Default"] = string.Empty,
        };
        WebApplicationBuilder builder = CreateBuilder("Production", config);

        Action act = () => builder.AddIcbankStartupSecretsGuard();

        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*ConnectionStrings:Default*");
    }

    [Fact]
    public void AddIcbankStartupSecretsGuard_BlankJwtSigningKey_ThrowsAndNeverAllowsBoot()
    {
        // Why: R-BE-043 specifically calls out the JWT signing key -- an empty key would let the
        // app boot and sign tokens with an effectively guessable secret, so this must never be
        // merely a warning.
        var config = new Dictionary<string, string?>(AllSecretsPresent)
        {
            ["Jwt:SigningKey"] = "   ",
        };
        WebApplicationBuilder builder = CreateBuilder("Production", config);

        Action act = () => builder.AddIcbankStartupSecretsGuard();

        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*Jwt:SigningKey*");
    }

    [Fact]
    public void AddIcbankStartupSecretsGuard_MissingCronApiKey_ThrowsNamingTheKey()
    {
        var config = new Dictionary<string, string?>(AllSecretsPresent);
        config.Remove("Cron:ApiKey");
        WebApplicationBuilder builder = CreateBuilder("Production", config);

        Action act = () => builder.AddIcbankStartupSecretsGuard();

        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*Cron:ApiKey*");
    }

    [Fact]
    public void AddIcbankStartupSecretsGuard_MultipleKeysMissing_NamesAllOfThemInOneException()
    {
        var config = new Dictionary<string, string?>
        {
            ["ConnectionStrings:Default"] = "Server=tcp:example;Database=icbank;",
        };
        WebApplicationBuilder builder = CreateBuilder("Production", config);

        Action act = () => builder.AddIcbankStartupSecretsGuard();

        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*Jwt:SigningKey*")
            .WithMessage("*Cron:ApiKey*");
    }

    [Fact]
    public void AddIcbankStartupSecretsGuard_DevelopmentEnvironment_SkipsValidationEvenWhenBlank()
    {
        WebApplicationBuilder builder = CreateBuilder("Development", new Dictionary<string, string?>());

        Action act = () => builder.AddIcbankStartupSecretsGuard();

        act.Should().NotThrow();
    }

    [Fact]
    public void AddIcbankStartupSecretsGuard_TestingEnvironment_SkipsValidationEvenWhenBlank()
    {
        WebApplicationBuilder builder = CreateBuilder("Testing", new Dictionary<string, string?>());

        Action act = () => builder.AddIcbankStartupSecretsGuard();

        act.Should().NotThrow();
    }

    [Fact]
    public void AddIcbankStartupSecretsGuard_StagingEnvironmentWithBlankSecret_Throws()
    {
        // Why: only Development and Testing are exempt -- Staging is a deployed environment that
        // must be held to the same bar as Production.
        var config = new Dictionary<string, string?>(AllSecretsPresent)
        {
            ["Cron:ApiKey"] = string.Empty,
        };
        WebApplicationBuilder builder = CreateBuilder("Staging", config);

        Action act = () => builder.AddIcbankStartupSecretsGuard();

        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*Cron:ApiKey*");
    }

    private static WebApplicationBuilder CreateBuilder(string environmentName, IReadOnlyDictionary<string, string?> configValues)
    {
        WebApplicationBuilder builder = WebApplication.CreateBuilder(new WebApplicationOptions
        {
            EnvironmentName = environmentName,
        });
        builder.Configuration.AddInMemoryCollection(configValues!);
        return builder;
    }
}
