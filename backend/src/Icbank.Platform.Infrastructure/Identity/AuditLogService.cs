using System.Text.Json;
using Icbank.Platform.Application.Common.Interfaces;
using Icbank.Platform.Domain.Identity;

namespace Icbank.Platform.Infrastructure.Identity;

/// <summary>
/// Writes the dedicated privileged-action audit trail (DOTNET-CONVENTIONS.md §5.5). Every
/// mutating admin action — role assignment, permission-matrix edits, lockouts, user CRUD —
/// records who did what to what, with a before/after JSON snapshot and the request's correlation
/// id, closing the task's audit-log requirement independently of the generic
/// <c>created_by</c>/<c>updated_by</c> columns which only capture the last touch, not the change.
/// </summary>
public sealed class AuditLogService : IAuditLogService
{
    private readonly IApplicationDbContext _dbContext;
    private readonly IRequestContext _requestContext;

    /// <summary>Initializes a new instance of the <see cref="AuditLogService"/> class.</summary>
    /// <param name="dbContext">The persistence port.</param>
    /// <param name="requestContext">The current request's correlation id and IP address.</param>
    public AuditLogService(IApplicationDbContext dbContext, IRequestContext requestContext)
    {
        _dbContext = dbContext;
        _requestContext = requestContext;
    }

    /// <inheritdoc />
    public async Task RecordAsync(
        int actorUserId,
        string action,
        string targetType,
        string targetId,
        object? before,
        object? after,
        CancellationToken cancellationToken)
    {
        var entry = new AuditLogEntry
        {
            ActorUserId = actorUserId,
            Action = action,
            TargetType = targetType,
            TargetId = targetId,
            BeforeJson = before is null ? null : JsonSerializer.Serialize(before),
            AfterJson = after is null ? null : JsonSerializer.Serialize(after),
            CorrelationId = _requestContext.CorrelationId,
            IpAddress = _requestContext.IpAddress,
        };

        _dbContext.Add(entry);
        await _dbContext.SaveChangesAsync(cancellationToken);
    }
}
