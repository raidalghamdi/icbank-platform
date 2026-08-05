using FluentValidation;

namespace Icbank.Platform.Application.Designs.IconEvent.Commands;

/// <summary>Validates <see cref="GenerateIconEventStudioCommand"/> (R-BE-034), matching the Node source's required-headline rule.</summary>
public sealed class GenerateIconEventStudioCommandValidator : AbstractValidator<GenerateIconEventStudioCommand>
{
    /// <summary>Initializes a new instance of the <see cref="GenerateIconEventStudioCommandValidator"/> class.</summary>
    public GenerateIconEventStudioCommandValidator()
    {
        RuleFor(command => command.Headline).NotEmpty().WithMessage("headline مطلوب");
    }
}
