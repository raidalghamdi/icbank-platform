using Icbank.Platform.Application.Common.Models;
using MediatR;

namespace Icbank.Platform.Application.Gac.Commands;

/// <summary>
/// Upserts a batch of news items into <c>gac_news_items</c>, the news half of the media-monitoring
/// source data.
/// </summary>
/// <remarks>
/// Added because the port shipped with only <c>social-feed/ingest</c>: the
/// <see cref="Domain.Gac.GacNewsItem"/> entity and table existed, but nothing could write to them,
/// so every report request failed the zero-source guard with <c>NO_SOURCE_DATA</c> no matter how the
/// request was shaped. Deduplication is on <see cref="IngestGacNewsItem.SourceUrl"/> rather than a
/// provider-issued id, because the same article legitimately arrives from more than one provider and
/// only the URL is stable across them.
/// </remarks>
/// <param name="Items">The batch to upsert (max 200, enforced by the validator).</param>
public sealed record IngestGacNewsItemsCommand(IReadOnlyList<IngestGacNewsItem> Items)
    : IRequest<Result<IngestGacNewsItemsResult>>;
