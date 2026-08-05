using FluentValidation;

namespace Icbank.Platform.Application.Admin.Commands;

/// <summary>Validates <see cref="UpdateSystemSettingsCommand"/> (R-BE-034).</summary>
public sealed class UpdateSystemSettingsCommandValidator : AbstractValidator<UpdateSystemSettingsCommand>
{
    /// <summary>Initializes a new instance of the <see cref="UpdateSystemSettingsCommandValidator"/> class.</summary>
    public UpdateSystemSettingsCommandValidator()
    {
        RuleFor(command => command.Settings).NotEmpty();
    }
}
