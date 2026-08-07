using Icbank.Platform.Domain.Common;

namespace Icbank.Platform.Domain.InternationalDays;

/// <summary>
/// Generic source-citation record for AI-search provenance (DATA-MODEL.md section 3.6
/// <c>intl_day_sources</c>). The source schema models <see cref="RelatedId"/> polymorphically via
/// <see cref="RelatedTable"/>, but in practice it is only ever used for
/// <see cref="InternationalDays.InternationalDay"/>. This port keeps the polymorphic
/// discriminator column for fidelity but adds a genuine, non-enforced-by-FK navigation via
/// <see cref="DayId"/>/<see cref="Day"/> populated only when <c>RelatedTable == "international_days"</c>,
/// documented as a deliberate deviation in DOMAIN-PORT-NOTES.md (kept soft per DATA-MODEL.md's
/// own recommendation for this specific relationship).
/// </summary>
public sealed class IntlDaySource : AuditableEntity
{
    /// <summary>Gets or sets the polymorphic discriminator, currently always "international_days".</summary>
    public string RelatedTable { get; set; } = "international_days";

    /// <summary>Gets or sets the polymorphic target row id.</summary>
    public int RelatedId { get; set; }

    /// <summary>Gets or sets the optional convenience FK mirroring <see cref="RelatedId"/> when it targets an international day.</summary>
    public int? DayId { get; set; }

    /// <summary>Gets or sets the day navigation property, populated only for day-scoped sources.</summary>
    public InternationalDay? Day { get; set; }

    /// <summary>Gets or sets the source URL.</summary>
    public string? SourceUrl { get; set; }

    /// <summary>Gets or sets the source title.</summary>
    public string? SourceTitle { get; set; }

    /// <summary>Gets or sets the source publisher.</summary>
    public string? SourcePublisher { get; set; }

    /// <summary>Gets or sets the UTC timestamp the source was accessed.</summary>
    public DateTimeOffset AccessedAt { get; set; }
}
