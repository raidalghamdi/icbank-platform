using Icbank.Platform.Application.Common.Interfaces;
using Icbank.Platform.Application.Common.Models;
using Icbank.Platform.Domain.MediaMonitoring;
using MediatR;

namespace Icbank.Platform.Application.MediaMonitoring.Commands;

/// <summary>Handles <see cref="CreateFinalMediaReportCommand"/>. Computes the report number and content hash, then persists a permanently-immutable row.</summary>
public sealed class CreateFinalMediaReportCommandHandler : IRequestHandler<CreateFinalMediaReportCommand, Result<FinalMediaReportDto>>
{
    private readonly IApplicationDbContext _dbContext;
    private readonly IAsyncQueryExecutor _queryExecutor;
    private readonly IAuditLogService _auditLogService;
    private readonly IDateTimeProvider _dateTimeProvider;

    /// <summary>Initializes a new instance of the <see cref="CreateFinalMediaReportCommandHandler"/> class.</summary>
    /// <param name="dbContext">The persistence port.</param>
    /// <param name="queryExecutor">The async LINQ execution port.</param>
    /// <param name="auditLogService">The privileged-action audit log port.</param>
    /// <param name="dateTimeProvider">The Riyadh-aware clock port.</param>
    public CreateFinalMediaReportCommandHandler(
        IApplicationDbContext dbContext, IAsyncQueryExecutor queryExecutor, IAuditLogService auditLogService, IDateTimeProvider dateTimeProvider)
    {
        _dbContext = dbContext;
        _queryExecutor = queryExecutor;
        _auditLogService = auditLogService;
        _dateTimeProvider = dateTimeProvider;
    }

    /// <inheritdoc />
    public async Task<Result<FinalMediaReportDto>> Handle(CreateFinalMediaReportCommand request, CancellationToken cancellationToken)
    {
        DateTimeOffset now = _dateTimeProvider.RiyadhNow;
        List<string> existingNumbers = await _queryExecutor.ToListAsync(_dbContext.FinalMediaReports.Select(r => r.ReportNumber), cancellationToken);
        var reportNumber = FinalReportNumberGenerator.Next(existingNumbers, now.Year);

        FinalMediaReport report = BuildReport(request, reportNumber, now);
        _dbContext.Add(report);
        await _dbContext.SaveChangesAsync(cancellationToken);

        await _auditLogService.RecordAsync(
            request.ActorUserId,
            "final_media_report.create",
            "FinalMediaReport",
            report.Id.ToString(System.Globalization.CultureInfo.InvariantCulture),
            before: null,
            after: new { report.ReportNumber, report.Title },
            cancellationToken);

        return Result<FinalMediaReportDto>.Success(FinalMediaReportMapper.ToSummaryDto(report));
    }

    private static FinalMediaReport BuildReport(CreateFinalMediaReportCommand request, string reportNumber, DateTimeOffset now)
    {
        MediaReportType reportType = Enum.TryParse(request.ReportType, ignoreCase: true, out MediaReportType parsed) ? parsed : MediaReportType.Weekly;
        var report = new FinalMediaReport
        {
            ReportNumber = reportNumber,
            Title = request.Title,
            ReportType = reportType,
            PeriodLabel = request.PeriodLabel,
            DateFrom = request.DateFrom,
            DateTo = request.DateTo,
            IssueDate = now,
            SourceItemsJson = "[]",
            GeneratedByUserId = request.ActorUserId,
            Status = request.AsDraft ? FinalMediaReportStatus.Draft : FinalMediaReportStatus.Final,
            LockedAt = now,
            ApprovedAt = request.AsDraft ? null : now,
            LayoutJson = FinalReportLayout.Parse(request.LayoutJson).ToJson(),
        };
        FinalReportDraftApplier.Apply(report, request.Draft);
        return report;
    }
}
