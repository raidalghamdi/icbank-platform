namespace Icbank.Platform.Application.Shorfah;

/// <summary>The four per-section permission tiers (BUSINESS-RULES.md §1.4).</summary>
public enum ShorfahSectionAccessTier
{
    /// <summary>Permission to view the section.</summary>
    View = 0,

    /// <summary>Permission to contribute content to the section.</summary>
    Contribute = 1,

    /// <summary>Permission to review submitted content.</summary>
    Review = 2,

    /// <summary>Permission to give final approval.</summary>
    Approve = 3,
}
