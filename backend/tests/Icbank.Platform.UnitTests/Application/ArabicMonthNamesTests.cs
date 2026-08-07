using FluentAssertions;
using Icbank.Platform.Application.Shorfah;
using Xunit;

namespace Icbank.Platform.UnitTests.Application;

/// <summary>Proves <see cref="ArabicMonthNames"/> matches the Node source's <c>arabicMonths</c> array exactly.</summary>
public sealed class ArabicMonthNamesTests
{
    [Theory]
    [InlineData(1, "يناير")]
    [InlineData(8, "أغسطس")]
    [InlineData(12, "ديسمبر")]
    public void For_ValidMonth_ReturnsArabicName(int month, string expected)
    {
        ArabicMonthNames.For(month).Should().Be(expected);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(13)]
    public void For_OutOfRangeMonth_FallsBackToNumericString(int month)
    {
        ArabicMonthNames.For(month).Should().Be(month.ToString(System.Globalization.CultureInfo.InvariantCulture));
    }
}
