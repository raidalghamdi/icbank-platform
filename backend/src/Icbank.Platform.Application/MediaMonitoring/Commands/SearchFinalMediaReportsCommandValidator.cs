using FluentValidation;

namespace Icbank.Platform.Application.MediaMonitoring.Commands;

/// <summary>Validates <see cref="SearchFinalMediaReportsCommand"/> (R-BE-034).</summary>
public sealed class SearchFinalMediaReportsCommandValidator : AbstractValidator<SearchFinalMediaReportsCommand>
{
    private const int MinLimit = 1;
    private const int MaxLimit = 50;

    /// <summary>Initializes a new instance of the <see cref="SearchFinalMediaReportsCommandValidator"/> class.</summary>
    public SearchFinalMediaReportsCommandValidator()
    {
        RuleFor(command => command.Mode).Must(mode => mode is "full" or "info").WithMessage("mode يجب أن يكون full أو info.");
        RuleFor(command => command.Query).NotEmpty();
        RuleFor(command => command.Limit).InclusiveBetween(MinLimit, MaxLimit).When(command => command.Limit.HasValue);
    }
}
