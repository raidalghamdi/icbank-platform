namespace Icbank.Platform.Domain.Designs;

/// <summary>Turns a resolved input into the copy one specific canvas will actually render.</summary>
public static class IconEventContentPlanner
{
    /// <summary>Plans the copy for a poster.</summary>
    /// <param name="input">The resolved input, after extraction.</param>
    /// <returns>The plan for <see cref="IconEventInput.Size"/>.</returns>
    public static IconEventContentPlan Plan(IconEventInput input)
    {
        ArgumentNullException.ThrowIfNull(input);
        IconEventTextStructure structure = IconEventTextStructureParser.Parse(input.Subtitle);

        IReadOnlyList<string> bulletTexts = CollectBullets(structure);
        var mainIcon = IconEventIconResolver.Resolve(input.MainIcon, IconTopic(input, structure));
        var used = new HashSet<string>(StringComparer.Ordinal) { mainIcon };

        return new IconEventContentPlan
        {
            Headline = IconEventTextTrimmer.Collapse(input.Headline ?? string.Empty),
            Lead = BuildLead(structure, input.Subtitle),
            Bullets = BuildBullets(bulletTexts),
            Stats = ResolveStats(input.Stats),
            ClosingNote = structure.ClosingNote,
            MetaChips = BuildMetaChips(input),
            MainIcon = mainIcon,
            SupportingIcons = IconEventIconResolver.ResolveMany(input.SupportingIcons, bulletTexts, 3, used),
        };
    }

    // The author's wording is the deliverable. Earlier revisions cut the copy to a per-canvas
    // character budget, so the same message said different things on a phone and on a wall; the
    // fitting pass now sizes the type instead, and every word survives at every size.
    private static string? BuildLead(IconEventTextStructure structure, string? subtitle) =>
        structure.IsStructured ? structure.Lead : subtitle;

    private static IReadOnlyList<string> CollectBullets(IconEventTextStructure structure)
    {
        var items = new List<string>(structure.Bullets);

        // A labelled section is list-shaped content that simply was not marked up as a list, so it
        // joins the list rather than being dropped or run back into the paragraph.
        foreach (IconEventTextSection section in structure.Sections)
        {
            items.Add(section.Body.Length > 0 ? $"{section.Label}: {section.Body}" : section.Label);
        }

        return items;
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

    private static IReadOnlyList<IconEventStat> ResolveStats(IEnumerable<IconEventStat>? stats) =>
        stats is null
            ? Array.Empty<IconEventStat>()
            : stats.Select(stat => stat with { Icon = IconEventIconResolver.Resolve(stat.Icon, stat.Label) }).ToList();

    private static IReadOnlyList<IconEventMetaChip> BuildMetaChips(IconEventInput input)
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
            .Select(candidate => new IconEventMetaChip(candidate.Icon, IconEventTextTrimmer.Collapse(candidate.Value!)))
            .ToList();
    }

    private static string IconTopic(IconEventInput input, IconEventTextStructure structure) =>
        string.Join(' ', new[] { input.Headline, structure.Lead ?? input.Subtitle }.Where(part => !string.IsNullOrWhiteSpace(part)));
}
