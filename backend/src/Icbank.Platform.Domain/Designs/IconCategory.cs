namespace Icbank.Platform.Domain.Designs;

/// <summary>
/// The category grouping for one entry of the icon-event icon catalogue (DATA-MODEL.md /
/// <c>composer/icon-library.ts</c>'s <c>IconCategory</c> union).
/// </summary>
public enum IconCategory
{
    /// <summary>Workshop / training related icons.</summary>
    Workshop,

    /// <summary>Meeting / conference related icons.</summary>
    Meeting,

    /// <summary>Launch / announcement related icons.</summary>
    Launch,

    /// <summary>Social / celebration related icons.</summary>
    Social,

    /// <summary>Common icons usable across any event type.</summary>
    Common,
}
