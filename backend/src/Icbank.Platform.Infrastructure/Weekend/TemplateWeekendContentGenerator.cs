using System.Text.Json;
using Icbank.Platform.Application.Weekend;

namespace Icbank.Platform.Infrastructure.Weekend;

/// <summary>
/// Deterministic, non-AI default <see cref="IWeekendContentGenerator"/> implementation. The Node
/// source called a Gemini→Perplexity fallback chain with the verbatim prompt in
/// BUSINESS-RULES.md §2.3; wiring a real LLM provider chain is deferred for Wave 1 (see
/// WAVE1-PORT-NOTES.md) — this implementation produces a schema-correct, clearly-labeled
/// placeholder bundle (matching the exact required counts: 4 places, 3 deal categories × 3 items,
/// 3 podcasts, 3 matches, 3 movies) so every downstream endpoint (approve/publish/wk2-data) is
/// fully exercisable end-to-end without an external AI dependency.
/// </summary>
public sealed class TemplateWeekendContentGenerator : IWeekendContentGenerator
{
    private const int PlaceCount = 4;
    private const int DealCategoryCount = 3;
    private const int DealItemsPerCategory = 3;
    private const int PodcastCount = 3;
    private const int MatchCount = 3;
    private const int MovieCount = 3;

    /// <inheritdoc />
    public Task<string> GenerateAsync(string weekendDate, CancellationToken cancellationToken)
    {
        var payload = new
        {
            summary = $"محتوى نهاية الأسبوع للرياض - {weekendDate} (نموذج مؤقت بانتظار ربط مزوّد الذكاء الاصطناعي)",
            places = Enumerable.Range(1, PlaceCount).Select(i => new { title = $"مكان {i}", body = "وصف مؤقت", maps_query = $"place-{i}-riyadh" }).ToList(),
            deals = Enumerable.Range(1, DealCategoryCount).Select(category => new
            {
                title = $"فئة عروض {category}",
                items = Enumerable.Range(1, DealItemsPerCategory).Select(item => new { place = $"علامة {item}", discount = "عرض مؤقت", detail = "تفاصيل مؤقتة", emoji = "🏷️" }).ToList(),
            }).ToList(),
            podcasts = Enumerable.Range(1, PodcastCount).Select(i => new { title = $"بودكاست {i}", field = "عام", episode = "حلقة", body = "وصف مؤقت", channel = "Spotify", tagline = "شعار" }).ToList(),
            matches = Enumerable.Range(1, MatchCount).Select(i => new { title = $"بطولة {i}", teams = "فريق × فريق", time = "TBD", channel = "TBD" }).ToList(),
            movies = Enumerable.Range(1, MovieCount).Select(i => new { title = $"فيلم {i}", genre = "عام", cinema = "VOX", rating = "عام", body = "وصف مؤقت" }).ToList(),
        };

        return Task.FromResult(JsonSerializer.Serialize(payload));
    }
}
