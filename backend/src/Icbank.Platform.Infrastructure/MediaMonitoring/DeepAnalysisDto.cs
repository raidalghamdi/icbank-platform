using System.Text.Json.Serialization;

namespace Icbank.Platform.Infrastructure.MediaMonitoring;

/// <summary>Wire shape for the <c>deepAnalysis</c> section.</summary>
public sealed class DeepAnalysisDto
{
    /// <summary>Gets or sets the extracted keyword list.</summary>
    [JsonPropertyName("keywords")]
    public List<KeywordDto>? Keywords { get; set; }

    /// <summary>Gets or sets the standout quote.</summary>
    [JsonPropertyName("quote")]
    public QuoteDto? Quote { get; set; }

    /// <summary>Gets or sets the identified strengths.</summary>
    [JsonPropertyName("strengths")]
    public List<string>? Strengths { get; set; }

    /// <summary>Gets or sets the identified weaknesses.</summary>
    [JsonPropertyName("weaknesses")]
    public List<string>? Weaknesses { get; set; }
}
