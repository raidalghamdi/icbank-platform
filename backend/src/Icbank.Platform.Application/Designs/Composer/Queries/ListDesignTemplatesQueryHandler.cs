using Icbank.Platform.Application.Common.Interfaces;
using Icbank.Platform.Application.Common.Models;
using Icbank.Platform.Domain.Designs;
using MediatR;

namespace Icbank.Platform.Application.Designs.Composer.Queries;

/// <summary>
/// Handles <see cref="ListDesignTemplatesQuery"/>. Behaviour change: returns the mandated
/// pagination envelope instead of the Node source's unbounded full-table list.
/// </summary>
public sealed class ListDesignTemplatesQueryHandler : IRequestHandler<ListDesignTemplatesQuery, Result<PagedResult<DesignTemplateDto>>>
{
    private readonly IApplicationDbContext _dbContext;
    private readonly IAsyncQueryExecutor _queryExecutor;

    /// <summary>Initializes a new instance of the <see cref="ListDesignTemplatesQueryHandler"/> class.</summary>
    /// <param name="dbContext">The persistence port.</param>
    /// <param name="queryExecutor">The async LINQ execution port.</param>
    public ListDesignTemplatesQueryHandler(IApplicationDbContext dbContext, IAsyncQueryExecutor queryExecutor)
    {
        _dbContext = dbContext;
        _queryExecutor = queryExecutor;
    }

    /// <inheritdoc />
    public async Task<Result<PagedResult<DesignTemplateDto>>> Handle(ListDesignTemplatesQuery request, CancellationToken cancellationToken)
    {
        IQueryable<DesignTemplate> query = _dbContext.DesignTemplates.OrderByDescending(t => t.CreatedAt);
        List<int> total = await _queryExecutor.ToListAsync(query.Select(t => t.Id), cancellationToken);
        List<DesignTemplate> page = await _queryExecutor.ToListAsync(
            query.Skip((request.Paging.Page - 1) * request.Paging.PageSize).Take(request.Paging.PageSize), cancellationToken);

        var items = page.Select(DesignTemplateMapper.ToDto).ToList();
        return Result<PagedResult<DesignTemplateDto>>.Success(new PagedResult<DesignTemplateDto>(items, request.Paging.Page, request.Paging.PageSize, total.Count));
    }
}
