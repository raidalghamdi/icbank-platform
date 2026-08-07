using FluentAssertions;
using FluentValidation.Results;
using Icbank.Platform.Application.MediaMonitoring.Commands;
using Xunit;

namespace Icbank.Platform.UnitTests.Application.MediaMonitoring.FinalReports;

/// <summary>Verifies <see cref="SendFinalMediaReportEmailCommandValidator"/> (R-BE-034), matching the Node source's <c>{recipients:email[]}</c> shape.</summary>
public sealed class SendFinalMediaReportEmailCommandValidatorTests
{
    private readonly SendFinalMediaReportEmailCommandValidator _validator = new();

    [Fact]
    public void Validate_ValidRecipients_Succeeds()
    {
        var command = new SendFinalMediaReportEmailCommand(1, 1, new List<string> { "a@example.com" }, null);

        ValidationResult result = _validator.Validate(command);

        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void Validate_EmptyRecipients_Fails()
    {
        var command = new SendFinalMediaReportEmailCommand(1, 1, new List<string>(), null);

        ValidationResult result = _validator.Validate(command);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().ContainSingle(e => e.PropertyName == nameof(SendFinalMediaReportEmailCommand.Recipients));
    }

    [Fact]
    public void Validate_MalformedEmailAddress_Fails()
    {
        var command = new SendFinalMediaReportEmailCommand(1, 1, new List<string> { "not-an-email" }, null);

        ValidationResult result = _validator.Validate(command);

        result.IsValid.Should().BeFalse();
    }

    [Fact]
    public void Validate_MixOfValidAndInvalidRecipients_Fails()
    {
        var command = new SendFinalMediaReportEmailCommand(1, 1, new List<string> { "a@example.com", "bad" }, "عنوان مخصص");

        ValidationResult result = _validator.Validate(command);

        result.IsValid.Should().BeFalse();
    }
}
