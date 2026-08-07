using FluentAssertions;
using FluentValidation.Results;
using Icbank.Platform.Application.MediaMonitoring.Commands;
using Xunit;

namespace Icbank.Platform.UnitTests.Application.MediaMonitoring;

/// <summary>Verifies <see cref="CreatePromptFrameworkCommandValidator"/> (R-BE-034), matching the Node source's <c>PromptCreateSchema</c> required fields.</summary>
public sealed class CreatePromptFrameworkCommandValidatorTests
{
    private readonly CreatePromptFrameworkCommandValidator _validator = new();

    [Fact]
    public void Validate_MissingNameAr_Fails()
    {
        var command = new CreatePromptFrameworkCommand(1, string.Empty, null, null, null, null, "نص", null, null, null, null, null);

        ValidationResult result = _validator.Validate(command);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().ContainSingle(e => e.PropertyName == nameof(CreatePromptFrameworkCommand.NameAr));
    }

    [Fact]
    public void Validate_MissingPromptText_Fails()
    {
        var command = new CreatePromptFrameworkCommand(1, "اسم", null, null, null, null, string.Empty, null, null, null, null, null);

        ValidationResult result = _validator.Validate(command);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().ContainSingle(e => e.PropertyName == nameof(CreatePromptFrameworkCommand.PromptText));
    }

    [Fact]
    public void Validate_VariableWithEmptyKey_Fails()
    {
        PromptVariableItem[] variables = { new(string.Empty, "التسمية", null, null) };
        var command = new CreatePromptFrameworkCommand(1, "اسم", null, null, null, null, "نص", variables, null, null, null, null);

        ValidationResult result = _validator.Validate(command);

        result.IsValid.Should().BeFalse();
    }

    [Fact]
    public void Validate_WellFormedCommand_Succeeds()
    {
        var command = new CreatePromptFrameworkCommand(1, "اسم", null, null, null, null, "نص", null, null, null, null, null);

        ValidationResult result = _validator.Validate(command);

        result.IsValid.Should().BeTrue();
    }
}
