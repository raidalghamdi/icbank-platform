using FluentAssertions;
using Icbank.Platform.Application.Shorfah;
using Icbank.Platform.Domain.Shorfah;
using Icbank.Platform.Infrastructure.Seeding;
using Xunit;

namespace Icbank.Platform.UnitTests.Infrastructure.Seeding;

/// <summary>
/// Verifies the startup reconciler brings issues created before the catalogue restructure in line
/// with it: old titles are corrected, missing paragraphs appear, an empty dropped paragraph is
/// deleted, a dropped paragraph somebody wrote into is kept, published issues are frozen, and a
/// second run is a no-op.
/// </summary>
public sealed class ShorfahCanonicalSectionReconcilerTests
{
    private static readonly Dictionary<ShorfahSectionType, int> SlaDefaults = new()
    {
        [ShorfahSectionType.News] = 5,
        [ShorfahSectionType.Settlements] = 9,
    };

    [Fact]
    public void Reconcile_ExistingParagraphWithOldWording_IsUpdatedInPlace()
    {
        ShorfahSection news = LegacySection(ShorfahSectionType.News, "أخبارنا", "تعريف قديم", 20);
        ShorfahIssue issue = Issue(ShorfahIssueStatus.Collecting, news);

        ShorfahSectionReconciliation plan = ShorfahCanonicalSectionReconciler.Reconcile(issue, SlaDefaults);

        plan.Updated.Should().Contain(news);
        news.TitleAr.Should().Be("أخبار المنافسة");
        news.DescriptionAr.Should().Be(ShorfahCanonicalSections.Templates[0].DescriptionAr);
        news.DisplayOrder.Should().Be(10);
    }

    [Fact]
    public void Reconcile_ExistingParagraph_KeepsContributedWorkAndWorkflowState()
    {
        ShorfahSection news = LegacySection(ShorfahSectionType.News, "أخبارنا", "تعريف قديم", 20);
        news.ContentMd = "محتوى المحرر";
        news.WorkflowStatus = ShorfahWorkflowStatus.Approved;
        news.SlaDays = 3;
        news.IncludeInPdf = false;
        news.ContributedByUserId = 7;
        news.ApprovedByUserId = 9;
        ShorfahIssue issue = Issue(ShorfahIssueStatus.InReview, news);

        ShorfahCanonicalSectionReconciler.Reconcile(issue, SlaDefaults);

        news.ContentMd.Should().Be("محتوى المحرر");
        news.WorkflowStatus.Should().Be(ShorfahWorkflowStatus.Approved);
        news.SlaDays.Should().Be(3);
        news.IncludeInPdf.Should().BeFalse();
        news.ContributedByUserId.Should().Be(7);
        news.ApprovedByUserId.Should().Be(9);
    }

    [Fact]
    public void Reconcile_MissingCanonicalParagraph_IsInsertedAsPendingContribution()
    {
        ShorfahIssue issue = Issue(ShorfahIssueStatus.Collecting, CanonicalSections().ToArray());
        ShorfahSection settlements = issue.Sections.First(section => section.SectionType == ShorfahSectionType.Settlements);
        issue.Sections.Remove(settlements);

        ShorfahSectionReconciliation plan = ShorfahCanonicalSectionReconciler.Reconcile(issue, SlaDefaults);

        ShorfahSection inserted = plan.Inserted.Should().ContainSingle().Subject;
        inserted.SectionType.Should().Be(ShorfahSectionType.Settlements);
        inserted.IssueId.Should().Be(issue.Id);
        inserted.TitleAr.Should().Be("تسويات");
        inserted.DisplayOrder.Should().Be(100);
        inserted.WorkflowStatus.Should().Be(ShorfahWorkflowStatus.PendingContribution);
        inserted.IncludeInPdf.Should().BeTrue();
        inserted.SlaDays.Should().Be(9);
    }

    [Fact]
    public void Reconcile_MissingParagraphWithNoSlaDefault_FallsBackToSevenDays()
    {
        ShorfahIssue issue = Issue(ShorfahIssueStatus.Collecting);

        ShorfahSectionReconciliation plan = ShorfahCanonicalSectionReconciler.Reconcile(issue, SlaDefaults);

        plan.Inserted.Should().HaveCount(ShorfahCanonicalSections.Templates.Count);
        plan.Inserted.First(section => section.SectionType == ShorfahSectionType.CaseOfMonth).SlaDays.Should()
            .Be(ShorfahSectionSeeder.DefaultSlaDays);
    }

    [Fact]
    public void Reconcile_LegacyThirteenSectionIssue_GainsTheSixNewParagraphs()
    {
        ShorfahIssue issue = LegacyThirteenSectionIssue();

        ShorfahSectionReconciliation plan = ShorfahCanonicalSectionReconciler.Reconcile(issue, SlaDefaults);

        plan.Inserted.Select(section => section.SectionType).Should().BeEquivalentTo(new[]
        {
            ShorfahSectionType.EconomicStudy,
            ShorfahSectionType.CaseOfMonth,
            ShorfahSectionType.CourtSessions,
            ShorfahSectionType.MonopolyComplaints,
            ShorfahSectionType.Settlements,
            ShorfahSectionType.CompetitionInMonth,
        });
    }

    [Fact]
    public void Reconcile_EmptyDroppedParagraph_IsDeleted()
    {
        ShorfahSection globalNews = LegacySection(ShorfahSectionType.GlobalNews, "أخبار دولية", "تعريف قديم", 10);
        ShorfahIssue issue = Issue(ShorfahIssueStatus.Collecting, globalNews);

        ShorfahSectionReconciliation plan = ShorfahCanonicalSectionReconciler.Reconcile(issue, SlaDefaults);

        plan.Removed.Should().ContainSingle().Which.Should().BeSameAs(globalNews);
    }

    [Fact]
    public void Reconcile_DroppedParagraphWithContent_IsKeptAndPushedToTheEnd()
    {
        ShorfahSection globalNews = LegacySection(ShorfahSectionType.GlobalNews, "أخبار دولية", "تعريف قديم", 10);
        globalNews.ContentMd = "خبر دولي كتبه المحرر";
        ShorfahIssue issue = Issue(ShorfahIssueStatus.Collecting, globalNews);

        ShorfahSectionReconciliation plan = ShorfahCanonicalSectionReconciler.Reconcile(issue, SlaDefaults);

        plan.Removed.Should().BeEmpty();
        plan.Updated.Should().Contain(globalNews);
        globalNews.ContentMd.Should().Be("خبر دولي كتبه المحرر");
        globalNews.TitleAr.Should().Be("أخبار دولية");
        globalNews.DisplayOrder.Should().Be(190);
    }

    [Fact]
    public void Reconcile_DroppedParagraphWithDependentRows_IsKept()
    {
        ShorfahSection globalNews = LegacySection(ShorfahSectionType.GlobalNews, "أخبار دولية", "تعريف قديم", 10);
        globalNews.Reminders.Add(new ShorfahReminder());
        ShorfahIssue issue = Issue(ShorfahIssueStatus.Collecting, globalNews);

        ShorfahSectionReconciliation plan = ShorfahCanonicalSectionReconciler.Reconcile(issue, SlaDefaults);

        plan.Removed.Should().BeEmpty();
        globalNews.DisplayOrder.Should().Be(190);
    }

    [Fact]
    public void Reconcile_PublishedIssue_IsLeftCompletelyUntouched()
    {
        ShorfahSection news = LegacySection(ShorfahSectionType.News, "أخبارنا", "تعريف قديم", 20);
        ShorfahSection globalNews = LegacySection(ShorfahSectionType.GlobalNews, "أخبار دولية", "تعريف قديم", 10);
        ShorfahIssue issue = Issue(ShorfahIssueStatus.Published, news, globalNews);

        ShorfahSectionReconciliation plan = ShorfahCanonicalSectionReconciler.Reconcile(issue, SlaDefaults);

        plan.HasChanges.Should().BeFalse();
        plan.Inserted.Should().BeEmpty();
        plan.Removed.Should().BeEmpty();
        news.TitleAr.Should().Be("أخبارنا");
        news.DisplayOrder.Should().Be(20);
        globalNews.TitleAr.Should().Be("أخبار دولية");
    }

    [Fact]
    public void Reconcile_AlreadyReconciledIssue_ReportsNoChanges()
    {
        ShorfahIssue issue = Issue(ShorfahIssueStatus.Collecting, CanonicalSections().ToArray());

        ShorfahSectionReconciliation plan = ShorfahCanonicalSectionReconciler.Reconcile(issue, SlaDefaults);

        plan.HasChanges.Should().BeFalse();
    }

    [Fact]
    public void Reconcile_RunTwice_MakesNoFurtherChangesOnTheSecondPass()
    {
        ShorfahIssue issue = LegacyThirteenSectionIssue();

        ShorfahSectionReconciliation first = ShorfahCanonicalSectionReconciler.Reconcile(issue, SlaDefaults);
        ApplyPlan(issue, first);
        ShorfahSectionReconciliation second = ShorfahCanonicalSectionReconciler.Reconcile(issue, SlaDefaults);

        first.HasChanges.Should().BeTrue();
        second.HasChanges.Should().BeFalse();
        issue.Sections.Should().HaveCount(ShorfahCanonicalSections.Templates.Count);
        issue.Sections.OrderBy(section => section.DisplayOrder).Select(section => section.TitleAr).Should()
            .Equal(ShorfahCanonicalSections.Templates.Select(template => template.TitleAr));
    }

    [Fact]
    public void Reconcile_RunTwiceOnAnIssueWithKeptLegacyContent_IsStillANoOpOnTheSecondPass()
    {
        ShorfahIssue issue = LegacyThirteenSectionIssue();
        issue.Sections.First(section => section.SectionType == ShorfahSectionType.GlobalNews).ContentMd = "خبر دولي";

        ApplyPlan(issue, ShorfahCanonicalSectionReconciler.Reconcile(issue, SlaDefaults));
        ShorfahSectionReconciliation second = ShorfahCanonicalSectionReconciler.Reconcile(issue, SlaDefaults);

        second.HasChanges.Should().BeFalse();
        issue.Sections.Should().HaveCount(ShorfahCanonicalSections.Templates.Count + 1);
    }

    // Stands in for the database applying the change set, so idempotency can be asserted on the
    // same object graph the reconciler already handed back.
    private static void ApplyPlan(ShorfahIssue issue, ShorfahSectionReconciliation plan)
    {
        foreach (ShorfahSection section in plan.Removed)
        {
            issue.Sections.Remove(section);
        }

        foreach (ShorfahSection section in plan.Inserted)
        {
            issue.Sections.Add(section);
        }
    }

    private static ShorfahIssue Issue(ShorfahIssueStatus status, params ShorfahSection[] sections)
    {
        var issue = new ShorfahIssue { TitleAr = "عدد", Month = 9, Year = 2026, Status = status };
        foreach (ShorfahSection section in sections)
        {
            section.IssueId = issue.Id;
            issue.Sections.Add(section);
        }

        return issue;
    }

    // The live dev database's issue #1: the pre-restructure catalogue, old Arabic titles and all.
    private static ShorfahIssue LegacyThirteenSectionIssue()
    {
        ShorfahSectionType[] legacyTypes =
        {
            ShorfahSectionType.GlobalNews,
            ShorfahSectionType.News,
            ShorfahSectionType.IntlParticipation,
            ShorfahSectionType.OurComms,
            ShorfahSectionType.EconomicObservatory,
            ShorfahSectionType.SystemIndex,
            ShorfahSectionType.LegalWindow,
            ShorfahSectionType.OfficeInterview,
            ShorfahSectionType.CompetitionCulture,
            ShorfahSectionType.OutsideBox,
            ShorfahSectionType.Events,
            ShorfahSectionType.AgencyLit,
            ShorfahSectionType.EmployeeQa,
        };

        var order = 10;
        ShorfahSection[] sections = legacyTypes
            .Select(type => LegacySection(type, $"عنوان قديم {type}", "تعريف قديم", order += 10))
            .ToArray();
        return Issue(ShorfahIssueStatus.Collecting, sections);
    }

    private static IEnumerable<ShorfahSection> CanonicalSections() =>
        ShorfahCanonicalSections.Templates.Select(template => ShorfahSectionSeeder.BuildSection(
            1,
            template,
            ShorfahSectionSeeder.SlaDaysFor(SlaDefaults, template.SectionType)));

    private static ShorfahSection LegacySection(ShorfahSectionType sectionType, string titleAr, string descriptionAr, int displayOrder) =>
        new()
        {
            SectionType = sectionType,
            TitleAr = titleAr,
            DescriptionAr = descriptionAr,
            DisplayOrder = displayOrder,
            WorkflowStatus = ShorfahWorkflowStatus.PendingContribution,
            SlaDays = 7,
        };
}
