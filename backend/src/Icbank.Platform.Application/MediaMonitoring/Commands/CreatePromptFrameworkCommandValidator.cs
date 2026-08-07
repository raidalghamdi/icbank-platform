using FluentValidation;

namespace Icbank.Platform.Application.MediaMonitoring.Commands;

/// <summary>Validates <see cref="CreatePromptFrameworkCommand"/> (R-BE-034), matching the Node source's <c>PromptCreateSchema</c> required fields.</summary>
public sealed class CreatePromptFrameworkCommandValidator : AbstractValidator<CreatePromptFrameworkCommand>
{
    private const int NameMaxLength = 200;

    /// <summary>Initializes a new instance of the <see cref="CreatePromptFrameworkCommandValidator"/> class.</summary>
    public CreatePromptFrameworkCommandValidator()
    {
        RuleFor(command => command.NameAr).NotEmpty().MaximumLength(NameMaxLength);
        RuleFor(command => command.PromptText).NotEmpty();
        RuleForEach(command => command.Variables!)
            .ChildRules(variable => variable.RuleFor(v => v.Key).NotEmpty())
            .When(command => command.Variables is not null);
    }
}
