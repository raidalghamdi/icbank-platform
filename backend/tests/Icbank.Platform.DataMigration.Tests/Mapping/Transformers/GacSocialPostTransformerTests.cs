using FluentAssertions;
using Icbank.Platform.DataMigration.Mapping.Dtos;
using Icbank.Platform.DataMigration.Mapping.Transformers;
using Icbank.Platform.DataMigration.Source;
using Icbank.Platform.DataMigration.Tests.Fixtures;
using Xunit;

namespace Icbank.Platform.DataMigration.Tests.Mapping.Transformers;

/// <summary>
/// Exhaustive unit tests for <see cref="GacSocialPostTransformer"/>, including the
/// media-type default (AMBIGUOUS: source allows null media_type, destination enum has no
/// implicit null) and the new-unique-index key exposed via <see cref="MappedGacSocialPost.UniqueKey"/>.
/// </summary>
public sealed class GacSocialPostTransformerTests
{
    [Fact]
    public void Transform_MapsAllScalarFields()
    {
        SourceRow row = SourceRowFixture.Build(BaseRow());

        MappedGacSocialPost result = GacSocialPostTransformer.Transform(row);

        result.SourceId.Should().Be(15);
        result.Platform.Should().Be("linkedin");
        result.ExternalId.Should().Be("urn:li:share:123456");
        result.ContentAr.Should().Be("محتوى");
        result.ContentEn.Should().Be("Content");
        result.PostUrl.Should().Be("https://linkedin.com/posts/123456");
        result.MediaUrl.Should().Be("https://cdn.example.com/img.png");
        result.MediaType.Should().Be("image");
        result.LikeCount.Should().Be(42);
        result.CommentCount.Should().Be(3);
        result.ShareCount.Should().Be(1);
        result.Account.Should().Be("@icbank");
    }

    [Fact]
    public void Transform_MediaTypeNull_DefaultsToNone()
    {
        Dictionary<string, object?> values = BaseRow();
        values["media_type"] = null;
        SourceRow row = SourceRowFixture.Build(values);

        MappedGacSocialPost result = GacSocialPostTransformer.Transform(row);

        result.MediaType.Should().Be("None");
    }

    [Fact]
    public void Transform_MediaTypeEmptyString_DefaultsToNone()
    {
        Dictionary<string, object?> values = BaseRow();
        values["media_type"] = string.Empty;
        SourceRow row = SourceRowFixture.Build(values);

        MappedGacSocialPost result = GacSocialPostTransformer.Transform(row);

        result.MediaType.Should().Be("None");
    }

    [Fact]
    public void Transform_PostedAtNull_MapsToNull()
    {
        Dictionary<string, object?> values = BaseRow();
        values["posted_at"] = null;
        SourceRow row = SourceRowFixture.Build(values);

        MappedGacSocialPost result = GacSocialPostTransformer.Transform(row);

        result.PostedAt.Should().BeNull();
    }

    [Fact]
    public void Transform_PostedAtPresent_ConvertsToUtcOffsetZero()
    {
        SourceRow row = SourceRowFixture.Build(BaseRow());

        MappedGacSocialPost result = GacSocialPostTransformer.Transform(row);

        result.PostedAt!.Value.Offset.Should().Be(TimeSpan.Zero);
    }

    [Fact]
    public void Transform_MetricsNull_MapToNullNotZero()
    {
        // Hard case: a null metric means "unknown", not "zero engagement" -- must not collapse.
        Dictionary<string, object?> values = BaseRow();
        values["metrics"] = null;
        SourceRow row = SourceRowFixture.Build(values);

        MappedGacSocialPost result = GacSocialPostTransformer.Transform(row);

        result.LikeCount.Should().BeNull();
        result.CommentCount.Should().BeNull();
        result.ShareCount.Should().BeNull();
    }

    [Fact]
    public void Transform_FetchedAtMissing_Throws()
    {
        Dictionary<string, object?> values = BaseRow();
        values.Remove("fetched_at");
        SourceRow row = SourceRowFixture.Build(values);

        Action act = () => GacSocialPostTransformer.Transform(row);

        act.Should().Throw<InvalidOperationException>();
    }

    [Fact]
    public void UniqueKey_ReflectsPlatformAndExternalId()
    {
        SourceRow row = SourceRowFixture.Build(BaseRow());

        MappedGacSocialPost result = GacSocialPostTransformer.Transform(row);

        result.UniqueKey.Should().Be(("linkedin", "urn:li:share:123456"));
    }

    [Theory]
    [InlineData("linkedin")]
    [InlineData("twitter")]
    [InlineData("instagram")]
    [InlineData("youtube")]
    public void Transform_EveryKnownPlatformValue_PassesThroughVerbatim(string platform)
    {
        Dictionary<string, object?> values = BaseRow();
        values["platform"] = platform;
        SourceRow row = SourceRowFixture.Build(values);

        MappedGacSocialPost result = GacSocialPostTransformer.Transform(row);

        result.Platform.Should().Be(platform);
    }

    [Fact]
    public void Transform_ContentArAndEnBothNull_MapToNull()
    {
        Dictionary<string, object?> values = BaseRow();
        values["content_ar"] = null;
        values["content_en"] = null;
        SourceRow row = SourceRowFixture.Build(values);

        MappedGacSocialPost result = GacSocialPostTransformer.Transform(row);

        result.ContentAr.Should().BeNull();
        result.ContentEn.Should().BeNull();
    }

    private static Dictionary<string, object?> BaseRow() => new()
    {
        ["id"] = 15,
        ["platform"] = "linkedin",
        ["external_id"] = "urn:li:share:123456",
        ["content_ar"] = "محتوى",
        ["content_en"] = "Content",
        ["post_url"] = "https://linkedin.com/posts/123456",
        ["media_url"] = "https://cdn.example.com/img.png",
        ["media_type"] = "image",
        ["posted_at"] = new DateTime(2026, 4, 1, 8, 30, 0),
        ["metrics"] = """{"likes":42,"comments":3,"shares":1}""",
        ["account"] = "@icbank",
        ["fetched_at"] = new DateTime(2026, 4, 1, 8, 31, 0),
    };
}
