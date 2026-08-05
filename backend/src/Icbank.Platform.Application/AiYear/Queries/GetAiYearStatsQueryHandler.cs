using Icbank.Platform.Application.Common.Interfaces;
using Icbank.Platform.Application.Common.Models;
using MediatR;

namespace Icbank.Platform.Application.AiYear.Queries;

/// <summary>
/// Handles <see cref="GetAiYearStatsQuery"/>. Ports BUSINESS-RULES.md §3's aggregation exactly
/// (all 12 months present in <c>ByMonth</c> even if zero, keyed maps for type/channel breakdowns).
/// </summary>
public sealed class GetAiYearStatsQueryHandler : IRequestHandler<GetAiYearStatsQuery, Result<AiYearStatsDto>>
{
    private const int MonthsInYear = 12;

    private readonly IApplicationDbContext _dbContext;
    private readonly IAsyncQueryExecutor _queryExecutor;

    /// <summary>Initializes a new instance of the <see cref="GetAiYearStatsQueryHandler"/> class.</summary>
    /// <param name="dbContext">The persistence port.</param>
    /// <param name="queryExecutor">The async LINQ execution port.</param>
    public GetAiYearStatsQueryHandler(IApplicationDbContext dbContext, IAsyncQueryExecutor queryExecutor)
    {
        _dbContext = dbContext;
        _queryExecutor = queryExecutor;
    }

    /// <inheritdoc />
    public async Task<Result<AiYearStatsDto>> Handle(GetAiYearStatsQuery request, CancellationToken cancellationToken)
    {
        List<Domain.AiYear.AiYearActivation> activations = await _queryExecutor.ToListAsync(_dbContext.AiYearActivations, cancellationToken);
        var mediaCount = (await _queryExecutor.ToListAsync(_dbContext.AiYearMedia.Select(m => m.Id), cancellationToken)).Count;
        List<Domain.AiYear.AiYearActivationChannel> channels = await _queryExecutor.ToListAsync(_dbContext.AiYearActivationChannels, cancellationToken);

        var byMonth = Enumerable.Range(1, MonthsInYear).ToDictionary(month => month, month => activations.Count(a => a.Month == month));
        var byType = activations.GroupBy(a => a.Type).ToDictionary(g => g.Key, g => g.Count());
        var byChannel = channels.GroupBy(c => c.Channel).ToDictionary(g => g.Key, g => g.Count());
        DateTime? lastUpdated = activations.Count == 0 ? null : activations.Max(a => a.UpdatedAt ?? a.CreatedAt);

        var dto = new AiYearStatsDto(
            activations.Count,
            mediaCount,
            byChannel.Count,
            lastUpdated,
            byMonth,
            byType,
            byChannel);

        return Result<AiYearStatsDto>.Success(dto);
    }
}
