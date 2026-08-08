using Icbank.Platform.Application.Common.Models;
using MediatR;

namespace Icbank.Platform.Application.Designs.IconEvent.Commands;

/// <summary>
/// Renders one already-chosen design at every requested output size. Deterministic: no AI call.
/// </summary>
/// <param name="Content">The chosen variant's content.</param>
/// <param name="Sizes">
/// The requested size preset wire values. An empty or wholly unrecognised list falls back to
/// <c>desktop-hd</c>, which is also the size the style previews are drawn at.
/// </param>
public sealed record GenerateIconEventStudioCommand(IconEventStudioContentDto Content, IReadOnlyList<string>? Sizes)
    : IRequest<Result<GenerateIconEventStudioResultDto>>;
