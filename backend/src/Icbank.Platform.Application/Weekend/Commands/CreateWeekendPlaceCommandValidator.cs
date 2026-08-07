using FluentValidation;

namespace Icbank.Platform.Application.Weekend.Commands;

/// <summary>Validates <see cref="CreateWeekendPlaceCommand"/>.</summary>
public sealed class CreateWeekendPlaceCommandValidator : AbstractValidator<CreateWeekendPlaceCommand>
{
    private const int NameMaxLength = 200;
    private const int DescriptionMaxLength = 2000;

    /// <summary>Initializes a new instance of the <see cref="CreateWeekendPlaceCommandValidator"/> class.</summary>
    public CreateWeekendPlaceCommandValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(NameMaxLength);
        RuleFor(x => x.Description).NotEmpty().MaximumLength(DescriptionMaxLength);
        RuleFor(x => x.SortOrder).GreaterThanOrEqualTo(0);
    }
}
