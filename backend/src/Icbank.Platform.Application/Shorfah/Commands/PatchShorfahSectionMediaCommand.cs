using Icbank.Platform.Application.Common.Models;
using MediatR;

namespace Icbank.Platform.Application.Shorfah.Commands;

/// <summary>
/// Command for <c>PATCH /shorfah/media/{id}</c> (API-SURFACE.md §19). Ports <c>shorfah.ts:599-612</c>.
/// Closes AMBIGUOUS-API-4: the Node source let any authenticated user edit any section's media
/// caption with no section-permission check; this port requires the same
/// contribute/review/approve/admin tier the upload endpoint already enforces.
/// </summary>
/// <param name="ActorUserId">The authenticated caller's id.</param>
/// <param name="MediaId">The media row being edited.</param>
/// <param name="CaptionAr">The new Arabic caption, if changing.</param>
/// <param name="DisplayOrder">The new display sort order, if changing.</param>
public sealed record PatchShorfahSectionMediaCommand(int ActorUserId, int MediaId, string? CaptionAr, int? DisplayOrder) : IRequest<Result<ShorfahSectionMediaDto>>;
