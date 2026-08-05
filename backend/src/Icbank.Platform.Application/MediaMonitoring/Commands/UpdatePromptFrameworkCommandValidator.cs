using FluentValidation;

namespace Icbank.Platform.Application.MediaMonitoring.Commands;

/// <summary>Validates <see cref="UpdatePromptFrameworkCommand"/> (R-BE-034).</summary>
public sealed class UpdatePromptFrameworkCommandValidator : AbstractValidator<UpdatePromptFrameworkCommand>
{
    private const int NameMaxLength = 200;

    /// <summary>Initializes a new instance of the <see cref="UpdatePromptFrameworkCommandValidator"/> class.</summary>
    public UpdatePromptFrameworkCommandValidator()
    {
        RuleFor(command => command.NameAr).MaximumLength(NameMaxLength).When(command => command.NameAr is not null);
        RuleFor(command => command.PromptText).NotEmpty().When(command => command.PromptText is not null);
    }
}
