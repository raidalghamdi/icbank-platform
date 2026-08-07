using FluentAssertions;
using Icbank.Platform.Application.MediaMonitoring;
using Xunit;

namespace Icbank.Platform.UnitTests.Application.MediaMonitoring;

/// <summary>Verifies <see cref="PromptVariableSubstitutor"/> substitutes <c>{{key}}</c> placeholders and leaves unmatched ones verbatim.</summary>
public sealed class PromptVariableSubstitutorTests
{
    [Fact]
    public void Substitute_MatchingKey_ReplacesPlaceholder()
    {
        var result = PromptVariableSubstitutor.Substitute("مرحبا {{name}}", new Dictionary<string, string> { ["name"] = "أحمد" });

        result.Should().Be("مرحبا أحمد");
    }

    [Fact]
    public void Substitute_UnmatchedKey_LeavesPlaceholderVerbatim()
    {
        var result = PromptVariableSubstitutor.Substitute("مرحبا {{name}}", new Dictionary<string, string>());

        result.Should().Be("مرحبا {{name}}");
    }

    [Fact]
    public void Substitute_MultiplePlaceholders_ReplacesAll()
    {
        var variables = new Dictionary<string, string> { ["a"] = "1", ["b"] = "2" };

        var result = PromptVariableSubstitutor.Substitute("{{a}}-{{b}}", variables);

        result.Should().Be("1-2");
    }

    [Fact]
    public void Substitute_WhitespaceInsidePlaceholder_StillMatches()
    {
        var result = PromptVariableSubstitutor.Substitute("{{ name }}", new Dictionary<string, string> { ["name"] = "قيمة" });

        result.Should().Be("قيمة");
    }
}
