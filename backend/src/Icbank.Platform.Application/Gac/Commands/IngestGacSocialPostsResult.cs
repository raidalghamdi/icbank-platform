namespace Icbank.Platform.Application.Gac.Commands;

/// <summary>The ingest outcome summary.</summary>
/// <param name="Inserted">The number of newly inserted posts.</param>
/// <param name="Updated">The number of updated (already-existing) posts.</param>
public sealed record IngestGacSocialPostsResult(int Inserted, int Updated);
