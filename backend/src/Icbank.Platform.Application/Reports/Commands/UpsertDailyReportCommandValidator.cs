using FluentValidation;

namespace Icbank.Platform.Application.Reports.Commands;

/// <summary>Validates <see cref="UpsertDailyReportCommand"/>.</summary>
public sealed class UpsertDailyReportCommandValidator : AbstractValidator<UpsertDailyReportCommand>
{
    private const string IsoDatePattern = @"^\d{4}-\d{2}-\d{2}$";

    /// <summary>Initializes a new instance of the <see cref="UpsertDailyReportCommandValidator"/> class.</summary>
    public UpsertDailyReportCommandValidator()
    {
        RuleFor(x => x.ReportDate)
            .NotEmpty()
            .Matches(IsoDatePattern).WithMessage("reportDate must be in YYYY-MM-DD format.");

        RuleFor(x => x.ReportDataJson).NotEmpty();
    }
}
