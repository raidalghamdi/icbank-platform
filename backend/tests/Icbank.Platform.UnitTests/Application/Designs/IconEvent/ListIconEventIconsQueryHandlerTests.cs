using FluentAssertions;
using Icbank.Platform.Application.Common.Models;
using Icbank.Platform.Application.Designs.IconEvent.Queries;
using Xunit;

namespace Icbank.Platform.UnitTests.Application.Designs.IconEvent;

/// <summary>Verifies <see cref="ListIconEventIconsQueryHandler"/> returns the full catalogue with a matching count.</summary>
public sealed class ListIconEventIconsQueryHandlerTests
{
    private readonly ListIconEventIconsQueryHandler _handler = new();

    [Fact]
    public async Task Handle_Always_ReturnsFullCatalogueWithMatchingCount()
    {
        Result<IconEventIconCatalogDto> result = await _handler.Handle(new ListIconEventIconsQuery(), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value!.Icons.Should().NotBeEmpty();
        result.Value.Count.Should().Be(result.Value.Icons.Count);
        result.Value.Icons.Should().Contain(icon => icon.Name == "shield");
    }

    [Fact]
    public async Task Handle_Always_EveryIconHasNonEmptyCategory()
    {
        Result<IconEventIconCatalogDto> result = await _handler.Handle(new ListIconEventIconsQuery(), CancellationToken.None);

        result.Value!.Icons.Should().OnlyContain(icon => !string.IsNullOrWhiteSpace(icon.Category));
    }
}
