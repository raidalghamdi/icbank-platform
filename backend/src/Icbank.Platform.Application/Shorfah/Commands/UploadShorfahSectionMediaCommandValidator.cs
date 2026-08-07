using FluentValidation;

namespace Icbank.Platform.Application.Shorfah.Commands;

/// <summary>
/// Validates <see cref="UploadShorfahSectionMediaCommand"/>. Ports the Node source's
/// <c>!dataBase64</c> check (<c>shorfah.ts:568</c>) and adds a real MIME allowlist (SEC-17 class:
/// the Node source validated size and base64-decodability only, with no content-type allowlist --
/// API-SURFACE.md §22 flags this gap explicitly).
/// </summary>
public sealed class UploadShorfahSectionMediaCommandValidator : AbstractValidator<UploadShorfahSectionMediaCommand>
{
    /// <summary>The allowed upload content types.</summary>
    public static readonly string[] AllowedContentTypes =
    {
        "image/png", "image/jpeg", "image/webp", "image/gif", "application/pdf",
    };

    /// <summary>Initializes a new instance of the <see cref="UploadShorfahSectionMediaCommandValidator"/> class.</summary>
    public UploadShorfahSectionMediaCommandValidator()
    {
        RuleFor(x => x.DataBase64).NotEmpty().WithMessage("dataBase64 مطلوب");
        RuleFor(x => x.ContentType)
            .Must(ct => string.IsNullOrEmpty(ct) || AllowedContentTypes.Contains(ct, StringComparer.OrdinalIgnoreCase))
            .WithMessage("نوع الملف غير مسموح به");
    }
}
