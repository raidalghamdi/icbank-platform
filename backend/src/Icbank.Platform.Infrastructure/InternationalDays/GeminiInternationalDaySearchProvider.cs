using System.Text.Json;
using Icbank.Platform.Application.InternationalDays;
using Icbank.Platform.Infrastructure.Gemini;

namespace Icbank.Platform.Infrastructure.InternationalDays;

/// <summary>
/// Gemini-backed <see cref="IInternationalDaySearchProvider"/>. Replaces the Node source's
/// Perplexity <c>sonar-pro</c> web-grounded search with Gemini's <c>google_search</c> tool.
/// Critical safeguard: unlike Perplexity, Gemini decides for itself whether to search, so it can
/// answer from parametric memory and return fluent, plausible, uncited content -- inventing
/// activations that look researched is far worse than a visible error. This adapter therefore
/// requests grounding (<see cref="GeminiCallOptions.RequireGrounding"/>) and
/// <see cref="IGeminiClient.GenerateJsonAsync"/> throws <see cref="GeminiGroundingAbsentException"/>
/// (propagated up through <c>SearchInternationalDayCommandHandler</c>, uncaught, to
/// <c>GlobalExceptionMiddleware</c>) whenever the response carries no search queries and no
/// citations, treating an ungrounded answer as a failure rather than a result.
/// <para>
/// Deliberately NOT plumbed through this adapter: <see cref="GeminiGenerationResult.SearchEntryPointHtml"/>
/// (Google's "Search Suggestions" HTML). <see cref="DaySearchResultDto"/> is persisted into a
/// normalized schema (<c>InternationalDay</c>/<c>DayActivation</c>/<c>IntlDaySource</c>, no HTML
/// column) and rebuilt from those rows on every 7-day cache hit -- threading the HTML through this
/// DTO would make it silently vanish on the majority of requests (cache hits), which is worse than
/// omitting it consistently. It IS captured at the <see cref="IGeminiClient"/> boundary for any
/// caller that wants it; wiring it further into this feature's persistence/API contract is a
/// deliberately out-of-scope product decision, not an oversight -- see GEMINI-ADAPTER-NOTES.md.
/// </para>
/// </summary>
public sealed class GeminiInternationalDaySearchProvider : IInternationalDaySearchProvider
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    private readonly IGeminiClient _client;
    private readonly GeminiOptions _options;

    /// <summary>Initializes a new instance of the <see cref="GeminiInternationalDaySearchProvider"/> class.</summary>
    /// <param name="client">The resilience-aware Gemini client.</param>
    /// <param name="options">The Gemini model configuration.</param>
    public GeminiInternationalDaySearchProvider(IGeminiClient client, GeminiOptions options)
    {
        _client = client;
        _options = options;
    }

    /// <inheritdoc />
    public async Task<DaySearchResultDto> SearchAsync(string dayName, int year, CancellationToken cancellationToken)
    {
        var prompt = InternationalDaySearchPromptTemplate.Build(dayName, year);
        var callOptions = new GeminiCallOptions(_options.TextModel, UseGoogleSearchTool: true, RequireGrounding: true, MaxOutputTokens: 8192);
        GeminiGenerationResult result = await _client.GenerateJsonAsync(prompt, callOptions, cancellationToken).ConfigureAwait(false);

        var dto = JsonSerializer.Deserialize<DaySearchResultJsonDto>(result.Text, JsonOptions)
            ?? throw new GeminiUnavailableException("Gemini returned an empty/null JSON payload for the international-day search.");

        return Map(dto, result);
    }

    private static DaySearchResultDto Map(DaySearchResultJsonDto dto, GeminiGenerationResult result)
    {
        var citationSources = result.Citations
            .Select(c => new DaySearchSourceDto(c.Url, c.Title, Publisher: null))
            .ToList();

        var mergedSources = (dto.Sources ?? [])
            .Select(s => new DaySearchSourceDto(s.Url, s.Title, s.Publisher))
            .Concat(citationSources)
            .ToList();

        return new DaySearchResultDto(
            dto.DayNameAr,
            dto.DayNameEn,
            dto.AnnualDate,
            dto.OfficialOrganizer,
            dto.OfficialOrganizerSource,
            dto.HistorySummary,
            dto.HistorySource,
            dto.CurrentThemeAr,
            dto.CurrentThemeEn,
            dto.ThemeSourceUrl,
            (dto.Activations ?? []).Select(a => new DaySearchActivationDto(
                a.EntityName, a.EntityType, a.ActivationType, a.Platform, a.Description, a.SourceUrl, a.Country, a.Year)).ToList(),
            (dto.DesignSamples ?? []).Select(d => new DaySearchDesignSampleDto(
                d.EntityName, d.EntityType, d.Platform, d.Description, d.PageUrl, d.ImageUrl, d.Country, d.Year)).ToList(),
            dto.Suggestions ?? [],
            mergedSources);
    }
}
