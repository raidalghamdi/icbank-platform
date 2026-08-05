using Icbank.Platform.Application.Common.Models;
using MediatR;

namespace Icbank.Platform.Application.Designs.IconEvent.Commands;

/// <summary>Ports <c>POST /designs/icon-event/studio</c> (API-SURFACE.md §18): deterministic, no-AI multi-size HTML generation.</summary>
/// <param name="Headline">The required headline.</param>
/// <param name="Subtitle">The optional subtitle.</param>
/// <param name="Department">The optional department.</param>
/// <param name="MainIcon">The main icon name, defaults to <c>star</c> if omitted.</param>
/// <param name="Sizes">The requested size presets; defaults to <c>[landscape]</c> if empty.</param>
/// <param name="Layout">The layout key, defaults to <c>hero</c>.</param>
/// <param name="LogoUrl">The optional logo URL.</param>
public sealed record GenerateIconEventStudioCommand(
    string Headline, string? Subtitle, string? Department, string? MainIcon, IReadOnlyList<string>? Sizes, string? Layout, string? LogoUrl)
    : IRequest<Result<GenerateIconEventStudioResultDto>>;
