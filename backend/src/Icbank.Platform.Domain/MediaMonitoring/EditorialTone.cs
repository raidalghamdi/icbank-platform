namespace Icbank.Platform.Domain.MediaMonitoring;

/// <summary>Typed shape for <c>final_media_reports.editorial_tone</c> (DATA-MODEL.md section 6, report section 5).</summary>
public sealed class EditorialTone
{
    /// <summary>Gets or sets the tone distribution breakdown.</summary>
    public List<EditorialToneBucket> Distribution { get; set; } = new();

    /// <summary>Gets or sets the topic classification breakdown.</summary>
    public List<EditorialToneBucket> Classification { get; set; } = new();

    /// <summary>Gets or sets the source-outlet breakdown.</summary>
    public List<EditorialToneBucket> Sources { get; set; } = new();
}

/// <summary>
/// One percentage bucket nested under <see cref="EditorialTone"/>. The source shapes for
/// distribution/classification/sources share this same <c>{label, percent, count}</c> structure
/// under different field names (<c>tone</c>/<c>topic</c>/<c>source</c>); this port unifies them
/// into a single <see cref="Label"/> field, a deliberate simplification recorded in
/// DOMAIN-PORT-NOTES.md.
/// </summary>
public sealed class EditorialToneBucket
{
    /// <summary>Gets or sets the bucket label (tone, topic, or source name depending on context).</summary>
    public string Label { get; set; } = string.Empty;

    /// <summary>Gets or sets the percentage share.</summary>
    public double Percent { get; set; }

    /// <summary>Gets or sets the raw count.</summary>
    public int Count { get; set; }
}
