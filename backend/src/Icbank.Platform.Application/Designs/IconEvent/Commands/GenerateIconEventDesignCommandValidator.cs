using FluentValidation;
using Icbank.Platform.Domain.Designs;

namespace Icbank.Platform.Application.Designs.IconEvent.Commands;

/// <summary>
/// Validates <see cref="GenerateIconEventDesignCommand"/> (R-BE-034): "raw_data (≥5 chars) or
/// headline (≥3 chars) required", plus an optional preview-size allowlist.
/// </summary>
public sealed class GenerateIconEventDesignCommandValidator : AbstractValidator<GenerateIconEventDesignCommand>
{
    private const int MinRawDataLength = 5;
    private const int MinHeadlineLength = 3;

    /// <summary>Initializes a new instance of the <see cref="GenerateIconEventDesignCommandValidator"/> class.</summary>
    public GenerateIconEventDesignCommandValidator()
    {
        RuleFor(command => command)
            .Must(HasSufficientInput)
            .WithMessage("يجب إدخال بيانات خام أو عنوان للفعالية");

        // Size is preview-only here: the designer picks output sizes after choosing a style, so an
        // absent size is normal and resolves to the preview preset rather than failing the request.
        RuleFor(command => command.Size!)
            .Must(size => IconEventSizeCatalog.TryParse(size, out _))
            .When(command => !string.IsNullOrWhiteSpace(command.Size))
            .WithMessage($"مقاس غير معروف — المقاسات المتاحة: {string.Join(", ", IconEventSizeCatalog.WireValues)}");
    }

    private static bool HasSufficientInput(GenerateIconEventDesignCommand command)
    {
        var hasRawData = !string.IsNullOrWhiteSpace(command.RawData) && command.RawData.Trim().Length >= MinRawDataLength;
        var hasHeadline = !string.IsNullOrWhiteSpace(command.Headline) && command.Headline.Trim().Length >= MinHeadlineLength;
        return hasRawData || hasHeadline;
    }
}
