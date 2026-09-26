using System.Text.Json;
using System.Text.Json.Serialization;

namespace Icbank.Platform.Application.MediaMonitoring;

/// <summary>
/// The reviewer's arrangement of a weekly media report: which sections appear, in what order and
/// under what title, the optional Contact Center figures, and any free-text sections the reviewer
/// added. Stored as JSON on the report so the arrangement survives a page refresh and is the one
/// the approved PDF is rendered from.
/// </summary>
/// <param name="Sections">The sections in display order.</param>
/// <param name="ContactCenter">The Contact Center figures, or null when none were entered.</param>
public sealed record FinalReportLayout(
    IReadOnlyList<FinalReportLayoutSection> Sections,
    ContactCenterFigures? ContactCenter)
{
    /// <summary>The key of the executive summary section.</summary>
    public const string Summary = "summary";

    /// <summary>The key of the top-news section.</summary>
    public const string News = "news";

    /// <summary>The key of the media-tone section.</summary>
    public const string Tone = "tone";

    /// <summary>The key of the deep-analysis section.</summary>
    public const string Analysis = "analysis";

    /// <summary>The key of the recommendations section.</summary>
    public const string Recommendations = "recommendations";

    /// <summary>The key of the methodology section.</summary>
    public const string Methodology = "methodology";

    /// <summary>The key of the optional Contact Center section.</summary>
    public const string ContactCenterKey = "contact_center";

    /// <summary>The key prefix of reviewer-added free-text sections.</summary>
    public const string CustomPrefix = "custom-";

    /// <summary>The largest layout document accepted, in characters.</summary>
    public const int MaxJsonLength = 200_000;

    private const int MaxSections = 40;

    private static readonly JsonSerializerOptions Options = new(JsonSerializerDefaults.Web)
    {
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
    };

    private static readonly (string Key, string Title)[] BuiltIns =
    {
        (Summary, "الملخص التنفيذي"),
        (News, "أبرز الأخبار خلال الفترة"),
        (Tone, "تحليل التوجه الإعلامي"),
        (Analysis, "تحليل عميق ومؤشرات قطاعية"),
        (Recommendations, "التوصيات والإجراءات المقترحة"),
        (Methodology, "المنهجية والمصادر"),
        (ContactCenterKey, "مركز الاتصال"),
    };

    /// <summary>Gets the layout every report had before review existed: the six built-in sections, Contact Center hidden.</summary>
    public static FinalReportLayout Default { get; } = new(
        BuiltIns.Select(b => new FinalReportLayoutSection(b.Key, b.Title, !string.Equals(b.Key, ContactCenterKey, StringComparison.Ordinal), null)).ToList(),
        null);

    /// <summary>Returns the default Arabic title for a built-in section key, or null for an unknown key.</summary>
    /// <param name="key">The section key.</param>
    /// <returns>The default title.</returns>
    public static string? DefaultTitleOf(string key) =>
        BuiltIns.Where(b => string.Equals(b.Key, key, StringComparison.Ordinal)).Select(b => b.Title).FirstOrDefault();

    /// <summary>Returns whether the key names a built-in section or a reviewer-added one.</summary>
    /// <param name="key">The section key.</param>
    /// <returns><see langword="true"/> when the key is accepted.</returns>
    public static bool IsKnownKey(string? key) =>
        key is not null && (DefaultTitleOf(key) is not null || (key.StartsWith(CustomPrefix, StringComparison.Ordinal) && key.Length <= 80));

    /// <summary>
    /// Reads a stored layout. An empty or unreadable document falls back to <see cref="Default"/>
    /// so a legacy report still renders the way it always did.
    /// </summary>
    /// <param name="json">The stored layout JSON.</param>
    /// <returns>The layout.</returns>
    public static FinalReportLayout Parse(string? json)
    {
        FinalReportLayout? parsed = TryDeserialize(json);
        if (parsed?.Sections is null || parsed.Sections.Count == 0)
        {
            return parsed?.ContactCenter is null ? Default : Default with { ContactCenter = parsed.ContactCenter };
        }

        var sections = parsed.Sections
            .Where(s => s is not null && IsKnownKey(s.Key))
            .GroupBy(s => s.Key, StringComparer.Ordinal)
            .Select(g => g.First())
            .Take(MaxSections)
            .ToList();
        return new FinalReportLayout(sections, parsed.ContactCenter);
    }

    /// <summary>
    /// Validates a layout document sent by the reviewer.
    /// </summary>
    /// <param name="json">The layout JSON.</param>
    /// <returns>An Arabic error message, or null when the document is acceptable.</returns>
    public static string? Validate(string? json)
    {
        if (string.IsNullOrWhiteSpace(json))
        {
            return null;
        }

        if (json.Length > MaxJsonLength)
        {
            return "تخطيط التقرير أكبر من الحد المسموح";
        }

        return TryDeserialize(json) is null ? "تخطيط التقرير غير صالح" : null;
    }

    /// <summary>Serialises the layout for storage.</summary>
    /// <returns>The JSON document.</returns>
    public string ToJson() => JsonSerializer.Serialize(this, Options);

    private static FinalReportLayout? TryDeserialize(string? json)
    {
        if (string.IsNullOrWhiteSpace(json))
        {
            return null;
        }

        try
        {
            return JsonSerializer.Deserialize<FinalReportLayout>(json, Options);
        }
        catch (JsonException)
        {
            return null;
        }
    }
}
