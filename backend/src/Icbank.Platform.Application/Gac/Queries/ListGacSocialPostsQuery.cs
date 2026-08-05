using Icbank.Platform.Application.Common.Models;
using MediatR;

namespace Icbank.Platform.Application.Gac.Queries;

/// <summary>Ports <c>GET /gac/social-feed</c> (API-SURFACE.md §12).</summary>
/// <param name="Query">The pagination request.</param>
/// <param name="Platform">Optional platform filter.</param>
public sealed record ListGacSocialPostsQuery(PagedQuery Query, string? Platform) : IRequest<Result<PagedResult<GacSocialPostDto>>>;
