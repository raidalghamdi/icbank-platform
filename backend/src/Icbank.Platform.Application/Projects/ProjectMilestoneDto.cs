namespace Icbank.Platform.Application.Projects;

/// <summary>A delivery checkpoint as the projects page renders it.</summary>
/// <param name="Id">The checkpoint identifier.</param>
/// <param name="Title">The checkpoint title.</param>
/// <param name="DueDate">The UTC date the checkpoint is due.</param>
/// <param name="IsCompleted">Whether the checkpoint has been delivered.</param>
public sealed record ProjectMilestoneDto(int Id, string Title, DateTime DueDate, bool IsCompleted);
