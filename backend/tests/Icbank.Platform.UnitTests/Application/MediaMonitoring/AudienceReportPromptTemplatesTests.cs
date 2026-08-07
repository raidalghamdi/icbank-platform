using FluentAssertions;
using Icbank.Platform.Application.MediaMonitoring;
using Icbank.Platform.Domain.MediaMonitoring;
using Xunit;

namespace Icbank.Platform.UnitTests.Application.MediaMonitoring;

/// <summary>Verifies the 3 audience-tiered report prompt templates resolve correctly (BUSINESS-RULES.md §5.1), including the manager-as-default/fallback rule.</summary>
public sealed class AudienceReportPromptTemplatesTests
{
    [Fact]
    public void Resolve_Executive_ReturnsExecutivePrompt()
    {
        var prompt = AudienceReportPromptTemplates.Resolve(MediaReportAudience.Executive);

        prompt.Should().Contain("محلل إعلامي تنفيذي").And.Contain("الملخص التنفيذي");
    }

    [Fact]
    public void Resolve_Analyst_ReturnsAnalystPrompt()
    {
        var prompt = AudienceReportPromptTemplates.Resolve(MediaReportAudience.Analyst);

        prompt.Should().Contain("محلل إعلامي خبير").And.Contain("تحليل كل منشور");
    }

    [Fact]
    public void Resolve_Manager_ReturnsManagerPrompt()
    {
        var prompt = AudienceReportPromptTemplates.Resolve(MediaReportAudience.Manager);

        prompt.Should().Contain("للإدارة الوسطى");
    }

    [Fact]
    public void Resolve_Full_FallsBackToManagerPrompt()
    {
        var prompt = AudienceReportPromptTemplates.Resolve(MediaReportAudience.Full);

        prompt.Should().Contain("للإدارة الوسطى");
    }
}
