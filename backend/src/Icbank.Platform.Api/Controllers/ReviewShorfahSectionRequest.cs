namespace Icbank.Platform.Api.Controllers;

/// <summary>Request body for <c>POST /api/v1/shorfah/sections/{sectionId}/review</c>.</summary>
/// <param name="Decision">Either <c>pass</c> or <c>reject</c>.</param>
/// <param name="Notes">Optional free-text notes.</param>
public sealed record ReviewShorfahSectionRequest(string? Decision, string? Notes);
