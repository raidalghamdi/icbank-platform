namespace Icbank.Platform.Api.Controllers;

/// <summary>Request body for <c>POST /api/v1/shorfah/sections/{sectionId}/approve</c>.</summary>
/// <param name="Notes">Optional free-text notes.</param>
public sealed record ApproveShorfahSectionRequest(string? Notes);
