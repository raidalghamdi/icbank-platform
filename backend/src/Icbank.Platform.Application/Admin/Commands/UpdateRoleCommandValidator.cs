using FluentValidation;

namespace Icbank.Platform.Application.Admin.Commands;

/// <summary>Validates <see cref="UpdateRoleCommand"/> (R-BE-034).</summary>
public sealed class UpdateRoleCommandValidator : AbstractValidator<UpdateRoleCommand>
{
    private const int MaxNameLength = 100;
    private const int MaxDescriptionLength = 500;

    /// <summary>Initializes a new instance of the <see cref="UpdateRoleCommandValidator"/> class.</summary>
    public UpdateRoleCommandValidator()
    {
        RuleFor(command => command.RoleId).GreaterThan(0);
        RuleFor(command => command.NameAr).MaximumLength(MaxNameLength);
        RuleFor(command => command.Description).MaximumLength(MaxDescriptionLength);
    }
}
