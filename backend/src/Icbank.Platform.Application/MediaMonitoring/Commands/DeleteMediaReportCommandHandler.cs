using Icbank.Platform.Application.Common.Interfaces;
using Icbank.Platform.Application.Common.Models;
using Icbank.Platform.Domain.MediaMonitoring;
using MediatR;

namespace Icbank.Platform.Application.MediaMonitoring.Commands;

/// <summary>Handles <see cref="DeleteMediaReportCommand"/>. Hard delete, matching the Node source (a lookup-style editable-tier row, not the immutable final tier).</summary>
public sealed class DeleteMediaReportCommandHandler : IRequestHandler<DeleteMediaReportCommand, Result<bool>>
{
    private readonly IApplicationDbContext _dbContext;
    private readonly IAsyncQueryExecutor _queryExecutor;
    private readonly IAuditLogService _auditLogService;

    /// <summary>Initializes a new instance of the <see cref="DeleteMediaReportCommandHandler"/> class.</summary>
    /// <param name="dbContext">The persistence port.</param>
    /// <param name="queryExecutor">The async LINQ execution port.</param>
    /// <param name="auditLogService">The privileged-action audit log port.</param>
    public DeleteMediaReportCommandHandler(IApplicationDbContext dbContext, IAsyncQueryExecutor queryExecutor, IAuditLogService auditLogService)
    {
        _dbContext = dbContext;
        _queryExecutor = queryExecutor;
        _auditLogService = auditLogService;
    }

    /// <inheritdoc />
    public async Task<Result<bool>> Handle(DeleteMediaReportCommand request, CancellationToken cancellationToken)
    {
        MediaReport? report = await _queryExecutor.SingleOrDefaultAsync(
            _dbContext.MediaReports.Where(r => r.Id == request.ReportId), cancellationToken);
        if (report is null)
        {
            return Result<bool>.Failure("التقرير غير موجود");
        }

        _dbContext.Remove(report);
        await _dbContext.SaveChangesAsync(cancellationToken);

        await _auditLogService.RecordAsync(
            request.ActorUserId,
            "media_report.delete",
            "MediaReport",
            request.ReportId.ToString(System.Globalization.CultureInfo.InvariantCulture),
            before: new { report.Title },
            after: null,
            cancellationToken);

        return Result<bool>.Success(true);
    }
}
