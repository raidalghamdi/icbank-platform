using Icbank.Platform.Application.Common.Models;
using MediatR;

namespace Icbank.Platform.Application.Gac.Commands;

/// <summary>
/// Ports <c>POST /gac/social-feed/seed-twitter</c> (API-SURFACE.md §12). Admin-only. Seeds 5
/// fixed sample Twitter/X posts — explicitly fixture data pending real X API v2 integration
/// (BUSINESS-RULES.md §8), preserved verbatim from the Node source.
/// </summary>
/// <param name="ActorUserId">The admin performing the seed, for the audit-log write.</param>
public sealed record SeedGacTwitterSamplesCommand(int ActorUserId) : IRequest<Result<SeedGacTwitterSamplesResult>>;
