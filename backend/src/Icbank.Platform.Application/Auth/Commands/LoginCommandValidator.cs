using FluentValidation;

namespace Icbank.Platform.Application.Auth.Commands;

/// <summary>Validates <see cref="LoginCommand"/> (R-BE-034).</summary>
public sealed class LoginCommandValidator : AbstractValidator<LoginCommand>
{
    /// <summary>Initializes a new instance of the <see cref="LoginCommandValidator"/> class.</summary>
    public LoginCommandValidator()
    {
        RuleFor(command => command.Email).NotEmpty().EmailAddress();
        RuleFor(command => command.Password).NotEmpty();
    }
}
