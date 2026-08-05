using System.Text.Json;
using FluentAssertions;
using Icbank.Platform.DataMigration.Reporting;

namespace Icbank.Platform.DataMigration.Tests.Reporting;

/// <summary>
/// <see cref="ReportWriter"/> produces the only artifact an operator reads after a real cutover
/// run to decide whether it is safe to bring the new system online. Before this test class it had
/// zero coverage: a defect that dropped a table's notes, mis-rendered pass/fail, or wrote to the
/// wrong path would not fail any build or test, it would just make the report silently wrong or
/// silently absent at the one moment it matters most.
/// </summary>
public sealed class ReportWriterTests : IDisposable
{
    private readonly string _tempDirectory = Path.Combine(Path.GetTempPath(), $"icbank-report-writer-tests-{Guid.NewGuid():N}");

    public void Dispose()
    {
        if (Directory.Exists(_tempDirectory))
        {
            Directory.Delete(_tempDirectory, recursive: true);
        }
    }

    [Fact]
    public void Write_CreatesBothJsonAndTextFiles_InTheGivenDirectory()
    {
        MigrationReport report = BuildSampleReport();

        (var jsonPath, var textPath) = ReportWriter.Write(report, _tempDirectory);

        File.Exists(jsonPath).Should().BeTrue();
        File.Exists(textPath).Should().BeTrue();
        jsonPath.Should().StartWith(_tempDirectory);
        textPath.Should().StartWith(_tempDirectory);
    }

    [Fact]
    public void Write_DirectoryDoesNotExist_CreatesItRatherThanThrowing()
    {
        Directory.Exists(_tempDirectory).Should().BeFalse("this test exists specifically to prove Write creates a missing directory");

        ReportWriter.Write(BuildSampleReport(), _tempDirectory);

        Directory.Exists(_tempDirectory).Should().BeTrue();
    }

    [Fact]
    public void Write_JsonFile_RoundTripsTableNamesAndCounts()
    {
        MigrationReport report = BuildSampleReport();

        (var jsonPath, _) = ReportWriter.Write(report, _tempDirectory);

        using var doc = JsonDocument.Parse(File.ReadAllText(jsonPath));
        JsonElement tables = doc.RootElement.GetProperty("Tables");
        tables.GetArrayLength().Should().Be(1);
        tables[0].GetProperty("TableName").GetString().Should().Be("roles");
        tables[0].GetProperty("SourceRowCount").GetInt64().Should().Be(5);
    }

    [Fact]
    public void RenderText_IncludesModeAndOverallResult()
    {
        MigrationReport report = BuildSampleReport();

        var text = ReportWriter.RenderText(report);

        text.Should().Contain("mode: Migrate");
        text.Should().Contain("Overall result: PASS");
    }

    [Fact]
    public void RenderText_FailingTable_RendersFailMarkerAndNotes()
    {
        var report = new MigrationReport { Mode = "Reconcile", StartedAtUtc = DateTimeOffset.UtcNow, OverallPass = false };
        report.Tables.Add(new TableReportEntry
        {
            TableName = "daily_reports",
            SourceRowCount = 2,
            DestinationRowCount = 0,
            Pass = false,
            Notes = { "Source has 2 row(s), destination has 0." },
        });

        var text = ReportWriter.RenderText(report);

        text.Should().Contain("[FAIL] daily_reports: source=2, destination=0");
        text.Should().Contain("Source has 2 row(s), destination has 0.");
    }

    [Fact]
    public void RenderText_RunLevelFindings_AreIncludedWhenPresent()
    {
        MigrationReport report = BuildSampleReport();
        report.AddFinding("Total source rows across 1 registered table(s): 5.");

        var text = ReportWriter.RenderText(report);

        text.Should().Contain("Run-level findings:");
        text.Should().Contain("Total source rows across 1 registered table(s): 5.");
    }

    [Fact]
    public void RenderText_NoFindings_OmitsFindingsSectionEntirely()
    {
        MigrationReport report = BuildSampleReport();

        var text = ReportWriter.RenderText(report);

        text.Should().NotContain("Run-level findings:");
    }

    [Fact]
    public void Write_FileNamesAreDerivedFromModeAndStartTimestamp()
    {
        var startedAt = new DateTimeOffset(2026, 3, 4, 5, 6, 7, TimeSpan.Zero);
        var report = new MigrationReport { Mode = "Validate", StartedAtUtc = startedAt };

        (var jsonPath, var textPath) = ReportWriter.Write(report, _tempDirectory);

        Path.GetFileName(jsonPath).Should().Be("validate-20260304-050607.json");
        Path.GetFileName(textPath).Should().Be("validate-20260304-050607.txt");
    }

    private static MigrationReport BuildSampleReport()
    {
        var report = new MigrationReport { Mode = "Migrate", StartedAtUtc = DateTimeOffset.UtcNow, FinishedAtUtc = DateTimeOffset.UtcNow };
        report.Tables.Add(new TableReportEntry { TableName = "roles", SourceRowCount = 5, Pass = true });
        return report;
    }
}
