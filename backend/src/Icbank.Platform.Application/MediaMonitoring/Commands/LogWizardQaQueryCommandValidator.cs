using FluentValidation;

namespace Icbank.Platform.Application.MediaMonitoring.Commands;

/// <summary>Validates <see cref="LogWizardQaQueryCommand"/> (R-BE-034).</summary>
public sealed class LogWizardQaQueryCommandValidator : AbstractValidator<LogWizardQaQueryCommand>
{
    private const int MaxFreeTextLength = 2000;

    /// <summary>Initializes a new instance of the <see cref="LogWizardQaQueryCommandValidator"/> class.</summary>
    public LogWizardQaQueryCommandValidator()
    {
        RuleFor(command => command.ActorUserId).GreaterThan(0);
        RuleFor(command => command.Period).MaximumLength(MaxFreeTextLength);
        RuleFor(command => command.Audience).MaximumLength(MaxFreeTextLength);
        RuleFor(command => command.FocusTopics).MaximumLength(MaxFreeTextLength);
        RuleFor(command => command.Mode).Must(mode => mode is null || mode is "generate" or "search")
            .WithMessage("mode يجب أن يكون generate أو search.");
    }
}
