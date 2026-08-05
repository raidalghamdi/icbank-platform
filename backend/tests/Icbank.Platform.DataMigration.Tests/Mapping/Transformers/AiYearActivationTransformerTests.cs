using FluentAssertions;
using Icbank.Platform.DataMigration.Mapping.Dtos;
using Icbank.Platform.DataMigration.Mapping.Transformers;
using Icbank.Platform.DataMigration.Source;
using Icbank.Platform.DataMigration.Tests.Fixtures;
using Xunit;

namespace Icbank.Platform.DataMigration.Tests.Mapping.Transformers;

/// <summary>
/// Exhaustive unit tests for <see cref="AiYearActivationTransformer"/>, focused on the
/// AMBIGUOUS-2 <c>channels</c> text[] fan-out and its dual input shape (native Npgsql
/// <c>string[]</c> for a live connection vs. a JSON-string fixture, per the transformer's own
/// documented support for both).
/// </summary>
public sealed class AiYearActivationTransformerTests
{
    private static readonly string[] ExpectedTwitterLinkedinChannels = { "twitter", "linkedin" };
    private static readonly string[] ExpectedAiLaunchTags = { "ai", "launch" };
    private static readonly string[] ExpectedOneTwoThreeTags = { "one", "two", "three" };

    [Fact]
    public void Transform_MapsAllScalarFields()
    {
        SourceRow row = SourceRowFixture.Build(BaseRow());

        MappedAiYearActivation result = AiYearActivationTransformer.Transform(row);

        result.SourceId.Should().Be(3);
        result.Title.Should().Be("Launch Event");
        result.Month.Should().Be(9);
        result.Year.Should().Be(2026);
        result.ActivationDate.Should().Be("2026-09-15");
        result.Type.Should().Be("conference");
        result.Description.Should().Be("Flagship AI Year launch");
        result.Status.Should().Be("published");
        result.Reach.Should().Be(100000);
        result.Engagement.Should().Be(5000);
        result.Notes.Should().Be("Coordinate with PR");
    }

    [Fact]
    public void Transform_YearMissing_DefaultsTo2026()
    {
        Dictionary<string, object?> values = BaseRow();
        values.Remove("year");
        SourceRow row = SourceRowFixture.Build(values);

        MappedAiYearActivation result = AiYearActivationTransformer.Transform(row);

        result.Year.Should().Be(2026);
    }

    [Fact]
    public void Transform_StatusNull_DefaultsToPublished()
    {
        Dictionary<string, object?> values = BaseRow();
        values["status"] = null;
        SourceRow row = SourceRowFixture.Build(values);

        MappedAiYearActivation result = AiYearActivationTransformer.Transform(row);

        result.Status.Should().Be("Published");
    }

    [Fact]
    public void Transform_ChannelsAsNativeStringArray_FansOutInOrder()
    {
        SourceRow row = SourceRowFixture.Build(BaseRow());

        MappedAiYearActivation result = AiYearActivationTransformer.Transform(row);

        result.Channels.Should().BeEquivalentTo(ExpectedTwitterLinkedinChannels, options => options.WithStrictOrdering());
    }

    [Fact]
    public void Transform_ChannelsAsJsonStringFixture_ParsesSameAsNativeArray()
    {
        Dictionary<string, object?> values = BaseRow();
        values["channels"] = "[\"twitter\",\"linkedin\"]";
        SourceRow row = SourceRowFixture.Build(values);

        MappedAiYearActivation result = AiYearActivationTransformer.Transform(row);

        result.Channels.Should().BeEquivalentTo(ExpectedTwitterLinkedinChannels, options => options.WithStrictOrdering());
    }

    [Fact]
    public void Transform_ChannelsNull_ReturnsEmptyList()
    {
        Dictionary<string, object?> values = BaseRow();
        values["channels"] = null;
        SourceRow row = SourceRowFixture.Build(values);

        MappedAiYearActivation result = AiYearActivationTransformer.Transform(row);

        result.Channels.Should().BeEmpty();
    }

    [Fact]
    public void Transform_ChannelsEmptyArray_ReturnsEmptyList()
    {
        Dictionary<string, object?> values = BaseRow();
        values["channels"] = Array.Empty<string>();
        SourceRow row = SourceRowFixture.Build(values);

        MappedAiYearActivation result = AiYearActivationTransformer.Transform(row);

        result.Channels.Should().BeEmpty();
    }

    [Fact]
    public void Transform_DuplicateChannelsCaseInsensitive_AreCollapsedToOne()
    {
        Dictionary<string, object?> values = BaseRow();
        values["channels"] = new[] { "Twitter", "twitter", "TWITTER", "linkedin" };
        SourceRow row = SourceRowFixture.Build(values);

        MappedAiYearActivation result = AiYearActivationTransformer.Transform(row);

        result.Channels.Should().HaveCount(2);
    }

    [Fact]
    public void Transform_TagsAsJsonStringFixture_ParsesToList()
    {
        SourceRow row = SourceRowFixture.Build(BaseRow());

        MappedAiYearActivation result = AiYearActivationTransformer.Transform(row);

        result.Tags.Should().BeEquivalentTo(ExpectedAiLaunchTags, options => options.WithStrictOrdering());
    }

    [Fact]
    public void Transform_TagsNull_ReturnsEmptyList()
    {
        Dictionary<string, object?> values = BaseRow();
        values["tags"] = null;
        SourceRow row = SourceRowFixture.Build(values);

        MappedAiYearActivation result = AiYearActivationTransformer.Transform(row);

        result.Tags.Should().BeEmpty();
    }

    [Fact]
    public void Transform_TagsAsNativeStringArray_AlsoSupported()
    {
        Dictionary<string, object?> values = BaseRow();
        values["tags"] = new[] { "one", "two", "three" };
        SourceRow row = SourceRowFixture.Build(values);

        MappedAiYearActivation result = AiYearActivationTransformer.Transform(row);

        result.Tags.Should().BeEquivalentTo(ExpectedOneTwoThreeTags, options => options.WithStrictOrdering());
    }

    [Fact]
    public void Transform_ActivationDateAndDescriptionNull_MapToNull()
    {
        Dictionary<string, object?> values = BaseRow();
        values["activation_date"] = null;
        values["description"] = null;
        values["notes"] = null;
        SourceRow row = SourceRowFixture.Build(values);

        MappedAiYearActivation result = AiYearActivationTransformer.Transform(row);

        result.ActivationDate.Should().BeNull();
        result.Description.Should().BeNull();
        result.Notes.Should().BeNull();
    }

    [Fact]
    public void Transform_CreatedAtMissing_Throws()
    {
        Dictionary<string, object?> values = BaseRow();
        values.Remove("created_at");
        SourceRow row = SourceRowFixture.Build(values);

        Action act = () => AiYearActivationTransformer.Transform(row);

        act.Should().Throw<InvalidOperationException>();
    }

    private static Dictionary<string, object?> BaseRow() => new()
    {
        ["id"] = 3,
        ["title"] = "Launch Event",
        ["month"] = 9,
        ["year"] = 2026,
        ["activation_date"] = "2026-09-15",
        ["type"] = "conference",
        ["description"] = "Flagship AI Year launch",
        ["tags"] = "[\"ai\",\"launch\"]",
        ["status"] = "published",
        ["reach"] = 100000,
        ["engagement"] = 5000,
        ["notes"] = "Coordinate with PR",
        ["channels"] = new[] { "twitter", "linkedin" },
        ["created_at"] = new DateTime(2026, 1, 5, 9, 0, 0),
    };
}
