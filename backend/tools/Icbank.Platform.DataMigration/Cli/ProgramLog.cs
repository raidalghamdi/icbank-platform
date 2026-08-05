using Microsoft.Extensions.Logging;

namespace Icbank.Platform.DataMigration.Cli;

/// <summary>Source-generated log messages for the CLI entry point (<c>Program.cs</c>, top-level statements, cannot itself be <c>partial</c>).</summary>
public static partial class ProgramLog
{
    /// <summary>Logs that the report was written to disk.</summary>
    /// <param name="logger">The logger.</param>
    /// <param name="jsonPath">The JSON report path.</param>
    /// <param name="textPath">The text report path.</param>
    [LoggerMessage(Level = LogLevel.Information, Message = "Report written to {JsonPath} and {TextPath}.")]
    public static partial void LogReportWritten(ILogger logger, string jsonPath, string textPath);

    /// <summary>Logs an unhandled failure running a mode.</summary>
    /// <param name="logger">The logger.</param>
    /// <param name="exception">The exception that terminated the run.</param>
    /// <param name="mode">The mode that was running.</param>
    [LoggerMessage(Level = LogLevel.Critical, Message = "Unhandled failure running mode {Mode}.")]
    public static partial void LogUnhandledFailure(ILogger logger, Exception exception, MigrationMode mode);
}
