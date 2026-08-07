using FluentValidation;

namespace Icbank.Platform.Application.InternationalDays.Commands;

/// <summary>Validates <see cref="SearchInternationalDayCommand"/> (R-BE-034).</summary>
public sealed class SearchInternationalDayCommandValidator : AbstractValidator<SearchInternationalDayCommand>
{
    /// <summary>Initializes a new instance of the <see cref="SearchInternationalDayCommandValidator"/> class.</summary>
    public SearchInternationalDayCommandValidator()
    {
        RuleFor(command => command.Query).NotEmpty().WithMessage("query مطلوب");
        RuleFor(command => command.IpAddress).NotEmpty();
    }
}
