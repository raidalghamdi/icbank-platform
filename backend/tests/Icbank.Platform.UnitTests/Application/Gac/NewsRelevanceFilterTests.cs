using FluentAssertions;
using Icbank.Platform.Application.Gac.News;
using Xunit;

namespace Icbank.Platform.UnitTests.Application.Gac;

/// <summary>Tests for <see cref="NewsRelevanceFilter"/>.</summary>
public sealed class NewsRelevanceFilterTests
{
    [Theory]
    [InlineData("هيئة المنافسة تُصدر 34 قراراً بخصوص طلبات التركز الاقتصادي")]
    [InlineData("الهيئة العامة للمنافسة تعدل نظام العقوبات وتعزز أدوات الرقابة")]
    [InlineData("تعديلات نظام المنافسة.. عقوبات مشددة ومكافآت للمبلغين")]
    [InlineData("بريطانيا توافق على استحواذ باراماونت على وارنر براذرز")]
    [InlineData("Saudi competition authority clears the merger")]
    public void IsRelevant_CompetitionCoverage_IsKept(string title)
    {
        NewsRelevanceFilter.IsRelevant(title, null).Should().BeTrue();
    }

    [Theory]
    [InlineData("الهيئة العامة للنقل تعتمد اللائحة التنفيذية لنقل البضائع بالدراجات")]
    [InlineData("وظائف جدارات: طرح 5891 فرصة عمل جديدة للسعوديين")]
    [InlineData("حكام دوري روشن يواصلون برنامجهم الإعدادي في معسكر إسبانيا")]
    [InlineData("تعديلات نظام المنافسات والمشتريات الحكومية: رفع الحد الأعلى للشراء المباشر")]
    [InlineData("تخريج دورة تأهيلية لضباط ومجندات الأمن العام")]
    public void IsRelevant_UnrelatedCoverage_IsDropped(string title)
    {
        NewsRelevanceFilter.IsRelevant(title, null).Should().BeFalse();
    }

    [Fact]
    public void IsRelevant_AuthorityStoryMentioningAnExcludedWord_IsStillKept()
    {
        // "وظائف" alone is noise, but not when the article is about the authority itself.
        NewsRelevanceFilter.IsRelevant(
            "هيئة المنافسة تعلن وظائف جديدة",
            "أعلنت الهيئة العامة للمنافسة عن وظائف شاغرة.").Should().BeTrue();
    }

    [Fact]
    public void IsRelevant_MatchOnlyInTheBody_IsKept()
    {
        NewsRelevanceFilter.IsRelevant("تفاصيل الخبر", "قررت هيئة المنافسة الموافقة على الطلب.")
            .Should().BeTrue();
    }

    [Theory]
    [InlineData(null, null)]
    [InlineData("", "")]
    [InlineData("   ", null)]
    public void IsRelevant_EmptyContent_IsDropped(string? title, string? body)
    {
        NewsRelevanceFilter.IsRelevant(title, body).Should().BeFalse();
    }
}
