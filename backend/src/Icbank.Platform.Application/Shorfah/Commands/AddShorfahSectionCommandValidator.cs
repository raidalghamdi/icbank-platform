using FluentValidation;
using Icbank.Platform.Domain.Shorfah;

namespace Icbank.Platform.Application.Shorfah.Commands;

/// <summary>Validates <see cref="AddShorfahSectionCommand"/>. Ports the Node source's <c>!sectionType || !titleAr</c> check plus a real enum check it lacked (API-SURFACE.md §22).</summary>
public sealed class AddShorfahSectionCommandValidator : AbstractValidator<AddShorfahSectionCommand>
{
    /// <summary>Initializes a new instance of the <see cref="AddShorfahSectionCommandValidator"/> class.</summary>
    public AddShorfahSectionCommandValidator()
    {
        RuleFor(x => x.SectionType)
            .NotEmpty().WithMessage("بيانات ناقصة")
            .Must(value => Enum.TryParse<ShorfahSectionType>(value, ignoreCase: true, out _)).WithMessage("نوع القسم غير صالح");
        RuleFor(x => x.TitleAr).NotEmpty().WithMessage("بيانات ناقصة");
        RuleFor(x => x.SlaDays).InclusiveBetween(1, 60).When(x => x.SlaDays.HasValue).WithMessage("عدد أيام SLA غير صالح");
    }
}
