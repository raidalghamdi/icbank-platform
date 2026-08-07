using FluentValidation;

namespace Icbank.Platform.Application.Designs.Composer.Commands;

/// <summary>Validates <see cref="GenerateBackgroundsCommand"/> (R-BE-034), matching the Node source's "prompt required" rule.</summary>
public sealed class GenerateBackgroundsCommandValidator : AbstractValidator<GenerateBackgroundsCommand>
{
    /// <summary>Initializes a new instance of the <see cref="GenerateBackgroundsCommandValidator"/> class.</summary>
    public GenerateBackgroundsCommandValidator()
    {
        RuleFor(command => command.Prompt).NotEmpty().WithMessage("prompt مطلوب");
    }
}
