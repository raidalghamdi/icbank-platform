using FluentAssertions;
using FluentValidation.Results;
using Icbank.Platform.Application.MediaMonitoring.Commands;
using Xunit;

namespace Icbank.Platform.UnitTests.Application.MediaMonitoring;

/// <summary>Verifies <see cref="GenerateMediaReportCommandValidator"/> (R-BE-034).</summary>
public sealed class GenerateMediaReportCommandValidatorTests
{
    private readonly GenerateMediaReportCommandValidator _validator = new();

    [Fact]
    public void Validate_DateToBeforeDateFrom_Fails()
    {
        var command = new GenerateMediaReportCommand(
            1, "manager", "weekly", DateTimeOffset.UtcNow, DateTimeOffset.UtcNow.AddDays(-1), null, null);

        ValidationResult result = _validator.Validate(command);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().ContainSingle(e => e.PropertyName == nameof(GenerateMediaReportCommand.DateTo));
    }

    [Fact]
    public void Validate_DateToOnOrAfterDateFrom_Succeeds()
    {
        var command = new GenerateMediaReportCommand(
            1, "manager", "weekly", DateTimeOffset.UtcNow.AddDays(-1), DateTimeOffset.UtcNow, null, null);

        ValidationResult result = _validator.Validate(command);

        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void Validate_NoDatesProvided_Succeeds()
    {
        var command = new GenerateMediaReportCommand(1, "manager", "weekly", null, null, null, null);

        ValidationResult result = _validator.Validate(command);

        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void Validate_TitleTooLong_Fails()
    {
        var command = new GenerateMediaReportCommand(1, "manager", "weekly", null, null, null, new string('س', 301));

        ValidationResult result = _validator.Validate(command);

        result.IsValid.Should().BeFalse();
    }
}
