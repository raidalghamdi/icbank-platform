using FluentAssertions;
using Icbank.Platform.Application.Common.Models;
using Icbank.Platform.Application.Designs.IconEvent;
using Icbank.Platform.Application.Designs.IconEvent.Commands;
using Icbank.Platform.Domain.Designs;
using NSubstitute;
using Xunit;

namespace Icbank.Platform.UnitTests.Application.Designs.IconEvent;

/// <summary>Verifies <see cref="GenerateIconEventStudioCommandHandler"/> renders every requested size deterministically without calling AI.</summary>
public sealed class GenerateIconEventStudioCommandHandlerTests
{
    private static readonly string[] SquareAndStorySizes = { "square", "story" };
    private static readonly string[] UnknownSize = { "not-a-size" };

    private readonly IIconEventHtmlRenderer _htmlRenderer = Substitute.For<IIconEventHtmlRenderer>();
    private readonly GenerateIconEventStudioCommandHandler _handler;

    public GenerateIconEventStudioCommandHandlerTests()
    {
        _htmlRenderer.Render(Arg.Any<IconEventInput>()).Returns("<html></html>");
        _handler = new GenerateIconEventStudioCommandHandler(_htmlRenderer);
    }

    [Fact]
    public async Task Handle_NoSizesRequested_DefaultsToLandscape()
    {
        var command = new GenerateIconEventStudioCommand("عنوان", null, null, null, Sizes: null, Layout: null, LogoUrl: null);

        Result<GenerateIconEventStudioResultDto> result = await _handler.Handle(command, CancellationToken.None);

        result.Value!.Variants.Should().HaveCount(1);
        result.Value.Variants[0].Size.Should().Be("landscape");
    }

    [Fact]
    public async Task Handle_MultipleSizesRequested_RendersEachWithCorrectDimensions()
    {
        var command = new GenerateIconEventStudioCommand("عنوان", "وصف", "الإدارة", "star", SquareAndStorySizes, "hero", null);

        Result<GenerateIconEventStudioResultDto> result = await _handler.Handle(command, CancellationToken.None);

        result.Value!.Variants.Should().HaveCount(2);
        result.Value.Variants.Should().Contain(v => v.Size == "square" && v.Width == 1200 && v.Height == 1200);
        result.Value.Variants.Should().Contain(v => v.Size == "story" && v.Width == 1200 && v.Height == 2133);
    }

    [Fact]
    public async Task Handle_UnknownSizeInRequestedList_IsFilteredOutFallsBackToLandscape()
    {
        var command = new GenerateIconEventStudioCommand("عنوان", null, null, null, UnknownSize, null, null);

        Result<GenerateIconEventStudioResultDto> result = await _handler.Handle(command, CancellationToken.None);

        result.Value!.Variants.Should().ContainSingle(v => v.Size == "landscape");
    }
}
