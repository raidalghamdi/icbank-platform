using FluentValidation;

namespace Icbank.Platform.Application.Shorfah;

/// <summary>
/// Validates an AI-generated <see cref="ShorfahGeneratedSectionContent"/> before persistence
/// (task requirement H-2 class). The Node source wrote <c>out.content_md</c> straight to the
/// database with only a truthiness check; this port requires real, non-whitespace content.
/// </summary>
public sealed class ShorfahGeneratedSectionContentValidator : AbstractValidator<ShorfahGeneratedSectionContent>
{
    /// <summary>Initializes a new instance of the <see cref="ShorfahGeneratedSectionContentValidator"/> class.</summary>
    public ShorfahGeneratedSectionContentValidator()
    {
        RuleFor(x => x.ContentMd).NotEmpty().WithMessage("فشل التوليد - الرد فارغ");
    }
}
