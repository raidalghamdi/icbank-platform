using Icbank.Platform.Application.Common.Interfaces;
using Icbank.Platform.Application.Common.Models;
using Icbank.Platform.Domain.Shorfah;
using MediatR;

namespace Icbank.Platform.Application.Shorfah.Queries;

/// <summary>Handles <see cref="ListShorfahSlaDefaultsQuery"/>. Ports <c>shorfah.ts:271-274</c>.</summary>
public sealed class ListShorfahSlaDefaultsQueryHandler : IRequestHandler<ListShorfahSlaDefaultsQuery, Result<IReadOnlyList<ShorfahSlaDefaultDto>>>
{
    private readonly IApplicationDbContext _dbContext;
    private readonly IAsyncQueryExecutor _queryExecutor;

    /// <summary>Initializes a new instance of the <see cref="ListShorfahSlaDefaultsQueryHandler"/> class.</summary>
    /// <param name="dbContext">The persistence port.</param>
    /// <param name="queryExecutor">The async LINQ execution port.</param>
    public ListShorfahSlaDefaultsQueryHandler(IApplicationDbContext dbContext, IAsyncQueryExecutor queryExecutor)
    {
        _dbContext = dbContext;
        _queryExecutor = queryExecutor;
    }

    /// <inheritdoc />
    public async Task<Result<IReadOnlyList<ShorfahSlaDefaultDto>>> Handle(ListShorfahSlaDefaultsQuery request, CancellationToken cancellationToken)
    {
        List<ShorfahSectionSlaDefault> rows = await _queryExecutor.ToListAsync(_dbContext.ShorfahSectionSlaDefaults, cancellationToken);
        IReadOnlyList<ShorfahSlaDefaultDto> items = rows
            .Select(r => new ShorfahSlaDefaultDto(r.SectionType.ToString(), r.SlaDays))
            .ToList();
        return Result<IReadOnlyList<ShorfahSlaDefaultDto>>.Success(items);
    }
}
