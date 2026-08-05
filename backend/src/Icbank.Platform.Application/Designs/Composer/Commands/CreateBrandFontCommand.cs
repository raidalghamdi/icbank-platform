using Icbank.Platform.Application.Common.Models;
using MediatR;

namespace Icbank.Platform.Application.Designs.Composer.Commands;

/// <summary>Ports <c>POST /designs/fonts</c> (API-SURFACE.md §17).</summary>
/// <param name="ActorUserId">The authenticated caller's user id, for audit.</param>
/// <param name="FontName">The font's display name.</param>
/// <param name="FontFileUrl">The already-uploaded object path.</param>
public sealed record CreateBrandFontCommand(int ActorUserId, string FontName, string FontFileUrl) : IRequest<Result<BrandFontDto>>;
