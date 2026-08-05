using FluentValidation;

namespace Icbank.Platform.Application.MediaMonitoring.Commands;

/// <summary>Validates <see cref="RunQuickAiToolCommand"/> (R-BE-034). The tool key itself is validated against the fixed 7-tool set at the handler level, since that mapping is domain knowledge, not shape validation.</summary>
public sealed class RunQuickAiToolCommandValidator : AbstractValidator<RunQuickAiToolCommand>
{
    private const int MinCount = 1;
    private const int MaxCount = 50;

    /// <summary>Initializes a new instance of the <see cref="RunQuickAiToolCommandValidator"/> class.</summary>
    public RunQuickAiToolCommandValidator()
    {
        RuleFor(command => command.Tool).NotEmpty();
        RuleFor(command => command.Input).NotEmpty();
        RuleFor(command => command.Count).InclusiveBetween(MinCount, MaxCount).When(command => command.Count.HasValue);
    }
}
