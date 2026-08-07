namespace Icbank.Platform.Application.Admin;

/// <summary>Admin-facing activity-log row (API-SURFACE.md §5 <c>GET /admin/activity</c>).</summary>
/// <param name="Id">The log entry's id.</param>
/// <param name="UserId">The acting user's id, if known.</param>
/// <param name="UserEmail">The acting user's email, if known.</param>
/// <param name="Action">The free-text action name, e.g. <c>login_success</c>.</param>
/// <param name="EntityType">The type of entity affected, if any.</param>
/// <param name="EntityId">The id of the entity affected, if any.</param>
/// <param name="IpAddress">The caller's IP address, if recorded.</param>
/// <param name="CreatedAt">The UTC timestamp the event occurred.</param>
public sealed record ActivityLogEntryDto(
    int Id, int? UserId, string? UserEmail, string Action, string? EntityType, string? EntityId, string? IpAddress, DateTime CreatedAt);
