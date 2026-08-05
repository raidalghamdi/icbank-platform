using Icbank.Platform.Application.Common.Models;
using MediatR;

namespace Icbank.Platform.Application.Designs.Composer.Commands;

/// <summary>Ports <c>DELETE /designs/logos/:id</c> (API-SURFACE.md §17).</summary>
/// <param name="ActorUserId">The authenticated caller's user id, for audit.</param>
/// <param name="LogoId">The logo id to delete.</param>
public sealed record DeleteBrandLogoCommand(int ActorUserId, int LogoId) : IRequest<Result<bool>>;
