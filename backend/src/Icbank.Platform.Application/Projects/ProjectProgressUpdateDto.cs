namespace Icbank.Platform.Application.Projects;

/// <summary>One progress report as the projects page renders it in the project's history strip.</summary>
/// <param name="Id">The update identifier.</param>
/// <param name="ProgressPercent">The completion percentage reported by this update.</param>
/// <param name="Note">The progress note.</param>
/// <param name="ReportedBy">The display name of the manager who logged it.</param>
/// <param name="ReportedAt">The UTC instant it was logged.</param>
public sealed record ProjectProgressUpdateDto(int Id, int ProgressPercent, string Note, string ReportedBy, DateTime ReportedAt);
