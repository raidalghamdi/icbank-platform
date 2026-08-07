using Icbank.Platform.Domain.Common;

namespace Icbank.Platform.Domain.Identity;

/// <summary>Audit trail of auth events plus admin actions (DATA-MODEL.md section 3.1 <c>activity_logs</c>).</summary>
public sealed class ActivityLog : AuditableEntity
{
    /// <summary>Gets or sets the acting user's id, if known.</summary>
    public int? UserId { get; set; }

    /// <summary>Gets or sets the user navigation property.</summary>
    public User? User { get; set; }

    /// <summary>Gets or sets the free-text action name, e.g. <c>login_success</c>.</summary>
    public string Action { get; set; } = string.Empty;

    /// <summary>Gets or sets the type of entity affected, if any.</summary>
    public string? EntityType { get; set; }

    /// <summary>Gets or sets the id of the entity affected, stored as text even for numeric ids.</summary>
    public string? EntityId { get; set; }

    /// <summary>Gets or sets the unstructured JSON details payload for this event.</summary>
    public string? DetailsJson { get; set; }

    /// <summary>Gets or sets the caller's IP address.</summary>
    public string? IpAddress { get; set; }

    /// <summary>Gets or sets the caller's user agent string.</summary>
    public string? UserAgent { get; set; }
}
