using FluentValidation;

namespace Icbank.Platform.Application.Designs.Composer.Queries;

/// <summary>
/// Validates <see cref="GetDesignAssetUploadUrlQuery"/> (R-BE-034). Matches the Node source's
/// "fileName required" rule and additionally enforces a MIME allowlist per folder (SEC-17
/// content-type validation the Node source did not perform for these two routes).
/// </summary>
public sealed class GetDesignAssetUploadUrlQueryValidator : AbstractValidator<GetDesignAssetUploadUrlQuery>
{
    private static readonly HashSet<string> AllowedLogoMimeTypes = new(StringComparer.OrdinalIgnoreCase) { "image/png", "image/jpeg", "image/webp", "image/svg+xml" };
    private static readonly HashSet<string> AllowedFontMimeTypes = new(StringComparer.OrdinalIgnoreCase) { "font/ttf", "font/otf", "font/woff", "font/woff2", "application/font-woff" };

    /// <summary>Initializes a new instance of the <see cref="GetDesignAssetUploadUrlQueryValidator"/> class.</summary>
    public GetDesignAssetUploadUrlQueryValidator()
    {
        RuleFor(query => query.FileName).NotEmpty().WithMessage("fileName مطلوب");
        RuleFor(query => query.Folder).Must(f => f is "logos" or "fonts");
        RuleFor(query => query.ContentType!)
            .Must((query, contentType) => IsAllowedForFolder(query.Folder, contentType))
            .When(query => !string.IsNullOrEmpty(query.ContentType))
            .WithMessage(query => $"نوع الملف غير مسموح: {query.ContentType}");
    }

    private static bool IsAllowedForFolder(string folder, string contentType) =>
        folder == "logos" ? AllowedLogoMimeTypes.Contains(contentType) : AllowedFontMimeTypes.Contains(contentType);
}
