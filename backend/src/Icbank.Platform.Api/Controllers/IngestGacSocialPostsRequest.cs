using Icbank.Platform.Application.Gac.Commands;

namespace Icbank.Platform.Api.Controllers;

/// <summary>Request body for <see cref="GacController.IngestSocialPostsAsync"/>.</summary>
/// <param name="Posts">The batch of social posts to upsert (max 100).</param>
public sealed record IngestGacSocialPostsRequest(IReadOnlyList<IngestGacSocialPostItem> Posts);
