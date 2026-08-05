using Icbank.Platform.Application.Common.Interfaces;
using Icbank.Platform.Application.Common.Models;
using Icbank.Platform.Domain.Shorfah;
using MediatR;

namespace Icbank.Platform.Application.Shorfah.Queries;

/// <summary>Handles <see cref="ListShorfahIssuesQuery"/>.</summary>
public sealed class ListShorfahIssuesQueryHandler : IRequestHandler<ListShorfahIssuesQuery, Result<PagedResult<ShorfahIssueDto>>>
{
    private readonly IApplicationDbContext _dbContext;
    private readonly IAsyncQueryExecutor _queryExecutor;

    /// <summary>Initializes a new instance of the <see cref="ListShorfahIssuesQueryHandler"/> class.</summary>
    /// <param name="dbContext">The persistence port.</param>
    /// <param name="queryExecutor">The async LINQ execution port.</param>
    public ListShorfahIssuesQueryHandler(IApplicationDbContext dbContext, IAsyncQueryExecutor queryExecutor)
    {
        _dbContext = dbContext;
        _queryExecutor = queryExecutor;
    }

    /// <inheritdoc />
    public async Task<Result<PagedResult<ShorfahIssueDto>>> Handle(ListShorfahIssuesQuery request, CancellationToken cancellationToken)
    {
        IQueryable<ShorfahIssue> ordered = _dbContext.ShorfahIssues
            .OrderByDescending(i => i.Year)
            .ThenByDescending(i => i.Month);

        List<int> total = await _queryExecutor.ToListAsync(ordered.Select(i => i.Id), cancellationToken);

        List<ShorfahIssue> page = await _queryExecutor.ToListAsync(
            ordered.Skip((request.Query.Page - 1) * request.Query.PageSize).Take(request.Query.PageSize), cancellationToken);

        var items = page.Select(ShorfahMappers.ToDto).ToList();
        return Result<PagedResult<ShorfahIssueDto>>.Success(
            new PagedResult<ShorfahIssueDto>(items, request.Query.Page, request.Query.PageSize, total.Count));
    }
}
