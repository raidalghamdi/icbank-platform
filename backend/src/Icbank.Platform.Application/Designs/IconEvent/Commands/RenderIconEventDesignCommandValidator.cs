using FluentValidation;

namespace Icbank.Platform.Application.Designs.IconEvent.Commands;

/// <summary>
/// Validates <see cref="RenderIconEventDesignCommand"/> (R-BE-034). Matches the Node source's
/// "html and size required" rule, plus a maximum HTML length cap that closes the resource-
/// exhaustion half of SEC-12 (BUSINESS-RULES.md §7.5) -- the Node source accepted arbitrary-length
/// HTML from any authenticated user with no cap at all.
/// </summary>
public sealed class RenderIconEventDesignCommandValidator : AbstractValidator<RenderIconEventDesignCommand>
{
    private const int MaxHtmlLength = 500_000;

    private static readonly HashSet<string> AllowedSizes = new(StringComparer.OrdinalIgnoreCase) { "square", "story", "landscape" };
    private static readonly HashSet<string> AllowedQualities = new(StringComparer.OrdinalIgnoreCase) { "hd", "ultra" };

    /// <summary>Initializes a new instance of the <see cref="RenderIconEventDesignCommandValidator"/> class.</summary>
    public RenderIconEventDesignCommandValidator()
    {
        RuleFor(command => command.Html).NotEmpty().MaximumLength(MaxHtmlLength).WithMessage("html و size مطلوبان");
        RuleFor(command => command.Size).Must(size => AllowedSizes.Contains(size)).WithMessage("html و size مطلوبان");
        RuleFor(command => command.Quality!).Must(q => AllowedQualities.Contains(q))
            .When(command => !string.IsNullOrEmpty(command.Quality))
            .WithMessage("quality يجب أن يكون hd أو ultra");
    }
}
