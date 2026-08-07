using FluentAssertions;
using Icbank.Platform.Application.Dashboard;
using Xunit;

namespace Icbank.Platform.UnitTests.Application;

/// <summary>Verifies the ported Arabic/numeric annual-date parser (BUSINESS-RULES.md §9).</summary>
public sealed class ArabicAnnualDateParserTests
{
    [Fact]
    public void Parse_ArabicMonthName_ReturnsMonthAndDay()
    {
        (int Month, int Day)? result = ArabicAnnualDateParser.Parse("17 مايو");

        result.Should().Be((5, 17));
    }

    [Theory]
    [InlineData("1 أبريل", 4, 1)]
    [InlineData("1 ابريل", 4, 1)]
    [InlineData("1 أغسطس", 8, 1)]
    [InlineData("1 اغسطس", 8, 1)]
    [InlineData("1 أكتوبر", 10, 1)]
    [InlineData("1 اكتوبر", 10, 1)]
    public void Parse_AlternateArabicSpellings_ResolvesToSameMonth(string raw, int expectedMonth, int expectedDay)
    {
        (int Month, int Day)? result = ArabicAnnualDateParser.Parse(raw);

        result.Should().Be((expectedMonth, expectedDay));
    }

    [Fact]
    public void Parse_NumericFormat_ReturnsMonthAndDay()
    {
        (int Month, int Day)? result = ArabicAnnualDateParser.Parse("05-17");

        result.Should().Be((5, 17));
    }

    [Fact]
    public void Parse_UnrecognizedFormat_ReturnsNull()
    {
        (int Month, int Day)? result = ArabicAnnualDateParser.Parse("not-a-date");

        result.Should().BeNull();
    }
}
