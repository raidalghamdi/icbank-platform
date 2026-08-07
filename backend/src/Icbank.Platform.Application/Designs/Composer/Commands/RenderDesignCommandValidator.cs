using FluentValidation;

namespace Icbank.Platform.Application.Designs.Composer.Commands;

/// <summary>Validates <see cref="RenderDesignCommand"/> (R-BE-034). Adds a <c>templateId</c> presence check the Node source enforced only with a bare 400, plus font-size bounds the Node source never validated (BUSINESS-RULES.md §22 gap).</summary>
public sealed class RenderDesignCommandValidator : AbstractValidator<RenderDesignCommand>
{
    private const double MinFontSize = 1;
    private const double MaxFontSize = 500;

    /// <summary>Initializes a new instance of the <see cref="RenderDesignCommandValidator"/> class.</summary>
    public RenderDesignCommandValidator()
    {
        RuleFor(command => command.TemplateId).GreaterThan(0).WithMessage("templateId مطلوب");
        RuleFor(command => command.TitleFontSize!.Value).InclusiveBetween(MinFontSize, MaxFontSize).When(command => command.TitleFontSize.HasValue);
        RuleFor(command => command.BodyFontSize!.Value).InclusiveBetween(MinFontSize, MaxFontSize).When(command => command.BodyFontSize.HasValue);
    }
}
