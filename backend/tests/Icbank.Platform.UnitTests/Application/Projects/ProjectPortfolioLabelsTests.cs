using FluentAssertions;
using Icbank.Platform.Application.Projects;
using Icbank.Platform.Domain.Projects;
using Xunit;

namespace Icbank.Platform.UnitTests.Application.Projects;

/// <summary>
/// Pins the Arabic vocabulary the projects page prints. The wording is a product decision, so a
/// silent edit to a label has to fail a test rather than reach the page unnoticed.
/// </summary>
public sealed class ProjectPortfolioLabelsTests
{
    [Fact]
    public void HealthLabel_AtRisk_ReadsTheAgreedFollowUpWording()
        => ProjectPortfolioLabels.HealthLabel(ProjectHealth.AtRisk).Should().Be("بحاجة إلى المتابعة");

    [Fact]
    public void HealthLabel_AtRisk_NoLongerUsesTheOldWording()
        => ProjectPortfolioLabels.HealthLabel(ProjectHealth.AtRisk).Should().NotBe("يحتاج متابعة");

    [Fact]
    public void HealthKey_AtRisk_IsUnchangedSoTheBrowserFilterStillMatches()
        => ProjectPortfolioLabels.HealthKey(ProjectHealth.AtRisk).Should().Be("at_risk");

    [Theory]
    [InlineData(ProjectHealth.OnTrack, "على المسار")]
    [InlineData(ProjectHealth.Delayed, "متأخر")]
    [InlineData(ProjectHealth.Completed, "مكتمل")]
    public void HealthLabel_OtherSignals_AreUntouched(ProjectHealth health, string expected)
        => ProjectPortfolioLabels.HealthLabel(health).Should().Be(expected);
}
