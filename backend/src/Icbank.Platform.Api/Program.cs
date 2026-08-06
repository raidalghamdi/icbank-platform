using Asp.Versioning.ApiExplorer;
using Icbank.Platform.Api.Extensions;
using Icbank.Platform.Api.Middleware;
using Icbank.Platform.Application;
using Icbank.Platform.Infrastructure;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Serilog;

// Why: QuestPDF requires an explicit license acceptance before any Document is composed
// (the Rendering feature's PDF renderers -- see Icbank.Platform.Infrastructure.Rendering).
// Community is free for individuals/small businesses/FOSS under the current QuestPDF terms;
// see RENDERING-NOTES.md for the exact license text and eligibility discussion.
QuestPDF.Settings.License = QuestPDF.Infrastructure.LicenseType.Community;

WebApplicationBuilder builder = WebApplication.CreateBuilder(args);

// Why: R-BE-079 — the Server header reveals the framework/version to attackers. Kestrel adds it
// itself before the response pipeline runs, so SecurityHeadersMiddleware.Remove("Server") alone
// cannot suppress it; it must be disabled at the server level too.
builder.WebHost.ConfigureKestrel(options => options.AddServerHeader = false);

// Why: must run before anything reads ConnectionStrings/Jwt/Cron config -- this is the Key Vault
// configuration source itself, layered on top of appsettings.json/environment variables using
// the App Service's managed identity (R-BE-043). A no-op in Development/Testing/local runs
// that never set KeyVault:VaultUri.
builder.AddIcbankKeyVault();

// Why: fails fast with a clear error before the host builds if a required secret is still empty
// after Key Vault has had a chance to supply it, rather than booting with an empty JWT signing
// key or connection string and failing confusingly later, or -- worse -- silently signing tokens
// with a guessable key.
builder.AddIcbankStartupSecretsGuard();

// Why: registers the Application Insights SDK (request/dependency/exception auto-collection)
// before Serilog is wired, so the Serilog sink's ReadFrom.Services(services) call below can see
// and reuse the same TelemetryConfiguration instead of building an uncorrelated second one.
builder.AddIcbankApplicationInsights();

// Why: Serilog replaces the default provider entirely so every log line — framework and
// application — flows through the same structured sinks (R-BE-050), including the Application
// Insights sink enabled in deployed environments.
builder.AddIcbankSerilog();

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.AddIcbankApiVersioning();
builder.Services.AddApplication();
builder.Services.AddInfrastructure(builder.Configuration);
builder.Services.AddIcbankHealthChecks(builder.Configuration);
builder.Services.AddIcbankRateLimiting();
builder.Services.AddIcbankCors(builder.Configuration);
builder.Services.AddIcbankJwtAuthentication(builder.Configuration);
builder.Services.AddIcbankAuthorization();
builder.Services.AddHttpContextAccessor();
builder.Services.AddOptions<Icbank.Platform.Api.Auth.CronApiKeyOptions>()
    .Bind(builder.Configuration.GetSection(Icbank.Platform.Api.Auth.CronApiKeyOptions.SectionName))
    .Validate(options => !string.IsNullOrWhiteSpace(options.ApiKey), "Cron:ApiKey must be configured.")
    .ValidateOnStart();

builder.Services.AddProblemDetails(options =>
{
    options.CustomizeProblemDetails = context =>
    {
        context.ProblemDetails.Extensions["traceId"] = context.HttpContext.TraceIdentifier;

        // Why: R-BE-079 forbids leaking stack traces; only Development gets extra detail.
        var isDevelopment = context.HttpContext.RequestServices
            .GetRequiredService<IHostEnvironment>()
            .IsDevelopment();
        if (!isDevelopment)
        {
            context.ProblemDetails.Extensions.Remove("exception");
        }
    };
});

WebApplication app = builder.Build();

if (!app.Environment.IsEnvironment("Testing"))
{
    using IServiceScope seedScope = app.Services.CreateScope();
    Icbank.Platform.Infrastructure.Seeding.DatabaseSeeder seeder =
        seedScope.ServiceProvider.GetRequiredService<Icbank.Platform.Infrastructure.Seeding.DatabaseSeeder>();

    // Why: seeding is the first thing that touches SQL, so a cold Azure SQL database or a
    // transient gateway blip used to abort the process (SIGABRT) before the host ever listened.
    // App Service then gives up and stops the whole site, so a momentary hiccup turned into an
    // outage needing manual intervention. Bounded retry with backoff rides out transient faults;
    // a genuinely wrong credential still fails loudly after the last attempt, which is correct --
    // booting with an unusable database would only hide the fault behind broken endpoints.
    const int seedAttempts = 3;
    string? generatedPassword = null;
    for (var attempt = 1; ; attempt++)
    {
        try
        {
            generatedPassword = await seeder.SeedAsync(CancellationToken.None);
            break;
        }
        catch (Exception ex) when (attempt < seedAttempts)
        {
            // Why: Serilog's request pipeline is not up yet, so this goes straight to stdout,
            // which App Service captures in the container log. No secret is included.
            Console.WriteLine(
                $"Database seeding attempt {attempt} of {seedAttempts} failed: " +
                $"{ex.GetType().Name}: {ex.Message}. Retrying in {attempt * 5} seconds.");
            await Task.Delay(TimeSpan.FromSeconds(attempt * 5));
        }
    }

    if (generatedPassword is not null)
    {
        // Why: R-BE-054 — the one-time generated super-admin password must never be written to
        // a log sink. Console.WriteLine bypasses Serilog entirely; this is the single place in
        // the whole codebase permitted to surface it, and only on the run that just created the
        // account (idempotent seeding never repeats this).
        Console.WriteLine("==================================================================");
        Console.WriteLine("Initial super-admin account created. This password is shown ONCE:");
        Console.WriteLine(generatedPassword);
        Console.WriteLine("==================================================================");
    }
}

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(options =>
    {
        IReadOnlyList<ApiVersionDescription> descriptions = app.DescribeApiVersions();
        foreach (ApiVersionDescription description in descriptions)
        {
            options.SwaggerEndpoint($"/swagger/{description.GroupName}/swagger.json", description.GroupName);
        }
    });
}

// Why: R-BE-070 — HTTPS/HSTS are non-negotiable in every environment except local HTTP dev loops.
app.UseHttpsRedirection();
if (!app.Environment.IsDevelopment())
{
    app.UseHsts();
}

app.UseMiddleware<SecurityHeadersMiddleware>();
app.UseMiddleware<GlobalExceptionMiddleware>();

// Why: R-BE-035 requires every error to be Problem Details. GlobalExceptionMiddleware only
// covers thrown exceptions and controllers only cover matched routes, which left bare
// status codes produced by the framework itself - an unmatched route, a wrong HTTP verb, an
// unauthenticated call - returning an empty body with no content type. UseStatusCodePages
// renders those through the registered ProblemDetails service so the contract holds for
// every response the client can observe, not just the ones a controller reached.
app.UseStatusCodePages();

app.UseCorrelationId();

app.UseCors(Icbank.Platform.Api.Extensions.CorsExtensions.FrontendPolicyName);

app.UseRateLimiter();

app.UseAuthentication();
app.UseMiddleware<Icbank.Platform.Api.Middleware.MustChangePasswordMiddleware>();
app.UseAuthorization();

app.MapControllers();
app.MapIcbankHealthChecks();

app.Run();

/// <summary>
/// Partial <c>Program</c> class declaration exposing the entry point type to
/// <c>WebApplicationFactory&lt;Program&gt;</c> in the integration test project (R-BE-081).
/// </summary>
public partial class Program
{
}
