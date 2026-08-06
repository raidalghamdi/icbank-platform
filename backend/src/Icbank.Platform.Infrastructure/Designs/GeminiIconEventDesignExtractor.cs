using System.Text.Json;
using System.Text.Json.Serialization;
using Icbank.Platform.Application.Designs.IconEvent;
using Icbank.Platform.Infrastructure.Gemini;

namespace Icbank.Platform.Infrastructure.Designs;

/// <summary>
/// Gemini-backed <see cref="IIconEventDesignExtractor"/>. The prompt is fully assembled by
/// <see cref="IconEventPromptBuilder"/> in the Application layer (BUSINESS-RULES.md §7.4,
/// verbatim); this adapter calls <see cref="IGeminiClient.GenerateJsonAsync"/> (the Node source's
/// <c>aiJSONWithFallback</c> equivalent) and maps the <c>{extracted, variants}</c> wire shape onto
/// the typed <see cref="IconEventExtractionResultDto"/>.
/// </summary>
public sealed class GeminiIconEventDesignExtractor : IIconEventDesignExtractor
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    private readonly IGeminiClient _client;
    private readonly GeminiOptions _options;

    /// <summary>Initializes a new instance of the <see cref="GeminiIconEventDesignExtractor"/> class.</summary>
    /// <param name="client">The resilience-aware Gemini client.</param>
    /// <param name="options">The Gemini model configuration.</param>
    public GeminiIconEventDesignExtractor(IGeminiClient client, GeminiOptions options)
    {
        _client = client;
        _options = options;
    }

    /// <inheritdoc />
    public async Task<IconEventExtractionResultDto> ExtractAsync(string prompt, CancellationToken cancellationToken)
    {
        var callOptions = new GeminiCallOptions(_options.TextModel);
        GeminiGenerationResult result = await _client.GenerateJsonAsync(prompt, callOptions, cancellationToken).ConfigureAwait(false);

        IconEventWireDto wire = JsonSerializer.Deserialize<IconEventWireDto>(result.Text, JsonOptions)
            ?? throw new GeminiUnavailableException("Gemini returned an empty/null JSON payload for icon-event extraction.");

        return new IconEventExtractionResultDto(MapExtracted(wire.Extracted), MapVariants(wire.Variants));
    }

    private static IconEventExtractedDataDto MapExtracted(IconEventExtractedWireDto? extracted)
    {
        if (extracted is null)
        {
            return new IconEventExtractedDataDto(string.Empty, string.Empty, string.Empty, string.Empty, string.Empty, string.Empty, []);
        }

        return new IconEventExtractedDataDto(
            extracted.Headline ?? string.Empty,
            extracted.Subtitle ?? string.Empty,
            extracted.Department ?? string.Empty,
            extracted.Hashtag ?? string.Empty,
            extracted.ContactEmail ?? string.Empty,
            extracted.ContactPhone ?? string.Empty,
            (extracted.Stats ?? []).Select(s => new IconEventStatDto(s.Icon ?? string.Empty, s.Value ?? string.Empty, s.Label ?? string.Empty)).ToList());
    }

    private static List<IconEventVariantProposalDto> MapVariants(List<IconEventVariantWireDto>? variants)
    {
        return (variants ?? [])
            .Select(v => new IconEventVariantProposalDto(v.Layout ?? string.Empty, v.MainIcon ?? string.Empty, v.SupportingIcons ?? [], v.Rationale ?? string.Empty))
            .ToList();
    }

    private sealed class IconEventWireDto
    {
        [JsonPropertyName("extracted")]
        public IconEventExtractedWireDto? Extracted { get; set; }

        [JsonPropertyName("variants")]
        public List<IconEventVariantWireDto>? Variants { get; set; }
    }

    private sealed class IconEventExtractedWireDto
    {
        [JsonPropertyName("headline")]
        public string? Headline { get; set; }

        [JsonPropertyName("subtitle")]
        public string? Subtitle { get; set; }

        [JsonPropertyName("department")]
        public string? Department { get; set; }

        [JsonPropertyName("hashtag")]
        public string? Hashtag { get; set; }

        [JsonPropertyName("contact_email")]
        public string? ContactEmail { get; set; }

        [JsonPropertyName("contact_phone")]
        public string? ContactPhone { get; set; }

        [JsonPropertyName("stats")]
        public List<IconEventStatWireDto>? Stats { get; set; }
    }

    private sealed class IconEventStatWireDto
    {
        [JsonPropertyName("icon")]
        public string? Icon { get; set; }

        [JsonPropertyName("value")]
        public string? Value { get; set; }

        [JsonPropertyName("label")]
        public string? Label { get; set; }
    }

    private sealed class IconEventVariantWireDto
    {
        [JsonPropertyName("layout")]
        public string? Layout { get; set; }

        [JsonPropertyName("main_icon")]
        public string? MainIcon { get; set; }

        [JsonPropertyName("supporting_icons")]
        public List<string>? SupportingIcons { get; set; }

        [JsonPropertyName("rationale")]
        public string? Rationale { get; set; }
    }
}
