using FluentAssertions;
using Icbank.Platform.DataMigration.Validation;
using Xunit;

namespace Icbank.Platform.DataMigration.Tests.Validation;

/// <summary>
/// Exhaustive unit tests for <see cref="DuplicateKeyDetector"/> -- the AMBIGUOUS-7 detector used
/// against the new <c>gac_social_posts(platform, external_id)</c> unique index the source data
/// may violate (task requirement 3/4: "detect and report duplicates rather than crashing").
/// </summary>
public sealed class DuplicateKeyDetectorTests
{
    private static readonly int[] ExpectedFirstAndSecondSourceIds = { 1, 2 };

    [Fact]
    public void FindDuplicates_NoDuplicateKeys_ReturnsEmpty()
    {
        Item[] items = new[]
        {
            new Item(1, "linkedin", "abc"),
            new Item(2, "twitter", "abc"),
            new Item(3, "linkedin", "def"),
        };

        IReadOnlyList<DuplicateKeyGroup<(string, string)>> result = DuplicateKeyDetector.FindDuplicates(
            items, i => (i.Platform, i.ExternalId), i => i.SourceId);

        result.Should().BeEmpty();
    }

    [Fact]
    public void FindDuplicates_OneDuplicatePair_ReturnsOneGroupWithBothSourceIds()
    {
        Item[] items = new[]
        {
            new Item(1, "linkedin", "abc"),
            new Item(2, "linkedin", "abc"),
        };

        IReadOnlyList<DuplicateKeyGroup<(string, string)>> result = DuplicateKeyDetector.FindDuplicates(
            items, i => (i.Platform, i.ExternalId), i => i.SourceId);

        result.Should().HaveCount(1);
        result[0].Key.Should().Be(("linkedin", "abc"));
        result[0].SourceIds.Should().BeEquivalentTo(ExpectedFirstAndSecondSourceIds, options => options.WithStrictOrdering());
    }

    [Fact]
    public void FindDuplicates_ThreeRowsShareOneKey_ReturnsAllThreeSourceIds()
    {
        Item[] items = new[]
        {
            new Item(1, "twitter", "x"),
            new Item(2, "twitter", "x"),
            new Item(3, "twitter", "x"),
        };

        IReadOnlyList<DuplicateKeyGroup<(string, string)>> result = DuplicateKeyDetector.FindDuplicates(
            items, i => (i.Platform, i.ExternalId), i => i.SourceId);

        result.Should().HaveCount(1);
        result[0].SourceIds.Should().HaveCount(3);
    }

    [Fact]
    public void FindDuplicates_MultipleIndependentDuplicateGroups_ReturnsEachGroup()
    {
        Item[] items = new[]
        {
            new Item(1, "linkedin", "a"),
            new Item(2, "linkedin", "a"),
            new Item(3, "twitter", "b"),
            new Item(4, "twitter", "b"),
            new Item(5, "instagram", "c"), // not duplicated
        };

        IReadOnlyList<DuplicateKeyGroup<(string, string)>> result = DuplicateKeyDetector.FindDuplicates(
            items, i => (i.Platform, i.ExternalId), i => i.SourceId);

        result.Should().HaveCount(2);
    }

    [Fact]
    public void FindDuplicates_SamePlatformDifferentExternalId_IsNotADuplicate()
    {
        Item[] items = new[]
        {
            new Item(1, "linkedin", "a"),
            new Item(2, "linkedin", "b"),
        };

        IReadOnlyList<DuplicateKeyGroup<(string, string)>> result = DuplicateKeyDetector.FindDuplicates(
            items, i => (i.Platform, i.ExternalId), i => i.SourceId);

        result.Should().BeEmpty();
    }

    [Fact]
    public void FindDuplicates_SameExternalIdDifferentPlatform_IsNotADuplicate()
    {
        // Realistic hard case: two platforms can coincidentally share the same numeric/opaque
        // external id -- the composite key means this must NOT be flagged.
        Item[] items = new[]
        {
            new Item(1, "linkedin", "12345"),
            new Item(2, "twitter", "12345"),
        };

        IReadOnlyList<DuplicateKeyGroup<(string, string)>> result = DuplicateKeyDetector.FindDuplicates(
            items, i => (i.Platform, i.ExternalId), i => i.SourceId);

        result.Should().BeEmpty();
    }

    [Fact]
    public void FindDuplicates_EmptyInput_ReturnsEmpty()
    {
        IReadOnlyList<DuplicateKeyGroup<(string, string)>> result = DuplicateKeyDetector.FindDuplicates(
            Array.Empty<Item>(), i => (i.Platform, i.ExternalId), i => i.SourceId);

        result.Should().BeEmpty();
    }

    [Fact]
    public void FindDuplicates_CaseSensitiveKeySelector_TreatsDifferentCasingAsDistinct()
    {
        // The key selector, not the detector, decides case sensitivity; string tuple equality is
        // ordinal by default, so "LinkedIn" and "linkedin" are distinct keys here.
        Item[] items = new[]
        {
            new Item(1, "linkedin", "a"),
            new Item(2, "LinkedIn", "a"),
        };

        IReadOnlyList<DuplicateKeyGroup<(string, string)>> result = DuplicateKeyDetector.FindDuplicates(
            items, i => (i.Platform, i.ExternalId), i => i.SourceId);

        result.Should().BeEmpty();
    }

    private sealed record Item(int SourceId, string Platform, string ExternalId);
}
