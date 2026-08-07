using Icbank.Platform.Application.Common.Interfaces;
using Icbank.Platform.Application.Common.Models;
using MediatR;

namespace Icbank.Platform.Application.AiYear.Queries;

/// <summary>Handles <see cref="GetAiYearReportDataQuery"/>.</summary>
public sealed class GetAiYearReportDataQueryHandler : IRequestHandler<GetAiYearReportDataQuery, Result<AiYearReportDataDto>>
{
    private const int TopByReachCount = 3;

    private readonly IApplicationDbContext _dbContext;
    private readonly IAsyncQueryExecutor _queryExecutor;

    /// <summary>Initializes a new instance of the <see cref="GetAiYearReportDataQueryHandler"/> class.</summary>
    /// <param name="dbContext">The persistence port.</param>
    /// <param name="queryExecutor">The async LINQ execution port.</param>
    public GetAiYearReportDataQueryHandler(IApplicationDbContext dbContext, IAsyncQueryExecutor queryExecutor)
    {
        _dbContext = dbContext;
        _queryExecutor = queryExecutor;
    }

    /// <inheritdoc />
    public async Task<Result<AiYearReportDataDto>> Handle(GetAiYearReportDataQuery request, CancellationToken cancellationToken)
    {
        List<Domain.AiYear.AiYearActivation> activations = await _queryExecutor.ToListAsync(
            _dbContext.AiYearActivations.OrderBy(a => a.Month).ThenByDescending(a => a.CreatedAt), cancellationToken);
        var mediaCount = (await _queryExecutor.ToListAsync(_dbContext.AiYearMedia.Select(m => m.Id), cancellationToken)).Count;
        List<Domain.AiYear.AiYearActivationChannel> channels = await _queryExecutor.ToListAsync(_dbContext.AiYearActivationChannels, cancellationToken);
        var channelsByActivation = channels.GroupBy(c => c.ActivationId).ToDictionary(g => g.Key, g => g.Select(c => c.Channel).ToList());

        var rows = activations.Select(a => ToRow(a, channelsByActivation)).ToList();
        var topByReach = rows.Where(r => r.Reach is not null).OrderByDescending(r => r.Reach).Take(TopByReachCount).ToList();
        var byType = activations.GroupBy(a => a.Type).ToDictionary(g => g.Key, g => g.Count());
        var distinctChannelCount = channels.Select(c => c.Channel).Distinct(StringComparer.Ordinal).Count();

        var dto = new AiYearReportDataDto(activations.Count, mediaCount, distinctChannelCount, byType, topByReach, rows);
        return Result<AiYearReportDataDto>.Success(dto);
    }

    private static AiYearReportRowDto ToRow(Domain.AiYear.AiYearActivation activation, Dictionary<int, List<string>> channelsByActivation)
    {
        List<string> channels = channelsByActivation.TryGetValue(activation.Id, out List<string>? list) ? list : new List<string>();
        var monthNameAr = Shorfah.ArabicMonthNames.For(activation.Month);
        return new AiYearReportRowDto(activation.Title, activation.Month, monthNameAr, activation.Type, channels, activation.Reach);
    }
}
