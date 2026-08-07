using FluentAssertions;
using FluentValidation.Results;
using Icbank.Platform.Application.MediaMonitoring.Commands;
using Xunit;

namespace Icbank.Platform.UnitTests.Application.MediaMonitoring.FinalReports;

/// <summary>Verifies <see cref="CreateFinalMediaReportCommandValidator"/> (R-BE-034).</summary>
public sealed class CreateFinalMediaReportCommandValidatorTests
{
    private readonly CreateFinalMediaReportCommandValidator _validator = new();

    [Fact]
    public void Validate_ValidCommand_Succeeds()
    {
        var command = new CreateFinalMediaReportCommand(
            1, "عنوان", "Weekly", "الأسبوع الأول", DateTimeOffset.UtcNow.AddDays(-7), DateTimeOffset.UtcNow, FinalMediaReportTestData.BuildDraftDto());

        ValidationResult result = _validator.Validate(command);

        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void Validate_EmptyTitle_Fails()
    {
        var command = new CreateFinalMediaReportCommand(
            1, string.Empty, "Weekly", "الأسبوع الأول", DateTimeOffset.UtcNow.AddDays(-7), DateTimeOffset.UtcNow, FinalMediaReportTestData.BuildDraftDto());

        ValidationResult result = _validator.Validate(command);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().ContainSingle(e => e.PropertyName == nameof(CreateFinalMediaReportCommand.Title));
    }

    [Fact]
    public void Validate_DateToBeforeDateFrom_Fails()
    {
        var command = new CreateFinalMediaReportCommand(
            1, "عنوان", "Weekly", "الأسبوع الأول", DateTimeOffset.UtcNow, DateTimeOffset.UtcNow.AddDays(-1), FinalMediaReportTestData.BuildDraftDto());

        ValidationResult result = _validator.Validate(command);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().ContainSingle(e => e.PropertyName == nameof(CreateFinalMediaReportCommand.DateTo));
    }

    [Fact]
    public void Validate_EmptyPeriodLabel_Fails()
    {
        var command = new CreateFinalMediaReportCommand(
            1, "عنوان", "Weekly", string.Empty, DateTimeOffset.UtcNow.AddDays(-7), DateTimeOffset.UtcNow, FinalMediaReportTestData.BuildDraftDto());

        ValidationResult result = _validator.Validate(command);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().ContainSingle(e => e.PropertyName == nameof(CreateFinalMediaReportCommand.PeriodLabel));
    }
}
