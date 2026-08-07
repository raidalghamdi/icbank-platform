using FluentValidation;

namespace Icbank.Platform.Application.Designs.Composer.Commands;

/// <summary>Validates <see cref="CreateBrandFontCommand"/> (R-BE-034), matching the Node source's <c>insertBrandFontSchema</c> required-field shape.</summary>
public sealed class CreateBrandFontCommandValidator : AbstractValidator<CreateBrandFontCommand>
{
    /// <summary>Initializes a new instance of the <see cref="CreateBrandFontCommandValidator"/> class.</summary>
    public CreateBrandFontCommandValidator()
    {
        RuleFor(command => command.FontName).NotEmpty();
        RuleFor(command => command.FontFileUrl).NotEmpty();
    }
}
