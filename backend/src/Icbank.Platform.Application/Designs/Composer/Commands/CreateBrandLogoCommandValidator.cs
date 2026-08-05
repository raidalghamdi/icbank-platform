using FluentValidation;

namespace Icbank.Platform.Application.Designs.Composer.Commands;

/// <summary>Validates <see cref="CreateBrandLogoCommand"/> (R-BE-034), matching the Node source's <c>insertBrandLogoSchema</c> required-field shape.</summary>
public sealed class CreateBrandLogoCommandValidator : AbstractValidator<CreateBrandLogoCommand>
{
    /// <summary>Initializes a new instance of the <see cref="CreateBrandLogoCommandValidator"/> class.</summary>
    public CreateBrandLogoCommandValidator()
    {
        RuleFor(command => command.LogoName).NotEmpty();
        RuleFor(command => command.FileUrl).NotEmpty();
    }
}
