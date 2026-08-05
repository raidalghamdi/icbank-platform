using System.Text.Json;

namespace Icbank.Platform.Application.Weekend;

/// <summary>
/// The public-facing weekend-page payload response shape (<c>GET /wk2-data</c>,
/// API-SURFACE.md §9, BUSINESS-RULES.md §2.4). Curated <see cref="Places"/> always win over the
/// latest published draft's AI-generated places when both exist; every other section comes
/// exclusively from the latest published draft.
/// </summary>
/// <param name="Places">Curated places, or the draft's AI-generated places if the curated table is empty.</param>
/// <param name="Deals">Deal categories from the latest published draft.</param>
/// <param name="Podcasts">Podcast recommendations from the latest published draft.</param>
/// <param name="AiTools">AI tool recommendations from the latest published draft (always empty today — no source field populates this).</param>
/// <param name="Matches">Sports matches from the latest published draft.</param>
/// <param name="Movies">Movie recommendations from the latest published draft.</param>
/// <param name="Summary">The draft's welcome summary paragraph, if any.</param>
/// <param name="PublishedAt">The latest published draft's publish timestamp, if any.</param>
/// <param name="WeekendDate">The latest published draft's target weekend date, if any.</param>
/// <param name="City">Always Riyadh — the product is Riyadh-only end to end (BUSINESS-RULES.md §2.4).</param>
public sealed record Wk2DataDto(
    IReadOnlyList<JsonElement> Places,
    IReadOnlyList<JsonElement> Deals,
    IReadOnlyList<JsonElement> Podcasts,
    IReadOnlyList<JsonElement> AiTools,
    IReadOnlyList<JsonElement> Matches,
    IReadOnlyList<JsonElement> Movies,
    string? Summary,
    DateTimeOffset? PublishedAt,
    string? WeekendDate,
    string City);
