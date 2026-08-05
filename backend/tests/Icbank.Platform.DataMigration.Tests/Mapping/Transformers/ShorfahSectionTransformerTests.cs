using FluentAssertions;
using Icbank.Platform.DataMigration.Mapping.Dtos;
using Icbank.Platform.DataMigration.Mapping.Transformers;
using Icbank.Platform.DataMigration.Source;
using Icbank.Platform.DataMigration.Tests.Fixtures;
using Xunit;

namespace Icbank.Platform.DataMigration.Tests.Mapping.Transformers;

/// <summary>
/// Exhaustive unit tests for <see cref="ShorfahSectionTransformer"/>, covering the AMBIGUOUS-8
/// nullable-timestamp backfill cascade, the nullable source <c>created_at</c> (DATA-MODEL.md
/// §3.8), and the self-referencing <c>parent_section_id</c> passthrough.
/// </summary>
public sealed class ShorfahSectionTransformerTests
{
    private static readonly DateTime MigrationRunTimestamp = new(2026, 8, 5, 22, 0, 0);

    [Fact]
    public void Transform_AllTimestampsPresent_NoneAreBackfilled()
    {
        SourceRow row = SourceRowFixture.Build(BaseRow());

        MappedShorfahSection result = ShorfahSectionTransformer.Transform(row, MigrationRunTimestamp);

        result.CreatedAtBackfilled.Should().BeFalse();
        result.ContributedAtBackfilled.Should().BeFalse();
        result.ReviewedAtBackfilled.Should().BeFalse();
        result.ApprovedAtBackfilled.Should().BeFalse();
        result.SlaStartsAtBackfilled.Should().BeFalse();
    }

    [Fact]
    public void Transform_CreatedAtNull_BackfillsFromMigrationRunTimestampAndFlagsIt()
    {
        Dictionary<string, object?> values = BaseRow();
        values["created_at"] = null;
        SourceRow row = SourceRowFixture.Build(values);

        MappedShorfahSection result = ShorfahSectionTransformer.Transform(row, MigrationRunTimestamp);

        result.CreatedAtUtc.Should().Be(MigrationRunTimestamp);
        result.CreatedAtBackfilled.Should().BeTrue();
    }

    [Fact]
    public void Transform_ContributedAtNullButReviewedAtPresent_BackfillsFromReviewedAt()
    {
        Dictionary<string, object?> values = BaseRow();
        values["contributed_at"] = null;
        SourceRow row = SourceRowFixture.Build(values);

        MappedShorfahSection result = ShorfahSectionTransformer.Transform(row, MigrationRunTimestamp);

        result.ContributedAtUtc.Should().Be(new DateTime(2026, 1, 2, 10, 0, 0));
        result.ContributedAtBackfilled.Should().BeTrue();
    }

    [Fact]
    public void Transform_AllWorkflowTimestampsNull_AllBackfillFromCreatedAt()
    {
        Dictionary<string, object?> values = BaseRow();
        values["contributed_at"] = null;
        values["reviewed_at"] = null;
        values["approved_at"] = null;
        values["sla_starts_at"] = null;
        SourceRow row = SourceRowFixture.Build(values);

        MappedShorfahSection result = ShorfahSectionTransformer.Transform(row, MigrationRunTimestamp);

        DateTime expectedCreatedAt = new(2025, 12, 31, 23, 0, 0);
        result.ContributedAtUtc.Should().Be(expectedCreatedAt);
        result.ReviewedAtUtc.Should().Be(expectedCreatedAt);
        result.ApprovedAtUtc.Should().Be(expectedCreatedAt);
        result.SlaStartsAtUtc.Should().Be(expectedCreatedAt);
        result.ContributedAtBackfilled.Should().BeTrue();
        result.ReviewedAtBackfilled.Should().BeTrue();
        result.ApprovedAtBackfilled.Should().BeTrue();
        result.SlaStartsAtBackfilled.Should().BeTrue();
    }

    [Fact]
    public void Transform_EverythingNullIncludingCreatedAt_CascadesAllTheWayToMigrationRunTimestamp()
    {
        Dictionary<string, object?> values = BaseRow();
        values["created_at"] = null;
        values["contributed_at"] = null;
        values["reviewed_at"] = null;
        values["approved_at"] = null;
        values["sla_starts_at"] = null;
        SourceRow row = SourceRowFixture.Build(values);

        MappedShorfahSection result = ShorfahSectionTransformer.Transform(row, MigrationRunTimestamp);

        result.CreatedAtUtc.Should().Be(MigrationRunTimestamp);
        result.ContributedAtUtc.Should().Be(MigrationRunTimestamp);
        result.ReviewedAtUtc.Should().Be(MigrationRunTimestamp);
        result.ApprovedAtUtc.Should().Be(MigrationRunTimestamp);
        result.SlaStartsAtUtc.Should().Be(MigrationRunTimestamp);
    }

    [Fact]
    public void Transform_ParentSectionIdPresent_PassesThroughAsSourceId()
    {
        Dictionary<string, object?> values = BaseRow();
        values["parent_section_id"] = 12;
        SourceRow row = SourceRowFixture.Build(values);

        MappedShorfahSection result = ShorfahSectionTransformer.Transform(row, MigrationRunTimestamp);

        result.ParentSectionSourceId.Should().Be(12);
    }

    [Fact]
    public void Transform_ParentSectionIdNull_TopLevelSection()
    {
        SourceRow row = SourceRowFixture.Build(BaseRow());

        MappedShorfahSection result = ShorfahSectionTransformer.Transform(row, MigrationRunTimestamp);

        result.ParentSectionSourceId.Should().BeNull();
    }

    [Fact]
    public void Transform_DisplayOrderNull_DefaultsToZero()
    {
        Dictionary<string, object?> values = BaseRow();
        values["display_order"] = null;
        SourceRow row = SourceRowFixture.Build(values);

        MappedShorfahSection result = ShorfahSectionTransformer.Transform(row, MigrationRunTimestamp);

        result.DisplayOrder.Should().Be(0);
    }

    [Fact]
    public void Transform_SectionTypeAndWorkflowStatusPassThroughAsRawSnakeCaseStrings()
    {
        // The transformer itself does NOT parse the enum -- that is the migrator's job via
        // SnakeCaseEnumParser -- so the DTO carries the raw source string verbatim.
        SourceRow row = SourceRowFixture.Build(BaseRow());

        MappedShorfahSection result = ShorfahSectionTransformer.Transform(row, MigrationRunTimestamp);

        result.SectionType.Should().Be("global_news");
        result.WorkflowStatus.Should().Be("approved");
    }

    [Fact]
    public void Transform_OwnerAndReviewerIdsAllNull_MapToNullNotZero()
    {
        Dictionary<string, object?> values = BaseRow();
        values["owner_user_id"] = null;
        values["contributed_by"] = null;
        values["reviewed_by"] = null;
        values["approved_by"] = null;
        SourceRow row = SourceRowFixture.Build(values);

        MappedShorfahSection result = ShorfahSectionTransformer.Transform(row, MigrationRunTimestamp);

        result.OwnerUserSourceId.Should().BeNull();
        result.ContributedBySourceId.Should().BeNull();
        result.ReviewedBySourceId.Should().BeNull();
        result.ApprovedBySourceId.Should().BeNull();
    }

    [Fact]
    public void Transform_SlaDeadlineNull_MapsToNull()
    {
        Dictionary<string, object?> values = BaseRow();
        values["sla_deadline"] = null;
        SourceRow row = SourceRowFixture.Build(values);

        MappedShorfahSection result = ShorfahSectionTransformer.Transform(row, MigrationRunTimestamp);

        result.SlaDeadlineUtc.Should().BeNull();
    }

    [Fact]
    public void Transform_ContentHtmlCarriedOverVerbatimDespiteDeadWritePathConcern()
    {
        SourceRow row = SourceRowFixture.Build(BaseRow());

        MappedShorfahSection result = ShorfahSectionTransformer.Transform(row, MigrationRunTimestamp);

        result.ContentHtml.Should().Be("<h1>Heading</h1>");
    }

    private static Dictionary<string, object?> BaseRow() => new()
    {
        ["id"] = 55,
        ["issue_id"] = 9,
        ["parent_section_id"] = null,
        ["section_type"] = "global_news",
        ["title_ar"] = "الأخبار العالمية",
        ["description_ar"] = "وصف",
        ["display_order"] = 1,
        ["owner_user_id"] = 4,
        ["owner_role"] = null,
        ["include_in_pdf"] = true,
        ["auto_generate"] = false,
        ["generation_prompt"] = null,
        ["workflow_status"] = "approved",
        ["content_md"] = "# Heading",
        ["content_html"] = "<h1>Heading</h1>",
        ["contributed_by"] = 4,
        ["contributed_at"] = new DateTime(2026, 1, 1, 10, 0, 0),
        ["reviewed_by"] = 5,
        ["reviewed_at"] = new DateTime(2026, 1, 2, 10, 0, 0),
        ["review_notes"] = "Looks good",
        ["approved_by"] = 6,
        ["approved_at"] = new DateTime(2026, 1, 3, 10, 0, 0),
        ["rejection_reason"] = null,
        ["sla_days"] = 7,
        ["sla_starts_at"] = new DateTime(2026, 1, 1, 8, 0, 0),
        ["sla_deadline"] = new DateTime(2026, 1, 8, 8, 0, 0),
        ["created_at"] = new DateTime(2025, 12, 31, 23, 0, 0),
    };
}
