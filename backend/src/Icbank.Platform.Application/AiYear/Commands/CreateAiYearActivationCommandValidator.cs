using FluentValidation;

namespace Icbank.Platform.Application.AiYear.Commands;

/// <summary>Validates <see cref="CreateAiYearActivationCommand"/> (R-BE-034), matching the Node source's required-field check plus the media-path regex.</summary>
public sealed class CreateAiYearActivationCommandValidator : AbstractValidator<CreateAiYearActivationCommand>
{
    private const int MinMonth = 1;
    private const int MaxMonth = 12;

    /// <summary>Initializes a new instance of the <see cref="CreateAiYearActivationCommandValidator"/> class.</summary>
    public CreateAiYearActivationCommandValidator()
    {
        RuleFor(command => command.Title).NotEmpty();
        RuleFor(command => command.Month).InclusiveBetween(MinMonth, MaxMonth);
        RuleFor(command => command.Type).NotEmpty();
        RuleFor(command => command.Channels).NotEmpty().WithMessage("الحقول المطلوبة: title, month, type, channels");
        When(command => command.Media is not null, () =>
        {
            RuleForEach(command => command.Media!)
                .Must(item => AiYearMediaPathValidator.IsValid(item.ObjectPath))
                .WithMessage("objectPath غير صالح.");
        });
    }
}
