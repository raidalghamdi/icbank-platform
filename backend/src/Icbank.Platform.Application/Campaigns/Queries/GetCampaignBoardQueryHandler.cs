using Icbank.Platform.Application.Common.Interfaces;
using Icbank.Platform.Application.Common.Models;
using Icbank.Platform.Domain.Campaigns;
using MediatR;

namespace Icbank.Platform.Application.Campaigns.Queries;

/// <summary>
/// Handles <see cref="GetCampaignBoardQuery"/>. Returns the cards, the headline figures and the
/// per-state counts already resolved in one round trip, so a campaigns page paints as soon as the
/// response lands and the filter chips can show their counts without a second request.
/// </summary>
public sealed class GetCampaignBoardQueryHandler : IRequestHandler<GetCampaignBoardQuery, Result<CampaignBoardDto>>
{
    private readonly IApplicationDbContext _dbContext;
    private readonly IAsyncQueryExecutor _queryExecutor;
    private readonly IDateTimeProvider _clock;

    /// <summary>Initializes a new instance of the <see cref="GetCampaignBoardQueryHandler"/> class.</summary>
    /// <param name="dbContext">The persistence port.</param>
    /// <param name="queryExecutor">The async LINQ execution port.</param>
    /// <param name="clock">The clock port.</param>
    public GetCampaignBoardQueryHandler(IApplicationDbContext dbContext, IAsyncQueryExecutor queryExecutor, IDateTimeProvider clock)
    {
        _dbContext = dbContext;
        _queryExecutor = queryExecutor;
        _clock = clock;
    }

    /// <inheritdoc />
    public async Task<Result<CampaignBoardDto>> Handle(GetCampaignBoardQuery request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        DateTime now = _clock.UtcNow.UtcDateTime;
        CampaignAudience? audience = CampaignLabels.ParseAudience(request.Audience);
        CampaignStatus? status = CampaignLabels.ParseStatus(request.Status);

        List<CampaignDto> all = await LoadAsync(audience, now, cancellationToken);

        // Why: the KPI row and the chip counts are computed over the whole audience, then the list
        // is narrowed. Counting after the filter would make every chip read as its own total and
        // the KPI row would change every time the user clicked a chip.
        List<CampaignDto> visible = status is null
            ? all
            : all.Where(c => string.Equals(c.Status, CampaignLabels.StatusKey(status.Value), StringComparison.Ordinal)).ToList();

        var payload = new CampaignBoardDto(BuildKpis(all), visible, BuildStatusCounts(all), now);
        return Result<CampaignBoardDto>.Success(payload);
    }

    private static CampaignBoardKpisDto BuildKpis(List<CampaignDto> cards)
    {
        var averageProgress = cards.Count == 0 ? 0 : (int)Math.Round(cards.Average(c => c.ProgressPercent), MidpointRounding.AwayFromZero);

        return new CampaignBoardKpisDto(
            cards.Count,
            CountOf(cards, CampaignStatus.Running),
            CountOf(cards, CampaignStatus.Upcoming),
            CountOf(cards, CampaignStatus.UnderReview),
            CountOf(cards, CampaignStatus.Completed),
            averageProgress,
            cards.Sum(c => c.Analytics.ReachCount));
    }

    private static Dictionary<string, int> BuildStatusCounts(List<CampaignDto> cards)
    {
        var counts = new Dictionary<string, int>(StringComparer.Ordinal) { ["all"] = cards.Count };
        foreach (CampaignStatus status in Enum.GetValues<CampaignStatus>())
        {
            counts[CampaignLabels.StatusKey(status)] = CountOf(cards, status);
        }

        return counts;
    }

    private static int CountOf(List<CampaignDto> cards, CampaignStatus status)
    {
        var key = CampaignLabels.StatusKey(status);
        return cards.Count(c => string.Equals(c.Status, key, StringComparison.Ordinal));
    }

    private async Task<List<CampaignDto>> LoadAsync(CampaignAudience? audience, DateTime now, CancellationToken cancellationToken)
    {
        List<Campaign> campaigns = await _queryExecutor.ToListAsync(
            _dbContext.Campaigns
                .Where(c => c.IsActive && (audience == null || c.Audience == audience))
                .OrderBy(c => c.Status)
                .ThenBy(c => c.SortOrder)
                .ThenBy(c => c.Id),
            cancellationToken);

        List<CampaignDeliverable> deliverables = await _queryExecutor.ToListAsync(
            _dbContext.CampaignDeliverables.OrderBy(d => d.CampaignId).ThenBy(d => d.SortOrder).ThenBy(d => d.Id),
            cancellationToken);

        List<CampaignChannel> channels = await _queryExecutor.ToListAsync(
            _dbContext.CampaignChannels.OrderBy(c => c.CampaignId).ThenBy(c => c.SortOrder).ThenBy(c => c.Id),
            cancellationToken);

        ILookup<int, CampaignDeliverable> deliverablesByCampaign = deliverables.ToLookup(d => d.CampaignId);
        ILookup<int, CampaignChannel> channelsByCampaign = channels.ToLookup(c => c.CampaignId);

        return campaigns
            .Select(campaign => CampaignMapper.ToDto(
                campaign,
                deliverablesByCampaign[campaign.Id].ToList(),
                channelsByCampaign[campaign.Id].ToList(),
                now))
            .ToList();
    }
}
