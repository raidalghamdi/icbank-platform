using FluentValidation;

namespace Icbank.Platform.Application.Admin.Commands;

/// <summary>Validates <see cref="SetUserPermissionOverrideCommand"/> (R-BE-034).</summary>
public sealed class SetUserPermissionOverrideCommandValidator : AbstractValidator<SetUserPermissionOverrideCommand>
{
    /// <summary>Initializes a new instance of the <see cref="SetUserPermissionOverrideCommandValidator"/> class.</summary>
    public SetUserPermissionOverrideCommandValidator()
    {
        RuleFor(command => command.TargetUserId).GreaterThan(0);
        RuleFor(command => command.PageSlug).NotEmpty();
        RuleFor(command => command.PermissionName).NotEmpty();
        RuleFor(command => command.GrantType)
            .Must(grantType => grantType is null || string.Equals(grantType, "allow", StringComparison.OrdinalIgnoreCase) || string.Equals(grantType, "deny", StringComparison.OrdinalIgnoreCase))
            .WithMessage("GrantType must be 'allow', 'deny', or null.");
    }
}
