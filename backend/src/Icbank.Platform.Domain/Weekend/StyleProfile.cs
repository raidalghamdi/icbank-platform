using Icbank.Platform.Domain.Common;

namespace Icbank.Platform.Domain.Weekend;

/// <summary>
/// Single learned writing-style profile derived from the archive -- a singleton table
/// (DATA-MODEL.md section 3.9 <c>style_profile</c>).
/// </summary>
/// <remarks>
/// Note: the source system enforces the singleton pattern only in application code
/// (select-then-upsert); no database constraint prevents a second row. This port does not add a
/// unique filtered index forcing exactly one row, since a hard DB constraint on "exactly one
/// row" is unusual and the safer app-layer pattern is preserved -- flagged for product review in
/// DOMAIN-PORT-NOTES.md.
/// </remarks>
public sealed class StyleProfile : AuditableEntity
{
    /// <summary>Gets or sets a summary of the learned tone.</summary>
    public string? ToneSummary { get; set; }

    /// <summary>Gets or sets the average paragraph length.</summary>
    public float? AvgParagraphLength { get; set; }

    /// <summary>Gets or sets recurring opener sentence patterns.</summary>
    public List<string> OpenerPatterns { get; set; } = new();

    /// <summary>Gets or sets recurring closer sentence patterns.</summary>
    public List<string> CloserPatterns { get; set; } = new();

    /// <summary>Gets or sets recurring keywords.</summary>
    public List<string> RecurringKeywords { get; set; } = new();

    /// <summary>Gets or sets the quote-usage frequency descriptor (dense/moderate/limited).</summary>
    public string? QuoteUsage { get; set; }
}
