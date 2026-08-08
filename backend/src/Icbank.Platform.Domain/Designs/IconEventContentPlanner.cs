namespace Icbank.Platform.Domain.Designs;

/// <summary>Turns a resolved input into the copy one specific canvas will actually render.</summary>
public static class IconEventContentPlanner
{
    private const int MaxSectionBullets = 3;

    /// <summary>Plans the copy for a poster.</summary>
    /// <param name="input">The resolved input, after extraction.</param>
    /// <returns>The budgeted plan for <see cref="IconEventInput.Size"/>.</returns>
    public static IconEventContentPlan Plan(IconEventInput input)
    {
        ArgumentNullException.ThrowIfNull(input);
        var budget = IconEventContentBudget.Resolve(input.Size);
        IconEventTextStructure structure = IconEventTextStructureParser.Parse(input.Subtitle);

        IReadOnlyList<string> bulletTexts = CollectBullets(structure, budget);
        var mainIcon = IconEventIconResolver.Resolve(input.MainIcon, IconTopic(input, structure));
        var used = new HashSet<string>(StringComparer.Ordinal) { mainIcon };

        return new IconEventContentPlan
        {
            Headline = IconEventTextTrimmer.Trim(input.Headline, budget.HeadlineChars) ?? string.Empty,
            Lead = BuildLead(structure, input.Subtitle, budget),
            Bullets = BuildBullets(bulletTexts),
            Stats = TakeStats(input.Stats, budget),
            ClosingNote = budget.ShowsClosingNote ? IconEventTextTrimmer.Trim(structure.ClosingNote, budget.BulletChars) : null,
            MetaChips = BuildMetaChips(input, budget),
            MainIcon = mainIcon,
            SupportingIcons = IconEventIconResolver.ResolveMany(input.SupportingIcons, bulletTexts, 3, used),
        };
    }

    private static string? BuildLead(IconEventTextStructure structure, string? subtitle, IconEventContentBudget budget)
    {
        // Structured copy already separated its opening paragraph; unstructured copy has to be cut
        // down from the whole body, and leading with its first sentences reads best.
        var source = structure.IsStructured ? structure.Lead : FirstSentences(subtitle, budget.LeadChars);
        return IconEventTextTrimmer.Trim(source, budget.LeadChars);
    }

    private static string? FirstSentences(string? subtitle, int budgetChars)
    {
        if (string.IsNullOrWhiteSpace(subtitle))
        {
            return null;
        }

        IReadOnlyList<string> sentences = IconEventTextStructureParser.SplitSentences(subtitle);
        if (sentences.Count == 0)
        {
            return subtitle;
        }

        var taken = new List<string>();
        var length = 0;
        foreach (var sentence in sentences)
        {
            if (taken.Count > 0 && length + sentence.Length > budgetChars)
            {
                break;
            }

            taken.Add(sentence);
            length += sentence.Length + 1;
        }

        return string.Join(' ', taken);
    }

    private static IReadOnlyList<string> CollectBullets(IconEventTextStructure structure, IconEventContentBudget budget)
    {
        if (budget.MaxBullets == 0)
        {
            return Array.Empty<string>();
        }

        var items = new List<string>(structure.Bullets);

        // A labelled section is list-shaped content that simply was not marked up as a list, so it
        // fills any remaining slots rather than being dropped or run back into the paragraph.
        foreach (IconEventTextSection section in structure.Sections.Take(MaxSectionBullets))
        {
            if (items.Count >= budget.MaxBullets)
            {
                break;
            }

            items.Add(section.Body.Length > 0 ? $"{section.Label}: {section.Body}" : section.Label);
        }

        return items
            .Take(budget.MaxBullets)
            .Select(item => IconEventTextTrimmer.Trim(item, budget.BulletChars))
            .OfType<string>()
            .ToList();
    }

    private static IReadOnlyList<IconEventBullet> BuildBullets(IReadOnlyList<string> texts)
    {
        var bullets = new List<IconEventBullet>(texts.Count);
        foreach (var text in texts)
        {
            // Only the immediately preceding item is blocked. Excluding the main icon too was
            // worse: an item genuinely about the poster's own subject was pushed onto an unrelated
            // glyph purely because the headline had already claimed the right one.
            var previous = bullets.Count > 0 ? bullets[^1].Icon : null;
            IReadOnlySet<string>? exclude = previous is null
                ? (IReadOnlySet<string>?)null
                : new HashSet<string>(StringComparer.Ordinal) { previous };
            bullets.Add(new IconEventBullet(IconEventIconResolver.Resolve(null, text, exclude), text));
        }

        return bullets;
    }

    private static IReadOnlyList<IconEventStat> TakeStats(IEnumerable<IconEventStat>? stats, IconEventContentBudget budget) =>
        stats is null
            ? Array.Empty<IconEventStat>()
            : stats.Take(budget.MaxStats)
                .Select(stat => stat with { Icon = IconEventIconResolver.Resolve(stat.Icon, stat.Label) })
                .ToList();

    private static IReadOnlyList<IconEventMetaChip> BuildMetaChips(IconEventInput input, IconEventContentBudget budget)
    {
        var candidates = new (string Icon, string? Value)[]
        {
            ("calendar", input.Date),
            ("clock", input.Time),
            ("map-pin", input.Location),
            ("phone", input.ContactPhone),
            ("mail", input.ContactEmail),
        };

        return candidates
            .Where(candidate => !string.IsNullOrWhiteSpace(candidate.Value))
            .Take(budget.MaxMetaChips)
            .Select(candidate => new IconEventMetaChip(candidate.Icon, IconEventTextTrimmer.Collapse(candidate.Value!)))
            .ToList();
    }

    private static string IconTopic(IconEventInput input, IconEventTextStructure structure) =>
        string.Join(' ', new[] { input.Headline, structure.Lead ?? input.Subtitle }.Where(part => !string.IsNullOrWhiteSpace(part)));
}
