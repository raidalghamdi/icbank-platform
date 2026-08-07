using System.Globalization;
using Icbank.Platform.Application.Common.Interfaces;
using Icbank.Platform.DataMigration.Cli;
using Icbank.Platform.DataMigration.Configuration;
using Icbank.Platform.DataMigration.Migration;
using Icbank.Platform.DataMigration.Reconciliation;
using Icbank.Platform.DataMigration.Reporting;
using Icbank.Platform.DataMigration.Source;
using Icbank.Platform.DataMigration.Validation;
using Icbank.Platform.Infrastructure.Identity;
using Microsoft.Extensions.Configuration;
using Serilog;
using Serilog.Extensions.Logging;

// CA1031 ("catch a more specific exception") is suppressed on the single top-level catch below by
// design: this CLI's Main is the last-resort exception boundary for the whole migration run --
// any unexpected failure must be logged and turned into a clean non-zero exit code rather than an
// unhandled-exception crash dump, matching the precedent set by
// Icbank.Platform.Api.Middleware.GlobalExceptionMiddleware for the API's own last-resort boundary.
[assembly: System.Diagnostics.CodeAnalysis.SuppressMessage(
    "Design",
    "CA1031:Do not catch general exception types",
    Justification = "CLI Main is the mandated single last-resort exception boundary for this tool, mirroring GlobalExceptionMiddleware's precedent.",
    Scope = "member",
    Target = "~M:Program.<Main>$(System.String[])~System.Threading.Tasks.Task{System.Int32}")]

IConfiguration configuration = new ConfigurationBuilder()
    .AddJsonFile("appsettings.json", optional: true)
    .AddEnvironmentVariables()
    .Build();

var options = new MigrationOptions();
configuration.GetSection(MigrationOptions.SectionName).Bind(options);

if (args.Length != 1 || !Enum.TryParse<MigrationMode>(args[0], ignoreCase: true, out MigrationMode mode))
{
    Console.WriteLine("Usage: Icbank.Platform.DataMigration <validate|migrate|reconcile>");
    Console.WriteLine("Connection strings and settings are read from the 'Migration' configuration section (appsettings.json or environment variables) -- never from command-line arguments.");
    return 1;
}

Directory.CreateDirectory(options.ReportDirectory);

Log.Logger = new LoggerConfiguration()
    .WriteTo.Console(formatProvider: CultureInfo.InvariantCulture)
    .WriteTo.File(
        Path.Combine(options.ReportDirectory, "migration-.log"),
        rollingInterval: RollingInterval.Day,
        formatProvider: CultureInfo.InvariantCulture)
    .Enrich.FromLogContext()
    .CreateLogger();

using var loggerFactory = new SerilogLoggerFactory(Log.Logger);
Microsoft.Extensions.Logging.ILogger logger = loggerFactory.CreateLogger("Icbank.Platform.DataMigration");

using var cancellationSource = new CancellationTokenSource();
Console.CancelKeyPress += (_, eventArgs) =>
{
    eventArgs.Cancel = true;
    cancellationSource.Cancel();
};

IDateTimeProvider clock = new SystemDateTimeProvider();
DateTimeOffset startedAt = clock.UtcNow;

try
{
    MigrationReport report = await RunModeAsync(mode, options, clock, logger, cancellationSource.Token);

    (var jsonPath, var textPath) = ReportWriter.Write(report, options.ReportDirectory);
    ProgramLog.LogReportWritten(logger, jsonPath, textPath);
    Console.WriteLine(ReportWriter.RenderText(report));

    return report.OverallPass ? 0 : 1;
}
catch (Exception ex)
{
    ProgramLog.LogUnhandledFailure(logger, ex, mode);
    return 2;
}
finally
{
    Log.CloseAndFlush();
}

static async Task<MigrationReport> RunModeAsync(
    MigrationMode mode,
    MigrationOptions options,
    IDateTimeProvider clock,
    Microsoft.Extensions.Logging.ILogger logger,
    CancellationToken cancellationToken)
{
    var source = new NpgsqlDataSource(options.SourceConnectionString);

    if (mode == MigrationMode.Validate)
    {
        var validationRunner = new ValidationRunner(source, logger);
        return await validationRunner.RunAsync(clock.UtcNow, cancellationToken);
    }

    await using var idMap = new IdMappingStore(options.DestinationConnectionString);
    var report = new MigrationReport { Mode = mode.ToString(), StartedAtUtc = clock.UtcNow };
    var context = new MigrationRunContext(source, idMap, options.DestinationConnectionString, clock, logger, report);

    if (mode == MigrationMode.Migrate)
    {
        await idMap.EnsureCreatedAsync(cancellationToken);
        var orchestrator = new MigrationOrchestrator(context, logger);
        return await orchestrator.RunAsync(clock.UtcNow, cancellationToken);
    }

    var reconciliationRunner = new ReconciliationRunner(context, logger);
    return await reconciliationRunner.RunAsync(clock.UtcNow, cancellationToken);
}
