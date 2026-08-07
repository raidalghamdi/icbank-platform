using FluentAssertions;
using FluentValidation.Results;
using Icbank.Platform.Application.MediaMonitoring.Commands;
using Xunit;

namespace Icbank.Platform.UnitTests.Application.MediaMonitoring.FinalReports;

/// <summary>Verifies <see cref="GenerateFinalMediaReportCommandValidator"/> (R-BE-034), matching the Node source's <c>generateSchema</c>.</summary>
public sealed class GenerateFinalMediaReportCommandValidatorTests
{
    private readonly GenerateFinalMediaReportCommandValidator _validator = new();

    [Fact]
    public void Validate_ValidCommand_Succeeds()
    {
        var command = new GenerateFinalMediaReportCommand(1, "يوليو 2026", "عام", DateTimeOffset.UtcNow.AddDays(-7), DateTimeOffset.UtcNow, null);

        ValidationResult result = _validator.Validate(command);

        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void Validate_EmptyPeriodLabel_Fails()
    {
        var command = new GenerateFinalMediaReportCommand(1, string.Empty, "عام", DateTimeOffset.UtcNow.AddDays(-7), DateTimeOffset.UtcNow, null);

        ValidationResult result = _validator.Validate(command);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().ContainSingle(e => e.PropertyName == nameof(GenerateFinalMediaReportCommand.PeriodLabel));
    }

    [Fact]
    public void Validate_DateToBeforeDateFrom_FailsWithArabicMessage()
    {
        var command = new GenerateFinalMediaReportCommand(1, "يوليو 2026", "عام", DateTimeOffset.UtcNow, DateTimeOffset.UtcNow.AddDays(-1), null);

        ValidationResult result = _validator.Validate(command);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().ContainSingle(e => e.PropertyName == nameof(GenerateFinalMediaReportCommand.DateTo));
    }

    [Fact]
    public void Validate_DateToEqualsDateFrom_Succeeds()
    {
        DateTimeOffset now = DateTimeOffset.UtcNow;
        var command = new GenerateFinalMediaReportCommand(1, "يوم واحد", "عام", now, now, null);

        ValidationResult result = _validator.Validate(command);

        result.IsValid.Should().BeTrue();
    }
}
