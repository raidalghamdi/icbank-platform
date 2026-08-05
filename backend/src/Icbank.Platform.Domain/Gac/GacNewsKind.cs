namespace Icbank.Platform.Domain.Gac;

/// <summary>Kind of a GAC news feed item (DATA-MODEL.md section 5).</summary>
public enum GacNewsKind
{
    /// <summary>A news item.</summary>
    News = 0,

    /// <summary>A formal decision.</summary>
    Decision = 1,

    /// <summary>An event announcement.</summary>
    Event = 2,

    /// <summary>A press release.</summary>
    PressRelease = 3,
}
