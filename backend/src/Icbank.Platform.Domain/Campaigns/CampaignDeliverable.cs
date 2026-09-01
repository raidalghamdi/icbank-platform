using Icbank.Platform.Domain.Common;

namespace Icbank.Platform.Domain.Campaigns;

/// <summary>
/// One headline output of a <see cref="Campaign"/> — a film, a poster set, a workshop, a press
/// release. A bare percentage says nothing about what the campaign actually produced, so every
/// campaign carries the outputs behind its figure.
/// </summary>
public sealed class CampaignDeliverable : AuditableEntity
{
    /// <summary>Gets or sets the owning campaign's identifier.</summary>
    public int CampaignId { get; set; }

    /// <summary>Gets or sets the output title.</summary>
    public string Title { get; set; } = string.Empty;

    /// <summary>Gets or sets the UTC date the output is due.</summary>
    public DateTime DueDate { get; set; }

    /// <summary>Gets or sets a value indicating whether the output has been delivered.</summary>
    public bool IsCompleted { get; set; }

    /// <summary>Gets or sets the display sort order within the campaign.</summary>
    public int SortOrder { get; set; }

    /// <summary>Gets or sets the owning campaign.</summary>
    public Campaign? Campaign { get; set; }
}
