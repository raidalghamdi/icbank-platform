using FluentValidation;

namespace Icbank.Platform.Application.AiYear.Queries;

/// <summary>Validates <see cref="GetAiYearUploadUrlQuery"/> (R-BE-034), matching the Node source's MIME allowlist and 50MB cap.</summary>
public sealed class GetAiYearUploadUrlQueryValidator : AbstractValidator<GetAiYearUploadUrlQuery>
{
    private const int MinMonth = 1;
    private const int MaxMonth = 12;
    private const long MaxFileSizeBytes = 50L * 1024 * 1024;

    private static readonly HashSet<string> AllowedMimeTypes = new(StringComparer.OrdinalIgnoreCase)
    {
        "image/jpeg", "image/png", "image/webp", "image/gif", "image/svg+xml", "video/mp4", "video/webm",
    };

    /// <summary>Initializes a new instance of the <see cref="GetAiYearUploadUrlQueryValidator"/> class.</summary>
    public GetAiYearUploadUrlQueryValidator()
    {
        RuleFor(query => query.Name).NotEmpty();
        RuleFor(query => query.ActivationId).GreaterThan(0).WithMessage("activationId غير صالح");
        RuleFor(query => query.Month).InclusiveBetween(MinMonth, MaxMonth).WithMessage("month يجب أن يكون رقماً بين 1 و12");
        RuleFor(query => query.ContentType!).Must(type => AllowedMimeTypes.Contains(type))
            .When(query => !string.IsNullOrEmpty(query.ContentType))
            .WithMessage(query => $"نوع الملف غير مسموح: {query.ContentType}");
        RuleFor(query => query.FileSize!.Value).LessThanOrEqualTo(MaxFileSizeBytes)
            .When(query => query.FileSize.HasValue)
            .WithMessage("حجم الملف يتجاوز الحد المسموح (50 ميغابايت)");
    }
}
