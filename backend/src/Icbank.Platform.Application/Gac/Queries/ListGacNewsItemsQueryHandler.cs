using Icbank.Platform.Application.Common.Interfaces;
using Icbank.Platform.Application.Common.Models;
using Icbank.Platform.Domain.Gac;
using MediatR;

namespace Icbank.Platform.Application.Gac.Queries;

/// <summary>Handles <see cref="ListGacNewsItemsQuery"/>.</summary>
public sealed class ListGacNewsItemsQueryHandler : IRequestHandler<ListGacNewsItemsQuery, Result<PagedResult<GacNewsItemDto>>>
{
    private readonly IApplicationDbContext _dbContext;
    private readonly IAsyncQueryExecutor _queryExecutor;

    /// <summary>Initializes a new instance of the <see cref="ListGacNewsItemsQueryHandler"/> class.</summary>
    /// <param name="dbContext">The persistence port.</param>
    /// <param name="queryExecutor">The async LINQ execution port.</param>
    public ListGacNewsItemsQueryHandler(IApplicationDbContext dbContext, IAsyncQueryExecutor queryExecutor)
    {
        _dbContext = dbContext;
        _queryExecutor = queryExecutor;
    }

    /// <inheritdoc />
    public async Task<Result<PagedResult<GacNewsItemDto>>> Handle(ListGacNewsItemsQuery request, CancellationToken cancellationToken)
    {
        IQueryable<GacNewsItem> query = _dbContext.GacNewsItems;

        if (!string.IsNullOrWhiteSpace(request.Kind) && Enum.TryParse(request.Kind, ignoreCase: true, out GacNewsKind kind))
        {
            query = query.Where(n => n.Kind == kind);
        }

        query = query.OrderByDescending(n => n.PublishedAt);

        List<int> allIds = await _queryExecutor.ToListAsync(query.Select(n => n.Id), cancellationToken);
        var total = allIds.Count;

        List<GacNewsItem> page = await _queryExecutor.ToListAsync(
            query.Skip((request.Query.Page - 1) * request.Query.PageSize).Take(request.Query.PageSize),
            cancellationToken);

        var items = page.Select(ToDto).ToList();
        return Result<PagedResult<GacNewsItemDto>>.Success(new PagedResult<GacNewsItemDto>(items, request.Query.Page, request.Query.PageSize, total));
    }

    private static GacNewsItemDto ToDto(GacNewsItem n) => new(
        n.Id, n.Kind.ToString(), n.TitleAr, n.TitleEn, n.BodyAr, n.Category?.ToString(), n.SourceUrl, n.ExternalRef, n.PublishedAt);
}
