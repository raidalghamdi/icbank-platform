using Icbank.Platform.Application.Common.Models;
using MediatR;

namespace Icbank.Platform.Application.MediaMonitoring.Commands;

/// <summary>
/// Seeds 6 realistic GAC-themed demo news items and 6 demo social posts, dated across the last 7
/// days, so the final-report generator has something to analyse
/// (<c>POST /final-media-reports/seed-demo</c>). The Node source performed a manual inline
/// <c>role in [admin, super_admin]</c> check rather than a route-level auth middleware; this port
/// expresses the same restriction declaratively via <c>[Authorize(Policy =
/// "media_monitoring:create")]</c> at the controller, which is the closest generated policy to
/// "admin-only write" available in this codebase's permission model.
/// </summary>
/// <param name="ActorUserId">The id of the authenticated caller triggering the seed.</param>
public sealed record SeedDemoNewsCommand(int ActorUserId) : IRequest<Result<SeedDemoNewsResultDto>>;
