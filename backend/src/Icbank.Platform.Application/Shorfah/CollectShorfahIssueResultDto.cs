namespace Icbank.Platform.Application.Shorfah;

/// <summary>The response shape for <c>POST /shorfah/issues/:id/collect</c> (API-SURFACE.md §19).</summary>
/// <param name="Issue">The issue after the collect operation.</param>
/// <param name="SectionsSeeded">The number of sections newly seeded (0 if the issue already had sections).</param>
/// <param name="SectionsExisting">The number of sections that already existed before this call.</param>
public sealed record CollectShorfahIssueResultDto(ShorfahIssueDto Issue, int SectionsSeeded, int SectionsExisting);
