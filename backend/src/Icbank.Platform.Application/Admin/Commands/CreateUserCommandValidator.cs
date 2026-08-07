using FluentValidation;

namespace Icbank.Platform.Application.Admin.Commands;

/// <summary>Validates <see cref="CreateUserCommand"/> (R-BE-034).</summary>
public sealed class CreateUserCommandValidator : AbstractValidator<CreateUserCommand>
{
    private const int MaxNameLength = 200;

    /// <summary>Initializes a new instance of the <see cref="CreateUserCommandValidator"/> class.</summary>
    public CreateUserCommandValidator()
    {
        RuleFor(command => command.Email).NotEmpty().EmailAddress();
        RuleFor(command => command.Name).NotEmpty().MaximumLength(MaxNameLength);
        RuleFor(command => command.RoleId).GreaterThan(0);
        RuleFor(command => command.Password).MinimumLength(8).When(command => !string.IsNullOrEmpty(command.Password));
    }
}
