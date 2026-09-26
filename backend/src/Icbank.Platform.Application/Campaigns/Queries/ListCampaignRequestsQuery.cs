using Icbank.Platform.Application.Common.Models;
using MediatR;

namespace Icbank.Platform.Application.Campaigns.Queries;

/// <summary>Lists internal campaign requests, newest first. Reviewers see every request; others see only their own.</summary>
/// <param name="ActorUserId">The caller.</param>
/// <param name="CanReview">Whether the caller reviews requests for Corporate Communications.</param>
public sealed record ListCampaignRequestsQuery(int ActorUserId, bool CanReview) : IRequest<Result<IReadOnlyList<CampaignRequestDto>>>;
