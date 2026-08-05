using FluentValidation;

namespace Icbank.Platform.Application.Shorfah.Commands;

/// <summary>Validates <see cref="UpdateShorfahSectionSlaCommand"/>. The Node source had no bounds check on <c>slaDays</c> here (API-SURFACE.md §22); this port adds the same [1, 60] bound used by the SLA-defaults endpoint.</summary>
public sealed class UpdateShorfahSectionSlaCommandValidator : AbstractValidator<UpdateShorfahSectionSlaCommand>
{
    /// <summary>Initializes a new instance of the <see cref="UpdateShorfahSectionSlaCommandValidator"/> class.</summary>
    public UpdateShorfahSectionSlaCommandValidator()
    {
        RuleFor(x => x.SlaDays).InclusiveBetween(1, 60).When(x => x.SlaDays.HasValue).WithMessage("عدد أيام SLA يجب أن يكون بين 1 و60");
    }
}
