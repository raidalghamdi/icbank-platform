using Icbank.Platform.Application.Common.Interfaces;
using Icbank.Platform.Application.Common.Models;
using Icbank.Platform.Domain.Identity;
using MediatR;

namespace Icbank.Platform.Application.Admin.Queries;

/// <summary>
/// Handles <see cref="ExportActivityLogQuery"/>. Filters exactly like
/// <see cref="ListActivityLogQueryHandler"/> (same predicates, same exact-match <c>Action</c>
/// semantics) but orders newest-first with no pagination and applies a hard <see cref="MaxRows"/>
/// cap instead — mirroring the old Node export's <c>.limit(5000)</c> row ceiling, chosen for
/// parity rather than an arbitrary new number. Writes a dedicated audit-log entry on every
/// successful export (task requirement: "exporting the full activity log is itself a
/// security-relevant action") recording the actor, the filters applied, and how many rows were
/// actually returned.
/// </summary>
public sealed class ExportActivityLogQueryHandler : IRequestHandler<ExportActivityLogQuery, Result<ActivityLogExportDto>>
{
    /// <summary>The hard row ceiling for a single export, mirroring the Node original's <c>.limit(5000)</c>.</summary>
    public const int MaxRows = 5000;

    private const string AuditAction = "activity_log.export";
    private const string AuditTargetType = "activity_log";

    private readonly IApplicationDbContext _dbContext;
    private readonly IAsyncQueryExecutor _queryExecutor;
    private readonly IAuditLogService _auditLogService;

    /// <summary>Initializes a new instance of the <see cref="ExportActivityLogQueryHandler"/> class.</summary>
    /// <param name="dbContext">The persistence port.</param>
    /// <param name="queryExecutor">The async LINQ execution port.</param>
    /// <param name="auditLogService">The privileged-action audit trail port.</param>
    public ExportActivityLogQueryHandler(IApplicationDbContext dbContext, IAsyncQueryExecutor queryExecutor, IAuditLogService auditLogService)
    {
        _dbContext = dbContext;
        _queryExecutor = queryExecutor;
        _auditLogService = auditLogService;
    }

    /// <inheritdoc />
    public async Task<Result<ActivityLogExportDto>> Handle(ExportActivityLogQuery request, CancellationToken cancellationToken)
    {
        IQueryable<ActivityLog> filtered = ApplyFilters(_dbContext.ActivityLogs, request);

        List<ActivityLog> allMatches = await _queryExecutor.ToListAsync(filtered, cancellationToken);
        var capped = allMatches
            .OrderByDescending(log => log.CreatedAt)
            .Take(MaxRows)
            .ToList();

        List<User> users = await _queryExecutor.ToListAsync(_dbContext.Users, cancellationToken);
        var rows = capped.Select(log => ToRow(log, users)).ToList();

        await _auditLogService.RecordAsync(
            request.ActorUserId,
            AuditAction,
            AuditTargetType,
            targetId: "*",
            before: null,
            after: new
            {
                request.UserId,
                request.Action,
                request.DateFrom,
                request.DateTo,
                rowsExported = rows.Count,
                totalMatched = allMatches.Count,
            },
            cancellationToken);

        return Result<ActivityLogExportDto>.Success(new ActivityLogExportDto(rows, allMatches.Count));
    }

    private static IQueryable<ActivityLog> ApplyFilters(IQueryable<ActivityLog> query, ExportActivityLogQuery request)
    {
        if (request.UserId is not null)
        {
            query = query.Where(log => log.UserId == request.UserId);
        }

        if (!string.IsNullOrWhiteSpace(request.Action))
        {
            query = query.Where(log => log.Action == request.Action);
        }

        if (request.DateFrom is not null)
        {
            query = query.Where(log => log.CreatedAt >= request.DateFrom);
        }

        if (request.DateTo is not null)
        {
            query = query.Where(log => log.CreatedAt <= request.DateTo);
        }

        return query;
    }

    private static ActivityLogExportRow ToRow(ActivityLog log, List<User> users)
    {
        User? user = users.SingleOrDefault(u => u.Id == log.UserId);
        return new ActivityLogExportRow(log.Id, user?.Name, user?.Email, log.Action, log.EntityType, log.EntityId, log.IpAddress, log.CreatedAt);
    }
}
