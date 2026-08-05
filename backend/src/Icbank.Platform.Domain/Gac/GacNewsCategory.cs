namespace Icbank.Platform.Domain.Gac;

/// <summary>Category of a GAC news feed item (DATA-MODEL.md section 5).</summary>
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
}
