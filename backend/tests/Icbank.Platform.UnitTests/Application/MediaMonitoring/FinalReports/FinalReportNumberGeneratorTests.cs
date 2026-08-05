using FluentAssertions;
using Icbank.Platform.Application.MediaMonitoring;
using Xunit;

namespace Icbank.Platform.UnitTests.Application.MediaMonitoring.FinalReports;

/// <summary>Verifies <see cref="FinalReportNumberGenerator"/> (BUSINESS-RULES.md §5.2).</summary>
public sealed class FinalReportNumberGeneratorTests
{
    private static readonly string[] MixedYearNumbers = { "GAC-MEDIA-1/2026", "GAC-MEDIA-5/2026", "GAC-MEDIA-3/2026" };
    private static readonly string[] OtherYearNumbers = { "GAC-MEDIA-9/2025", "GAC-MEDIA-14/2027" };
    private static readonly string[] MalformedAndValidNumbers = { "not-a-report-number/2026", "GAC-MEDIA-2/2026" };

    [Fact]
    public void Next_NoExistingNumbers_ReturnsSequenceOne()
    {
        var next = FinalReportNumberGenerator.Next(Array.Empty<string>(), 2026);

        next.Should().Be("GAC-MEDIA-1/2026");
    }

    [Fact]
    public void Next_ExistingNumbersForYear_ReturnsMaxPlusOne()
    {
        var next = FinalReportNumberGenerator.Next(MixedYearNumbers, 2026);

        next.Should().Be("GAC-MEDIA-6/2026");
    }

    [Fact]
    public void Next_OnlyOtherYearsExist_IgnoresThemAndStartsAtOne()
    {
        var next = FinalReportNumberGenerator.Next(OtherYearNumbers, 2026);

        next.Should().Be("GAC-MEDIA-1/2026");
    }

    [Fact]
    public void Next_MalformedExistingNumber_IsIgnoredNotThrown()
    {
        var next = FinalReportNumberGenerator.Next(MalformedAndValidNumbers, 2026);

        next.Should().Be("GAC-MEDIA-3/2026");
    }
}
