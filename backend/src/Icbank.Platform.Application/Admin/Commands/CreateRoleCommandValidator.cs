using FluentValidation;

namespace Icbank.Platform.Application.Admin.Commands;

/// <summary>Validates <see cref="CreateRoleCommand"/> (R-BE-034).</summary>
public sealed class CreateRoleCommandValidator : AbstractValidator<CreateRoleCommand>
{
    private const int MaxNameLength = 100;
    private const int MaxDescriptionLength = 500;

    /// <summary>Initializes a new instance of the <see cref="CreateRoleCommandValidator"/> class.</summary>
    public CreateRoleCommandValidator()
    {
        RuleFor(command => command.Name).NotEmpty().MaximumLength(MaxNameLength).Matches("^[a-z0-9_]+$");
        RuleFor(command => command.NameAr).NotEmpty().MaximumLength(MaxNameLength);
        RuleFor(command => command.Description).MaximumLength(MaxDescriptionLength);
    }
}
