namespace Icbank.Platform.Domain.Gac;

/// <summary>
/// Category of a GAC news feed item. The original five members came from the unverified
/// DATA-MODEL.md section 5 assumption; the additional members are values observed in production data.
/// </summary>
public enum GacNewsCategory
{
    /// <summary>Merger approved.</summary>
    MergerApproval = 0,

    /// <summary>Merger conditionally approved.</summary>
    MergerConditional = 1,

    /// <summary>Merger blocked.</summary>
    MergerBlock = 2,

    /// <summary>Enforcement action.</summary>
    Enforcement = 3,

    /// <summary>Awareness/education content.</summary>
    Awareness = 4,

    /// <summary>Regulatory content.</summary>
    Regulation = 5,

    /// <summary>Authority decision content.</summary>
    Decision = 6,

    /// <summary>Official announcement content.</summary>
    Announcement = 7,

    /// <summary>General news content.</summary>
    News = 8,

    /// <summary>Statistical content.</summary>
    Statistics = 9,

    /// <summary>Official press-release content.</summary>
    PressRelease = 10,

    /// <summary>Event content.</summary>
    Event = 11,

    /// <summary>Career content.</summary>
    Careers = 12,

    /// <summary>Digital-service content.</summary>
    Digital = 13,
}
