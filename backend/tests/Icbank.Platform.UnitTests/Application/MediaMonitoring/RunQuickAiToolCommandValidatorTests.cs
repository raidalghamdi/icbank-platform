using FluentAssertions;
using FluentValidation.Results;
using Icbank.Platform.Application.MediaMonitoring.Commands;
using Xunit;

namespace Icbank.Platform.UnitTests.Application.MediaMonitoring;

/// <summary>Verifies <see cref="RunQuickAiToolCommandValidator"/> (R-BE-034).</summary>
public sealed class RunQuickAiToolCommandValidatorTests
{
    private readonly RunQuickAiToolCommandValidator _validator = new();

    [Fact]
    public void Validate_MissingTool_Fails()
    {
        var command = new RunQuickAiToolCommand(1, string.Empty, "input", null, null);

        ValidationResult result = _validator.Validate(command);

        result.IsValid.Should().BeFalse();
    }

    [Fact]
    public void Validate_MissingInput_Fails()
    {
        var command = new RunQuickAiToolCommand(1, "summary", string.Empty, null, null);

        ValidationResult result = _validator.Validate(command);

        result.IsValid.Should().BeFalse();
    }

    [Fact]
    public void Validate_CountOutOfRange_Fails()
    {
        var command = new RunQuickAiToolCommand(1, "headlines", "input", null, 0);

        ValidationResult result = _validator.Validate(command);

        result.IsValid.Should().BeFalse();
    }

    [Fact]
    public void Validate_WellFormedCommand_Succeeds()
    {
        var command = new RunQuickAiToolCommand(1, "headlines", "input", "رسمية", 5);

        ValidationResult result = _validator.Validate(command);

        result.IsValid.Should().BeTrue();
    }
}
