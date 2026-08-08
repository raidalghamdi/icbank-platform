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
    private static readonly string[] TwoSizes = { "uhd-4k", "web-mini" };
    private static readonly string[] UnknownSize = { "not-a-size" };
    private static readonly string[] DuplicatedSize = { "web-standard", "web-standard" };
    private static readonly string[] SupportingIcons = { "calendar", "clock" };

    private readonly IIconEventHtmlRenderer _htmlRenderer = Substitute.For<IIconEventHtmlRenderer>();
    private readonly GenerateIconEventStudioCommandHandler _handler;

    public GenerateIconEventStudioCommandHandlerTests()
    {
        _htmlRenderer.Render(Arg.Any<IconEventInput>()).Returns("<html></html>");
        _handler = new GenerateIconEventStudioCommandHandler(_htmlRenderer);
    }

    [Fact]
    public async Task Handle_NoSizesRequested_DefaultsToThePreviewPreset()
    {
        Result<GenerateIconEventStudioResultDto> result = await _handler.Handle(Build(null), CancellationToken.None);

        result.Value!.Variants.Should().ContainSingle();
        result.Value.Variants[0].Size.Should().Be("desktop-hd");
    }

    [Fact]
    public async Task Handle_MultipleSizesRequested_RendersEachWithCorrectDimensions()
    {
        Result<GenerateIconEventStudioResultDto> result = await _handler.Handle(Build(TwoSizes), CancellationToken.None);

        result.Value!.Variants.Should().HaveCount(2);
        result.Value.Variants.Should().Contain(v => v.Size == "uhd-4k" && v.Width == 3840 && v.Height == 2160);
        result.Value.Variants.Should().Contain(v => v.Size == "web-mini" && v.Width == 639 && v.Height == 479);
    }

    [Fact]
    public async Task Handle_UnknownSizeInRequestedList_IsFilteredOutAndFallsBackToThePreviewPreset()
    {
        Result<GenerateIconEventStudioResultDto> result = await _handler.Handle(Build(UnknownSize), CancellationToken.None);

        result.Value!.Variants.Should().ContainSingle(v => v.Size == "desktop-hd");
    }

    [Fact]
    public async Task Handle_SameSizeRequestedTwice_RendersItOnce()
    {
        Result<GenerateIconEventStudioResultDto> result = await _handler.Handle(Build(DuplicatedSize), CancellationToken.None);

        result.Value!.Variants.Should().ContainSingle(v => v.Size == "web-standard");
    }

    [Fact]
    public async Task Handle_ChosenVariantContent_IsPassedThroughToEverySize()
    {
        await _handler.Handle(Build(TwoSizes), CancellationToken.None);

        _htmlRenderer.Received(2).Render(Arg.Is<IconEventInput>(input =>
            input.Headline == "ملتقى الامتثال"
            && input.Layout == IconEventLayoutType.StatsHero
            && input.Stats.Count == 1
            && input.SupportingIcons.Count == 2
            && input.ContactEmail == "info@gac.gov.sa"));
    }

    [Fact]
    public async Task Handle_BlankMainIcon_LeavesTheChoiceToTheContentPlanner()
    {
        IconEventStudioContentDto content = Content() with { MainIcon = "  " };

        await _handler.Handle(new GenerateIconEventStudioCommand(content, null), CancellationToken.None);

        // Substituting a placeholder here would read downstream as a deliberate choice and stop the
        // planner from picking a glyph that actually matches the copy.
        _htmlRenderer.Received(1).Render(Arg.Is<IconEventInput>(input => input.MainIcon.Length == 0));
    }

    private static IconEventStudioContentDto Content() => new(
        "  ملتقى الامتثال  ",
        "النسخة الثانية",
        "إدارة الاتصال",
        "#هيئة_المنافسة",
        "info@gac.gov.sa",
        "920000000",
        "2026-08-10",
        "10:00",
        "الرياض",
        "users",
        SupportingIcons,
        new[] { new IconEventStatDto("users", "135+", "مشارك") },
        "stats-hero",
        null);

    private static GenerateIconEventStudioCommand Build(IReadOnlyList<string>? sizes) => new(Content(), sizes);
}
