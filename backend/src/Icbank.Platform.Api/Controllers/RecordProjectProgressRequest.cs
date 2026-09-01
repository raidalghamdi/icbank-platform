namespace Icbank.Platform.Api.Controllers;

/// <summary>Request body for <c>POST /api/v1/projects/{projectId}/progress</c>.</summary>
/// <param name="ProgressPercent">The completion percentage now reached, 0-100.</param>
/// <param name="Note">The progress note explaining what moved.</param>
/// <param name="ReportedBy">Optional display name of the reporter; the access token's name is used when omitted.</param>
public sealed record RecordProjectProgressRequest(int ProgressPercent, string? Note, string? ReportedBy);
