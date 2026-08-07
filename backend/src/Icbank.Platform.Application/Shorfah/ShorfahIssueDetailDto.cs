namespace Icbank.Platform.Application.Shorfah;

/// <summary>Issue + ordered sections response shape for <c>GET /shorfah/issues/:id</c> (API-SURFACE.md §19).</summary>
/// <param name="Issue">The issue.</param>
/// <param name="Sections">The issue's sections, ordered by display order.</param>
public sealed record ShorfahIssueDetailDto(ShorfahIssueDto Issue, IReadOnlyList<ShorfahSectionDto> Sections);
