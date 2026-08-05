using FluentAssertions;
using Icbank.Platform.Application.InternationalDays;
using Xunit;

namespace Icbank.Platform.UnitTests.Application.InternationalDays;

/// <summary>Verifies the verbatim prompt substitution rule (BUSINESS-RULES.md §4.2).</summary>
public sealed class InternationalDaySearchPromptTemplateTests
{
    [Fact]
    public void Build_SubstitutesDayNameAndYearWindow()
    {
        var prompt = InternationalDaySearchPromptTemplate.Build("اليوم العالمي للغة العربية", 2026);

        prompt.Should().Contain("اليوم العالمي للغة العربية");
        prompt.Should().Contain("عام 2026 بالعربية");
        prompt.Should().Contain("اجمع تفعيلات من الأعوام 2024 و2025 و2026 فقط");
        prompt.Should().Contain("\"year\": 2025");
    }

    [Fact]
    public void Build_ContainsMandatedCountInstructions()
    {
        var prompt = InternationalDaySearchPromptTemplate.Build("Test Day", 2026);

        prompt.Should().Contain("8 إلى 15 تفعيلاً");
        prompt.Should().Contain("3-5 أمثلة بصرية");
        prompt.Should().Contain("5 أفكار تفعيل مقترحة");
    }
}
