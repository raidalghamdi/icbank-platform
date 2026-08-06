namespace Icbank.Platform.Application.Admin.Queries;

/// <summary>
/// One activity-log row shaped for CSV export (adds the user's display name alongside the email,
/// matching the old Node export's "المستخدم" column — the JSON list endpoint's
/// <see cref="ActivityLogEntryDto"/> does not carry this field).
/// </summary>
/// <param name="Id">The log entry's id.</param>
/// <param name="UserName">The acting user's display name, or <c>null</c> if the user is unknown/deleted.</param>
/// <param name="UserEmail">The acting user's email, or <c>null</c> if the user is unknown/deleted.</param>
/// <param name="Action">The free-text action name.</param>
/// <param name="EntityType">The type of entity affected, if any.</param>
/// <param name="EntityId">The id of the entity affected, if any.</param>
/// <param name="IpAddress">The caller's IP address, if recorded.</param>
/// <param name="CreatedAt">When the event was recorded (UTC).</param>
public sealed record ActivityLogExportRow(
    int Id,
    string? UserName,
    string? UserEmail,
    string Action,
    string? EntityType,
    string? EntityId,
    string? IpAddress,
    DateTime CreatedAt);
