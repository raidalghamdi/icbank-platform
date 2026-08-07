using FluentAssertions;
using Icbank.Platform.Application.MediaMonitoring;
using Icbank.Platform.Domain.MediaMonitoring;
using Xunit;

namespace Icbank.Platform.UnitTests.Application.MediaMonitoring.FinalReports;

/// <summary>Verifies <see cref="ReportArchiveContextBuilder"/> (BUSINESS-RULES.md §5.5's context-block truncation lengths).</summary>
public sealed class ReportArchiveContextBuilderTests
{
    [Fact]
    public void Build_EmptyReportList_ReturnsEmptyString()
    {
        var context = ReportArchiveContextBuilder.Build(Array.Empty<FinalMediaReport>());

        context.Should().BeEmpty();
    }

    [Fact]
    public void Build_SingleReport_IncludesReportNumberAndExecutiveSummary()
    {
        FinalMediaReport report = FinalMediaReportTestData.BuildEntity(1, "GAC-MEDIA-3/2026");

        var context = ReportArchiveContextBuilder.Build(new[] { report });

        context.Should().Contain("GAC-MEDIA-3/2026");
        context.Should().Contain(report.ExecutiveSummary!);
    }

    [Fact]
    public void Build_MultipleReports_ConcatenatesEachSectionInOrder()
    {
        FinalMediaReport first = FinalMediaReportTestData.BuildEntity(1, "GAC-MEDIA-1/2026");
        FinalMediaReport second = FinalMediaReportTestData.BuildEntity(2, "GAC-MEDIA-2/2026");

        var context = ReportArchiveContextBuilder.Build(new[] { first, second });

        context.IndexOf("GAC-MEDIA-1/2026", StringComparison.Ordinal)
            .Should().BeLessThan(context.IndexOf("GAC-MEDIA-2/2026", StringComparison.Ordinal));
    }
}
