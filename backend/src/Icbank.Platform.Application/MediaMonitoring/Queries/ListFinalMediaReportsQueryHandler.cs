using Icbank.Platform.Application.Common.Interfaces;
using Icbank.Platform.Application.Common.Models;
using Icbank.Platform.Domain.MediaMonitoring;
using MediatR;

namespace Icbank.Platform.Application.MediaMonitoring.Queries;

/// <summary>Handles <see cref="ListFinalMediaReportsQuery"/>.</summary>
public sealed class ListFinalMediaReportsQueryHandler : IRequestHandler<ListFinalMediaReportsQuery, Result<PagedResult<FinalMediaReportDto>>>
{
    private readonly IApplicationDbContext _dbContext;
    private readonly IAsyncQueryExecutor _queryExecutor;

    /// <summary>Initializes a new instance of the <see cref="ListFinalMediaReportsQueryHandler"/> class.</summary>
    /// <param name="dbContext">The persistence port.</param>
    /// <param name="queryExecutor">The async LINQ execution port.</param>
    public ListFinalMediaReportsQueryHandler(IApplicationDbContext dbContext, IAsyncQueryExecutor queryExecutor)
    {
        _dbContext = dbContext;
        _queryExecutor = queryExecutor;
    }

    /// <inheritdoc />
    public async Task<Result<PagedResult<FinalMediaReportDto>>> Handle(ListFinalMediaReportsQuery request, CancellationToken cancellationToken)
    {
        IQueryable<FinalMediaReport> query = _dbContext.FinalMediaReports;

        if (!string.IsNullOrWhiteSpace(request.ReportType) &&
            Enum.TryParse(request.ReportType, ignoreCase: true, out MediaReportType reportType))
        {
            query = query.Where(r => r.ReportType == reportType);
        }

        if (request.Year.HasValue)
        {
            query = query.Where(r => r.IssueDate.Year == request.Year.Value);
        }

        query = query.OrderByDescending(r => r.CreatedAt);

        List<int> allIds = await _queryExecutor.ToListAsync(query.Select(r => r.Id), cancellationToken);
        var total = allIds.Count;
        List<FinalMediaReport> page = await _queryExecutor.ToListAsync(
            query.Skip((request.Query.Page - 1) * request.Query.PageSize).Take(request.Query.PageSize), cancellationToken);

        var items = page.Select(FinalMediaReportMapper.ToSummaryDto).ToList();
        return Result<PagedResult<FinalMediaReportDto>>.Success(new PagedResult<FinalMediaReportDto>(items, request.Query.Page, request.Query.PageSize, total));
    }
}
