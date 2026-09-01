using Icbank.Platform.Domain.Common;

namespace Icbank.Platform.Domain.Campaigns;

/// <summary>
/// One communications campaign the department is running, tracked end to end: its lifecycle
/// state, how far along it is, who owns it, what it must produce, where it publishes, and what
/// the published material actually achieved. The campaigns pages and the executive dashboard both
/// read these rows, so a campaign is stored once and rendered in both places.
/// </summary>
public sealed class Campaign : AuditableEntity
{
    /// <summary>Gets or sets the short human-readable reference shown on the card, e.g. <c>INT-01</c>.</summary>
    public string Code { get; set; } = string.Empty;

    /// <summary>Gets or sets the campaign name.</summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>Gets or sets the one-line description of what the campaign is about.</summary>
    public string Description { get; set; } = string.Empty;

    /// <summary>Gets or sets the communications objective the campaign is measured against.</summary>
    public string Objective { get; set; } = string.Empty;

    /// <summary>Gets or sets whether the campaign targets employees or an outside audience.</summary>
    public CampaignAudience Audience { get; set; }

    /// <summary>Gets or sets the lifecycle state the campaigns page filters on.</summary>
    public CampaignStatus Status { get; set; }

    /// <summary>Gets or sets the name of the person accountable for the campaign.</summary>
    public string Owner { get; set; } = string.Empty;

    /// <summary>Gets or sets the owning organisational unit.</summary>
    public string Department { get; set; } = string.Empty;

    /// <summary>Gets or sets the reported completion percentage, 0-100.</summary>
    public int ProgressPercent { get; set; }

    /// <summary>Gets or sets the UTC date the campaign starts publishing.</summary>
    public DateTime StartDate { get; set; }

    /// <summary>Gets or sets the UTC date the campaign closes.</summary>
    public DateTime EndDate { get; set; }

    /// <summary>Gets or sets the latest progress note shown under the bar.</summary>
    public string LatestUpdate { get; set; } = string.Empty;

    /// <summary>Gets or sets how many people the published material reached.</summary>
    public int ReachCount { get; set; }

    /// <summary>Gets or sets how many times the published material was displayed.</summary>
    public int ImpressionsCount { get; set; }

    /// <summary>Gets or sets how many interactions the published material drew.</summary>
    public int EngagementCount { get; set; }

    /// <summary>Gets or sets how many pieces of content the campaign has published so far.</summary>
    public int PublishedItems { get; set; }

    /// <summary>Gets or sets the display sort order within its audience.</summary>
    public int SortOrder { get; set; }

    /// <summary>Gets or sets a value indicating whether the campaign is currently tracked.</summary>
    public bool IsActive { get; set; } = true;

    /// <summary>Gets the campaign's headline outputs, which make the progress figure auditable.</summary>
    public ICollection<CampaignDeliverable> Deliverables { get; } = new List<CampaignDeliverable>();

    /// <summary>Gets the channels the campaign publishes through, each with its own reach.</summary>
    public ICollection<CampaignChannel> Channels { get; } = new List<CampaignChannel>();
}
