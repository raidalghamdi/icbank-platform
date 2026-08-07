using FluentAssertions;
using FluentValidation.Results;
using Icbank.Platform.Application.MediaMonitoring.Commands;
using Xunit;

namespace Icbank.Platform.UnitTests.Application.MediaMonitoring.FinalReports;

/// <summary>Verifies <see cref="SearchFinalMediaReportsCommandValidator"/> (R-BE-034).</summary>
public sealed class SearchFinalMediaReportsCommandValidatorTests
{
    private readonly SearchFinalMediaReportsCommandValidator _validator = new();

    [Theory]
    [InlineData("full")]
    [InlineData("info")]
    public void Validate_RecognisedMode_Succeeds(string mode)
    {
        var command = new SearchFinalMediaReportsCommand(1, mode, "استعلام", null);

        ValidationResult result = _validator.Validate(command);

        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void Validate_UnrecognisedMode_Fails()
    {
        var command = new SearchFinalMediaReportsCommand(1, "unknown", "استعلام", null);

        ValidationResult result = _validator.Validate(command);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().ContainSingle(e => e.PropertyName == nameof(SearchFinalMediaReportsCommand.Mode));
    }

    [Fact]
    public void Validate_EmptyQuery_Fails()
    {
        var command = new SearchFinalMediaReportsCommand(1, "full", string.Empty, null);

        ValidationResult result = _validator.Validate(command);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().ContainSingle(e => e.PropertyName == nameof(SearchFinalMediaReportsCommand.Query));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(51)]
    public void Validate_LimitOutOfRange_Fails(int limit)
    {
        var command = new SearchFinalMediaReportsCommand(1, "full", "استعلام", limit);

        ValidationResult result = _validator.Validate(command);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().ContainSingle(e => e.PropertyName == nameof(SearchFinalMediaReportsCommand.Limit));
    }

    [Fact]
    public void Validate_LimitOmitted_Succeeds()
    {
        var command = new SearchFinalMediaReportsCommand(1, "full", "استعلام", null);

        ValidationResult result = _validator.Validate(command);

        result.IsValid.Should().BeTrue();
    }
}
