using Icbank.Platform.Domain.Common;

namespace Icbank.Platform.Domain.AiYear;

/// <summary>
/// Campaign/activation record for the "Year of AI 2026" initiative
/// (DATA-MODEL.md section 3.2 <c>ai_year_activations</c>).
/// </summary>
/// <remarks>
/// Deviation: the source <c>channels text[]</c> native Postgres array (AMBIGUOUS-2 in
/// DATA-MODEL.md) is ported as a normalized child table <see cref="AiYearActivationChannel"/>
/// rather than a JSON string, for referential/query integrity. See DOMAIN-PORT-NOTES.md.
/// </remarks>
public sealed class AiYearActivation : AuditableEntity
{
    /// <summary>Gets or sets the activation title.</summary>
    public string Title { get; set; } = string.Empty;

    /// <summary>Gets or sets the calendar month (1-12) the activation belongs to.</summary>
    public int Month { get; set; }

    /// <summary>Gets or sets the calendar year, defaults to 2026.</summary>
    public int Year { get; set; } = 2026;

    /// <summary>Gets or sets the free-text activation date as captured by the source system.</summary>
    public string? ActivationDate { get; set; }

    /// <summary>
    /// Gets or sets the activation type. Free text: DATA-MODEL.md AMBIGUOUS-4 notes no fixed
    /// value list exists in source code for this column, so it is not modeled as an enum.
    /// </summary>
    public string Type { get; set; } = string.Empty;

    /// <summary>Gets or sets a free-text description.</summary>
    public string? Description { get; set; }

    /// <summary>Gets or sets the tag list (was <c>jsonb string[]</c> in source).</summary>
    public List<string> Tags { get; set; } = new();

    /// <summary>Gets or sets the publication status.</summary>
    public AiYearActivationStatus Status { get; set; } = AiYearActivationStatus.Published;

    /// <summary>Gets or sets the reach metric, if recorded.</summary>
    public int? Reach { get; set; }

    /// <summary>Gets or sets the engagement metric, if recorded.</summary>
    public int? Engagement { get; set; }

    /// <summary>Gets or sets free-text notes.</summary>
    public string? Notes { get; set; }

    /// <summary>Gets the channels this activation was run on (normalized from the source text array).</summary>
    public ICollection<AiYearActivationChannel> Channels { get; init; } = new List<AiYearActivationChannel>();

    /// <summary>Gets the media attached to this activation.</summary>
    public ICollection<AiYearMedia> Media { get; init; } = new List<AiYearMedia>();

    /// <summary>Gets the free-form metrics attached to this activation.</summary>
    public ICollection<AiYearMetric> Metrics { get; init; } = new List<AiYearMetric>();
}
