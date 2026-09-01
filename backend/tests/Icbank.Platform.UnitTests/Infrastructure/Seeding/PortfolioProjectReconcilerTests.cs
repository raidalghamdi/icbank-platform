using FluentAssertions;
using Icbank.Platform.Domain.Projects;
using Icbank.Platform.Infrastructure.Seeding;
using Xunit;

namespace Icbank.Platform.UnitTests.Infrastructure.Seeding;

/// <summary>
/// Verifies the portfolio seeder reconciles instead of only inserting: the catalogue is
/// authoritative in both directions, so a project the department no longer tracks is deleted from
/// an already-seeded database rather than left on the page, and a re-run changes nothing.
/// </summary>
public sealed class PortfolioProjectReconcilerTests
{
    private static readonly DateTime SeededAt = new(2026, 9, 1, 6, 0, 0, DateTimeKind.Utc);
    private static readonly string[] StaleCodes = { "OPS-04", "STR-02", "OLD-1" };

    [Fact]
    public void Catalog_ListsExactlyTheFourTrackedProjects()
    {
        PortfolioProjectSeedCatalog.Rows.Select(row => row.Code).Should()
            .Equal("OPS-01", "OPS-02", "OPS-03", "STR-01");
    }

    [Fact]
    public void Catalog_NamesTheProjectsAsTheAuthorityRunsThem()
    {
        PortfolioProjectSeedCatalog.Rows.Select(row => row.Name).Should().Equal(
            "تشغيل مركز الاتصال الموحد للهيئة العامة للمنافسة للعام 2023م.",
            "إعداد التقرير السنوي 2025.",
            "تقديم خدمات الترجمة للهيئة العامة للمنافسة.",
            "مشروع حملة التوعية بالاستراتيجية وتعزيز القيم");
    }

    [Fact]
    public void Catalog_HasOneStrategicProgrammeAndThreeOperationalProjects()
    {
        PortfolioProjectSeedCatalog.Rows.Count(row => row.Category == ProjectCategory.Strategic).Should().Be(1);
        PortfolioProjectSeedCatalog.Rows.Count(row => row.Category == ProjectCategory.Operational).Should().Be(3);
    }

    [Fact]
    public void Reconcile_EmptyTable_InsertsTheWholeCatalogue()
    {
        PortfolioProjectReconciliation plan = PortfolioProjectReconciler.Reconcile(Array.Empty<PortfolioProject>(), SeededAt);

        plan.Added.Select(project => project.Code).Should().Equal("OPS-01", "OPS-02", "OPS-03", "STR-01");
        plan.Removed.Should().BeEmpty();
        plan.Updated.Should().BeEmpty();
        plan.HasChanges.Should().BeTrue();
    }

    [Fact]
    public void Reconcile_AlreadySeededTable_IsANoOp()
    {
        List<PortfolioProject> tracked = SeededPortfolio();

        PortfolioProjectReconciliation plan = PortfolioProjectReconciler.Reconcile(tracked, SeededAt.AddDays(9));

        plan.Added.Should().BeEmpty();
        plan.Updated.Should().BeEmpty();
        plan.Removed.Should().BeEmpty();
        plan.RemovedMilestones.Should().BeEmpty();
        plan.HasChanges.Should().BeFalse();
    }

    [Fact]
    public void Reconcile_ProjectOutsideTheCatalogue_IsRemovedWithItsChildren()
    {
        List<PortfolioProject> tracked = SeededPortfolio();
        PortfolioProject stale = Stale("OPS-99", "مشروع قديم لم يعد متابعاً");
        tracked.Add(stale);

        PortfolioProjectReconciliation plan = PortfolioProjectReconciler.Reconcile(tracked, SeededAt);

        plan.Removed.Should().ContainSingle().Which.Should().BeSameAs(stale);
        plan.RemovedMilestones.Should().ContainSingle().Which.Title.Should().Be("مرحلة قديمة");
        plan.RemovedProgressUpdates.Should().ContainSingle().Which.Note.Should().Be("تحديث قديم");
        plan.HasChanges.Should().BeTrue();
    }

    [Fact]
    public void Reconcile_SeveralStaleProjects_AreAllRemoved()
    {
        List<PortfolioProject> tracked = SeededPortfolio();
        tracked.Add(Stale("OPS-04", "مشروع أ"));
        tracked.Add(Stale("STR-02", "مشروع ب"));
        tracked.Add(Stale("OLD-1", "مشروع ج"));

        PortfolioProjectReconciliation plan = PortfolioProjectReconciler.Reconcile(tracked, SeededAt);

        plan.Removed.Select(project => project.Code).Should().BeEquivalentTo(StaleCodes);
        plan.Added.Should().BeEmpty();
    }

    [Fact]
    public void Reconcile_DuplicateRowClaimingACatalogueCode_KeepsOneAndRemovesTheOther()
    {
        List<PortfolioProject> tracked = SeededPortfolio();
        PortfolioProject duplicate = Stale("OPS-01", "نسخة مكررة");
        tracked.Add(duplicate);

        PortfolioProjectReconciliation plan = PortfolioProjectReconciler.Reconcile(tracked, SeededAt);

        plan.Removed.Should().ContainSingle().Which.Should().BeSameAs(duplicate);
        plan.Added.Should().BeEmpty();
    }

    [Fact]
    public void Reconcile_RenamedProject_IsOverwrittenFromTheCatalogue()
    {
        List<PortfolioProject> tracked = SeededPortfolio();
        PortfolioProject drifted = tracked[0];
        drifted.Name = "اسم قديم من نسخة سابقة";
        drifted.Owner = "مالك قديم";

        PortfolioProjectReconciliation plan = PortfolioProjectReconciler.Reconcile(tracked, SeededAt);

        plan.Updated.Should().ContainSingle().Which.Should().BeSameAs(drifted);
        drifted.Name.Should().Be(PortfolioProjectSeedCatalog.Rows[0].Name);
        drifted.Owner.Should().Be(PortfolioProjectSeedCatalog.Rows[0].Owner);
        plan.Removed.Should().BeEmpty();
    }

    [Fact]
    public void Reconcile_DeactivatedProjectStillInTheCatalogue_IsBroughtBack()
    {
        List<PortfolioProject> tracked = SeededPortfolio();
        tracked[1].IsActive = false;

        PortfolioProjectReconciliation plan = PortfolioProjectReconciler.Reconcile(tracked, SeededAt);

        plan.Updated.Should().ContainSingle();
        tracked[1].IsActive.Should().BeTrue();
    }

    [Fact]
    public void Reconcile_TamperedCheckpointSet_IsReplacedByTheCatalogueSet()
    {
        List<PortfolioProject> tracked = SeededPortfolio();
        PortfolioProject drifted = tracked[2];
        ProjectMilestone removedMilestone = drifted.Milestones.First();
        drifted.Milestones.Remove(removedMilestone);

        PortfolioProjectReconciliation plan = PortfolioProjectReconciler.Reconcile(tracked, SeededAt);

        plan.Updated.Should().ContainSingle().Which.Should().BeSameAs(drifted);
        plan.RemovedMilestones.Should().NotBeEmpty();
        drifted.Milestones.Select(milestone => milestone.Title).Should()
            .Equal(PortfolioProjectSeedCatalog.Rows[2].Milestones.Select(milestone => milestone.Title));
    }

    [Fact]
    public void Reconcile_MissingCatalogueCodeOnAPopulatedTable_IsInsertedWithoutTouchingTheRest()
    {
        List<PortfolioProject> tracked = SeededPortfolio();
        tracked.RemoveAt(3);

        PortfolioProjectReconciliation plan = PortfolioProjectReconciler.Reconcile(tracked, SeededAt);

        plan.Added.Should().ContainSingle().Which.Code.Should().Be("STR-01");
        plan.Updated.Should().BeEmpty();
        plan.Removed.Should().BeEmpty();
    }

    [Fact]
    public void Build_CatalogueRow_AnchorsDatesAndCheckpointsToTheSeedInstant()
    {
        PortfolioProjectSeedRow row = PortfolioProjectSeedCatalog.Rows[0];

        PortfolioProject project = PortfolioProjectReconciler.Build(row, SeededAt);

        project.Code.Should().Be(row.Code);
        project.StartDate.Should().Be(SeededAt.AddDays(row.StartOffsetDays).Date);
        project.DueDate.Should().Be(SeededAt.AddDays(row.DueOffsetDays).Date);
        project.IsActive.Should().BeTrue();
        project.CreatedBy.Should().Be("seeder");
        project.Milestones.Should().HaveCount(row.Milestones.Count);
        project.Milestones.Select(milestone => milestone.SortOrder).Should().Equal(
            Enumerable.Range(1, row.Milestones.Count));
    }

    // A table that a previous seeder run already brought in line with the catalogue, with the
    // creation instant that anchors its relative dates recorded on every row.
    private static List<PortfolioProject> SeededPortfolio()
    {
        var tracked = new List<PortfolioProject>();
        foreach (PortfolioProjectSeedRow row in PortfolioProjectSeedCatalog.Rows)
        {
            PortfolioProject project = PortfolioProjectReconciler.Build(row, SeededAt);
            project.CreatedAt = SeededAt;
            tracked.Add(project);
        }

        return tracked;
    }

    private static PortfolioProject Stale(string code, string name)
    {
        var project = new PortfolioProject
        {
            Code = code,
            Name = name,
            Description = "وصف",
            Category = ProjectCategory.Operational,
            Stage = ProjectStage.InProgress,
            Owner = "مسؤول",
            Department = "إدارة",
            ProgressPercent = 30,
            TeamSize = 3,
            StartDate = SeededAt.AddDays(-60),
            DueDate = SeededAt.AddDays(30),
            LatestUpdate = "تحديث",
            SortOrder = 9,
            CreatedAt = SeededAt,
        };
        project.Milestones.Add(new ProjectMilestone { Title = "مرحلة قديمة", DueDate = SeededAt, SortOrder = 1 });
        project.ProgressUpdates.Add(new ProjectProgressUpdate
        {
            ProgressPercent = 30,
            Note = "تحديث قديم",
            ReportedBy = "مسؤول",
            ReportedAt = SeededAt,
        });
        return project;
    }
}
