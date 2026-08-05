using Icbank.Platform.Application.Common.Interfaces;
using Icbank.Platform.Application.Common.Models;
using Icbank.Platform.Domain.Reports;
using MediatR;

namespace Icbank.Platform.Application.Reports.Queries;

/// <summary>Handles <see cref="GetLatestDailyReportQuery"/>.</summary>
public sealed class GetLatestDailyReportQueryHandler : IRequestHandler<GetLatestDailyReportQuery, Result<DailyReportDto>>
{
    private readonly IApplicationDbContext _dbContext;
    private readonly IAsyncQueryExecutor _queryExecutor;

    /// <summary>Initializes a new instance of the <see cref="GetLatestDailyReportQueryHandler"/> class.</summary>
    /// <param name="dbContext">The persistence port.</param>
    /// <param name="queryExecutor">The async LINQ execution port.</param>
    public GetLatestDailyReportQueryHandler(IApplicationDbContext dbContext, IAsyncQueryExecutor queryExecutor)
    {
        _dbContext = dbContext;
        _queryExecutor = queryExecutor;
    }

    /// <inheritdoc />
    public async Task<Result<DailyReportDto>> Handle(GetLatestDailyReportQuery request, CancellationToken cancellationToken)
    {
        List<DailyReport> latest = await _queryExecutor.ToListAsync(
            _dbContext.DailyReports.OrderByDescending(r => r.ReportDate).Take(1), cancellationToken);

        DailyReport? report = latest.FirstOrDefault();
        return report is null
            ? Result<DailyReportDto>.Failure("No report found")
            : Result<DailyReportDto>.Success(new DailyReportDto(report.Id, report.ReportDate, report.ReportDataJson));
    }
}
