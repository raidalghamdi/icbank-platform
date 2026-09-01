using Icbank.Platform.Application.Common.Interfaces;
using Icbank.Platform.Application.Common.Models;
using Icbank.Platform.Domain.Campaigns;
using MediatR;

namespace Icbank.Platform.Application.Campaigns.Queries;

/// <summary>
/// Handles <see cref="GetCampaignByIdQuery"/>. The detail page is reachable by direct link from
/// the dashboard, so it reads its campaign on its own rather than assuming the board payload is
/// already in the browser.
/// </summary>
public sealed class GetCampaignByIdQueryHandler : IRequestHandler<GetCampaignByIdQuery, Result<CampaignDto>>
{
    private readonly IApplicationDbContext _dbContext;
    private readonly IAsyncQueryExecutor _queryExecutor;
    private readonly IDateTimeProvider _clock;

    /// <summary>Initializes a new instance of the <see cref="GetCampaignByIdQueryHandler"/> class.</summary>
    /// <param name="dbContext">The persistence port.</param>
    /// <param name="queryExecutor">The async LINQ execution port.</param>
    /// <param name="clock">The clock port.</param>
    public GetCampaignByIdQueryHandler(IApplicationDbContext dbContext, IAsyncQueryExecutor queryExecutor, IDateTimeProvider clock)
    {
        _dbContext = dbContext;
        _queryExecutor = queryExecutor;
        _clock = clock;
    }

    /// <inheritdoc />
    public async Task<Result<CampaignDto>> Handle(GetCampaignByIdQuery request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        Campaign? campaign = await _queryExecutor.SingleOrDefaultAsync(
            _dbContext.Campaigns.Where(c => c.Id == request.CampaignId && c.IsActive),
            cancellationToken);

        if (campaign is null)
        {
            return Result<CampaignDto>.Failure(GetCampaignByIdQuery.CampaignNotFoundError);
        }

        List<CampaignDeliverable> deliverables = await _queryExecutor.ToListAsync(
            _dbContext.CampaignDeliverables
                .Where(d => d.CampaignId == campaign.Id)
                .OrderBy(d => d.SortOrder)
                .ThenBy(d => d.Id),
            cancellationToken);

        List<CampaignChannel> channels = await _queryExecutor.ToListAsync(
            _dbContext.CampaignChannels
                .Where(c => c.CampaignId == campaign.Id)
                .OrderBy(c => c.SortOrder)
                .ThenBy(c => c.Id),
            cancellationToken);

        DateTime now = _clock.UtcNow.UtcDateTime;
        return Result<CampaignDto>.Success(CampaignMapper.ToDto(campaign, deliverables, channels, now));
    }
}
