using FluentAssertions;
using FluentValidation.Results;
using Icbank.Platform.Application.MediaMonitoring.Commands;
using Xunit;

namespace Icbank.Platform.UnitTests.Application.MediaMonitoring.FinalReports;

/// <summary>Verifies <see cref="LogWizardQaQueryCommandValidator"/> (R-BE-034).</summary>
public sealed class LogWizardQaQueryCommandValidatorTests
{
    private readonly LogWizardQaQueryCommandValidator _validator = new();

    [Fact]
    public void Validate_ValidCommand_Succeeds()
    {
        var command = new LogWizardQaQueryCommand(1, "أسبوعي", "تنفيذي", new List<string> { "أخبار" }, "منافسة", "ar", "a@b.com", "generate");

        ValidationResult result = _validator.Validate(command);

        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void Validate_ZeroActorUserId_Fails()
    {
        var command = new LogWizardQaQueryCommand(0, null, null, null, null, null, null, null);

        ValidationResult result = _validator.Validate(command);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().ContainSingle(e => e.PropertyName == nameof(LogWizardQaQueryCommand.ActorUserId));
    }

    [Fact]
    public void Validate_UnrecognisedMode_Fails()
    {
        var command = new LogWizardQaQueryCommand(1, null, null, null, null, null, null, "not-a-mode");

        ValidationResult result = _validator.Validate(command);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().ContainSingle(e => e.PropertyName == nameof(LogWizardQaQueryCommand.Mode));
    }

    [Fact]
    public void Validate_NullMode_Succeeds()
    {
        var command = new LogWizardQaQueryCommand(1, null, null, null, null, null, null, null);

        ValidationResult result = _validator.Validate(command);

        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void Validate_PeriodTooLong_Fails()
    {
        var command = new LogWizardQaQueryCommand(1, new string('س', 2001), null, null, null, null, null, null);

        ValidationResult result = _validator.Validate(command);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().ContainSingle(e => e.PropertyName == nameof(LogWizardQaQueryCommand.Period));
    }
}
