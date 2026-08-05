using FluentValidation;

namespace Icbank.Platform.Application.MediaMonitoring.Commands;

/// <summary>Validates <see cref="CreateFinalMediaReportCommand"/> (R-BE-034).</summary>
public sealed class CreateFinalMediaReportCommandValidator : AbstractValidator<CreateFinalMediaReportCommand>
{
    /// <summary>Initializes a new instance of the <see cref="CreateFinalMediaReportCommandValidator"/> class.</summary>
    public CreateFinalMediaReportCommandValidator()
    {
        RuleFor(command => command.Title).NotEmpty();
        RuleFor(command => command.PeriodLabel).NotEmpty();
        RuleFor(command => command.DateTo).GreaterThanOrEqualTo(command => command.DateFrom);
        RuleFor(command => command.Draft).NotNull();
    }
}
