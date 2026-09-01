namespace Icbank.Platform.Domain.Projects;

/// <summary>
/// The tracking signal shown next to a project. Derived from schedule versus progress rather than
/// stored, so the portfolio never shows a stale "on track" badge on a project whose deadline
/// quietly passed.
/// </summary>
public enum ProjectHealth
{
    /// <summary>Progress is keeping up with the elapsed schedule.</summary>
    OnTrack = 0,

    /// <summary>Progress is drifting behind the elapsed schedule, or delivery is paused.</summary>
    AtRisk = 1,

    /// <summary>The deadline has passed, or progress is far behind the elapsed schedule.</summary>
    Delayed = 2,

    /// <summary>Delivered and closed.</summary>
    Completed = 3,
}
