using FluentAssertions;
using FluentValidation.Results;
using Icbank.Platform.Application.InternationalDays;
using Xunit;

namespace Icbank.Platform.UnitTests.Application.InternationalDays;

/// <summary>
/// Proves DEFECT-LOG.md DATA-04/H-2 is closed: malformed or adversarial AI-provider JSON is
/// rejected by <see cref="DaySearchResultValidator"/> before it can reach persistence.
/// </summary>
public sealed class DaySearchResultValidatorTests
{
    private readonly DaySearchResultValidator _validator = new();

    [Fact]
    public void Validate_MissingDayNameAr_Fails()
    {
        var result = new DaySearchResultDto(null, "Test", null, null, null, null, null, null, null, null, null, null, null, null);

        ValidationResult validationResult = _validator.Validate(result);

        validationResult.IsValid.Should().BeFalse();
        validationResult.Errors.Should().Contain(e => e.PropertyName == nameof(DaySearchResultDto.DayNameAr));
    }

    [Fact]
    public void Validate_ActivationWithFabricatedNonUrlSourceUrl_Fails()
    {
        var activation = new DaySearchActivationDto("Entity", "حكومي", "منشور", "تويتر", "desc", "not-a-real-url", "السعودية", 2025);
        var result = new DaySearchResultDto("Day", "Day EN", null, null, null, null, null, null, null, null, new[] { activation }, null, null, null);

        ValidationResult validationResult = _validator.Validate(result);

        validationResult.IsValid.Should().BeFalse("a fabricated non-URL string must never be persisted as a source_url");
    }

    [Fact]
    public void Validate_WellFormedResult_Passes()
    {
        var activation = new DaySearchActivationDto("Entity", "حكومي", "منشور", "تويتر", "desc", "https://example.com/post", "السعودية", 2025);
        var source = new DaySearchSourceDto("https://example.com", "Title", "Publisher");
        DaySearchActivationDto[] activations = new[] { activation };
        DaySearchDesignSampleDto[] designSamples = Array.Empty<DaySearchDesignSampleDto>();
        var suggestions = new[] { "suggestion" };
        DaySearchSourceDto[] sources = new[] { source };
        var result = new DaySearchResultDto(
            "Day",
            "Day EN",
            "21 مارس",
            "Org",
            null,
            "History",
            null,
            "Theme",
            "Theme EN",
            null,
            activations,
            designSamples,
            suggestions,
            sources);

        ValidationResult validationResult = _validator.Validate(result);

        validationResult.IsValid.Should().BeTrue();
    }

    [Fact]
    public void Validate_SourceWithEmptyUrl_Fails()
    {
        var source = new DaySearchSourceDto(string.Empty, "Title", "Publisher");
        var result = new DaySearchResultDto("Day", null, null, null, null, null, null, null, null, null, null, null, null, new[] { source });

        ValidationResult validationResult = _validator.Validate(result);

        validationResult.IsValid.Should().BeFalse();
    }
}
