namespace Icbank.Platform.Domain.Projects;

/// <summary>Where a project sits in its lifecycle.</summary>
public enum ProjectStage
{
    /// <summary>Scoped but not started.</summary>
    Planning = 0,

    /// <summary>Actively being delivered.</summary>
    InProgress = 1,

    /// <summary>Paused pending a decision or a dependency.</summary>
    OnHold = 2,

    /// <summary>Delivered and closed.</summary>
    Completed = 3,
}
