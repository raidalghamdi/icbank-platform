using Icbank.Platform.Application.Common.Interfaces;
using Icbank.Platform.Application.Common.Models;
using Icbank.Platform.Domain.Weekend;
using MediatR;

namespace Icbank.Platform.Application.Weekend.Queries;

/// <summary>Handles <see cref="ListWeekendPlacesQuery"/>.</summary>
public sealed class ListWeekendPlacesQueryHandler : IRequestHandler<ListWeekendPlacesQuery, Result<PagedResult<WeekendPlaceDto>>>
{
    private readonly IApplicationDbContext _dbContext;
    private readonly IAsyncQueryExecutor _queryExecutor;

    /// <summary>Initializes a new instance of the <see cref="ListWeekendPlacesQueryHandler"/> class.</summary>
    /// <param name="dbContext">The persistence port.</param>
    /// <param name="queryExecutor">The async LINQ execution port.</param>
    public ListWeekendPlacesQueryHandler(IApplicationDbContext dbContext, IAsyncQueryExecutor queryExecutor)
    {
        _dbContext = dbContext;
        _queryExecutor = queryExecutor;
    }

    /// <inheritdoc />
    public async Task<Result<PagedResult<WeekendPlaceDto>>> Handle(ListWeekendPlacesQuery request, CancellationToken cancellationToken)
    {
        IQueryable<WeekendPlace> ordered = _dbContext.WeekendPlaces.OrderBy(p => p.SortOrder).ThenBy(p => p.CreatedAt);
        var total = (await _queryExecutor.ToListAsync(ordered, cancellationToken)).Count;

        List<WeekendPlace> page = await _queryExecutor.ToListAsync(
            ordered.Skip((request.Query.Page - 1) * request.Query.PageSize).Take(request.Query.PageSize), cancellationToken);

        var items = page.Select(ToDto).ToList();
        return Result<PagedResult<WeekendPlaceDto>>.Success(new PagedResult<WeekendPlaceDto>(items, request.Query.Page, request.Query.PageSize, total));
    }

    private static WeekendPlaceDto ToDto(WeekendPlace place) =>
        new(place.Id, place.Name, place.Description, place.ImageUrl, place.City, place.MapsQuery, place.IsActive, place.SortOrder);
}
