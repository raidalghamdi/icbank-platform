using FluentValidation;

namespace Icbank.Platform.Application.AiYear.Commands;

/// <summary>Validates <see cref="UpdateAiYearActivationCommand"/> (R-BE-034), including the media-path regex (ported even though the Node source only checked it inside the handler, not before it).</summary>
public sealed class UpdateAiYearActivationCommandValidator : AbstractValidator<UpdateAiYearActivationCommand>
{
    private const int MinMonth = 1;
    private const int MaxMonth = 12;

    /// <summary>Initializes a new instance of the <see cref="UpdateAiYearActivationCommandValidator"/> class.</summary>
    public UpdateAiYearActivationCommandValidator()
    {
        RuleFor(command => command.Month!.Value).InclusiveBetween(MinMonth, MaxMonth).When(command => command.Month.HasValue);
        When(command => command.Media is not null, () =>
        {
            RuleForEach(command => command.Media!)
                .Must(item => AiYearMediaPathValidator.IsValid(item.ObjectPath))
                .WithMessage("objectPath غير صالح.");
        });
    }
}
