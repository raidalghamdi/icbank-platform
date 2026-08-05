using Icbank.Platform.Application.Common.Interfaces;
using Icbank.Platform.Application.Common.Models;
using Icbank.Platform.Domain.Designs;
using MediatR;

namespace Icbank.Platform.Application.Designs.Composer.Queries;

/// <summary>Handles <see cref="ListBrandFontsQuery"/>. Behaviour change: paginated envelope instead of the Node source's unbounded list.</summary>
public sealed class ListBrandFontsQueryHandler : IRequestHandler<ListBrandFontsQuery, Result<PagedResult<BrandFontDto>>>
{
    private readonly IApplicationDbContext _dbContext;
    private readonly IAsyncQueryExecutor _queryExecutor;

    /// <summary>Initializes a new instance of the <see cref="ListBrandFontsQueryHandler"/> class.</summary>
    /// <param name="dbContext">The persistence port.</param>
    /// <param name="queryExecutor">The async LINQ execution port.</param>
    public ListBrandFontsQueryHandler(IApplicationDbContext dbContext, IAsyncQueryExecutor queryExecutor)
    {
        _dbContext = dbContext;
        _queryExecutor = queryExecutor;
    }

    /// <inheritdoc />
    public async Task<Result<PagedResult<BrandFontDto>>> Handle(ListBrandFontsQuery request, CancellationToken cancellationToken)
    {
        IQueryable<BrandFont> query = _dbContext.BrandFonts.OrderByDescending(f => f.CreatedAt);
        List<int> allIds = await _queryExecutor.ToListAsync(query.Select(f => f.Id), cancellationToken);
        List<BrandFont> page = await _queryExecutor.ToListAsync(
            query.Skip((request.Paging.Page - 1) * request.Paging.PageSize).Take(request.Paging.PageSize), cancellationToken);

        var items = page.Select(BrandAssetMapper.ToDto).ToList();
        return Result<PagedResult<BrandFontDto>>.Success(new PagedResult<BrandFontDto>(items, request.Paging.Page, request.Paging.PageSize, allIds.Count));
    }
}
