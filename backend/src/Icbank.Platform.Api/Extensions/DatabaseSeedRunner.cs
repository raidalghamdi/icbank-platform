using Icbank.Platform.Infrastructure.Seeding;

namespace Icbank.Platform.Api.Extensions;

/// <summary>
/// Runs <see cref="DatabaseSeeder"/> during startup with a bounded retry.
/// <para>
/// Extracted from <c>Program.cs</c>'s top-level statements so the startup path stays inside the
/// 40-line method gate (R-BE-091). Behaviour is unchanged from the inline version.
/// </para>
/// </summary>
internal static class DatabaseSeedRunner
{
    private const int SeedAttempts = 3;

    /// <summary>Seeds the database, then surfaces a freshly generated super-admin password once.</summary>
    /// <param name="app">The built host, used to resolve a scoped seeder.</param>
    /// <returns>A task that completes when seeding has succeeded.</returns>
    public static async Task RunAsync(WebApplication app)
    {
        using IServiceScope seedScope = app.Services.CreateScope();
        DatabaseSeeder seeder = seedScope.ServiceProvider.GetRequiredService<DatabaseSeeder>();

        var generatedPassword = await SeedWithRetryAsync(seeder);
        if (generatedPassword is not null)
        {
            AnnounceGeneratedPassword(generatedPassword);
        }
    }

    // Why: seeding is the first thing that touches SQL, so a cold Azure SQL database or a
    // transient gateway blip used to abort the process (SIGABRT) before the host ever listened.
    // App Service then gives up and stops the whole site, so a momentary hiccup turned into an
    // outage needing manual intervention. Bounded retry with backoff rides out transient faults;
    // a genuinely wrong credential still fails loudly after the last attempt, which is correct --
    // booting with an unusable database would only hide the fault behind broken endpoints.
    private static async Task<string?> SeedWithRetryAsync(DatabaseSeeder seeder)
    {
        for (var attempt = 1; ; attempt++)
        {
            try
            {
                return await seeder.SeedAsync(CancellationToken.None);
            }
            catch (Exception ex) when (attempt < SeedAttempts)
            {
                // Why: Serilog's request pipeline is not up yet, so this goes straight to stdout,
                // which App Service captures in the container log. No secret is included.
                Console.WriteLine(
                    $"Database seeding attempt {attempt} of {SeedAttempts} failed: " +
                    $"{ex.GetType().Name}: {ex.Message}. Retrying in {attempt * 5} seconds.");
                await Task.Delay(TimeSpan.FromSeconds(attempt * 5));
            }
        }
    }

    // Why: R-BE-054 — the one-time generated super-admin password must never be written to
    // a log sink. Console.WriteLine bypasses Serilog entirely; this is the single place in
    // the whole codebase permitted to surface it, and only on the run that just created the
    // account (idempotent seeding never repeats this).
    private static void AnnounceGeneratedPassword(string generatedPassword)
    {
        Console.WriteLine("==================================================================");
        Console.WriteLine("Initial super-admin account created. This password is shown ONCE:");
        Console.WriteLine(generatedPassword);
        Console.WriteLine("==================================================================");
    }
}
