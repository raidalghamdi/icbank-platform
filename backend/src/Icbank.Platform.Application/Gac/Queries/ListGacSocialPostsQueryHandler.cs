using Icbank.Platform.Application.Common.Interfaces;
using Icbank.Platform.Application.Common.Models;
using Icbank.Platform.Domain.Gac;
using MediatR;

namespace Icbank.Platform.Application.Gac.Queries;

/// <summary>Handles <see cref="ListGacSocialPostsQuery"/>.</summary>
public sealed class ListGacSocialPostsQueryHandler : IRequestHandler<ListGacSocialPostsQuery, Result<PagedResult<GacSocialPostDto>>>
{
    private readonly IApplicationDbContext _dbContext;
    private readonly IAsyncQueryExecutor _queryExecutor;

    /// <summary>Initializes a new instance of the <see cref="ListGacSocialPostsQueryHandler"/> class.</summary>
    /// <param name="dbContext">The persistence port.</param>
    /// <param name="queryExecutor">The async LINQ execution port.</param>
    public ListGacSocialPostsQueryHandler(IApplicationDbContext dbContext, IAsyncQueryExecutor queryExecutor)
    {
        _dbContext = dbContext;
        _queryExecutor = queryExecutor;
    }

    /// <inheritdoc />
    public async Task<Result<PagedResult<GacSocialPostDto>>> Handle(ListGacSocialPostsQuery request, CancellationToken cancellationToken)
    {
        IQueryable<GacSocialPost> query = _dbContext.GacSocialPosts;

        if (!string.IsNullOrWhiteSpace(request.Platform) &&
            Enum.TryParse(request.Platform, ignoreCase: true, out GacSocialPlatform platform))
        {
            query = query.Where(p => p.Platform == platform);
        }

        query = query.OrderByDescending(p => p.PostedAt);

        List<int> allIds = await _queryExecutor.ToListAsync(query.Select(p => p.Id), cancellationToken);
        var total = allIds.Count;

        List<GacSocialPost> page = await _queryExecutor.ToListAsync(
            query.Skip((request.Query.Page - 1) * request.Query.PageSize).Take(request.Query.PageSize),
            cancellationToken);

        var items = page.Select(ToDto).ToList();
        return Result<PagedResult<GacSocialPostDto>>.Success(new PagedResult<GacSocialPostDto>(items, request.Query.Page, request.Query.PageSize, total));
    }

    private static GacSocialPostDto ToDto(GacSocialPost p) => new(
        p.Id, p.Platform.ToString(), p.ExternalId, p.ContentAr, p.ContentEn, p.PostUrl, p.MediaUrl, p.MediaType.ToString(), p.PostedAt, p.Account);
}
