using FluentValidation;

namespace Icbank.Platform.Application.Designs.Composer.Commands;

/// <summary>Validates <see cref="CreateDesignTemplateCommand"/> (R-BE-034), matching the Node source's <c>insertDesignTemplateSchema</c> required-field shape.</summary>
public sealed class CreateDesignTemplateCommandValidator : AbstractValidator<CreateDesignTemplateCommand>
{
    private const int MinCanvasDimension = 1;

    /// <summary>Initializes a new instance of the <see cref="CreateDesignTemplateCommandValidator"/> class.</summary>
    public CreateDesignTemplateCommandValidator()
    {
        RuleFor(command => command.TemplateNameAr).NotEmpty();
        RuleFor(command => command.Category).NotEmpty();
        RuleFor(command => command.CanvasWidth).GreaterThanOrEqualTo(MinCanvasDimension);
        RuleFor(command => command.CanvasHeight).GreaterThanOrEqualTo(MinCanvasDimension);
    }
}
