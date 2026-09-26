using System.Text.Json;
using Icbank.Platform.Application.Common.Interfaces;
using Icbank.Platform.Application.Common.Models;
using Icbank.Platform.Domain.Campaigns;
using MediatR;

namespace Icbank.Platform.Application.Campaigns.Commands;

/// <summary>Handles <see cref="ReviewCampaignRequestCommand"/>.</summary>
public sealed class ReviewCampaignRequestCommandHandler : IRequestHandler<ReviewCampaignRequestCommand, Result<CampaignRequestDto>>
{
    private const int NoteMaxLength = 600;
    private const int NameMaxLength = 150;

    private readonly IApplicationDbContext _dbContext;
    private readonly IAsyncQueryExecutor _queryExecutor;
    private readonly IDateTimeProvider _clock;

    /// <summary>Initializes a new instance of the <see cref="ReviewCampaignRequestCommandHandler"/> class.</summary>
    /// <param name="dbContext">The database context.</param>
    /// <param name="queryExecutor">The async query executor.</param>
    /// <param name="clock">The clock.</param>
    public ReviewCampaignRequestCommandHandler(IApplicationDbContext dbContext, IAsyncQueryExecutor queryExecutor, IDateTimeProvider clock)
    {
        _dbContext = dbContext;
        _queryExecutor = queryExecutor;
        _clock = clock;
    }

    /// <summary>Builds the starting plan of a converted request: one deliverable per requested support type.</summary>
    /// <param name="request">The approved request.</param>
    /// <returns>The plan JSON.</returns>
    public static string StartingPlan(CampaignRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);
        var deliverables = CampaignRequestMapper.SplitSupport(request.SupportTypes)
            .Select((key, index) => new Dictionary<string, object?>(StringComparer.Ordinal)
            {
                ["id"] = "d" + (index + 1).ToString(System.Globalization.CultureInfo.InvariantCulture),
                ["title"] = CampaignRequestMapper.SupportLabels[key],
                ["dueDate"] = request.ProposedEnd.ToString("yyyy-MM-dd", System.Globalization.CultureInfo.InvariantCulture),
                ["done"] = false,
                ["files"] = Array.Empty<object>(),
            })
            .ToList();
        var plan = new Dictionary<string, object?>(StringComparer.Ordinal)
        {
            ["milestones"] = Array.Empty<object>(),
            ["tasks"] = Array.Empty<object>(),
            ["deliverables"] = deliverables,
            ["content"] = Array.Empty<object>(),
            ["files"] = Array.Empty<object>(),
            ["support"] = CampaignRequestMapper.SplitSupport(request.SupportTypes),
        };
        return JsonSerializer.Serialize(plan);
    }

    /// <inheritdoc />
    public async Task<Result<CampaignRequestDto>> Handle(ReviewCampaignRequestCommand request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        CampaignRequest? entity = await _queryExecutor.SingleOrDefaultAsync(
            _dbContext.CampaignRequests.Where(r => r.Id == request.RequestId), cancellationToken);
        if (entity is null)
        {
            return Result<CampaignRequestDto>.Failure(ReviewCampaignRequestCommand.NotFoundError);
        }

        if (entity.Status != CampaignRequestStatus.Submitted)
        {
            return Result<CampaignRequestDto>.Failure(ReviewCampaignRequestCommand.AlreadyReviewedError);
        }

        var note = (request.Note ?? string.Empty).Trim();
        if (!request.Approve && note.Length == 0)
        {
            return Result<CampaignRequestDto>.Failure(ReviewCampaignRequestCommand.NoteRequiredError);
        }

        entity.ReviewNote = note.Length <= NoteMaxLength ? note : note[..NoteMaxLength];
        entity.ReviewedByName = request.ReviewerName.Length <= NameMaxLength ? request.ReviewerName : request.ReviewerName[..NameMaxLength];
        entity.ReviewedAt = _clock.UtcNow.UtcDateTime;
        entity.Status = request.Approve ? CampaignRequestStatus.Approved : CampaignRequestStatus.Rejected;

        if (request.Approve)
        {
            Campaign campaign = Convert(entity);
            _dbContext.Add(campaign);
            await _dbContext.SaveChangesAsync(cancellationToken);
            entity.CampaignId = campaign.Id;
        }

        await _dbContext.SaveChangesAsync(cancellationToken);
        return Result<CampaignRequestDto>.Success(CampaignRequestMapper.ToDto(entity));
    }

    private static Campaign Convert(CampaignRequest request) => new()
    {
        Code = string.Concat("INT-R-", request.Id.ToString("D4", System.Globalization.CultureInfo.InvariantCulture)),
        Name = request.Name,
        Description = request.Objective,
        Objective = request.Objective,
        TargetAudience = request.TargetAudience,
        KeyMessages = request.KeyMessages,
        Audience = CampaignAudience.Internal,
        Stage = CampaignStage.Planning,
        Status = CampaignLabels.StatusOf(CampaignStage.Planning),
        Owner = request.ReviewedByName,
        Department = request.RequestingDepartment,
        StartDate = request.ProposedStart,
        EndDate = request.ProposedEnd,
        LatestUpdate = "تحوّلت من طلب حملة داخلية معتمد.",
        SortOrder = 1000,
        IsActive = true,
        IsUserManaged = true,
        SourceRequestId = request.Id,
        PlanJson = StartingPlan(request),
    };
}
