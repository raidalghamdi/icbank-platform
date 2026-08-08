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

    /// <summary>
    /// Regression: the Node source ranked the first line of <c>raw_data</c> above the extracted
    /// headline. Raw input is normally one newline-free paragraph, so that made the model's short
    /// headline unreachable in raw mode and rendered a whole sentence in the headline slot.
    /// </summary>
    [Fact]
    public async Task Handle_RawModeSingleParagraph_PrefersExtractedHeadlineOverWholeParagraph()
    {
        const string paragraph = "ورشة عمل عن الامتثال لأحكام نظام المنافسة بحضور 40 موظفاً من عدة إدارات، تستمر ثلاثة أيام";
        _extractor.ExtractAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(BuildExtraction(aiHeadline: "ورشة عمل: الامتثال لنظام المنافسة"));

        Result<GenerateIconEventDesignResultDto> result = await _handler.Handle(
            BuildCommand(rawData: paragraph), CancellationToken.None);

        result.Value!.Variants.Should().OnlyContain(v => v.Headline == "ورشة عمل: الامتثال لنظام المنافسة");
        result.Value.Variants.Should().OnlyContain(v => v.Headline != paragraph);
    }

    [Fact]
    public async Task Handle_ExplicitHeadlineSupplied_OutranksExtractedHeadline()
    {
        _extractor.ExtractAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(BuildExtraction(aiHeadline: "عنوان من النموذج"));

        Result<GenerateIconEventDesignResultDto> result = await _handler.Handle(
            BuildCommand(rawData: "نص خام طويل للاختبار", headline: "عنوان المستخدم"), CancellationToken.None);

        result.Value!.Variants.Should().OnlyContain(v => v.Headline == "عنوان المستخدم");
    }

    [Fact]
    public async Task Handle_ExtractedHeadlineBlank_FallsBackToTruncatedRawFirstLine()
    {
        var paragraph = new string('أ', 200);
        _extractor.ExtractAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(BuildExtraction(aiHeadline: "   "));

        Result<GenerateIconEventDesignResultDto> result = await _handler.Handle(
            BuildCommand(rawData: paragraph), CancellationToken.None);

        var headline = result.Value!.Variants[0].Headline;
        headline.Should().HaveLength(61).And.EndWith("…");
        headline.Should().NotBe(paragraph);
    }

    [Fact]
    public async Task Handle_Always_WritesAuditEntry()
    {
        _extractor.ExtractAsync(Arg.Any<string>(), Arg.Any<CancellationToken>()).Returns(BuildExtraction());

        await _handler.Handle(BuildCommand(rawData: "إعلان مهم"), CancellationToken.None);

        await _auditLogService.Received(1).RecordAsync(
            ActorUserId, "design.icon_event.generate", "IconEventDesign", Arg.Any<string>(), Arg.Any<object?>(), Arg.Any<object?>(), Arg.Any<CancellationToken>());
    }

    private static GenerateIconEventDesignCommand BuildCommand(
        string? rawData = null,
        string? mainIconOverride = null,
        string? eventType = null,
        string? headline = null) =>
        new(ActorUserId, rawData, headline, Subtitle: null, Department: null, Hashtag: null, Date: null, Time: null, Location: null, eventType, "desktop-hd", mainIconOverride);

    private static IconEventExtractionResultDto BuildExtraction(
        IReadOnlyList<IconEventStatDto>? stats = null,
        IReadOnlyList<string>? layouts = null,
        string aiHeadline = "عنوان الفعالية")
    {
        var extracted = new IconEventExtractedDataDto(aiHeadline, "وصف", string.Empty, string.Empty, string.Empty, string.Empty, stats ?? Array.Empty<IconEventStatDto>());
        IReadOnlyList<string> resolvedLayouts = layouts ?? DefaultLayouts;
        var variants = resolvedLayouts.Select(l => new IconEventVariantProposalDto(l, "sparkles", SupportingIconStub, "سبب")).ToList();
        return new IconEventExtractionResultDto(extracted, variants);
    }
}
