using FluentValidation;

namespace Icbank.Platform.Application.InternationalDays;

/// <summary>
/// Validates a <see cref="DaySearchResultDto"/> before it is allowed to reach persistence
/// (closes DEFECT-LOG.md DATA-04/H-2). The Node source wrote AI-provider JSON directly into the
/// database with zero schema validation; this validator rejects malformed/adversarial output with
/// a clear error rather than silently persisting it.
/// </summary>
public sealed class DaySearchResultValidator : AbstractValidator<DaySearchResultDto>
{
    /// <summary>Initializes a new instance of the <see cref="DaySearchResultValidator"/> class.</summary>
    public DaySearchResultValidator()
    {
        RuleFor(result => result.DayNameAr).NotEmpty().WithMessage("day_name_ar is required.");
        RuleForEach(result => result.Activations!).SetValidator(new DaySearchActivationValidator())
            .When(result => result.Activations is not null);
        RuleForEach(result => result.DesignSamples!).SetValidator(new DaySearchDesignSampleValidator())
            .When(result => result.DesignSamples is not null);
        RuleForEach(result => result.Sources!).SetValidator(new DaySearchSourceValidator())
            .When(result => result.Sources is not null);
    }

    private static bool BeAValidUrlOrNull(string? value) =>
        string.IsNullOrWhiteSpace(value) || Uri.TryCreate(value, UriKind.Absolute, out _);

    private sealed class DaySearchActivationValidator : AbstractValidator<DaySearchActivationDto>
    {
        public DaySearchActivationValidator()
        {
            RuleFor(activation => activation.EntityName).MaximumLength(500);
            RuleFor(activation => activation.SourceUrl).Must(BeAValidUrlOrNull)
                .WithMessage("source_url must be a valid absolute URL or null -- never a fabricated non-URL string.");
        }
    }

    private sealed class DaySearchDesignSampleValidator : AbstractValidator<DaySearchDesignSampleDto>
    {
        public DaySearchDesignSampleValidator()
        {
            RuleFor(sample => sample.EntityName).MaximumLength(500);
            RuleFor(sample => sample.PageUrl).Must(BeAValidUrlOrNull);
            RuleFor(sample => sample.ImageUrl).Must(BeAValidUrlOrNull);
        }
    }

    private sealed class DaySearchSourceValidator : AbstractValidator<DaySearchSourceDto>
    {
        public DaySearchSourceValidator()
        {
            RuleFor(source => source.Url).NotEmpty().Must(BeAValidUrlOrNull)
                .WithMessage("source.url must be a non-empty, valid absolute URL.");
        }
    }
}
