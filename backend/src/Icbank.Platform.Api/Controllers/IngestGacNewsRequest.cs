using Icbank.Platform.Application.Gac.Commands;

namespace Icbank.Platform.Api.Controllers;

/// <summary>Request body for <see cref="GacController.IngestNewsAsync"/>.</summary>
/// <param name="Items">The batch of news items to upsert (max 200).</param>
public sealed record IngestGacNewsRequest(IReadOnlyList<IngestGacNewsItem> Items);
