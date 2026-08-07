using Icbank.Platform.Domain.Designs;

namespace Icbank.Platform.Application.Designs.IconEvent.Commands;

/// <summary>
/// Builds the final <see cref="GenerateIconEventDesignResultDto"/> from either the AI extraction
/// result or the deterministic local fallback, applying every code-enforced post-processing rule
/// from BUSINESS-RULES.md §7.4.
/// </summary>
public static class IconEventVariantAssembler
{
    private const int MaxStats = 3;
    private const int FallbackHeadlineLength = 60;
    private const string LogoUrl = "/brand-assets/logos/gac-white.png";

    private static readonly string[] FallbackSupportingIcons = { "calendar", "clock", "map-pin" };

    /// <summary>Builds the result from a successful AI extraction.</summary>
    /// <param name="extraction">The AI's typed extraction result.</param>
    /// <param name="request">The original command.</param>
    /// <param name="hasNumbersInInput">Whether the source text contains any digit.</param>
    /// <param name="size">The resolved size preset.</param>
    /// <param name="htmlRenderer">The HTML rendering port.</param>
    /// <returns>The assembled result.</returns>
    public static GenerateIconEventDesignResultDto BuildFromAi(
        IconEventExtractionResultDto extraction, GenerateIconEventDesignCommand request, bool hasNumbersInInput, IconEventSizePreset size, IIconEventHtmlRenderer htmlRenderer)
    {
        var rawFull = (request.RawData ?? string.Empty).Trim();
        var finalHeadline = ResolveHeadline(request.Headline, request.RawData, extraction.Extracted.Headline);
        var contactEmail = IconEventContactExtractor.ExtractEmail(rawFull, extraction.Extracted.ContactEmail);
        var contactPhone = IconEventContactExtractor.ExtractPhone(rawFull, extraction.Extracted.ContactPhone);
        var finalSubtitle = ResolveSubtitle(request.Subtitle, rawFull, finalHeadline, contactEmail, contactPhone, extraction.Extracted.Subtitle);
        List<IconEventStat> stats = BuildStats(extraction.Extracted.Stats, hasNumbersInInput);

        var context = new IconEventVariantContext
        {
            Headline = finalHeadline,
            Subtitle = finalSubtitle,
            Department = request.Department,
            Hashtag = request.Hashtag,
            Date = request.Date,
            Time = request.Time,
            Location = request.Location,
            Size = size,
            HtmlRenderer = htmlRenderer,
        };

        IReadOnlyList<IconEventLayoutType> layouts = IconEventLayoutNormalizer.Normalize(extraction.Variants.Select(v => v.Layout).ToList(), hasNumbersInInput);
        var variants = new List<IconEventVariantDto>();
        for (var i = 0; i < layouts.Count; i++)
        {
            variants.Add(BuildVariant(i, layouts[i], extraction.Variants[i], request.MainIconOverride, stats, context));
        }

        return new GenerateIconEventDesignResultDto(variants, variants.Count, extraction.Extracted, Warning: null);
    }

    /// <summary>Builds the deterministic local fallback used when the AI call fails entirely (BUSINESS-RULES.md §7.4 rule 7).</summary>
    /// <param name="request">The original command.</param>
    /// <param name="size">The resolved size preset.</param>
    /// <param name="htmlRenderer">The HTML rendering port.</param>
    /// <returns>The assembled fallback result.</returns>
    public static GenerateIconEventDesignResultDto BuildFallback(GenerateIconEventDesignCommand request, IconEventSizePreset size, IIconEventHtmlRenderer htmlRenderer)
    {
        var fallbackIcon = ResolveFallbackIcon(request.MainIconOverride, request.EventType);
        var fallbackHeadline = request.Headline ?? (request.RawData is { Length: > 0 }
            ? request.RawData.Split('\n')[0][..Math.Min(FallbackHeadlineLength, request.RawData.Split('\n')[0].Length)]
            : "فعالية");
        var fallbackStats = new List<IconEventStat> { new("users", "—", "مشاركة"), new("building", "—", "إدارة"), new("calendar", "—", "فعالية") };
        IconEventLayoutType[] layouts = new[] { IconEventLayoutType.StatsHero, IconEventLayoutType.Hero, IconEventLayoutType.Split };

        var context = new IconEventVariantContext
        {
            Headline = fallbackHeadline,
            Subtitle = request.Subtitle,
            Department = request.Department,
            Hashtag = request.Hashtag,
            Date = request.Date,
            Time = request.Time,
            Location = request.Location,
            Size = size,
            HtmlRenderer = htmlRenderer,
        };

        var variants = layouts.Select((layout, i) => BuildDto(
            i,
            layout,
            fallbackIcon,
            FallbackSupportingIcons.ToList(),
            fallbackStats,
            context,
            "تنويعة افتراضية (تعذّر الاتصال بـ AI)")).ToList();

        var extracted = new IconEventExtractedDataDto(fallbackHeadline, request.Subtitle ?? string.Empty, request.Department ?? string.Empty, request.Hashtag ?? string.Empty, string.Empty, string.Empty, Array.Empty<IconEventStatDto>());
        return new GenerateIconEventDesignResultDto(variants, variants.Count, extracted, "تم استخدام التنويعات الافتراضية لتعذّر الاتصال بـ AI — يمكنك إعادة المحاولة");
    }

    /// <summary>Chooses the headline to render, preferring the user's own wording, then the model's.</summary>
    /// <param name="explicitHeadline">The headline the user typed in structured mode, if any.</param>
    /// <param name="rawData">The raw free-text input supplied in raw mode.</param>
    /// <param name="aiHeadline">The short headline the extractor produced.</param>
    /// <returns>The headline to render on every variant.</returns>
    /// <remarks>
    /// Why this diverges from the Node source: <c>routes/icon-event-designs.ts</c> ordered this
    /// <c>headline || rawFirstLine || extracted.headline</c> and labelled it "Priority: (1) explicit
    /// headline from user, (2) first non-empty line of raw_data, (3) AI extracted". Raw input is
    /// almost always a single paragraph with no newline, so "first non-empty line" evaluated to the
    /// entire paragraph and the extracted headline was unreachable in raw mode -- the one mode whose
    /// whole purpose is extraction. The prompt asks the model for <c>عنوان 2-6 كلمات</c>
    /// (IconEventExtractionPrompts), so honouring the raw paragraph instead rendered a full sentence
    /// in the headline slot of a 1080x1080 canvas and duplicated it into the subtitle.
    /// The AI headline now outranks the raw text, which is only used as a truncated fallback for when
    /// extraction returns nothing (or the local template extractor is active).
    /// </remarks>
    private static string ResolveHeadline(string? explicitHeadline, string? rawData, string aiHeadline)
    {
        if (!string.IsNullOrWhiteSpace(explicitHeadline))
        {
            return explicitHeadline.Trim();
        }

        if (!string.IsNullOrWhiteSpace(aiHeadline))
        {
            return aiHeadline.Trim();
        }

        var firstLine = (rawData ?? string.Empty).Split('\n').Select(l => l.Trim()).FirstOrDefault(l => l.Length > 0);
        if (!string.IsNullOrEmpty(firstLine))
        {
            return firstLine.Length > FallbackHeadlineLength
                ? firstLine[..FallbackHeadlineLength].TrimEnd() + "…"
                : firstLine;
        }

        return "عنوان الفعالية";
    }

    private static string ResolveSubtitle(string? explicitSubtitle, string rawFull, string finalHeadline, string contactEmail, string contactPhone, string aiSubtitle)
    {
        if (!string.IsNullOrWhiteSpace(explicitSubtitle))
        {
            return explicitSubtitle.Trim();
        }

        if (rawFull.Length == 0)
        {
            return aiSubtitle.Trim();
        }

        var formatted = IconEventSubtitleFormatter.Build(rawFull, finalHeadline, contactEmail, contactPhone);
        return string.IsNullOrEmpty(formatted) ? aiSubtitle.Trim() : formatted;
    }

    private static List<IconEventStat> BuildStats(IReadOnlyList<IconEventStatDto> aiStats, bool hasNumbersInInput)
    {
        if (!hasNumbersInInput)
        {
            return new List<IconEventStat>();
        }

        return aiStats
            .Where(s => !string.IsNullOrWhiteSpace(s.Value) && s.Value.Trim() != "—")
            .Take(MaxStats)
            .Select(s => new IconEventStat(IconLibrary.ValidNames.Contains(s.Icon) ? s.Icon : "sparkles", s.Value.Trim(), (s.Label ?? string.Empty).Trim()))
            .ToList();
    }

    private static IconEventVariantDto BuildVariant(
        int index,
        IconEventLayoutType layout,
        IconEventVariantProposalDto proposal,
        string? mainIconOverride,
        List<IconEventStat> stats,
        IconEventVariantContext context)
    {
        var mainIcon = ResolveMainIcon(mainIconOverride, proposal.MainIcon);
        var supporting = proposal.SupportingIcons.Where(IconLibrary.ValidNames.Contains).Take(3).ToList();
        var layoutUsesStats = layout is IconEventLayoutType.StatsHero or IconEventLayoutType.Grid;
        List<IconEventStat> variantStats = layoutUsesStats ? stats : new List<IconEventStat>();

        return BuildDto(index, layout, mainIcon, supporting, variantStats, context, proposal.Rationale);
    }

    private static IconEventVariantDto BuildDto(
        int index,
        IconEventLayoutType layout,
        string mainIcon,
        List<string> supportingIcons,
        List<IconEventStat> stats,
        IconEventVariantContext context,
        string rationale)
    {
        var input = new IconEventInput
        {
            Headline = context.Headline,
            Subtitle = context.Subtitle,
            Department = context.Department,
            Hashtag = context.Hashtag,
            Date = context.Date,
            Time = context.Time,
            Location = context.Location,
            MainIcon = mainIcon,
            SupportingIcons = supportingIcons,
            Stats = stats,
            Layout = layout,
            Size = context.Size,
            LogoUrl = LogoUrl,
        };

        return new IconEventVariantDto(
            $"variant-{index + 1}",
            IconEventLayoutNormalizer.ToKey(layout),
            mainIcon,
            supportingIcons,
            "teal",
            context.Headline,
            context.Subtitle ?? string.Empty,
            context.Department ?? string.Empty,
            context.Hashtag ?? string.Empty,
            stats.Select(s => new IconEventStatDto(s.Icon, s.Value, s.Label)).ToList(),
            rationale,
            context.HtmlRenderer.Render(input));
    }

    private static string ResolveMainIcon(string? mainIconOverride, string aiMainIcon)
    {
        if (!string.IsNullOrEmpty(mainIconOverride) && IconLibrary.ValidNames.Contains(mainIconOverride))
        {
            return mainIconOverride;
        }

        return IconLibrary.ValidNames.Contains(aiMainIcon) ? aiMainIcon : "sparkles";
    }

    private static string ResolveFallbackIcon(string? mainIconOverride, string? eventType)
    {
        if (!string.IsNullOrEmpty(mainIconOverride) && IconLibrary.ValidNames.Contains(mainIconOverride))
        {
            return mainIconOverride;
        }

        return eventType switch
        {
            "workshop" => "graduation-cap",
            "meeting" => "users",
            "launch" => "rocket",
            "social" => "party-popper",
            _ => "sparkles",
        };
    }
}
