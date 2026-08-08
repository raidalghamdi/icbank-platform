using Icbank.Platform.Application.Common.Models;
using Icbank.Platform.Domain.Designs;
using MediatR;

namespace Icbank.Platform.Application.Designs.IconEvent.Commands;

/// <summary>Handles <see cref="GenerateIconEventStudioCommand"/>.</summary>
public sealed class GenerateIconEventStudioCommandHandler : IRequestHandler<GenerateIconEventStudioCommand, Result<GenerateIconEventStudioResultDto>>
{
    private const IconEventSizePreset DefaultSize = IconEventSizePreset.DesktopHd;
    private const int MaxSupportingIcons = 3;

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
        IconEventStudioContentDto content = request.Content;
        IReadOnlyList<IconEventSizePreset> sizes = ResolveSizes(request.Sizes);

        var variants = sizes.Select(size => RenderSize(content, size)).ToList();
        return Task.FromResult(Result<GenerateIconEventStudioResultDto>.Success(new GenerateIconEventStudioResultDto(variants)));
    }

    private static IReadOnlyList<IconEventSizePreset> ResolveSizes(IReadOnlyList<string>? requestedSizes)
    {
        if (requestedSizes is null)
        {
            return new[] { DefaultSize };
        }

        var resolved = new List<IconEventSizePreset>();
        foreach (var candidate in requestedSizes)
        {
            if (IconEventSizeCatalog.TryParse(candidate, out IconEventSizePreset preset) && !resolved.Contains(preset))
            {
                resolved.Add(preset);
            }
        }

        return resolved.Count == 0 ? new[] { DefaultSize } : resolved;
    }

    private static IconEventInput BuildInput(IconEventStudioContentDto content, IconEventSizePreset size) => new()
    {
        Headline = content.Headline.Trim(),
        Subtitle = content.Subtitle?.Trim(),
        Department = content.Department?.Trim(),
        Hashtag = content.Hashtag?.Trim(),
        ContactEmail = content.ContactEmail?.Trim(),
        ContactPhone = content.ContactPhone?.Trim(),
        Date = content.Date?.Trim(),
        Time = content.Time?.Trim(),
        Location = content.Location?.Trim(),
        MainIcon = (content.MainIcon ?? string.Empty).Trim(),
        SupportingIcons = (content.SupportingIcons ?? Array.Empty<string>()).Take(MaxSupportingIcons).ToList(),
        Stats = (content.Stats ?? Array.Empty<IconEventStatDto>()).Select(s => new IconEventStat(s.Icon, s.Value, s.Label)).ToList(),
        Layout = IconEventLayoutNormalizer.ToLayout(content.Layout),
        Size = size,
        LogoUrl = content.LogoUrl,
    };

    private IconEventStudioVariantDto RenderSize(IconEventStudioContentDto content, IconEventSizePreset size)
    {
        IconEventSizeSpec spec = IconEventSizeCatalog.Resolve(size);
        IconEventInput input = BuildInput(content, size);
        return new IconEventStudioVariantDto(
            spec.WireValue, spec.Width, spec.Height, spec.AspectLabel, spec.ArabicLabel, _htmlRenderer.Render(input));
    }
}
