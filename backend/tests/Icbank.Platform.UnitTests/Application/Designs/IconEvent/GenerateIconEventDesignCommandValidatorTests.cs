using FluentAssertions;
using FluentValidation.Results;
using Icbank.Platform.Application.Designs.IconEvent.Commands;
using Xunit;

namespace Icbank.Platform.UnitTests.Application.Designs.IconEvent;

/// <summary>Verifies <see cref="GenerateIconEventDesignCommandValidator"/> matches the Node source's input-sufficiency and size-allowlist rules.</summary>
public sealed class GenerateIconEventDesignCommandValidatorTests
{
    private readonly GenerateIconEventDesignCommandValidator _validator = new();

    [Fact]
    public void Validate_RawDataTooShortAndNoHeadline_Fails()
    {
        GenerateIconEventDesignCommand command = Build(rawData: "abcd", headline: null, size: "landscape");

        ValidationResult result = _validator.Validate(command);

        result.IsValid.Should().BeFalse();
    }

    [Fact]
    public void Validate_HeadlineLongEnough_Passes()
    {
        GenerateIconEventDesignCommand command = Build(rawData: null, headline: "عنوان جيد", size: "landscape");

        ValidationResult result = _validator.Validate(command);

        result.IsValid.Should().BeTrue();
    }

    [Theory]
    [InlineData("square")]
    [InlineData("story")]
    [InlineData("landscape")]
    public void Validate_AllowedSize_Passes(string size)
    {
        GenerateIconEventDesignCommand command = Build(rawData: null, headline: "عنوان جيد", size: size);

        ValidationResult result = _validator.Validate(command);

        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void Validate_UnknownSize_Fails()
    {
        GenerateIconEventDesignCommand command = Build(rawData: null, headline: "عنوان جيد", size: "poster");

        ValidationResult result = _validator.Validate(command);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == nameof(GenerateIconEventDesignCommand.Size));
    }

    private static GenerateIconEventDesignCommand Build(string? rawData, string? headline, string size) =>
        new(1, rawData, headline, Subtitle: null, Department: null, Hashtag: null, Date: null, Time: null, Location: null, EventType: null, size, MainIconOverride: null);
}
