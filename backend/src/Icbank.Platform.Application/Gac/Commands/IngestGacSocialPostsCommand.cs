using Icbank.Platform.Application.Common.Models;
using MediatR;

namespace Icbank.Platform.Application.Gac.Commands;

/// <summary>
/// Ports <c>POST /gac/social-feed/ingest</c> (API-SURFACE.md §12). Called by an hourly external
/// cron; upserts on <c>(platform, external_id)</c>, matching <see cref="Domain.Gac.GacSocialPost"/>'s
/// real unique index (DOMAIN-PORT-NOTES.md AMBIGUOUS-7).
/// </summary>
/// <param name="Posts">The batch of social posts to upsert (max 100, enforced by the validator).</param>
public sealed record IngestGacSocialPostsCommand(IReadOnlyList<IngestGacSocialPostItem> Posts) : IRequest<Result<IngestGacSocialPostsResult>>;
