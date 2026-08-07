using FluentAssertions;
using Icbank.Platform.Application.Weekend;
using Xunit;

namespace Icbank.Platform.UnitTests.Application;

/// <summary>Verifies the ported style-profile recomputation rule (BUSINESS-RULES.md §2.5), including the exact quote-usage thresholds.</summary>
public sealed class StyleProfileRecalculatorTests
{
    [Fact]
    public void Recompute_EmptyArchive_ReturnsNull()
    {
        StyleProfileComputation? result = StyleProfileRecalculator.Recompute(Array.Empty<string>());

        result.Should().BeNull();
    }

    [Fact]
    public void Recompute_NoQuoteTriggerWords_ClassifiesAsLimited()
    {
        var entries = new[] { "مرحبا بكم في بداية أسبوع جديد ملهم للجميع." };

        StyleProfileComputation? result = StyleProfileRecalculator.Recompute(entries);

        result.Should().NotBeNull();
        result!.QuoteUsage.Should().Be("محدود");
    }

    [Fact]
    public void Recompute_QuoteCountExceedsTwiceEntryCount_ClassifiesAsDense()
    {
        // 1 entry; quoteCount must exceed entries.length * 2 = 2, so 3+ trigger-word hits.
        var entries = new[] { "قال تعالى وقال الحديث وقال آية وقال قرآن" };

        StyleProfileComputation? result = StyleProfileRecalculator.Recompute(entries);

        result.Should().NotBeNull();
        result!.QuoteUsage.Should().Be("كثيف");
    }

    [Fact]
    public void Recompute_KeywordsExcludeStopwordsAndShortWords()
    {
        var entries = new[] { "في هذا الأسبوع نحتفل بالإنجازات الكبيرة والتطور المستمر" };

        StyleProfileComputation? result = StyleProfileRecalculator.Recompute(entries);

        result.Should().NotBeNull();
        result!.RecurringKeywords.Should().NotContain("في", "stopwords must be excluded from recurring keywords");
    }
}
