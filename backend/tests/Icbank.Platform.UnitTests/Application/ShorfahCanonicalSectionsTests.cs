using FluentAssertions;
using Icbank.Platform.Application.Shorfah;
using Icbank.Platform.Domain.Shorfah;
using Xunit;

namespace Icbank.Platform.UnitTests.Application;

/// <summary>
/// Proves the canonical paragraph list matches the restructured table of contents exactly: the 18
/// paragraphs, their order, their verbatim Arabic titles and definitions, and the icon key each
/// one ships to the browser.
/// </summary>
public sealed class ShorfahCanonicalSectionsTests
{
    /// <summary>Gets the expected catalogue, in order: type, title, display order, icon key.</summary>
    public static TheoryData<int, ShorfahSectionType, string, int, string> ExpectedParagraphs => new()
    {
        { 0, ShorfahSectionType.News, "أخبار المنافسة", 10, "newspaper" },
        { 1, ShorfahSectionType.IntlParticipation, "مشاركاتنا الدولية", 20, "globe" },
        { 2, ShorfahSectionType.OurComms, "تواصلنا", 30, "handshake" },
        { 3, ShorfahSectionType.EconomicObservatory, "المرصد الاقتصادي", 40, "chart" },
        { 4, ShorfahSectionType.EconomicStudy, "دراسة اقتصادية", 50, "research" },
        { 5, ShorfahSectionType.CaseOfMonth, "قضية الشهر", 60, "folder" },
        { 6, ShorfahSectionType.SystemIndex, "مؤشر النظام", 70, "gauge" },
        { 7, ShorfahSectionType.CourtSessions, "جلسات قضائية", 80, "gavel" },
        { 8, ShorfahSectionType.MonopolyComplaints, "شكاوى وممارسات احتكارية", 90, "alert" },
        { 9, ShorfahSectionType.Settlements, "تسويات", 100, "check-circle" },
        { 10, ShorfahSectionType.LegalWindow, "نافذة قانونية", 110, "scale" },
        { 11, ShorfahSectionType.OfficeInterview, "حوار شهري", 120, "mic" },
        { 12, ShorfahSectionType.CompetitionCulture, "ثقافة المنافسة", 130, "lightbulb" },
        { 13, ShorfahSectionType.CompetitionInMonth, "المنافسة في شهر", 140, "calendar" },
        { 14, ShorfahSectionType.OutsideBox, "خارج الصندوق", 150, "sparkles" },
        { 15, ShorfahSectionType.Events, "فعالياتنا", 160, "ticket" },
        { 16, ShorfahSectionType.AgencyLit, "نشرتنا الهيئة", 170, "user-plus" },
        { 17, ShorfahSectionType.EmployeeQa, "أعطنا علومك", 180, "quote" },
    };

    [Fact]
    public void Templates_HasExactlyEighteenParagraphs()
    {
        ShorfahCanonicalSections.Templates.Should().HaveCount(18);
    }

    [Theory]
    [MemberData(nameof(ExpectedParagraphs))]
    public void Templates_AtPosition_CarriesTheAgreedTypeTitleOrderAndIcon(
        int index,
        ShorfahSectionType sectionType,
        string titleAr,
        int displayOrder,
        string iconKey)
    {
        ShorfahCanonicalSectionTemplate template = ShorfahCanonicalSections.Templates[index];

        template.SectionType.Should().Be(sectionType);
        template.TitleAr.Should().Be(titleAr);
        template.DisplayOrder.Should().Be(displayOrder);
        template.IconKey.Should().Be(iconKey);
    }

    [Fact]
    public void Templates_NewsParagraph_CarriesItsDefinitionVerbatim()
    {
        ShorfahCanonicalSections.Templates[0].DescriptionAr.Should().Be(
            "أبرز أخبار ومستجدات المنافسة خلال الشهر، وتشمل آخر التطورات والأخبار المتعلقة بالمنافسة بشكل عام، مثل القضايا الجديدة وأبرز المستجدات ذات العلاقة.");
    }

    [Fact]
    public void Templates_EmployeeQaParagraph_CarriesItsDefinitionVerbatim()
    {
        ShorfahCanonicalSections.Templates[^1].DescriptionAr.Should().Be(
            "مجموعة من الأسئلة الخفيفة والتعريفية التي تساعد الموظفين على التعرف على الموظف بشكل أفضل، من خلال التعرف على شخصيته واهتماماته وتجربته.");
    }

    [Fact]
    public void Templates_GlobalNews_IsNoLongerSeeded()
    {
        ShorfahCanonicalSections.Templates.Should().NotContain(t => t.SectionType == ShorfahSectionType.GlobalNews);
    }

    [Fact]
    public void Templates_DisplayOrdersAreStrictlyIncreasingByTen()
    {
        var orders = ShorfahCanonicalSections.Templates.Select(t => t.DisplayOrder).ToList();
        orders.Should().BeInAscendingOrder();
        orders.Should().OnlyHaveUniqueItems();
        orders.Should().AllSatisfy(order => (order % 10).Should().Be(0));
    }

    [Fact]
    public void Templates_EverySectionTypeIsDistinct()
    {
        ShorfahCanonicalSections.Templates.Select(t => t.SectionType).Should().OnlyHaveUniqueItems();
    }

    [Fact]
    public void Templates_EveryIconKeyIsLowercaseKebabCase()
    {
        ShorfahCanonicalSections.Templates.Select(t => t.IconKey).Should()
            .AllSatisfy(key => key.Should().MatchRegex("^[a-z]+(-[a-z]+)*$"));
    }

    [Fact]
    public void IconKeyFor_CatalogueType_ReturnsTheCataloguedKey()
    {
        ShorfahCanonicalSections.IconKeyFor(ShorfahSectionType.Settlements).Should().Be("check-circle");
    }

    [Fact]
    public void IconKeyFor_DroppedLegacyType_FallsBackToItsLegacyKey()
    {
        ShorfahCanonicalSections.IconKeyFor(ShorfahSectionType.GlobalNews).Should().Be("globe");
    }

    [Fact]
    public void IconKeyFor_UnknownType_FallsBackToTheDefaultKey()
    {
        ShorfahCanonicalSections.IconKeyFor((ShorfahSectionType)999).Should().Be(ShorfahCanonicalSections.FallbackIconKey);
    }
}
