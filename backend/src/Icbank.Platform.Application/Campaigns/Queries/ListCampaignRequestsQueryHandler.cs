using Icbank.Platform.Application.Common.Interfaces;
using Icbank.Platform.Application.Common.Models;
using Icbank.Platform.Domain.Campaigns;
using MediatR;

namespace Icbank.Platform.Application.Campaigns.Queries;

/// <summary>Handles <see cref="ListCampaignRequestsQuery"/>.</summary>
public sealed class ListCampaignRequestsQueryHandler : IRequestHandler<ListCampaignRequestsQuery, Result<IReadOnlyList<CampaignRequestDto>>>
{
    private readonly IApplicationDbContext _dbContext;
    private readonly IAsyncQueryExecutor _queryExecutor;

    /// <summary>Initializes a new instance of the <see cref="ListCampaignRequestsQueryHandler"/> class.</summary>
    /// <param name="dbContext">The database context.</param>
    /// <param name="queryExecutor">The async query executor.</param>
    public ListCampaignRequestsQueryHandler(IApplicationDbContext dbContext, IAsyncQueryExecutor queryExecutor)
    {
        _dbContext = dbContext;
        _queryExecutor = queryExecutor;
    }

    /// <inheritdoc />
    public async Task<Result<IReadOnlyList<CampaignRequestDto>>> Handle(ListCampaignRequestsQuery request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        List<CampaignRequest> rows = await _queryExecutor.ToListAsync(
            _dbContext.CampaignRequests
                .Where(r => request.CanReview || r.SubmittedByUserId == request.ActorUserId)
                .OrderByDescending(r => r.CreatedAt)
                .ThenByDescending(r => r.Id),
            cancellationToken);
        return Result<IReadOnlyList<CampaignRequestDto>>.Success(rows.Select(CampaignRequestMapper.ToDto).ToList());
    }
}
