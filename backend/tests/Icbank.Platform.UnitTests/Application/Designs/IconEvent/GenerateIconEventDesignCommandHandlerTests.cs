using FluentAssertions;
using Icbank.Platform.Application.Common.Interfaces;
using Icbank.Platform.Application.Common.Models;
using Icbank.Platform.Application.Designs;
using Icbank.Platform.Application.Designs.IconEvent;
using Icbank.Platform.Application.Designs.IconEvent.Commands;
using Icbank.Platform.Domain.Designs;
using NSubstitute;
using Xunit;

namespace Icbank.Platform.UnitTests.Application.Designs.IconEvent;

/// <summary>
/// Verifies <see cref="GenerateIconEventDesignCommandHandler"/> applies the anti-hallucination
/// post-processing rules (BUSINESS-RULES.md §7.4) and the rate-limit/fallback/audit contract.
/// </summary>
public sealed class GenerateIconEventDesignCommandHandlerTests
{
    private const int ActorUserId = 42;

    private static readonly string[] StatsHeroSplitGridLayouts = { "stats-hero", "split", "grid" };
    private static readonly string[] AllStatsHeroLayouts = { "stats-hero", "stats-hero", "stats-hero" };
    private static readonly string[] StatsHeroHeroGridLayouts = { "stats-hero", "hero", "grid" };
    private static readonly string[] DefaultLayouts = { "hero", "split", "typography" };
    private static readonly string[] SupportingIconStub = { "calendar" };

    private readonly IIconEventDesignExtractor _extractor = Substitute.For<IIconEventDesignExtractor>();
    private readonly IIconEventHtmlRenderer _htmlRenderer = Substitute.For<IIconEventHtmlRenderer>();
    private readonly IDesignGenerationRateLimiter _rateLimiter = Substitute.For<IDesignGenerationRateLimiter>();
    private readonly IAuditLogService _auditLogService = Substitute.For<IAuditLogService>();
    private readonly GenerateIconEventDesignCommandHandler _handler;

    public GenerateIconEventDesignCommandHandlerTests()
    {
        _rateLimiter.TryConsume(Arg.Any<int>()).Returns(true);
        _htmlRenderer.Render(Arg.Any<IconEventInput>()).Returns(ci => $"<html>{ci.Arg<IconEventInput>().Headline}</html>");
        _handler = new GenerateIconEventDesignCommandHandler(_extractor, _htmlRenderer, _rateLimiter, _auditLogService);
    }

    [Fact]
    public async Task Handle_RateLimitExceeded_ReturnsFailureAndNeverCallsExtractor()
    {
        _rateLimiter.TryConsume(ActorUserId).Returns(false);

        Result<GenerateIconEventDesignResultDto> result = await _handler.Handle(BuildCommand(rawData: "ورشة عمل عن الابتكار"), CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        await _extractor.DidNotReceive().ExtractAsync(Arg.Any<string>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_NoNumbersInInput_ForcesStatsEmptyRegardlessOfAiOutput()
    {
        IconEventExtractionResultDto extraction = BuildExtraction(stats: new[] { new IconEventStatDto("users", "135+", "موظف") });
        _extractor.ExtractAsync(Arg.Any<string>(), Arg.Any<CancellationToken>()).Returns(extraction);

        Result<GenerateIconEventDesignResultDto> result = await _handler.Handle(
            BuildCommand(rawData: "مبروك للفريق على الإنجاز الرائع"), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value!.Variants.Should().OnlyContain(v => v.Stats.Count == 0);
    }

    [Fact]
    public async Task Handle_NumbersPresentInInput_PreservesAiStatsUpToThree()
    {
        IconEventExtractionResultDto extraction = BuildExtraction(
            stats: new[] { new IconEventStatDto("users", "20", "إدارة"), new IconEventStatDto("building", "135", "موظف") },
            layouts: StatsHeroSplitGridLayouts);
        _extractor.ExtractAsync(Arg.Any<string>(), Arg.Any<CancellationToken>()).Returns(extraction);

        Result<GenerateIconEventDesignResultDto> result = await _handler.Handle(
            BuildCommand(rawData: "ورشة استراتيجية — 20 إدارة، 135 موظف"), CancellationToken.None);

        result.Value!.Variants.First(v => v.Layout == "stats-hero").Stats.Should().HaveCount(2);
    }

    [Fact]
    public async Task Handle_AllThreeLayoutsIdentical_ForcesDiversityFallbackTriplet()
    {
        IconEventExtractionResultDto extraction = BuildExtraction(layouts: AllStatsHeroLayouts);
        _extractor.ExtractAsync(Arg.Any<string>(), Arg.Any<CancellationToken>()).Returns(extraction);

        Result<GenerateIconEventDesignResultDto> result = await _handler.Handle(
            BuildCommand(rawData: "20 إدارة، 135 موظف، 10 جلسات"), CancellationToken.None);

        var layouts = result.Value!.Variants.Select(v => v.Layout).ToList();
        layouts.Should().OnlyHaveUniqueItems();
        layouts.Should().Contain("typography");
    }

    [Fact]
    public async Task Handle_NoTypographyProposed_ForcesThirdSlotToTypography()
    {
        IconEventExtractionResultDto extraction = BuildExtraction(layouts: StatsHeroHeroGridLayouts);
        _extractor.ExtractAsync(Arg.Any<string>(), Arg.Any<CancellationToken>()).Returns(extraction);

        Result<GenerateIconEventDesignResultDto> result = await _handler.Handle(
            BuildCommand(rawData: "20 إدارة، 135 موظف"), CancellationToken.None);

        result.Value!.Variants[2].Layout.Should().Be("typography");
    }

    [Fact]
    public async Task Handle_MainIconOverrideValid_TakesPrecedenceOverAiSelection()
    {
        IconEventExtractionResultDto extraction = BuildExtraction();
        _extractor.ExtractAsync(Arg.Any<string>(), Arg.Any<CancellationToken>()).Returns(extraction);

        Result<GenerateIconEventDesignResultDto> result = await _handler.Handle(
            BuildCommand(rawData: "إعلان مهم", mainIconOverride: "shield"), CancellationToken.None);

        result.Value!.Variants.Should().OnlyContain(v => v.MainIcon == "shield");
    }

    [Fact]
    public async Task Handle_ColorSchemeAlwaysTeal_RegardlessOfAiOutput()
    {
        IconEventExtractionResultDto extraction = BuildExtraction();
        _extractor.ExtractAsync(Arg.Any<string>(), Arg.Any<CancellationToken>()).Returns(extraction);

        Result<GenerateIconEventDesignResultDto> result = await _handler.Handle(BuildCommand(rawData: "إعلان مهم"), CancellationToken.None);

        result.Value!.Variants.Should().OnlyContain(v => v.ColorScheme == "teal");
    }

    [Fact]
    public async Task Handle_ExtractorThrows_ReturnsDeterministicLocalFallback()
    {
        _extractor.ExtractAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromException<IconEventExtractionResultDto>(new InvalidOperationException("AI down")));

        Result<GenerateIconEventDesignResultDto> result = await _handler.Handle(
            BuildCommand(rawData: "خبر عاجل: تعليق العمل غداً", eventType: "workshop"), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value!.Warning.Should().NotBeNull();
        result.Value.Variants.Should().HaveCount(3);
        result.Value.Variants.Should().OnlyContain(v => v.MainIcon == "graduation-cap");
    }

    [Fact]
    public async Task Handle_Always_WritesAuditEntry()
    {
        _extractor.ExtractAsync(Arg.Any<string>(), Arg.Any<CancellationToken>()).Returns(BuildExtraction());

        await _handler.Handle(BuildCommand(rawData: "إعلان مهم"), CancellationToken.None);

        await _auditLogService.Received(1).RecordAsync(
            ActorUserId, "design.icon_event.generate", "IconEventDesign", Arg.Any<string>(), Arg.Any<object?>(), Arg.Any<object?>(), Arg.Any<CancellationToken>());
    }

    private static GenerateIconEventDesignCommand BuildCommand(string? rawData = null, string? mainIconOverride = null, string? eventType = null) =>
        new(ActorUserId, rawData, Headline: null, Subtitle: null, Department: null, Hashtag: null, Date: null, Time: null, Location: null, eventType, "landscape", mainIconOverride);

    private static IconEventExtractionResultDto BuildExtraction(IReadOnlyList<IconEventStatDto>? stats = null, IReadOnlyList<string>? layouts = null)
    {
        var extracted = new IconEventExtractedDataDto("عنوان الفعالية", "وصف", string.Empty, string.Empty, string.Empty, string.Empty, stats ?? Array.Empty<IconEventStatDto>());
        IReadOnlyList<string> resolvedLayouts = layouts ?? DefaultLayouts;
        var variants = resolvedLayouts.Select(l => new IconEventVariantProposalDto(l, "sparkles", SupportingIconStub, "سبب")).ToList();
        return new IconEventExtractionResultDto(extracted, variants);
    }
}
