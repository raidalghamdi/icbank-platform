using FluentValidation;

namespace Icbank.Platform.Application.MediaMonitoring.Commands;

/// <summary>Validates <see cref="GenerateFinalMediaReportCommand"/> (R-BE-034), matching the Node source's <c>generateSchema</c>.</summary>
public sealed class GenerateFinalMediaReportCommandValidator : AbstractValidator<GenerateFinalMediaReportCommand>
{
    /// <summary>Initializes a new instance of the <see cref="GenerateFinalMediaReportCommandValidator"/> class.</summary>
    public GenerateFinalMediaReportCommandValidator()
    {
        RuleFor(command => command.PeriodLabel).NotEmpty();
        RuleFor(command => command.DateTo)
            .GreaterThanOrEqualTo(command => command.DateFrom)
            .WithMessage("dateTo يجب أن يكون بعد أو يساوي dateFrom.");
    }
}
