using FluentAssertions;
using Icbank.Platform.Application.Shorfah;
using Icbank.Platform.Domain.Shorfah;
using Xunit;

namespace Icbank.Platform.UnitTests.Application;

/// <summary>Proves the canonical section list matches BUSINESS-RULES.md §1.2 exactly: count, order, and the fixed first/last entries.</summary>
public sealed class ShorfahCanonicalSectionsTests
{
    [Fact]
    public void Templates_HasExactlyThirteenEntries()
    {
        ShorfahCanonicalSections.Templates.Should().HaveCount(13);
    }

    [Fact]
    public void Templates_FirstEntryIsGlobalNewsAtOrderTen()
    {
        ShorfahCanonicalSectionTemplate first = ShorfahCanonicalSections.Templates[0];
        first.SectionType.Should().Be(ShorfahSectionType.GlobalNews);
        first.DisplayOrder.Should().Be(10);
        first.TitleAr.Should().Be("أخبار دولية");
    }

    [Fact]
    public void Templates_LastEntryIsEmployeeQaAtOrderOneThirty()
    {
        ShorfahCanonicalSectionTemplate last = ShorfahCanonicalSections.Templates[^1];
        last.SectionType.Should().Be(ShorfahSectionType.EmployeeQa);
        last.DisplayOrder.Should().Be(130);
    }

    [Fact]
    public void Templates_DisplayOrdersAreStrictlyIncreasingByTen()
    {
        var orders = ShorfahCanonicalSections.Templates.Select(t => t.DisplayOrder).ToList();
        orders.Should().BeInAscendingOrder();
        orders.Should().OnlyHaveUniqueItems();
    }

    [Fact]
    public void Templates_EverySectionTypeIsDistinct()
    {
        ShorfahCanonicalSections.Templates.Select(t => t.SectionType).Should().OnlyHaveUniqueItems();
    }
}
