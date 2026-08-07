using FluentValidation;

namespace Icbank.Platform.Application.Weekend.Commands;

/// <summary>Validates <see cref="GenerateWeekStartMessagesCommand"/>.</summary>
public sealed class GenerateWeekStartMessagesCommandValidator : AbstractValidator<GenerateWeekStartMessagesCommand>
{
    private const int TopicMaxLength = 300;

    /// <summary>Initializes a new instance of the <see cref="GenerateWeekStartMessagesCommandValidator"/> class.</summary>
    public GenerateWeekStartMessagesCommandValidator()
    {
        RuleFor(x => x.Topic).NotEmpty().MaximumLength(TopicMaxLength);
    }
}
