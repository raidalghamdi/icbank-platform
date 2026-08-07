namespace Icbank.Platform.Api.Controllers;

/// <summary>Request body for <c>POST /api/v1/shorfah/sections/{sectionId}/remind</c>.</summary>
/// <param name="UserId">The single recipient user's id.</param>
public sealed record RemindShorfahSectionRequest(int UserId);
