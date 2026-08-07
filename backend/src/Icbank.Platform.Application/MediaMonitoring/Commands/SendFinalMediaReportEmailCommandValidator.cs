using FluentValidation;

namespace Icbank.Platform.Application.MediaMonitoring.Commands;

/// <summary>Validates <see cref="SendFinalMediaReportEmailCommand"/> (R-BE-034), matching the Node source's <c>{recipients:email[]}</c> shape.</summary>
public sealed class SendFinalMediaReportEmailCommandValidator : AbstractValidator<SendFinalMediaReportEmailCommand>
{
    /// <summary>Initializes a new instance of the <see cref="SendFinalMediaReportEmailCommandValidator"/> class.</summary>
    public SendFinalMediaReportEmailCommandValidator()
    {
        RuleFor(command => command.Recipients).NotEmpty();
        RuleForEach(command => command.Recipients).EmailAddress();
    }
}
