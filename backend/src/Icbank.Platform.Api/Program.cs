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

WebApplicationBuilder builder = WebApplication.CreateBuilder(args);

// Why: R-BE-079 — the Server header reveals the framework/version to attackers. Kestrel adds it
// itself before the response pipeline runs, so SecurityHeadersMiddleware.Remove("Server") alone
// cannot suppress it; it must be disabled at the server level too.
builder.WebHost.ConfigureKestrel(options => options.AddServerHeader = false);

// Why: Serilog replaces the default provider entirely so every log line — framework and
// application — flows through the same structured sinks (R-BE-050).
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
    .Bind(builder.Configuration.GetSection(Icbank.Platform.Api.Auth.CronApiKeyOptions.SectionName));

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
    var generatedPassword = await seeder.SeedAsync(CancellationToken.None);
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
app.UseCorrelationId();

app.UseCors(Icbank.Platform.Api.Extensions.CorsExtensions.FrontendPolicyName);

app.UseRateLimiter();

app.UseAuthentication();
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
