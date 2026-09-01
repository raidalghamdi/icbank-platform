using FluentAssertions;
using Icbank.Platform.Domain.Campaigns;
using Icbank.Platform.Infrastructure.Seeding;
using Xunit;

namespace Icbank.Platform.UnitTests.Infrastructure.Seeding;

/// <summary>
/// Verifies the campaign seeder reconciles instead of only inserting: the catalogue is
/// authoritative in both directions, so a campaign the department no longer runs is deleted from an
/// already-seeded database rather than left on the page, and a re-run changes nothing.
/// </summary>
public sealed class CampaignReconcilerTests
{
    private static readonly DateTime SeededAt = new(2026, 9, 1, 6, 0, 0, DateTimeKind.Utc);

    [Fact]
    public void Catalog_CoversBothBooksOfWork()
    {
        CampaignSeedCatalog.Rows.Count(row => row.Audience == CampaignAudience.Internal).Should().Be(5);
        CampaignSeedCatalog.Rows.Count(row => row.Audience == CampaignAudience.External).Should().Be(6);
    }

    [Fact]
    public void Catalog_CoversEveryLifecycleStateSoNoFilterChipIsEverEmpty()
    {
        foreach (CampaignStatus status in Enum.GetValues<CampaignStatus>())
        {
            CampaignSeedCatalog.Rows.Should().Contain(row => row.Status == status, "status {0} needs at least one campaign", status);
        }
    }

    [Fact]
    public void Catalog_CoversEveryLifecycleStateInsideEachAudience()
    {
        foreach (CampaignAudience audience in Enum.GetValues<CampaignAudience>())
        {
            foreach (CampaignStatus status in Enum.GetValues<CampaignStatus>())
            {
                CampaignSeedCatalog.Rows.Should().Contain(
                    row => row.Audience == audience && row.Status == status,
                    "audience {0} needs a campaign in state {1}",
                    audience,
                    status);
            }
        }
    }

    [Fact]
    public void Catalog_UsesADistinctCodePerCampaign()
    {
        CampaignSeedCatalog.Rows.Select(row => row.Code).Should().OnlyHaveUniqueItems();
    }

    [Fact]
    public void Catalog_GivesEveryCampaignOutputsAndChannels()
    {
        CampaignSeedCatalog.Rows.Should().OnlyContain(row => row.Deliverables.Count > 0 && row.Channels.Count > 0);
    }

    [Fact]
    public void Catalog_KeepsEveryReportedPercentageInsideZeroToOneHundred()
    {
        CampaignSeedCatalog.Rows.Should().OnlyContain(row => row.ProgressPercent >= 0 && row.ProgressPercent <= 100);
    }

    [Fact]
    public void Catalog_EndsEveryCampaignAfterItStarts()
    {
        CampaignSeedCatalog.Rows.Should().OnlyContain(row => row.EndOffsetDays > row.StartOffsetDays);
    }

    [Fact]
    public void Catalog_ReportsCompletedCampaignsAsFullyDelivered()
    {
        CampaignSeedCatalog.Rows
            .Where(row => row.Status == CampaignStatus.Completed)
            .Should().OnlyContain(row => row.ProgressPercent == 100 && row.Deliverables.All(d => d.IsCompleted));
    }

    [Fact]
    public void Catalog_ReportsNoReachForCampaignsThatHaveNotStartedPublishing()
    {
        CampaignSeedCatalog.Rows
            .Where(row => row.Status == CampaignStatus.Upcoming)
            .Should().OnlyContain(row => row.ReachCount == 0 && row.PublishedItems == 0);
    }

    [Fact]
    public void Reconcile_EmptyTable_AddsEveryCatalogueCampaignWithItsChildren()
    {
        CampaignReconciliation plan = CampaignReconciler.Reconcile(Array.Empty<Campaign>(), SeededAt);

        plan.Added.Should().HaveCount(CampaignSeedCatalog.Rows.Count);
        plan.Removed.Should().BeEmpty();
        plan.Added.Should().OnlyContain(campaign => campaign.Deliverables.Count > 0 && campaign.Channels.Count > 0);
        plan.HasChanges.Should().BeTrue();
    }

    [Fact]
    public void Reconcile_AlreadySeededTable_ChangesNothingOnASecondRun()
    {
        List<Campaign> seeded = SeedEverything();

        CampaignReconciliation plan = CampaignReconciler.Reconcile(seeded, SeededAt);

        plan.HasChanges.Should().BeFalse();
        plan.RemovedDeliverables.Should().BeEmpty();
        plan.RemovedChannels.Should().BeEmpty();
    }

    [Fact]
    public void Reconcile_CampaignNoLongerInTheCatalogue_RemovesItWithItsChildren()
    {
        List<Campaign> seeded = SeedEverything();
        Campaign retired = CampaignReconciler.Build(CampaignSeedCatalog.Rows[0], SeededAt);
        retired.Code = "OLD-99";
        seeded.Add(retired);

        CampaignReconciliation plan = CampaignReconciler.Reconcile(seeded, SeededAt);

        plan.Removed.Should().ContainSingle(campaign => campaign.Code == "OLD-99");
        plan.RemovedDeliverables.Should().HaveCount(retired.Deliverables.Count);
        plan.RemovedChannels.Should().HaveCount(retired.Channels.Count);
    }

    [Fact]
    public void Reconcile_DuplicateRowClaimingACatalogueCode_KeepsOnlyOne()
    {
        List<Campaign> seeded = SeedEverything();
        seeded.Add(CampaignReconciler.Build(CampaignSeedCatalog.Rows[0], SeededAt));

        CampaignReconciliation plan = CampaignReconciler.Reconcile(seeded, SeededAt);

        plan.Removed.Should().ContainSingle(campaign => campaign.Code == CampaignSeedCatalog.Rows[0].Code);
        plan.Added.Should().BeEmpty();
    }

    [Fact]
    public void Reconcile_RenamedCampaign_OverwritesTheStaleTitleInsteadOfLeavingIt()
    {
        List<Campaign> seeded = SeedEverything();
        seeded[0].Name = "اسم قديم";

        CampaignReconciliation plan = CampaignReconciler.Reconcile(seeded, SeededAt);

        plan.Updated.Should().ContainSingle();
        seeded[0].Name.Should().Be(CampaignSeedCatalog.Rows[0].Name);
    }

    [Fact]
    public void Reconcile_ReactivatesACampaignThatWasSwitchedOff()
    {
        List<Campaign> seeded = SeedEverything();
        seeded[0].IsActive = false;

        CampaignReconciliation plan = CampaignReconciler.Reconcile(seeded, SeededAt);

        plan.Updated.Should().ContainSingle();
        seeded[0].IsActive.Should().BeTrue();
    }

    [Fact]
    public void Reconcile_ChangedOutputSet_ReplacesItAndReportsTheOldRowsForDeletion()
    {
        List<Campaign> seeded = SeedEverything();
        seeded[0].Deliverables.Remove(seeded[0].Deliverables.First());
        var expectedRemovals = seeded[0].Deliverables.Count;

        CampaignReconciliation plan = CampaignReconciler.Reconcile(seeded, SeededAt);

        plan.Updated.Should().ContainSingle();
        plan.RemovedDeliverables.Should().HaveCount(expectedRemovals);
        seeded[0].Deliverables.Should().HaveCount(CampaignSeedCatalog.Rows[0].Deliverables.Count);
    }

    [Fact]
    public void Reconcile_ChangedChannelFigures_RewritesThemFromTheCatalogue()
    {
        List<Campaign> seeded = SeedEverything();
        CampaignChannel channel = seeded[0].Channels.First();
        channel.ReachCount += 5000;

        CampaignReconciliation plan = CampaignReconciler.Reconcile(seeded, SeededAt);

        plan.Updated.Should().ContainSingle();
        seeded[0].Channels.First().ReachCount.Should().Be(CampaignSeedCatalog.Rows[0].Channels[0].ReachCount);
    }

    [Fact]
    public void Reconcile_ExistingRow_AnchorsDatesToItsOwnCreationInstantNotToNow()
    {
        List<Campaign> seeded = SeedEverything();
        DateTime expectedStart = SeededAt.AddDays(CampaignSeedCatalog.Rows[0].StartOffsetDays).Date;

        CampaignReconciler.Reconcile(seeded, SeededAt.AddDays(40));

        seeded[0].StartDate.Should().Be(expectedStart);
    }

    private static List<Campaign> SeedEverything()
    {
        var seeded = new List<Campaign>();
        foreach (CampaignSeedRow row in CampaignSeedCatalog.Rows)
        {
            Campaign campaign = CampaignReconciler.Build(row, SeededAt);
            campaign.CreatedAt = SeededAt;
            seeded.Add(campaign);
        }

        return seeded;
    }
}
