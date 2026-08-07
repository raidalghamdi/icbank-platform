using FluentValidation;

namespace Icbank.Platform.Application.InternationalDays.Commands;

/// <summary>
/// Validates <see cref="SaveInternationalDayCommand"/> (R-BE-034). Delegates the AI-result shape
/// check to <see cref="DaySearchResultValidator"/>, closing DEFECT-LOG.md DATA-04/H-2: the Node
/// source wrote AI-provider JSON straight into the database with zero schema validation despite
/// an equivalent Zod schema already existing and going unused.
/// </summary>
public sealed class SaveInternationalDayCommandValidator : AbstractValidator<SaveInternationalDayCommand>
{
    /// <summary>Initializes a new instance of the <see cref="SaveInternationalDayCommandValidator"/> class.</summary>
    public SaveInternationalDayCommandValidator()
    {
        RuleFor(command => command.Data).NotNull().SetValidator(new DaySearchResultValidator());
    }
}
