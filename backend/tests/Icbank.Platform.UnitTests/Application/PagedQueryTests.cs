using FluentAssertions;
using Icbank.Platform.Application.Common.Models;
using Xunit;

namespace Icbank.Platform.UnitTests.Application;

/// <summary>Verifies the pagination envelope clamps <see cref="PagedQuery.PageSize"/> as mandated by R-BE-033.</summary>
public sealed class PagedQueryTests
{
    [Fact]
    public void PageSize_WhenRequestedSizeExceedsMax_ClampsToMaxPageSize()
    {
        PagedQuery query = new() { PageSize = 500 };

        query.PageSize.Should().Be(PagedQuery.MaxPageSize);
    }

    [Fact]
    public void PageSize_WhenRequestedSizeIsZeroOrNegative_ClampsToOne()
    {
        PagedQuery query = new() { PageSize = -10 };

        query.PageSize.Should().Be(1);
    }

    [Fact]
    public void PageSize_WhenNotSpecified_DefaultsToDefaultPageSize()
    {
        PagedQuery query = new();

        query.PageSize.Should().Be(PagedQuery.DefaultPageSize);
    }

    [Fact]
    public void PageSize_WhenWithinRange_IsUnchanged()
    {
        PagedQuery query = new() { PageSize = 50 };

        query.PageSize.Should().Be(50);
    }
}
