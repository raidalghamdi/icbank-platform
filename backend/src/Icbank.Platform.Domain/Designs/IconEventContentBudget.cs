namespace Icbank.Platform.Domain.Designs;

/// <summary>How much copy one canvas can carry before the composition stops being readable.</summary>
/// <param name="HeadlineChars">The longest headline the canvas can hold.</param>
/// <param name="LeadChars">The longest opening paragraph the canvas can hold; zero hides it.</param>
/// <param name="MaxBullets">The most list items the canvas can hold; zero hides the list.</param>
/// <param name="BulletChars">The longest single list item.</param>
/// <param name="MaxStats">The most statistic chips the canvas can hold.</param>
/// <param name="MaxMetaChips">The most date/time/location/contact chips the canvas can hold.</param>
/// <param name="ShowsClosingNote">Whether the closing sign-off line is worth the space.</param>
public sealed record IconEventContentBudget(
    int HeadlineChars,
    int LeadChars,
    int MaxBullets,
    int BulletChars,
    int MaxStats,
    int MaxMetaChips,
    bool ShowsClosingNote)
{
    private static readonly Dictionary<IconEventSizePreset, IconEventContentBudget> Budgets = new()
    {
        // The two largest canvases carry the full message. Below them the copy is progressively
        // reduced rather than shrunk, because type that keeps scaling down stops being legible long
        // before it stops overflowing.
        [IconEventSizePreset.Uhd4k] = new(64, 420, 4, 120, 3, 4, true),
        [IconEventSizePreset.DesktopHd] = new(64, 380, 4, 110, 3, 4, true),
        [IconEventSizePreset.WebStandard] = new(52, 260, 3, 90, 3, 4, false),
        [IconEventSizePreset.WebSmall] = new(44, 150, 2, 64, 2, 2, false),

        // The mini card is barely larger than a thumbnail: a headline, one line of context and two
        // chips is everything that can be read at that size.
        [IconEventSizePreset.WebMini] = new(38, 110, 0, 0, 2, 1, false),
    };

    /// <summary>Gets the budget for a canvas size.</summary>
    /// <param name="preset">The size preset.</param>
    /// <returns>The copy budget for that canvas.</returns>
    public static IconEventContentBudget Resolve(IconEventSizePreset preset) => Budgets[preset];

    /// <summary>Gets the opening-paragraph allowance once a list is also competing for the space.</summary>
    /// <param name="bulletCount">How many list items the canvas will carry.</param>
    /// <returns>The reduced character allowance for the opening paragraph.</returns>
    /// <remarks>
    /// The full allowance assumes the paragraph is the whole message. When a list follows it, a
    /// paragraph of that length pushes the list into the bottom third at a fraction of the size it
    /// deserves, and the reader sees a wall of prose with some small print under it.
    /// </remarks>
    /// <summary>Gets how many meta chips fit once a list is also competing for the space.</summary>
    /// <param name="bulletCount">How many list items the canvas will carry.</param>
    /// <returns>The reduced chip allowance.</returns>
    /// <remarks>
    /// Chips wrap rather than shrink, so a full row of them under a long list silently steals two
    /// or three lines from the composition and lands flush against the bottom edge.
    /// </remarks>
    public int MaxMetaChipsBeside(int bulletCount) =>
        bulletCount == 0 ? MaxMetaChips : Math.Min(MaxMetaChips, bulletCount >= 3 ? 2 : 3);

    public int LeadCharsBeside(int bulletCount) =>
        bulletCount == 0 ? LeadChars : Math.Max(80, LeadChars / (bulletCount >= 3 ? 3 : 2));
}
