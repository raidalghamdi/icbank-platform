using FluentAssertions;
using Icbank.Platform.DataMigration.Mapping;
using Xunit;

namespace Icbank.Platform.DataMigration.Tests.Mapping;

/// <summary>
/// Exhaustive unit tests for <see cref="ShorfahTimestampBackfill"/> -- the AMBIGUOUS-8 decision
/// covering every priority tier: own value, earliest sibling, section created_at, migration-run
/// timestamp as absolute last resort.
/// </summary>
public sealed class ShorfahTimestampBackfillTests
{
    private static readonly DateTime SectionCreatedAt = new(2024, 1, 1, 8, 0, 0);
    private static readonly DateTime MigrationRunTimestamp = new(2026, 8, 5, 22, 0, 0);

    [Fact]
    public void Resolve_OwnValuePresent_UsesOwnValueAndIsNotBackfilled()
    {
        var ownValue = new DateTime(2024, 2, 1, 9, 0, 0);

        ShorfahTimestampBackfill.BackfillResult result = ShorfahTimestampBackfill.Resolve(
            ownValue, Array.Empty<DateTime?>(), SectionCreatedAt, MigrationRunTimestamp);

        result.Value.Should().Be(ownValue);
        result.WasBackfilled.Should().BeFalse();
    }

    [Fact]
    public void Resolve_OwnValuePresent_IgnoresSiblingsEvenIfEarlier()
    {
        var ownValue = new DateTime(2024, 2, 1, 9, 0, 0);
        var earlierSibling = new DateTime(2024, 1, 15, 0, 0, 0);

        ShorfahTimestampBackfill.BackfillResult result = ShorfahTimestampBackfill.Resolve(
            ownValue, new DateTime?[] { earlierSibling }, SectionCreatedAt, MigrationRunTimestamp);

        result.Value.Should().Be(ownValue);
        result.WasBackfilled.Should().BeFalse();
    }

    [Fact]
    public void Resolve_OwnValueNullOneSiblingPresent_UsesSiblingAndFlagsBackfilled()
    {
        var sibling = new DateTime(2024, 3, 1, 10, 0, 0);

        ShorfahTimestampBackfill.BackfillResult result = ShorfahTimestampBackfill.Resolve(
            null, new DateTime?[] { sibling }, SectionCreatedAt, MigrationRunTimestamp);

        result.Value.Should().Be(sibling);
        result.WasBackfilled.Should().BeTrue();
    }

    [Fact]
    public void Resolve_OwnValueNullMultipleSiblings_UsesEarliestSibling()
    {
        var later = new DateTime(2024, 5, 1, 0, 0, 0);
        var earlier = new DateTime(2024, 3, 1, 0, 0, 0);

        ShorfahTimestampBackfill.BackfillResult result = ShorfahTimestampBackfill.Resolve(
            null, new DateTime?[] { later, earlier }, SectionCreatedAt, MigrationRunTimestamp);

        result.Value.Should().Be(earlier);
        result.WasBackfilled.Should().BeTrue();
    }

    [Fact]
    public void Resolve_OwnValueNullSiblingsAllNull_FallsBackToSectionCreatedAt()
    {
        ShorfahTimestampBackfill.BackfillResult result = ShorfahTimestampBackfill.Resolve(
            null, new DateTime?[] { null, null }, SectionCreatedAt, MigrationRunTimestamp);

        result.Value.Should().Be(SectionCreatedAt);
        result.WasBackfilled.Should().BeTrue();
    }

    [Fact]
    public void Resolve_OwnValueNullNoSiblings_FallsBackToSectionCreatedAt()
    {
        ShorfahTimestampBackfill.BackfillResult result = ShorfahTimestampBackfill.Resolve(
            null, Array.Empty<DateTime?>(), SectionCreatedAt, MigrationRunTimestamp);

        result.Value.Should().Be(SectionCreatedAt);
        result.WasBackfilled.Should().BeTrue();
    }

    [Fact]
    public void Resolve_EverythingUnavailableIncludingSectionCreatedAt_FallsBackToMigrationRunTimestamp()
    {
        ShorfahTimestampBackfill.BackfillResult result = ShorfahTimestampBackfill.Resolve(
            null, Array.Empty<DateTime?>(), default, MigrationRunTimestamp);

        result.Value.Should().Be(MigrationRunTimestamp);
        result.WasBackfilled.Should().BeTrue();
    }

    [Fact]
    public void Resolve_SiblingEqualsDefaultDateTime_IsTreatedAsUnusableAndFallsThrough()
    {
        // default(DateTime) is 0001-01-01 -- a real value would never legitimately be this, so
        // Resolve treats it the same as "no sibling" rather than backfilling from year 1.
        ShorfahTimestampBackfill.BackfillResult result = ShorfahTimestampBackfill.Resolve(
            null, new DateTime?[] { default }, SectionCreatedAt, MigrationRunTimestamp);

        result.Value.Should().Be(SectionCreatedAt);
        result.WasBackfilled.Should().BeTrue();
    }
}
