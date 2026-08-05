using Icbank.Platform.Application.Common.Models;
using MediatR;

namespace Icbank.Platform.Application.Designs.IconEvent.Commands;

/// <summary>Ports <c>POST /designs/icon-event/render</c> (API-SURFACE.md §18): renders client-supplied HTML to an image.</summary>
/// <param name="ActorUserId">The authenticated caller's user id, for audit and rate limiting.</param>
/// <param name="Html">The HTML document to rasterize.</param>
/// <param name="Size">The target size preset key.</param>
/// <param name="Quality">The render quality, <c>hd</c> (3x, default) or <c>ultra</c> (4x).</param>
public sealed record RenderIconEventDesignCommand(int ActorUserId, string Html, string Size, string? Quality)
    : IRequest<Result<RenderIconEventDesignResultDto>>;
