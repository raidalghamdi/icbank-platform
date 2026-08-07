namespace Icbank.Platform.Application.Shorfah;

/// <summary>The Shorfah workflow-log-entry response shape (API-SURFACE.md §19).</summary>
/// <param name="Id">The log entry id.</param>
/// <param name="SectionId">The owning section's id.</param>
/// <param name="ActorUserId">The id of the actor who performed the transition, if known.</param>
/// <param name="Action">The free-text action name, e.g. <c>submitted</c>, <c>approved</c>.</param>
/// <param name="FromStatus">The workflow status before the transition.</param>
/// <param name="ToStatus">The workflow status after the transition.</param>
/// <param name="Notes">Free-text notes.</param>
/// <param name="CreatedAt">The UTC timestamp the log entry was written.</param>
public sealed record ShorfahWorkflowLogDto(
    int Id,
    int SectionId,
    int? ActorUserId,
    string Action,
    string? FromStatus,
    string? ToStatus,
    string? Notes,
    DateTime CreatedAt);
