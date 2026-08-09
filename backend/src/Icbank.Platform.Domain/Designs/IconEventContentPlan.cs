namespace Icbank.Platform.Domain.Designs;

/// <summary>
/// The copy for one poster after it has been given structure and cut to what the canvas can hold.
/// </summary>
/// <remarks>
/// Layouts render this rather than the raw subtitle. Fitting used to be attempted only in the
/// browser, by shrinking type until it stopped overflowing, which silently produced unreadable
/// posters for long copy and clipped ones for very long copy. Deciding in advance how much copy a
/// canvas gets makes the outcome predictable and testable.
/// </remarks>
public sealed class IconEventContentPlan
{
    /// <summary>Gets the headline.</summary>
    public string Headline { get; init; } = string.Empty;

    /// <summary>Gets the opening paragraph, or <see langword="null"/> when the canvas has no room.</summary>
    public string? Lead { get; init; }

    /// <summary>Gets the list items to render.</summary>
    public IReadOnlyList<IconEventBullet> Bullets { get; init; } = Array.Empty<IconEventBullet>();

    /// <summary>Gets the statistic chips to render.</summary>
    public IReadOnlyList<IconEventStat> Stats { get; init; } = Array.Empty<IconEventStat>();

    /// <summary>Gets the closing sign-off line, or <see langword="null"/> when dropped.</summary>
    public string? ClosingNote { get; init; }

    /// <summary>Gets the meta chips (date, time, location, contacts) the canvas can carry.</summary>
    public IReadOnlyList<IconEventMetaChip> MetaChips { get; init; } = Array.Empty<IconEventMetaChip>();

    /// <summary>Gets the resolved main icon name.</summary>
    public string MainIcon { get; init; } = string.Empty;

    /// <summary>Gets the resolved supporting icon names.</summary>
    public IReadOnlyList<string> SupportingIcons { get; init; } = Array.Empty<string>();

    /// <summary>Gets a value indicating whether there is any body copy to render.</summary>
    public bool HasBody => !string.IsNullOrWhiteSpace(Lead) || Bullets.Count > 0 || !string.IsNullOrWhiteSpace(ClosingNote);
}
