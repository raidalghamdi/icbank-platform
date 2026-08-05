using FluentAssertions;
using Icbank.Platform.DataMigration.Mapping;
using Icbank.Platform.Domain.Shorfah;
using Xunit;

namespace Icbank.Platform.DataMigration.Tests.Mapping;

/// <summary>
/// Exhaustive unit tests for <see cref="SnakeCaseEnumParser"/> against every literal value
/// enumerated in DATA-MODEL.md section 5 for <see cref="ShorfahSectionType"/> (13 values) and
/// <see cref="ShorfahWorkflowStatus"/> (5 values), plus hard cases (single-word values, unknown
/// values, casing, empty input).
/// </summary>
public sealed class SnakeCaseEnumParserTests
{
    [Theory]
    [InlineData("global_news", ShorfahSectionType.GlobalNews)]
    [InlineData("news", ShorfahSectionType.News)]
    [InlineData("intl_participation", ShorfahSectionType.IntlParticipation)]
    [InlineData("our_comms", ShorfahSectionType.OurComms)]
    [InlineData("economic_observatory", ShorfahSectionType.EconomicObservatory)]
    [InlineData("system_index", ShorfahSectionType.SystemIndex)]
    [InlineData("legal_window", ShorfahSectionType.LegalWindow)]
    [InlineData("office_interview", ShorfahSectionType.OfficeInterview)]
    [InlineData("competition_culture", ShorfahSectionType.CompetitionCulture)]
    [InlineData("outside_box", ShorfahSectionType.OutsideBox)]
    [InlineData("events", ShorfahSectionType.Events)]
    [InlineData("agency_lit", ShorfahSectionType.AgencyLit)]
    [InlineData("employee_qa", ShorfahSectionType.EmployeeQa)]
    public void Parse_EveryShorfahSectionTypeSourceValue_ParsesToExpectedMember(string source, ShorfahSectionType expected)
    {
        SnakeCaseEnumParser.Parse<ShorfahSectionType>(source).Should().Be(expected);
    }

    [Theory]
    [InlineData("pending_contribution", ShorfahWorkflowStatus.PendingContribution)]
    [InlineData("submitted", ShorfahWorkflowStatus.Submitted)]
    [InlineData("in_review", ShorfahWorkflowStatus.InReview)]
    [InlineData("approved", ShorfahWorkflowStatus.Approved)]
    [InlineData("rejected", ShorfahWorkflowStatus.Rejected)]
    public void Parse_EveryShorfahWorkflowStatusSourceValue_ParsesToExpectedMember(string source, ShorfahWorkflowStatus expected)
    {
        SnakeCaseEnumParser.Parse<ShorfahWorkflowStatus>(source).Should().Be(expected);
    }

    [Fact]
    public void Parse_UppercaseSourceValue_StillParses()
    {
        SnakeCaseEnumParser.Parse<ShorfahWorkflowStatus>("IN_REVIEW").Should().Be(ShorfahWorkflowStatus.InReview);
    }

    [Fact]
    public void Parse_MixedCaseSourceValue_StillParses()
    {
        SnakeCaseEnumParser.Parse<ShorfahSectionType>("Global_News").Should().Be(ShorfahSectionType.GlobalNews);
    }

    [Fact]
    public void Parse_UnknownValue_ThrowsArgumentException()
    {
        Action act = () => SnakeCaseEnumParser.Parse<ShorfahWorkflowStatus>("archived");

        act.Should().Throw<ArgumentException>().WithParameterName("snakeCaseValue");
    }

    [Fact]
    public void Parse_EmptyString_ThrowsArgumentException()
    {
        Action act = () => SnakeCaseEnumParser.Parse<ShorfahWorkflowStatus>(string.Empty);

        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void Parse_TrailingUnderscore_IsIgnoredByRemoveEmptyEntries()
    {
        SnakeCaseEnumParser.Parse<ShorfahSectionType>("news_").Should().Be(ShorfahSectionType.News);
    }

    [Fact]
    public void Parse_LeadingUnderscore_IsIgnoredByRemoveEmptyEntries()
    {
        SnakeCaseEnumParser.Parse<ShorfahSectionType>("_news").Should().Be(ShorfahSectionType.News);
    }

    [Fact]
    public void Parse_DoubleUnderscore_CollapsesEmptySegments()
    {
        SnakeCaseEnumParser.Parse<ShorfahSectionType>("global__news").Should().Be(ShorfahSectionType.GlobalNews);
    }

    [Theory]
    [InlineData("global_news", "GlobalNews")]
    [InlineData("intl_participation", "IntlParticipation")]
    [InlineData("employee_qa", "EmployeeQa")]
    [InlineData("pending_contribution", "PendingContribution")]
    [InlineData("events", "Events")]
    [InlineData("", "")]
    public void ToPascalCase_ConvertsAsExpected(string source, string expected)
    {
        SnakeCaseEnumParser.ToPascalCase(source).Should().Be(expected);
    }
}
