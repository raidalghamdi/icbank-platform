using Icbank.Platform.Domain.Common;

namespace Icbank.Platform.Domain.Campaigns;

/// <summary>
/// One channel a <see cref="Campaign"/> publishes through, with the reach it produced. Stored per
/// channel rather than as a single campaign total so the detail page can show which channel is
/// carrying the campaign instead of just how big the number is.
/// </summary>
public sealed class CampaignChannel : AuditableEntity
{
    /// <summary>Gets or sets the owning campaign's identifier.</summary>
    public int CampaignId { get; set; }

    /// <summary>Gets or sets the channel's Arabic display name, e.g. <c>البريد الداخلي</c>.</summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>Gets or sets the number of items published on this channel.</summary>
    public int PublishedItems { get; set; }

    /// <summary>Gets or sets how many people this channel reached.</summary>
    public int ReachCount { get; set; }

    /// <summary>Gets or sets how many interactions this channel drew.</summary>
    public int EngagementCount { get; set; }

    /// <summary>Gets or sets the display sort order within the campaign.</summary>
    public int SortOrder { get; set; }

    /// <summary>Gets or sets the owning campaign.</summary>
    public Campaign? Campaign { get; set; }
}
