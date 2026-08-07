using System.Text.Json;
using Icbank.Platform.Application.Common.Interfaces;
using Icbank.Platform.Application.Common.Models;
using Icbank.Platform.Domain.MediaMonitoring;
using MediatR;

namespace Icbank.Platform.Application.MediaMonitoring.Commands;

/// <summary>Handles <see cref="RegenerateExecutiveSummaryCommand"/>.</summary>
public sealed class RegenerateExecutiveSummaryCommandHandler : IRequestHandler<RegenerateExecutiveSummaryCommand, Result<RegenerateExecutiveSummaryResultDto>>
{
    private const int TopNewsLimit = 5;
    private const int RecommendationsLimit = 3;

    private readonly IApplicationDbContext _dbContext;
    private readonly IAsyncQueryExecutor _queryExecutor;
    private readonly IExecutiveSummaryRegenerator _regenerator;
    private readonly IAuditLogService _auditLogService;

    /// <summary>Initializes a new instance of the <see cref="RegenerateExecutiveSummaryCommandHandler"/> class.</summary>
    /// <param name="dbContext">The persistence port.</param>
    /// <param name="queryExecutor">The async LINQ execution port.</param>
    /// <param name="regenerator">The executive-summary regeneration port.</param>
    /// <param name="auditLogService">The privileged-action audit log port.</param>
    public RegenerateExecutiveSummaryCommandHandler(
        IApplicationDbContext dbContext, IAsyncQueryExecutor queryExecutor, IExecutiveSummaryRegenerator regenerator, IAuditLogService auditLogService)
    {
        _dbContext = dbContext;
        _queryExecutor = queryExecutor;
        _regenerator = regenerator;
        _auditLogService = auditLogService;
    }

    /// <inheritdoc />
    public async Task<Result<RegenerateExecutiveSummaryResultDto>> Handle(RegenerateExecutiveSummaryCommand request, CancellationToken cancellationToken)
    {
        FinalMediaReport? report = await _queryExecutor.SingleOrDefaultAsync(
            _dbContext.FinalMediaReports.Where(r => r.Id == request.ReportId), cancellationToken);
        if (report is null)
        {
            return Result<RegenerateExecutiveSummaryResultDto>.Failure("التقرير غير موجود");
        }

        var summary = await _regenerator.RegenerateAsync(
            report.Title,
            report.PeriodLabel,
            JsonSerializer.Serialize(report.Kpis),
            JsonSerializer.Serialize(report.TopNews.Take(TopNewsLimit)),
            JsonSerializer.Serialize(report.Recommendations.Take(RecommendationsLimit)),
            cancellationToken);

        await _auditLogService.RecordAsync(
            request.ActorUserId,
            "final_media_report.exec_summary_regenerate",
            "FinalMediaReport",
            request.ReportId.ToString(System.Globalization.CultureInfo.InvariantCulture),
            before: null,
            after: null,
            cancellationToken);

        return Result<RegenerateExecutiveSummaryResultDto>.Success(new RegenerateExecutiveSummaryResultDto(summary, report.ReportNumber));
    }
}
