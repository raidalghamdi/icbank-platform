using Icbank.Platform.Application.Common.Interfaces;
using Icbank.Platform.Application.Common.Models;
using Icbank.Platform.Domain.Designs;
using MediatR;

namespace Icbank.Platform.Application.Designs.Composer.Queries;

/// <summary>Handles <see cref="ListBrandLogosQuery"/>. Behaviour change: paginated envelope instead of the Node source's unbounded list.</summary>
public sealed class ListBrandLogosQueryHandler : IRequestHandler<ListBrandLogosQuery, Result<PagedResult<BrandLogoDto>>>
{
    private readonly IApplicationDbContext _dbContext;
    private readonly IAsyncQueryExecutor _queryExecutor;

    /// <summary>Initializes a new instance of the <see cref="ListBrandLogosQueryHandler"/> class.</summary>
    /// <param name="dbContext">The persistence port.</param>
    /// <param name="queryExecutor">The async LINQ execution port.</param>
    public ListBrandLogosQueryHandler(IApplicationDbContext dbContext, IAsyncQueryExecutor queryExecutor)
    {
        _dbContext = dbContext;
        _queryExecutor = queryExecutor;
    }

    /// <inheritdoc />
    public async Task<Result<PagedResult<BrandLogoDto>>> Handle(ListBrandLogosQuery request, CancellationToken cancellationToken)
    {
        IQueryable<BrandLogo> query = _dbContext.BrandLogos.OrderByDescending(l => l.CreatedAt);
        List<int> allIds = await _queryExecutor.ToListAsync(query.Select(l => l.Id), cancellationToken);
        List<BrandLogo> page = await _queryExecutor.ToListAsync(
            query.Skip((request.Paging.Page - 1) * request.Paging.PageSize).Take(request.Paging.PageSize), cancellationToken);

        var items = page.Select(BrandAssetMapper.ToDto).ToList();
        return Result<PagedResult<BrandLogoDto>>.Success(new PagedResult<BrandLogoDto>(items, request.Paging.Page, request.Paging.PageSize, allIds.Count));
    }
}
