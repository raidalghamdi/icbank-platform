using Icbank.Platform.Domain.Common;

namespace Icbank.Platform.Domain.Campaigns;

/// <summary>
/// An internal campaign request raised by one of the authority's departments and reviewed by
/// Corporate Communications. Approving it converts it into a campaign carrying the same details,
/// so nobody has to type them in twice.
/// </summary>
public sealed class CampaignRequest : AuditableEntity
{
    /// <summary>Gets or sets the requesting department.</summary>
    public string RequestingDepartment { get; set; } = string.Empty;

    /// <summary>Gets or sets the proposed campaign name.</summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>Gets or sets the campaign objective.</summary>
    public string Objective { get; set; } = string.Empty;

    /// <summary>Gets or sets the target audience.</summary>
    public string TargetAudience { get; set; } = string.Empty;

    /// <summary>Gets or sets the first day of the proposed period.</summary>
    public DateTime ProposedStart { get; set; }

    /// <summary>Gets or sets the last day of the proposed period.</summary>
    public DateTime ProposedEnd { get; set; }

    /// <summary>Gets or sets the key messages, one per line.</summary>
    public string KeyMessages { get; set; } = string.Empty;

    /// <summary>Gets or sets the required support keys (content, design, publishing, activation), comma separated.</summary>
    public string SupportTypes { get; set; } = string.Empty;

    /// <summary>Gets or sets the review state.</summary>
    public CampaignRequestStatus Status { get; set; }

    /// <summary>Gets or sets the numeric id of the employee who submitted the request.</summary>
    public int SubmittedByUserId { get; set; }

    /// <summary>Gets or sets the display name of the employee who submitted the request.</summary>
    public string SubmittedByName { get; set; } = string.Empty;

    /// <summary>Gets or sets the reviewer's note.</summary>
    public string ReviewNote { get; set; } = string.Empty;

    /// <summary>Gets or sets the reviewer's display name.</summary>
    public string ReviewedByName { get; set; } = string.Empty;

    /// <summary>Gets or sets when the request was reviewed (UTC).</summary>
    public DateTime? ReviewedAt { get; set; }

    /// <summary>Gets or sets the campaign the request was converted into.</summary>
    public int? CampaignId { get; set; }
}
