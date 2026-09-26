using System.Globalization;
using Icbank.Platform.Application.Common.Interfaces;
using Icbank.Platform.Application.Common.Models;
using Icbank.Platform.Domain.MediaMonitoring;
using MediatR;

namespace Icbank.Platform.Application.MediaMonitoring.Commands;

/// <summary>Handles <see cref="ApproveFinalMediaReportCommand"/>.</summary>
public sealed class ApproveFinalMediaReportCommandHandler : IRequestHandler<ApproveFinalMediaReportCommand, Result<FinalMediaReportDto>>
{
    private readonly IApplicationDbContext _dbContext;
    private readonly IAsyncQueryExecutor _queryExecutor;
    private readonly IAuditLogService _auditLogService;
    private readonly IDateTimeProvider _clock;

    /// <summary>Initializes a new instance of the <see cref="ApproveFinalMediaReportCommandHandler"/> class.</summary>
    /// <param name="dbContext">The application database context.</param>
    /// <param name="queryExecutor">The async query executor.</param>
    /// <param name="auditLogService">The audit log.</param>
    /// <param name="clock">The clock.</param>
    public ApproveFinalMediaReportCommandHandler(
        IApplicationDbContext dbContext, IAsyncQueryExecutor queryExecutor, IAuditLogService auditLogService, IDateTimeProvider clock)
    {
        _dbContext = dbContext;
        _queryExecutor = queryExecutor;
        _auditLogService = auditLogService;
        _clock = clock;
    }

    /// <inheritdoc />
    public async Task<Result<FinalMediaReportDto>> Handle(ApproveFinalMediaReportCommand request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        FinalMediaReport? report = await _queryExecutor.SingleOrDefaultAsync(
            _dbContext.FinalMediaReports.Where(r => r.Id == request.ReportId), cancellationToken);
        if (report is null)
        {
            return Result<FinalMediaReportDto>.Failure(UpdateFinalMediaReportDraftCommand.NotFoundError);
        }

        if (report.Status != FinalMediaReportStatus.Draft)
        {
            return Result<FinalMediaReportDto>.Failure(ApproveFinalMediaReportCommand.AlreadyApprovedError);
        }

        DateTimeOffset now = _clock.RiyadhNow;
        report.Status = FinalMediaReportStatus.Final;
        report.LockedAt = now;
        report.IssueDate = now;
        report.ApprovedAt = now;
        report.ApprovedByName = string.IsNullOrWhiteSpace(request.ApproverName) ? null : request.ApproverName.Trim();
        await _dbContext.SaveChangesAsync(cancellationToken);

        await _auditLogService.RecordAsync(
            request.ActorUserId,
            "final_media_report.approve",
            "FinalMediaReport",
            report.Id.ToString(CultureInfo.InvariantCulture),
            before: new { Status = FinalMediaReportStatus.Draft.ToString() },
            after: new { report.ReportNumber, Status = report.Status.ToString() },
            cancellationToken);

        return Result<FinalMediaReportDto>.Success(FinalMediaReportMapper.ToSummaryDto(report));
    }
}
