using Icbank.Platform.Application.Common.Models;
using MediatR;

namespace Icbank.Platform.Application.Designs.Composer.Commands;

/// <summary>Ports <c>PATCH /designs/fonts/:id/default</c> (API-SURFACE.md §17).</summary>
/// <param name="ActorUserId">The authenticated caller's user id, for audit.</param>
/// <param name="FontId">The font id to set as default.</param>
public sealed record SetDefaultBrandFontCommand(int ActorUserId, int FontId) : IRequest<Result<BrandFontDto>>;
