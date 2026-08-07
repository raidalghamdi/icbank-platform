using FluentAssertions;
using Icbank.Platform.DataMigration.Mapping.Dtos;
using Icbank.Platform.DataMigration.Mapping.Transformers;
using Icbank.Platform.DataMigration.Source;
using Icbank.Platform.DataMigration.Tests.Fixtures;
using Xunit;

namespace Icbank.Platform.DataMigration.Tests.Mapping.Transformers;

/// <summary>
/// Exhaustive unit tests for <see cref="ShorfahIssueTransformer"/>, covering the AMBIGUOUS-8
/// nullable-audit-timestamp backfill (source <c>created_at</c>/<c>updated_at</c> are nullable
/// despite <c>defaultNow()</c>) and straightforward field passthrough.
/// </summary>
public sealed class ShorfahIssueTransformerTests
{
    private static readonly DateTime MigrationRunTimestamp = new(2026, 8, 5, 22, 0, 0);

    [Fact]
    public void Transform_AllTimestampsPresent_NoneAreBackfilled()
    {
        SourceRow row = SourceRowFixture.Build(BaseRow());

        MappedShorfahIssue result = ShorfahIssueTransformer.Transform(row, MigrationRunTimestamp);

        result.CreatedAtBackfilled.Should().BeFalse();
        result.UpdatedAtBackfilled.Should().BeFalse();
    }

    [Fact]
    public void Transform_CreatedAtNull_BackfillsFromMigrationRunTimestampAndFlagsIt()
    {
        Dictionary<string, object?> values = BaseRow();
        values["created_at"] = null;
        SourceRow row = SourceRowFixture.Build(values);

        MappedShorfahIssue result = ShorfahIssueTransformer.Transform(row, MigrationRunTimestamp);

        result.CreatedAtUtc.Should().Be(MigrationRunTimestamp);
        result.CreatedAtBackfilled.Should().BeTrue();
    }

    [Fact]
    public void Transform_UpdatedAtNullButCreatedAtPresent_BackfillsFromCreatedAt()
    {
        Dictionary<string, object?> values = BaseRow();
        values["updated_at"] = null;
        SourceRow row = SourceRowFixture.Build(values);

        MappedShorfahIssue result = ShorfahIssueTransformer.Transform(row, MigrationRunTimestamp);

        result.UpdatedAtUtc.Should().Be(new DateTime(2026, 1, 1, 9, 0, 0));
        result.UpdatedAtBackfilled.Should().BeTrue();
    }

    [Fact]
    public void Transform_BothCreatedAndUpdatedAtNull_BothCascadeToMigrationRunTimestamp()
    {
        Dictionary<string, object?> values = BaseRow();
        values["created_at"] = null;
        values["updated_at"] = null;
        SourceRow row = SourceRowFixture.Build(values);

        MappedShorfahIssue result = ShorfahIssueTransformer.Transform(row, MigrationRunTimestamp);

        result.CreatedAtUtc.Should().Be(MigrationRunTimestamp);
        result.UpdatedAtUtc.Should().Be(MigrationRunTimestamp);
        result.CreatedAtBackfilled.Should().BeTrue();
        result.UpdatedAtBackfilled.Should().BeTrue();
    }

    [Fact]
    public void Transform_CreatedBySourceIdNull_MapsToNullNotZero()
    {
        Dictionary<string, object?> values = BaseRow();
        values["created_by"] = null;
        SourceRow row = SourceRowFixture.Build(values);

        MappedShorfahIssue result = ShorfahIssueTransformer.Transform(row, MigrationRunTimestamp);

        result.CreatedBySourceId.Should().BeNull();
    }

    [Fact]
    public void Transform_PublishedFieldsNull_MapToNull()
    {
        Dictionary<string, object?> values = BaseRow();
        values["published_pdf_url"] = null;
        values["published_at"] = null;
        SourceRow row = SourceRowFixture.Build(values);

        MappedShorfahIssue result = ShorfahIssueTransformer.Transform(row, MigrationRunTimestamp);

        result.PublishedPdfUrl.Should().BeNull();
        result.PublishedAtUtc.Should().BeNull();
    }

    [Fact]
    public void Transform_StatusPassesThroughAsRawSnakeCaseString()
    {
        // The transformer does NOT parse the enum -- that is the migrator's job via
        // SnakeCaseEnumParser -- so the DTO carries the raw source string verbatim.
        SourceRow row = SourceRowFixture.Build(BaseRow());

        MappedShorfahIssue result = ShorfahIssueTransformer.Transform(row, MigrationRunTimestamp);

        result.Status.Should().Be("in_review");
    }

    [Fact]
    public void Transform_IssueNoAndTitlePassThrough()
    {
        SourceRow row = SourceRowFixture.Build(BaseRow());

        MappedShorfahIssue result = ShorfahIssueTransformer.Transform(row, MigrationRunTimestamp);

        result.IssueNo.Should().Be(7);
        result.TitleAr.Should().Be("عدد أغسطس");
        result.Month.Should().Be(8);
        result.Year.Should().Be(2026);
    }

    private static Dictionary<string, object?> BaseRow() => new()
    {
        ["id"] = 3,
        ["issue_no"] = 7,
        ["title_ar"] = "عدد أغسطس",
        ["subtitle_ar"] = null,
        ["month"] = 8,
        ["year"] = 2026,
        ["cover_image_url"] = "https://example.com/cover.png",
        ["editor_letter"] = "Welcome",
        ["contributions_open_at"] = new DateTime(2026, 7, 1, 0, 0, 0),
        ["contributions_close_at"] = new DateTime(2026, 7, 25, 0, 0, 0),
        ["status"] = "in_review",
        ["published_pdf_url"] = null,
        ["published_at"] = null,
        ["created_by"] = 2,
        ["created_at"] = new DateTime(2026, 1, 1, 9, 0, 0),
        ["updated_at"] = new DateTime(2026, 1, 1, 9, 30, 0),
    };
}
