using FluentValidation;

namespace Icbank.Platform.Application.Designs.IconEvent.Commands;

/// <summary>
/// Validates <see cref="GenerateIconEventDesignCommand"/> (R-BE-034), matching the Node source's
/// "raw_data (≥5 chars) or headline (≥3 chars) required" rule and its size-preset allowlist
/// (BUSINESS-RULES.md §7.4/§7.5).
/// </summary>
public sealed class GenerateIconEventDesignCommandValidator : AbstractValidator<GenerateIconEventDesignCommand>
{
    private const int MinRawDataLength = 5;
    private const int MinHeadlineLength = 3;

    private static readonly HashSet<string> AllowedSizes = new(StringComparer.OrdinalIgnoreCase) { "square", "story", "landscape" };

    /// <summary>Initializes a new instance of the <see cref="GenerateIconEventDesignCommandValidator"/> class.</summary>
    public GenerateIconEventDesignCommandValidator()
    {
        RuleFor(command => command)
            .Must(HasSufficientInput)
            .WithMessage("يجب إدخال بيانات خام أو عنوان للفعالية");

        RuleFor(command => command.Size)
            .Must(size => AllowedSizes.Contains(size))
            .WithMessage("المقاس مطلوب (square | story | landscape)");
    }

    private static bool HasSufficientInput(GenerateIconEventDesignCommand command)
    {
        var hasRawData = !string.IsNullOrWhiteSpace(command.RawData) && command.RawData.Trim().Length >= MinRawDataLength;
        var hasHeadline = !string.IsNullOrWhiteSpace(command.Headline) && command.Headline.Trim().Length >= MinHeadlineLength;
        return hasRawData || hasHeadline;
    }
}
