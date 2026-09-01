using Icbank.Platform.Application.Common.Models;
using MediatR;

namespace Icbank.Platform.Application.Campaigns.Queries;

/// <summary>
/// Reads one campaigns page: the campaigns for an audience, optionally narrowed to a single
/// lifecycle state, with the headline figures and the per-state counts the filter chips show.
/// </summary>
/// <param name="Audience">The audience key, <c>internal</c> or <c>external</c>; anything else reads both books of work.</param>
/// <param name="Status">The state key to narrow to, or <c>null</c>/<c>all</c> for every state.</param>
public sealed record GetCampaignBoardQuery(string? Audience, string? Status) : IRequest<Result<CampaignBoardDto>>;
