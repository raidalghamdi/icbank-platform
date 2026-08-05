using Icbank.Platform.Application.Common.Interfaces;
using Icbank.Platform.Application.Common.Models;
using Icbank.Platform.Domain.Gac;
using MediatR;

namespace Icbank.Platform.Application.Gac.Queries;

/// <summary>Handles <see cref="ListGacPublicationCategoriesQuery"/>.</summary>
public sealed class ListGacPublicationCategoriesQueryHandler
    : IRequestHandler<ListGacPublicationCategoriesQuery, Result<IReadOnlyList<GacPublicationCategoryCountDto>>>
{
    private readonly IApplicationDbContext _dbContext;
    private readonly IAsyncQueryExecutor _queryExecutor;

    /// <summary>Initializes a new instance of the <see cref="ListGacPublicationCategoriesQueryHandler"/> class.</summary>
    /// <param name="dbContext">The persistence port.</param>
    /// <param name="queryExecutor">The async LINQ execution port.</param>
    public ListGacPublicationCategoriesQueryHandler(IApplicationDbContext dbContext, IAsyncQueryExecutor queryExecutor)
    {
        _dbContext = dbContext;
        _queryExecutor = queryExecutor;
    }

    /// <inheritdoc />
    public async Task<Result<IReadOnlyList<GacPublicationCategoryCountDto>>> Handle(
        ListGacPublicationCategoriesQuery request, CancellationToken cancellationToken)
    {
        List<GacPublicationCategory> categories = await _queryExecutor.ToListAsync(
            _dbContext.GacPublications.Where(p => p.Status == GacPublicationStatus.Published).Select(p => p.Category),
            cancellationToken);

        IReadOnlyList<GacPublicationCategoryCountDto> counts = categories
            .GroupBy(c => c)
            .Select(g => new GacPublicationCategoryCountDto(g.Key.ToString(), g.Count()))
            .OrderBy(c => c.Category, StringComparer.Ordinal)
            .ToList();

        return Result<IReadOnlyList<GacPublicationCategoryCountDto>>.Success(counts);
    }
}
