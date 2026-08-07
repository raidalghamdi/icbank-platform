using Icbank.Platform.Application.Common.Interfaces;
using Icbank.Platform.Application.Common.Models;
using Icbank.Platform.Domain.Shorfah;
using MediatR;

namespace Icbank.Platform.Application.Shorfah.Queries;

/// <summary>Handles <see cref="ListShorfahWorkflowLogQuery"/>. Ports <c>shorfah.ts:536-541</c>.</summary>
public sealed class ListShorfahWorkflowLogQueryHandler : IRequestHandler<ListShorfahWorkflowLogQuery, Result<PagedResult<ShorfahWorkflowLogDto>>>
{
    private readonly IApplicationDbContext _dbContext;
    private readonly IAsyncQueryExecutor _queryExecutor;

    /// <summary>Initializes a new instance of the <see cref="ListShorfahWorkflowLogQueryHandler"/> class.</summary>
    /// <param name="dbContext">The persistence port.</param>
    /// <param name="queryExecutor">The async LINQ execution port.</param>
    public ListShorfahWorkflowLogQueryHandler(IApplicationDbContext dbContext, IAsyncQueryExecutor queryExecutor)
    {
        _dbContext = dbContext;
        _queryExecutor = queryExecutor;
    }

    /// <inheritdoc />
    public async Task<Result<PagedResult<ShorfahWorkflowLogDto>>> Handle(ListShorfahWorkflowLogQuery request, CancellationToken cancellationToken)
    {
        IQueryable<ShorfahWorkflowLog> ordered = _dbContext.ShorfahWorkflowLogs
            .Where(l => l.SectionId == request.SectionId)
            .OrderByDescending(l => l.CreatedAt);

        List<int> allIds = await _queryExecutor.ToListAsync(ordered.Select(l => l.Id), cancellationToken);

        List<ShorfahWorkflowLog> page = await _queryExecutor.ToListAsync(
            ordered.Skip((request.Query.Page - 1) * request.Query.PageSize).Take(request.Query.PageSize), cancellationToken);

        var items = page
            .Select(l => new ShorfahWorkflowLogDto(l.Id, l.SectionId, l.ActorUserId, l.Action, l.FromStatus, l.ToStatus, l.Notes, l.CreatedAt))
            .ToList();

        return Result<PagedResult<ShorfahWorkflowLogDto>>.Success(
            new PagedResult<ShorfahWorkflowLogDto>(items, request.Query.Page, request.Query.PageSize, allIds.Count));
    }
}
