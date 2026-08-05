using FluentValidation;

namespace Icbank.Platform.Application.Weekend.Commands;

/// <summary>Validates <see cref="SendWeekendReportCommand"/>. Ports the Node source's "channels must be a non-empty array" check.</summary>
public sealed class SendWeekendReportCommandValidator : AbstractValidator<SendWeekendReportCommand>
{
    /// <summary>Initializes a new instance of the <see cref="SendWeekendReportCommandValidator"/> class.</summary>
    public SendWeekendReportCommandValidator()
    {
        RuleFor(x => x.Channels).NotEmpty().WithMessage("لا توجد قنوات محددة");
    }
}
