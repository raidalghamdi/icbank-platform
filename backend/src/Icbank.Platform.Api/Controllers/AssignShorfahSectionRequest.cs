namespace Icbank.Platform.Api.Controllers;

/// <summary>Request body for <c>POST /api/v1/shorfah/sections/{sectionId}/assign</c>.</summary>
/// <param name="UserId">The user being assigned.</param>
/// <param name="Role">The assignment role label; defaults to <c>contributor</c> when omitted.</param>
public sealed record AssignShorfahSectionRequest(int UserId, string? Role);
