using Icbank.Platform.Application.Common.Models;
using Icbank.Platform.Domain.Designs;
using MediatR;

namespace Icbank.Platform.Application.Designs.IconEvent.Commands;

/// <summary>Handles <see cref="GenerateIconEventStudioCommand"/>. Deterministic, no AI call (BUSINESS-RULES.md §7.4 endpoint table).</summary>
public sealed class GenerateIconEventStudioCommandHandler : IRequestHandler<GenerateIconEventStudioCommand, Result<GenerateIconEventStudioResultDto>>
{
    private static readonly Dictionary<string, IconEventLayoutType> LayoutsByKey = new(StringComparer.OrdinalIgnoreCase)
    {
        ["stats-hero"] = IconEventLayoutType.StatsHero,
        ["hero"] = IconEventLayoutType.Hero,
        ["grid"] = IconEventLayoutType.Grid,
        ["split"] = IconEventLayoutType.Split,
        ["typography"] = IconEventLayoutType.Typography,
    };

    private readonly IIconEventHtmlRenderer _htmlRenderer;

    /// <summary>Initializes a new instance of the <see cref="GenerateIconEventStudioCommandHandler"/> class.</summary>
    /// <param name="htmlRenderer">The HTML rendering port.</param>
    public GenerateIconEventStudioCommandHandler(IIconEventHtmlRenderer htmlRenderer)
    {
        _htmlRenderer = htmlRenderer;
    }

    /// <inheritdoc />
    public Task<Result<GenerateIconEventStudioResultDto>> Handle(GenerateIconEventStudioCommand request, CancellationToken cancellationToken)
    {
        IconEventLayoutType layout = request.Layout is not null && LayoutsByKey.TryGetValue(request.Layout, out IconEventLayoutType parsedLayout) ? parsedLayout : IconEventLayoutType.Hero;
        var mainIcon = string.IsNullOrWhiteSpace(request.MainIcon) ? "star" : request.MainIcon;
        List<IconEventSizePreset> sizes = ResolveSizes(request.Sizes);

        var variants = sizes.Select(size =>
        {
            (var width, var height) = IconEventSizeCatalog.Resolve(size);
            var input = new IconEventInput
            {
                Headline = request.Headline.Trim(),
                Subtitle = request.Subtitle?.Trim(),
                Department = request.Department?.Trim(),
                MainIcon = mainIcon,
                Layout = layout,
                Size = size,
                LogoUrl = request.LogoUrl,
            };
            return new IconEventStudioVariantDto(size.ToString().ToLowerInvariant(), width, height, _htmlRenderer.Render(input));
        }).ToList();

        return Task.FromResult(Result<GenerateIconEventStudioResultDto>.Success(new GenerateIconEventStudioResultDto(variants)));
    }

    private static List<IconEventSizePreset> ResolveSizes(IReadOnlyList<string>? requestedSizes)
    {
        if (requestedSizes is null || requestedSizes.Count == 0)
        {
            return new List<IconEventSizePreset> { IconEventSizePreset.Landscape };
        }

        var resolved = requestedSizes
            .Where(s => Enum.TryParse<IconEventSizePreset>(s, ignoreCase: true, out _))
            .Select(s => Enum.Parse<IconEventSizePreset>(s, ignoreCase: true))
            .ToList();
        return resolved.Count == 0 ? new List<IconEventSizePreset> { IconEventSizePreset.Landscape } : resolved;
    }
}
