namespace Icbank.Platform.Domain.Gac;

/// <summary>Lifecycle status of a GAC publication (DATA-MODEL.md section 5).</summary>
public enum GacPublicationStatus
{
    /// <summary>Published and visible.</summary>
    Published = 0,

    /// <summary>Draft, not yet published.</summary>
    Draft = 1,

    /// <summary>Archived, no longer current.</summary>
    Archived = 2,
}
