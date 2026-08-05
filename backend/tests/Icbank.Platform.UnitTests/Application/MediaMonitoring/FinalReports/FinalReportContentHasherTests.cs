using System.Text.RegularExpressions;
using FluentAssertions;
using Icbank.Platform.Application.MediaMonitoring;
using Icbank.Platform.Application.MediaMonitoring.Commands;
using Xunit;

namespace Icbank.Platform.UnitTests.Application.MediaMonitoring.FinalReports;

/// <summary>Verifies <see cref="FinalReportContentHasher"/> (BUSINESS-RULES.md §5.2's audit-artifact fingerprint).</summary>
public sealed class FinalReportContentHasherTests
{
    [Fact]
    public void ComputeSha256_SameDraftTwice_ProducesIdenticalHash()
    {
        FinalReportDraftDto draft = FinalMediaReportTestData.BuildDraftDto();

        var hash1 = FinalReportContentHasher.ComputeSha256(draft);
        var hash2 = FinalReportContentHasher.ComputeSha256(draft);

        hash1.Should().Be(hash2);
    }

    [Fact]
    public void ComputeSha256_ReturnsLowercase64CharacterHex()
    {
        FinalReportDraftDto draft = FinalMediaReportTestData.BuildDraftDto();

        var hash = FinalReportContentHasher.ComputeSha256(draft);

        hash.Should().HaveLength(64);
        Regex.IsMatch(hash, "^[0-9a-f]{64}$").Should().BeTrue();
    }

    [Fact]
    public void ComputeSha256_DifferentDrafts_ProduceDifferentHashes()
    {
        FinalReportDraftDto draft = FinalMediaReportTestData.BuildDraftDto();
        FinalReportDraftDto modified = draft with { ExecutiveSummary = "ملخص مختلف تماماً" };

        var hash1 = FinalReportContentHasher.ComputeSha256(draft);
        var hash2 = FinalReportContentHasher.ComputeSha256(modified);

        hash1.Should().NotBe(hash2);
    }
}
