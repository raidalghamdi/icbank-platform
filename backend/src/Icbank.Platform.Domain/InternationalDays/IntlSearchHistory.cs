using Icbank.Platform.Domain.Common;

namespace Icbank.Platform.Domain.InternationalDays;

/// <summary>
/// Log of every AI search query, for rate-limiting and audit (DATA-MODEL.md section 3.6
/// <c>intl_search_history</c>).
/// </summary>
/// <remarks>
/// Deviation: <c>day_id</c> was an unenforced implied FK in the source schema (DATA-MODEL.md
/// section 4). It is now a proper, enforced, optional foreign key.
/// </remarks>
public sealed class IntlSearchHistory : AuditableEntity
{
    /// <summary>Gets or sets the search query text.</summary>
    public string Query { get; set; } = string.Empty;

    /// <summary>Gets or sets the related day's id, if the search was scoped to one.</summary>
    public int? DayId { get; set; }

    /// <summary>Gets or sets the day navigation property.</summary>
    public InternationalDay? Day { get; set; }

    /// <summary>Gets or sets the caller's IP address.</summary>
    public string? IpAddress { get; set; }

    /// <summary>Gets or sets the UTC timestamp of the search.</summary>
    public DateTimeOffset SearchedAt { get; set; }
}
