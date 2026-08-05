using FluentAssertions;
using FluentValidation.Results;
using Icbank.Platform.Application.AiYear.Commands;
using Xunit;

namespace Icbank.Platform.UnitTests.Application.AiYear;

/// <summary>Verifies the ported required-field check and media-path regex (BUSINESS-RULES.md §3, API-SURFACE.md §22).</summary>
public sealed class CreateAiYearActivationCommandValidatorTests
{
    private static readonly string[] NoChannels = Array.Empty<string>();
    private static readonly string[] TwitterChannel = { "twitter" };
    private static readonly CreateAiYearActivationMediaItem[] InvalidMedia =
    {
        new("/objects/other/2026/1/1/x.jpg", null, null, null),
    };

    private static readonly CreateAiYearActivationMediaItem[] ValidMedia =
    {
        new("/objects/ai-year/2026/5/1/photo.jpg", "photo.jpg", "image/jpeg", 0),
    };

    private readonly CreateAiYearActivationCommandValidator _validator = new();

    [Fact]
    public void Validate_MissingChannels_Fails()
    {
        var command = new CreateAiYearActivationCommand(1, "Title", 5, 2026, null, "منشور", NoChannels, null, null, null, null, null, null, null, null);

        ValidationResult result = _validator.Validate(command);

        result.IsValid.Should().BeFalse();
    }

    [Fact]
    public void Validate_MonthOutOfRange_Fails()
    {
        var command = new CreateAiYearActivationCommand(1, "Title", 13, 2026, null, "منشور", TwitterChannel, null, null, null, null, null, null, null, null);

        ValidationResult result = _validator.Validate(command);

        result.IsValid.Should().BeFalse();
    }

    [Fact]
    public void Validate_MediaWithInvalidObjectPath_Fails()
    {
        var command = new CreateAiYearActivationCommand(1, "Title", 5, 2026, null, "منشور", TwitterChannel, null, null, null, null, null, null, InvalidMedia, null);

        ValidationResult result = _validator.Validate(command);

        result.IsValid.Should().BeFalse("a client-supplied objectPath outside the ai-year/2026 structure must be rejected");
    }

    [Fact]
    public void Validate_WellFormedCommand_Passes()
    {
        var command = new CreateAiYearActivationCommand(1, "Title", 5, 2026, null, "منشور", TwitterChannel, "desc", null, null, null, null, null, ValidMedia, null);

        ValidationResult result = _validator.Validate(command);

        result.IsValid.Should().BeTrue();
    }
}
