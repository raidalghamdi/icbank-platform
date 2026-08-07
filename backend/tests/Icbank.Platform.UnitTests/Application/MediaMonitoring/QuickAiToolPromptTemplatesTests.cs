using FluentAssertions;
using Icbank.Platform.Application.MediaMonitoring;
using Xunit;

namespace Icbank.Platform.UnitTests.Application.MediaMonitoring;

/// <summary>Verifies the 7 fixed AI Quick prompt templates are built verbatim (BUSINESS-RULES.md §5.6).</summary>
public sealed class QuickAiToolPromptTemplatesTests
{
    [Fact]
    public void Build_GenerateWithoutTone_OmitsToneClause()
    {
        var prompt = QuickAiToolPromptTemplates.Build("generate", "الموضوع", null, null);

        prompt.Should().Be("أنت محرر محتوى محترف في هيئة حكومية. اكتب محتوى عربي واضح ومتماسك عن الموضوع التالي:\n\nالموضوع");
    }

    [Fact]
    public void Build_GenerateWithTone_IncludesToneClause()
    {
        var prompt = QuickAiToolPromptTemplates.Build("generate", "الموضوع", "رسمية", null);

        prompt.Should().Be("أنت محرر محتوى محترف في هيئة حكومية. اكتب محتوى عربي واضح ومتماسك عن الموضوع التالي بنبرة رسمية:\n\nالموضوع");
    }

    [Fact]
    public void Build_ToneWithoutOverride_DefaultsToFormal()
    {
        var prompt = QuickAiToolPromptTemplates.Build("tone", "نص", null, null);

        prompt.Should().Contain("بنبرة رسمية");
    }

    [Fact]
    public void Build_HeadlinesWithoutCount_UsesDefaultOfEight()
    {
        var prompt = QuickAiToolPromptTemplates.Build("headlines", "خبر", null, null);

        prompt.Should().Contain("اقترح 8 عناوين");
    }

    [Fact]
    public void Build_HeadlinesWithCount_UsesSuppliedCount()
    {
        var prompt = QuickAiToolPromptTemplates.Build("headlines", "خبر", null, 3);

        prompt.Should().Contain("اقترح 3 عناوين");
    }

    [Theory]
    [InlineData("rephrase")]
    [InlineData("rewrite")]
    [InlineData("summary")]
    public void Build_TextOnlyTools_ContainInput(string tool)
    {
        var prompt = QuickAiToolPromptTemplates.Build(tool, "المدخل", null, null);

        prompt.Should().Contain("المدخل");
    }

    [Fact]
    public void Build_MessagesWithTone_IncludesToneClause()
    {
        var prompt = QuickAiToolPromptTemplates.Build("messages", "رسالة", "ودّية", null);

        prompt.Should().Contain("وبنبرة ودّية");
    }

    [Fact]
    public void Build_UnknownTool_ReturnsNull()
    {
        var prompt = QuickAiToolPromptTemplates.Build("not-a-tool", "input", null, null);

        prompt.Should().BeNull();
    }
}
