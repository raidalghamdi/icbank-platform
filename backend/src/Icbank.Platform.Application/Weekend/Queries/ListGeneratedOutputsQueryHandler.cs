using Icbank.Platform.Application.Common.Interfaces;
using Icbank.Platform.Application.Common.Models;
using Icbank.Platform.Domain.Weekend;
using MediatR;

namespace Icbank.Platform.Application.Weekend.Queries;

/// <summary>Handles <see cref="ListGeneratedOutputsQuery"/>.</summary>
public sealed class ListGeneratedOutputsQueryHandler : IRequestHandler<ListGeneratedOutputsQuery, Result<PagedResult<GeneratedOutputDto>>>
{
    private readonly IApplicationDbContext _dbContext;
    private readonly IAsyncQueryExecutor _queryExecutor;

    /// <summary>Initializes a new instance of the <see cref="ListGeneratedOutputsQueryHandler"/> class.</summary>
    /// <param name="dbContext">The persistence port.</param>
    /// <param name="queryExecutor">The async LINQ execution port.</param>
    public ListGeneratedOutputsQueryHandler(IApplicationDbContext dbContext, IAsyncQueryExecutor queryExecutor)
    {
        _dbContext = dbContext;
        _queryExecutor = queryExecutor;
    }

    /// <inheritdoc />
    public async Task<Result<PagedResult<GeneratedOutputDto>>> Handle(ListGeneratedOutputsQuery request, CancellationToken cancellationToken)
    {
        IQueryable<GeneratedOutput> ordered = _dbContext.GeneratedOutputs.OrderByDescending(o => o.CreatedAt);
        var total = (await _queryExecutor.ToListAsync(ordered, cancellationToken)).Count;

        List<GeneratedOutput> page = await _queryExecutor.ToListAsync(
            ordered.Skip((request.Query.Page - 1) * request.Query.PageSize).Take(request.Query.PageSize), cancellationToken);

        var items = page.Select(ToDto).ToList();
        return Result<PagedResult<GeneratedOutputDto>>.Success(new PagedResult<GeneratedOutputDto>(items, request.Query.Page, request.Query.PageSize, total));
    }

    private static GeneratedOutputDto ToDto(GeneratedOutput output) =>
        new(output.Id, output.Topic, output.ModelName, output.OutputText, output.Selected, output.CreatedAt);
}
