using FluentAssertions;
using Icbank.Platform.DataMigration.Mapping;
using Xunit;

namespace Icbank.Platform.DataMigration.Tests.Mapping;

/// <summary>
/// Exhaustive unit tests for <see cref="TimestampConverter"/> -- the single place the
/// "naive-or-UTC?" decision (task requirement 3) is made for every business-specific
/// <c>datetimeoffset(3)</c> column.
/// </summary>
public sealed class TimestampConverterTests
{
    [Fact]
    public void ToDestinationOffset_NaiveDateTime_TreatsAsUtcWithZeroOffset()
    {
        var raw = new DateTime(2024, 6, 1, 12, 0, 0, DateTimeKind.Unspecified);

        DateTimeOffset result = TimestampConverter.ToDestinationOffset(raw);

        result.Offset.Should().Be(TimeSpan.Zero);
        result.Year.Should().Be(2024);
        result.Month.Should().Be(6);
        result.Day.Should().Be(1);
        result.Hour.Should().Be(12);
    }

    [Fact]
    public void ToDestinationOffset_UtcKindDateTime_PreservesInstant()
    {
        var raw = new DateTime(2024, 6, 1, 12, 0, 0, DateTimeKind.Utc);

        DateTimeOffset result = TimestampConverter.ToDestinationOffset(raw);

        result.Should().Be(new DateTimeOffset(2024, 6, 1, 12, 0, 0, TimeSpan.Zero));
    }

    [Fact]
    public void ToDestinationOffset_TruncatesSubMillisecondPrecision()
    {
        // 100-nanosecond ticks beyond whole milliseconds must be dropped to match datetimeoffset(3).
        DateTime raw = new DateTime(2024, 6, 1, 12, 0, 0, 500).AddTicks(4567);

        DateTimeOffset result = TimestampConverter.ToDestinationOffset(raw);

        result.Millisecond.Should().Be(500);
        (result.Ticks % TimeSpan.TicksPerMillisecond).Should().Be(0);
    }

    [Fact]
    public void ToDestinationOffset_NullableNull_ReturnsNull()
    {
        DateTime? raw = null;

        TimestampConverter.ToDestinationOffset(raw).Should().BeNull();
    }

    [Fact]
    public void ToDestinationOffset_NullableWithValue_ConvertsSameAsNonNullableOverload()
    {
        DateTime? raw = new DateTime(2024, 1, 1, 0, 0, 0);

        DateTimeOffset? result = TimestampConverter.ToDestinationOffset(raw);

        result.Should().Be(TimestampConverter.ToDestinationOffset(raw.Value));
    }

    [Fact]
    public void ToDestinationOffset_MidnightBoundary_HandlesCorrectly()
    {
        var raw = new DateTime(2024, 1, 1, 0, 0, 0, 0);

        DateTimeOffset result = TimestampConverter.ToDestinationOffset(raw);

        result.Should().Be(new DateTimeOffset(2024, 1, 1, 0, 0, 0, TimeSpan.Zero));
    }

    [Fact]
    public void ToDestinationOffset_YearBoundary_HandlesCorrectly()
    {
        var raw = new DateTime(2023, 12, 31, 23, 59, 59, 999);

        DateTimeOffset result = TimestampConverter.ToDestinationOffset(raw);

        result.Should().Be(new DateTimeOffset(2023, 12, 31, 23, 59, 59, 999, TimeSpan.Zero));
    }

    [Fact]
    public void ToRiyadhDisplay_AddsThreeHourOffsetForDisplayOnly()
    {
        var utcValue = new DateTimeOffset(2024, 6, 1, 9, 0, 0, TimeSpan.Zero);

        DateTimeOffset displayed = TimestampConverter.ToRiyadhDisplay(utcValue);

        displayed.Offset.Should().Be(TimeSpan.FromHours(3));
        displayed.Hour.Should().Be(12);
        displayed.Should().Be(utcValue); // same instant, different representation
    }

    [Fact]
    public void ToRiyadhDisplay_MidnightUtc_RollsToNextDayInRiyadh()
    {
        var utcValue = new DateTimeOffset(2024, 6, 1, 22, 30, 0, TimeSpan.Zero);

        DateTimeOffset displayed = TimestampConverter.ToRiyadhDisplay(utcValue);

        displayed.Day.Should().Be(2);
        displayed.Hour.Should().Be(1);
        displayed.Minute.Should().Be(30);
    }
}
