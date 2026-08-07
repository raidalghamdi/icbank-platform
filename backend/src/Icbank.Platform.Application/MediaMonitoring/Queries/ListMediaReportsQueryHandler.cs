using Icbank.Platform.Application.Common.Interfaces;
using Icbank.Platform.Application.Common.Models;
using Icbank.Platform.Domain.MediaMonitoring;
using MediatR;

namespace Icbank.Platform.Application.MediaMonitoring.Queries;

/// <summary>
/// Handles <see cref="ListMediaReportsQuery"/>. Node source (<c>GET /media-reports</c>) only
/// ever returned <c>published</c>-status reports; this port replicates that filter and adds the
/// mandated pagination envelope (R-BE-033) in place of the Node source's ad-hoc <c>limit</c> cap.
/// </summary>
public sealed class ListMediaReportsQueryHandler : IRequestHandler<ListMediaReportsQuery, Result<PagedResult<MediaReportDto>>>
{
    private readonly IApplicationDbContext _dbContext;
    private readonly IAsyncQueryExecutor _queryExecutor;

    /// <summary>Initializes a new instance of the <see cref="ListMediaReportsQueryHandler"/> class.</summary>
    /// <param name="dbContext">The persistence port.</param>
    /// <param name="queryExecutor">The async LINQ execution port.</param>
    public ListMediaReportsQueryHandler(IApplicationDbContext dbContext, IAsyncQueryExecutor queryExecutor)
    {
        _dbContext = dbContext;
        _queryExecutor = queryExecutor;
    }

    /// <inheritdoc />
    public async Task<Result<PagedResult<MediaReportDto>>> Handle(ListMediaReportsQuery request, CancellationToken cancellationToken)
    {
        IQueryable<MediaReport> query = _dbContext.MediaReports.Where(r => r.Status == MediaReportStatus.Published);

        if (!string.IsNullOrWhiteSpace(request.Audience) &&
            Enum.TryParse(request.Audience, ignoreCase: true, out MediaReportAudience audience))
        {
            query = query.Where(r => r.Audience == audience);
        }

        if (!string.IsNullOrWhiteSpace(request.ReportType) &&
            Enum.TryParse(request.ReportType, ignoreCase: true, out MediaReportType reportType))
        {
            query = query.Where(r => r.ReportType == reportType);
        }

        query = query.OrderByDescending(r => r.CreatedAt);

        List<int> allIds = await _queryExecutor.ToListAsync(query.Select(r => r.Id), cancellationToken);
        var total = allIds.Count;
        List<MediaReport> page = await _queryExecutor.ToListAsync(
            query.Skip((request.Query.Page - 1) * request.Query.PageSize).Take(request.Query.PageSize), cancellationToken);

        var items = page.Select(MediaReportMapper.ToDto).ToList();
        return Result<PagedResult<MediaReportDto>>.Success(new PagedResult<MediaReportDto>(items, request.Query.Page, request.Query.PageSize, total));
    }
}
