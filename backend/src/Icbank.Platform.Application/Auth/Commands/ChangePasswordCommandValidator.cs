using FluentValidation;

namespace Icbank.Platform.Application.Auth.Commands;

/// <summary>Validates the self-service password-change request.</summary>
public sealed class ChangePasswordCommandValidator : AbstractValidator<ChangePasswordCommand>
{
    /// <summary>Initializes a new instance of the <see cref="ChangePasswordCommandValidator"/> class.</summary>
    public ChangePasswordCommandValidator()
    {
        RuleFor(command => command.CurrentPassword).NotEmpty();
        RuleFor(command => command.NewPassword).NotEmpty().MinimumLength(8);
        RuleFor(command => command.NewPassword).NotEqual(command => command.CurrentPassword);
    }
}
