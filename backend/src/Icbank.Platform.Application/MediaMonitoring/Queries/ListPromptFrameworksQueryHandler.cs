using Icbank.Platform.Application.Common.Interfaces;
using Icbank.Platform.Application.Common.Models;
using Icbank.Platform.Domain.MediaMonitoring;
using MediatR;

namespace Icbank.Platform.Application.MediaMonitoring.Queries;

/// <summary>Handles <see cref="ListPromptFrameworksQuery"/>. Node source only ever returned <c>active</c>-status frameworks; this port replicates that filter.</summary>
public sealed class ListPromptFrameworksQueryHandler : IRequestHandler<ListPromptFrameworksQuery, Result<PagedResult<PromptFrameworkDto>>>
{
    private readonly IApplicationDbContext _dbContext;
    private readonly IAsyncQueryExecutor _queryExecutor;

    /// <summary>Initializes a new instance of the <see cref="ListPromptFrameworksQueryHandler"/> class.</summary>
    /// <param name="dbContext">The persistence port.</param>
    /// <param name="queryExecutor">The async LINQ execution port.</param>
    public ListPromptFrameworksQueryHandler(IApplicationDbContext dbContext, IAsyncQueryExecutor queryExecutor)
    {
        _dbContext = dbContext;
        _queryExecutor = queryExecutor;
    }

    /// <inheritdoc />
    public async Task<Result<PagedResult<PromptFrameworkDto>>> Handle(ListPromptFrameworksQuery request, CancellationToken cancellationToken)
    {
        IQueryable<PromptFramework> query = _dbContext.PromptFrameworks.Where(f => f.Status == PromptFrameworkStatus.Active);

        if (!string.IsNullOrWhiteSpace(request.Category) &&
            Enum.TryParse(request.Category, ignoreCase: true, out PromptFrameworkCategory category))
        {
            query = query.Where(f => f.Category == category);
        }

        if (!string.IsNullOrWhiteSpace(request.Kind) &&
            Enum.TryParse(request.Kind, ignoreCase: true, out PromptFrameworkKind kind))
        {
            query = query.Where(f => f.Kind == kind);
        }

        query = query.OrderByDescending(f => f.CreatedAt);

        List<int> allIds = await _queryExecutor.ToListAsync(query.Select(f => f.Id), cancellationToken);
        var total = allIds.Count;
        List<PromptFramework> page = await _queryExecutor.ToListAsync(
            query.Skip((request.Query.Page - 1) * request.Query.PageSize).Take(request.Query.PageSize), cancellationToken);

        var items = page.Select(PromptFrameworkMapper.ToDto).ToList();
        return Result<PagedResult<PromptFrameworkDto>>.Success(new PagedResult<PromptFrameworkDto>(items, request.Query.Page, request.Query.PageSize, total));
    }
}
