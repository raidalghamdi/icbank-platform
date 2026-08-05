namespace Icbank.Platform.Application.Shorfah;

/// <summary>The Shorfah section-assignment response shape (API-SURFACE.md §19).</summary>
/// <param name="Id">The assignment id.</param>
/// <param name="SectionId">The owning section's id.</param>
/// <param name="UserId">The assigned user's id.</param>
/// <param name="Role">The assignment role label.</param>
public sealed record ShorfahAssignmentDto(int Id, int SectionId, int UserId, string? Role);
