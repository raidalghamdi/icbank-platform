using Icbank.Platform.Domain.Common;

namespace Icbank.Platform.Domain.Identity;

/// <summary>
/// Dedicated audit-log record for privileged actions (DOTNET-CONVENTIONS.md §5.5: role changes,
/// permission-matrix edits, deletions of another user's data get a durable
/// <c>actor_id, action, target_type, target_id, payload_json, created_at</c> row beyond the
/// generic <c>created_by</c>/<c>updated_by</c> columns, because those columns only capture *who
/// last touched the row*, not *what changed*). Every mutating admin action records the before and
/// after state plus a correlation id so a single HTTP request's effects can be traced end to end.
/// </summary>
public sealed class AuditLogEntry : AuditableEntity
{
    /// <summary>Gets or sets the id of the user who performed the action.</summary>
    public int ActorUserId { get; set; }

    /// <summary>Gets or sets the actor navigation property.</summary>
    public User ActorUser { get; set; } = null!;

    /// <summary>Gets or sets the machine-readable action name, e.g. <c>user.role.assign</c>.</summary>
    public string Action { get; set; } = string.Empty;

    /// <summary>Gets or sets the type of entity the action targeted, e.g. <c>User</c>, <c>RolePermission</c>.</summary>
    public string TargetType { get; set; } = string.Empty;

    /// <summary>Gets or sets the identifier of the targeted entity, stored as text even for numeric ids.</summary>
    public string TargetId { get; set; } = string.Empty;

    /// <summary>Gets or sets the JSON snapshot of the target's state before the action, if applicable.</summary>
    public string? BeforeJson { get; set; }

    /// <summary>Gets or sets the JSON snapshot of the target's state after the action, if applicable.</summary>
    public string? AfterJson { get; set; }

    /// <summary>Gets or sets the correlation id (trace id) of the HTTP request that performed this action.</summary>
    public string CorrelationId { get; set; } = string.Empty;

    /// <summary>Gets or sets the caller's IP address.</summary>
    public string? IpAddress { get; set; }
}
