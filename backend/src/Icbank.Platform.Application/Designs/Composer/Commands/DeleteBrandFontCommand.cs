using Icbank.Platform.Application.Common.Models;
using MediatR;

namespace Icbank.Platform.Application.Designs.Composer.Commands;

/// <summary>Ports <c>DELETE /designs/fonts/:id</c> (API-SURFACE.md §17).</summary>
/// <param name="ActorUserId">The authenticated caller's user id, for audit.</param>
/// <param name="FontId">The font id to delete.</param>
public sealed record DeleteBrandFontCommand(int ActorUserId, int FontId) : IRequest<Result<bool>>;
