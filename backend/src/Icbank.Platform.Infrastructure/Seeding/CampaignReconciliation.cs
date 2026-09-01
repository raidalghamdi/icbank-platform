using Icbank.Platform.Domain.Campaigns;

namespace Icbank.Platform.Infrastructure.Seeding;

/// <summary>
/// The change set <see cref="CampaignReconciler"/> produced for the tracked campaign book.
/// Returned as data rather than applied inline so the reconciliation rules can be exercised
/// without a database.
/// </summary>
/// <param name="Added">Campaigns present in the catalogue but missing from the table.</param>
/// <param name="Updated">Tracked campaigns whose fields and children were refreshed from the catalogue.</param>
/// <param name="Removed">Tracked campaigns whose code is no longer in the catalogue.</param>
/// <param name="RemovedDeliverables">Outputs to delete: those of removed campaigns plus the replaced sets of updated ones.</param>
/// <param name="RemovedChannels">Channels to delete, on the same rule as the outputs.</param>
internal sealed record CampaignReconciliation(
    IReadOnlyList<Campaign> Added,
    IReadOnlyList<Campaign> Updated,
    IReadOnlyList<Campaign> Removed,
    IReadOnlyList<CampaignDeliverable> RemovedDeliverables,
    IReadOnlyList<CampaignChannel> RemovedChannels)
{
    /// <summary>Gets a value indicating whether the reconciliation would change anything at all.</summary>
    internal bool HasChanges => Added.Count > 0 || Updated.Count > 0 || Removed.Count > 0;
}
