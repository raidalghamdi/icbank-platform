using FluentValidation;
using Icbank.Platform.Domain.Designs;

namespace Icbank.Platform.Application.Designs.IconEvent.Commands;

/// <summary>
/// Validates <see cref="RenderIconEventDesignCommand"/> (R-BE-034). Matches the Node source's
/// "html and size required" rule, plus a maximum HTML length cap that closes the resource-
/// exhaustion half of SEC-12 (BUSINESS-RULES.md §7.5), and a remote-resource-reference rejection
/// rule (<see cref="HtmlRemoteResourceScanner"/>) that closes the SSRF half: the render endpoint
/// hands <see cref="RenderIconEventDesignCommand.Html"/> to a renderer verbatim, so any embedded
/// <c>img</c>/<c>script</c>/<c>link</c>/<c>iframe</c>/<c>video</c>/<c>audio</c>/<c>source</c>/
/// <c>object</c>/<c>embed</c> reference, inline-style <c>url()</c>, <c>@import</c>, or SVG
/// <c>xlink:href</c> pointing at any network-reachable location -- public, private, or
/// link-local/metadata address alike -- is rejected outright at this input boundary. This holds
/// even though today's renderer (<c>TemplateIconEventImageRenderer</c>) never fetches anything, so
/// the fix stays in force the moment a real renderer replaces it.
/// </summary>
public sealed class RenderIconEventDesignCommandValidator : AbstractValidator<RenderIconEventDesignCommand>
{
    private const int MaxHtmlLength = 500_000;

    private static readonly HashSet<string> AllowedQualities = new(StringComparer.OrdinalIgnoreCase) { "hd", "ultra" };

    /// <summary>Initializes a new instance of the <see cref="RenderIconEventDesignCommandValidator"/> class.</summary>
    public RenderIconEventDesignCommandValidator()
    {
        RuleFor(command => command.Html).NotEmpty().MaximumLength(MaxHtmlLength).WithMessage("html و size مطلوبان");
        RuleFor(command => command.Html)
            .Must(html => HtmlRemoteResourceScanner.FindRemoteReferences(html).Count == 0)
            .When(command => !string.IsNullOrEmpty(command.Html))
            .WithMessage("لا يُسمح بمراجع موارد خارجية (صور/سكربتات/روابط) داخل الـ HTML");
        RuleFor(command => command.Size)
            .Must(size => IconEventSizeCatalog.TryParse(size, out _))
            .WithMessage($"مقاس غير معروف — المقاسات المتاحة: {string.Join(", ", IconEventSizeCatalog.WireValues)}");
        RuleFor(command => command.Quality!).Must(q => AllowedQualities.Contains(q))
            .When(command => !string.IsNullOrEmpty(command.Quality))
            .WithMessage("quality يجب أن يكون hd أو ultra");
    }
}
