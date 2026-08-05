using FluentValidation;

namespace Icbank.Platform.Application.MediaMonitoring.Commands;

/// <summary>Validates <see cref="GenerateMediaReportCommand"/> (R-BE-034).</summary>
public sealed class GenerateMediaReportCommandValidator : AbstractValidator<GenerateMediaReportCommand>
{
    /// <summary>Initializes a new instance of the <see cref="GenerateMediaReportCommandValidator"/> class.</summary>
    public GenerateMediaReportCommandValidator()
    {
        RuleFor(command => command.DateTo)
            .GreaterThanOrEqualTo(command => command.DateFrom!.Value)
            .When(command => command.DateFrom.HasValue && command.DateTo.HasValue)
            .WithMessage("dateTo يجب أن يكون بعد أو يساوي dateFrom.");

        RuleFor(command => command.CustomTitle)
            .MaximumLength(300);
    }
}
