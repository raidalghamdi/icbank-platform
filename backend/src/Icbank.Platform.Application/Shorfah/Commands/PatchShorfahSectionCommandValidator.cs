using FluentValidation;

namespace Icbank.Platform.Application.Shorfah.Commands;

/// <summary>Validates <see cref="PatchShorfahSectionCommand"/>. The Node source had zero shape validation on this endpoint (API-SURFACE.md §22); this port adds real bounds.</summary>
public sealed class PatchShorfahSectionCommandValidator : AbstractValidator<PatchShorfahSectionCommand>
{
    /// <summary>Initializes a new instance of the <see cref="PatchShorfahSectionCommandValidator"/> class.</summary>
    public PatchShorfahSectionCommandValidator()
    {
        RuleFor(x => x.TitleAr).NotEmpty().When(x => x.TitleAr is not null).WithMessage("عنوان القسم لا يمكن أن يكون فارغاً");
        RuleFor(x => x.DisplayOrder).GreaterThanOrEqualTo(0).When(x => x.DisplayOrder.HasValue).WithMessage("ترتيب العرض غير صالح");
        RuleFor(x => x.SlaDays).InclusiveBetween(1, 60).When(x => x.SlaDays.HasValue).WithMessage("عدد أيام SLA يجب أن يكون بين 1 و60");
        RuleFor(x => x.SlaStartsAt)
            .LessThanOrEqualTo(x => x.SlaDeadline)
            .When(x => x.SlaStartsAt.HasValue && x.SlaDeadline.HasValue)
            .WithMessage("تاريخ بدء SLA يجب أن يسبق تاريخ الاستحقاق");
    }
}
