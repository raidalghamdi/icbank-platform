using FluentAssertions;
using FluentValidation.Results;
using Icbank.Platform.Application.Gac.Commands;
using Xunit;

namespace Icbank.Platform.UnitTests.Application.Gac;

/// <summary>
/// Verifies <see cref="ReseedGacPublicationsCommandValidator"/> validates every publication item
/// in the batch and rejects unknown category/language/sourceDomain values with the enumerated
/// list of accepted values in the error message (R-BE-034).
/// </summary>
public sealed class ReseedGacPublicationsCommandValidatorTests
{
    private readonly ReseedGacPublicationsCommandValidator _validator = new();

    [Fact]
    public void Validate_AllValidItems_Passes()
    {
        var command = new ReseedGacPublicationsCommand(1, new[] { ValidItem() });

        ValidationResult result = _validator.Validate(command);

        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void Validate_EmptyTitleAr_Fails()
    {
        ReseedGacPublicationItem item = ValidItem() with { TitleAr = string.Empty };
        var command = new ReseedGacPublicationsCommand(1, new[] { item });

        ValidationResult result = _validator.Validate(command);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName.EndsWith("TitleAr", StringComparison.Ordinal));
    }

    [Fact]
    public void Validate_EmptyFileUrl_Fails()
    {
        ReseedGacPublicationItem item = ValidItem() with { FileUrl = string.Empty };
        var command = new ReseedGacPublicationsCommand(1, new[] { item });

        ValidationResult result = _validator.Validate(command);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName.EndsWith("FileUrl", StringComparison.Ordinal));
    }

    [Fact]
    public void Validate_UnknownCategory_FailsWithEnumeratedCategoryList()
    {
        ReseedGacPublicationItem item = ValidItem() with { Category = "NotACategory" };
        var command = new ReseedGacPublicationsCommand(1, new[] { item });

        ValidationResult result = _validator.Validate(command);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.ErrorMessage.StartsWith("category must be one of:", StringComparison.Ordinal));
    }

    [Fact]
    public void Validate_UnknownLanguage_FailsWithEnumeratedLanguageList()
    {
        ReseedGacPublicationItem item = ValidItem() with { Language = "Klingon" };
        var command = new ReseedGacPublicationsCommand(1, new[] { item });

        ValidationResult result = _validator.Validate(command);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.ErrorMessage.StartsWith("language must be one of:", StringComparison.Ordinal));
    }

    [Fact]
    public void Validate_UnknownSourceDomain_FailsWithEnumeratedSourceDomainList()
    {
        ReseedGacPublicationItem item = ValidItem() with { SourceDomain = "Mars" };
        var command = new ReseedGacPublicationsCommand(1, new[] { item });

        ValidationResult result = _validator.Validate(command);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.ErrorMessage.StartsWith("sourceDomain must be one of:", StringComparison.Ordinal));
    }

    [Fact]
    public void Validate_CategoryLanguageSourceDomainCaseInsensitive_Passes()
    {
        ReseedGacPublicationItem item = ValidItem() with { Category = "statistics", Language = "ar", SourceDomain = "direct" };
        var command = new ReseedGacPublicationsCommand(1, new[] { item });

        ValidationResult result = _validator.Validate(command);

        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void Validate_MixOfValidAndInvalidItems_FailsOnlyForTheInvalidOne()
    {
        var command = new ReseedGacPublicationsCommand(1, new[] { ValidItem(), ValidItem() with { TitleAr = string.Empty } });

        ValidationResult result = _validator.Validate(command);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().ContainSingle();
    }

    [Fact]
    public void Validate_EmptyPublicationsList_Passes()
    {
        var command = new ReseedGacPublicationsCommand(1, Array.Empty<ReseedGacPublicationItem>());

        ValidationResult result = _validator.Validate(command);

        result.IsValid.Should().BeTrue();
    }

    private static ReseedGacPublicationItem ValidItem() => new(
        "عنوان", "Title", "Statistics", "Ar", null, null, "https://files/x.pdf", 1024, 10, null, "Direct", 1);
}
