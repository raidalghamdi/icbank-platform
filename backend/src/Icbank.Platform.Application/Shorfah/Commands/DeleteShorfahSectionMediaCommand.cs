using Icbank.Platform.Application.Common.Models;
using MediatR;

namespace Icbank.Platform.Application.Shorfah.Commands;

/// <summary>
/// Command for <c>DELETE /shorfah/media/{id}</c> (API-SURFACE.md §19). Ports <c>shorfah.ts:615-619</c>.
/// Closes AMBIGUOUS-API-4 the same way <see cref="PatchShorfahSectionMediaCommand"/> does.
/// </summary>
/// <param name="ActorUserId">The authenticated caller's id.</param>
/// <param name="MediaId">The media row being deleted.</param>
public sealed record DeleteShorfahSectionMediaCommand(int ActorUserId, int MediaId) : IRequest<Result<bool>>;
