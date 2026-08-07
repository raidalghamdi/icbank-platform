using Icbank.Platform.Application.Common.Models;
using MediatR;

namespace Icbank.Platform.Application.Designs.Composer.Commands;

/// <summary>Ports <c>POST /designs/logos</c> (API-SURFACE.md §17).</summary>
/// <param name="ActorUserId">The authenticated caller's user id, for audit.</param>
/// <param name="LogoName">The logo's display name.</param>
/// <param name="FileUrl">The already-uploaded object path.</param>
/// <param name="Transparent">Whether the logo has a transparent background.</param>
/// <param name="DefaultWidth">The optional default render width.</param>
public sealed record CreateBrandLogoCommand(int ActorUserId, string LogoName, string FileUrl, bool Transparent, int? DefaultWidth) : IRequest<Result<BrandLogoDto>>;
