namespace Icbank.Platform.Application.Common.Interfaces;

/// <summary>
/// Port for writing the dedicated privileged-action audit trail (DOTNET-CONVENTIONS.md §5.5,
/// task requirement 5: "Every mutating action writes an audit log entry (actor, action, target,
/// before/after, timestamp, correlation id)").
/// </summary>
public interface IAuditLogService
{
    /// <summary>Writes a single audit-log entry for a privileged action.</summary>
    /// <param name="actorUserId">The id of the user performing the action.</param>
    /// <param name="action">The machine-readable action name, e.g. <c>user.role.assign</c>.</param>
    /// <param name="targetType">The type of entity targeted, e.g. <c>User</c>.</param>
    /// <param name="targetId">The identifier of the targeted entity.</param>
    /// <param name="before">The object graph representing state before the action, serialized to JSON. Pass <c>null</c> if not applicable.</param>
    /// <param name="after">The object graph representing state after the action, serialized to JSON. Pass <c>null</c> if not applicable.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task RecordAsync(
        int actorUserId,
        string action,
        string targetType,
        string targetId,
        object? before,
        object? after,
        CancellationToken cancellationToken);
}
