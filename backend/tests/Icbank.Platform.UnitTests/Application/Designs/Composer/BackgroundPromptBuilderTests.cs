using FluentAssertions;
using Icbank.Platform.Application.Designs.Composer;
using Icbank.Platform.Domain.Designs;
using Xunit;

namespace Icbank.Platform.UnitTests.Application.Designs.Composer;

/// <summary>Verifies <see cref="BackgroundPromptBuilder"/> reproduces BUSINESS-RULES.md §7.3's exact spatial-hint thresholds.</summary>
public sealed class BackgroundPromptBuilderTests
{
    [Fact]
    public void Build_NoTemplate_AppendsOnlyQualitySuffix()
    {
        var result = BackgroundPromptBuilder.Build("منظر جبلي", template: null);

        result.Should().Contain("منظر جبلي");
        result.Should().Contain("no text or watermarks");
        result.Should().NotContain("calm");
    }

    [Fact]
    public void Build_PanelBelow55PercentHeight_AppendsBottomThirdHint()
    {
        DesignTemplate template = BuildTemplate(canvasHeight: 1080, panelY: 700, panelHeight: 200);

        var result = BackgroundPromptBuilder.Build("منظر", template);

        result.Should().Contain("bottom third");
    }

    [Fact]
    public void Build_PanelInTop30PercentAndShorterThan40Percent_AppendsTopThirdHint()
    {
        DesignTemplate template = BuildTemplate(canvasHeight: 1000, panelY: 100, panelHeight: 300);

        var result = BackgroundPromptBuilder.Build("منظر", template);

        result.Should().Contain("top third");
    }

    [Fact]
    public void Build_PanelInMiddleZone_AppendsGenericCalmHint()
    {
        DesignTemplate template = BuildTemplate(canvasHeight: 1000, panelY: 500, panelHeight: 300);

        var result = BackgroundPromptBuilder.Build("منظر", template);

        result.Should().Contain("calm, low-contrast visual zone in the lower portion");
    }

    private static DesignTemplate BuildTemplate(int canvasHeight, double panelY, double panelHeight) => new()
    {
        TemplateNameAr = "test",
        Category = "general",
        CanvasHeight = canvasHeight,
        BackgroundPanelConfig = new BackgroundPanelConfig { X = 0, Y = panelY, Width = 100, Height = panelHeight, Color = "#000", Opacity = 1 },
    };
}
