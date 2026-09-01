using Icbank.Platform.Domain.Campaigns;

namespace Icbank.Platform.Infrastructure.Seeding;

/// <summary>
/// Makes the tracked campaign book match <see cref="CampaignSeedCatalog"/> exactly, on the same
/// rule the project portfolio already follows: codes the catalogue no longer lists are deleted
/// with their children, codes it lists are overwritten field by field. Inserting only the missing
/// codes would leave retired campaigns on the page for ever and let a renamed campaign keep its
/// old title in every environment that had already been seeded.
/// </summary>
internal static class CampaignReconciler
{
    /// <summary>Builds the change set that brings <paramref name="tracked"/> in line with the catalogue.</summary>
    /// <param name="tracked">Every campaign currently in the table, with its outputs and channels loaded.</param>
    /// <param name="seededAt">The instant relative catalogue dates are resolved against for brand-new rows.</param>
    /// <returns>The reconciliation to apply.</returns>
    internal static CampaignReconciliation Reconcile(IReadOnlyCollection<Campaign> tracked, DateTime seededAt)
    {
        (Dictionary<string, Campaign> survivors, List<Campaign> removed) = Partition(tracked);

        var added = new List<Campaign>();
        var updated = new List<Campaign>();
        var removedDeliverables = removed.SelectMany(campaign => campaign.Deliverables).ToList();
        var removedChannels = removed.SelectMany(campaign => campaign.Channels).ToList();

        foreach (CampaignSeedRow row in CampaignSeedCatalog.Rows)
        {
            if (!survivors.TryGetValue(row.Code, out Campaign? existing))
            {
                added.Add(Build(row, seededAt));
                continue;
            }

            // Why: relative dates are anchored to the row's own creation instant, not to "now", so
            // a restart does not silently shift every campaign's schedule by a day.
            DateTime anchor = existing.CreatedAt == default ? seededAt : existing.CreatedAt;
            List<CampaignDeliverable> replacedDeliverables = RefreshDeliverables(existing, row, anchor);
            List<CampaignChannel> replacedChannels = RefreshChannels(existing, row);
            var changed = Apply(existing, row, anchor) || replacedDeliverables.Count > 0 || replacedChannels.Count > 0;
            removedDeliverables.AddRange(replacedDeliverables);
            removedChannels.AddRange(replacedChannels);
            if (changed)
            {
                updated.Add(existing);
            }
        }

        return new CampaignReconciliation(added, updated, removed, removedDeliverables, removedChannels);
    }

    /// <summary>Creates a brand-new tracked campaign from a catalogue row.</summary>
    /// <param name="row">The catalogue row.</param>
    /// <param name="seededAt">The instant relative dates are resolved against.</param>
    /// <returns>The campaign, with its outputs and channels attached.</returns>
    internal static Campaign Build(CampaignSeedRow row, DateTime seededAt)
    {
        var campaign = new Campaign { Code = row.Code, CreatedBy = "seeder" };
        Apply(campaign, row, seededAt);
        AddDeliverables(campaign, row, seededAt);
        AddChannels(campaign, row);
        return campaign;
    }

    // Splits the table into the row that owns each catalogue code and everything else. A duplicate
    // row claiming a catalogue code is as stale as an unknown code: only one row can be the
    // campaign the catalogue describes.
    private static (Dictionary<string, Campaign> Survivors, List<Campaign> Removed) Partition(
        IReadOnlyCollection<Campaign> tracked)
    {
        var catalogCodes = CampaignSeedCatalog.Rows.Select(row => row.Code).ToHashSet(StringComparer.Ordinal);
        var survivors = new Dictionary<string, Campaign>(StringComparer.Ordinal);
        var removed = new List<Campaign>();

        foreach (Campaign campaign in tracked)
        {
            if (!catalogCodes.Contains(campaign.Code) || !survivors.TryAdd(campaign.Code, campaign))
            {
                removed.Add(campaign);
            }
        }

        return (survivors, removed);
    }

    // Why: every catalogue-owned field is overwritten, so a campaign that was renamed or re-scoped
    // in the catalogue stops showing its old wording. Returns whether anything moved, which keeps a
    // no-op run from logging a reconciliation that did not happen.
    private static bool Apply(Campaign campaign, CampaignSeedRow row, DateTime anchor)
    {
        DateTime startDate = anchor.AddDays(row.StartOffsetDays).Date;
        DateTime endDate = anchor.AddDays(row.EndOffsetDays).Date;
        var changed = TextChanged(campaign, row) || NumbersChanged(campaign, row)
            || campaign.Audience != row.Audience
            || campaign.Status != row.Status
            || campaign.StartDate != startDate
            || campaign.EndDate != endDate
            || !campaign.IsActive;

        campaign.Name = row.Name;
        campaign.Description = row.Description;
        campaign.Objective = row.Objective;
        campaign.Audience = row.Audience;
        campaign.Status = row.Status;
        campaign.Owner = row.Owner;
        campaign.Department = row.Department;
        campaign.ProgressPercent = row.ProgressPercent;
        campaign.StartDate = startDate;
        campaign.EndDate = endDate;
        campaign.LatestUpdate = row.LatestUpdate;
        campaign.ReachCount = row.ReachCount;
        campaign.ImpressionsCount = row.ImpressionsCount;
        campaign.EngagementCount = row.EngagementCount;
        campaign.PublishedItems = row.PublishedItems;
        campaign.SortOrder = row.SortOrder;
        campaign.IsActive = true;
        return changed;
    }

    private static bool TextChanged(Campaign campaign, CampaignSeedRow row)
        => !string.Equals(campaign.Name, row.Name, StringComparison.Ordinal)
            || !string.Equals(campaign.Description, row.Description, StringComparison.Ordinal)
            || !string.Equals(campaign.Objective, row.Objective, StringComparison.Ordinal)
            || !string.Equals(campaign.Owner, row.Owner, StringComparison.Ordinal)
            || !string.Equals(campaign.Department, row.Department, StringComparison.Ordinal)
            || !string.Equals(campaign.LatestUpdate, row.LatestUpdate, StringComparison.Ordinal);

    private static bool NumbersChanged(Campaign campaign, CampaignSeedRow row)
        => campaign.ProgressPercent != row.ProgressPercent
            || campaign.ReachCount != row.ReachCount
            || campaign.ImpressionsCount != row.ImpressionsCount
            || campaign.EngagementCount != row.EngagementCount
            || campaign.PublishedItems != row.PublishedItems
            || campaign.SortOrder != row.SortOrder;

    // Replaces the output set only when it actually differs from the catalogue: rewriting an
    // identical set on every restart would churn the rows' identities for no reason.
    private static List<CampaignDeliverable> RefreshDeliverables(Campaign campaign, CampaignSeedRow row, DateTime anchor)
    {
        var current = campaign.Deliverables.OrderBy(deliverable => deliverable.SortOrder).ToList();
        if (DeliverablesMatch(current, row, anchor))
        {
            return new List<CampaignDeliverable>();
        }

        campaign.Deliverables.Clear();
        AddDeliverables(campaign, row, anchor);
        return current;
    }

    private static bool DeliverablesMatch(List<CampaignDeliverable> current, CampaignSeedRow row, DateTime anchor)
    {
        if (current.Count != row.Deliverables.Count)
        {
            return false;
        }

        return !current.Where((deliverable, index) =>
            !string.Equals(deliverable.Title, row.Deliverables[index].Title, StringComparison.Ordinal)
            || deliverable.DueDate != anchor.AddDays(row.Deliverables[index].DueOffsetDays).Date
            || deliverable.IsCompleted != row.Deliverables[index].IsCompleted).Any();
    }

    private static void AddDeliverables(Campaign campaign, CampaignSeedRow row, DateTime anchor)
    {
        var order = 1;
        foreach (CampaignDeliverableSeedRow deliverable in row.Deliverables)
        {
            campaign.Deliverables.Add(new CampaignDeliverable
            {
                Title = deliverable.Title,
                DueDate = anchor.AddDays(deliverable.DueOffsetDays).Date,
                IsCompleted = deliverable.IsCompleted,
                SortOrder = order++,
                CreatedBy = "seeder",
            });
        }
    }

    private static List<CampaignChannel> RefreshChannels(Campaign campaign, CampaignSeedRow row)
    {
        var current = campaign.Channels.OrderBy(channel => channel.SortOrder).ToList();
        if (ChannelsMatch(current, row))
        {
            return new List<CampaignChannel>();
        }

        campaign.Channels.Clear();
        AddChannels(campaign, row);
        return current;
    }

    private static bool ChannelsMatch(List<CampaignChannel> current, CampaignSeedRow row)
    {
        if (current.Count != row.Channels.Count)
        {
            return false;
        }

        return !current.Where((channel, index) =>
            !string.Equals(channel.Name, row.Channels[index].Name, StringComparison.Ordinal)
            || channel.PublishedItems != row.Channels[index].PublishedItems
            || channel.ReachCount != row.Channels[index].ReachCount
            || channel.EngagementCount != row.Channels[index].EngagementCount).Any();
    }

    private static void AddChannels(Campaign campaign, CampaignSeedRow row)
    {
        var order = 1;
        foreach (CampaignChannelSeedRow channel in row.Channels)
        {
            campaign.Channels.Add(new CampaignChannel
            {
                Name = channel.Name,
                PublishedItems = channel.PublishedItems,
                ReachCount = channel.ReachCount,
                EngagementCount = channel.EngagementCount,
                SortOrder = order++,
                CreatedBy = "seeder",
            });
        }
    }
}
