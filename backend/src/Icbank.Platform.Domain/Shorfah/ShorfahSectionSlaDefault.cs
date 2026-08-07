using Icbank.Platform.Domain.Identity;

namespace Icbank.Platform.Domain.Shorfah;

/// <summary>
/// Default SLA-day count per section type -- template-level config
/// (DATA-MODEL.md section 3.8 <c>shorfah_section_sla_defaults</c>).
/// </summary>
/// <remarks>
/// Deviation: this is the only table in the source schema whose primary key is a natural key
/// (<c>section_type text</c>) rather than a surrogate <c>serial</c> id (flagged in
/// DATA-MODEL.md section 2 for a port decision). This port keeps the natural key for fidelity,
/// since <see cref="SectionType"/> is already a closed, stable 13-value domain
/// (<see cref="ShorfahSectionType"/>) -- see DOMAIN-PORT-NOTES.md. It therefore does NOT derive
/// from <see cref="Common.AuditableEntity"/> (which assumes an int surrogate key); audit columns
/// are declared directly to still satisfy the audit-column closure requirement.
/// </remarks>
public sealed class ShorfahSectionSlaDefault
{
    /// <summary>Gets or sets the section type, the natural primary key.</summary>
    public ShorfahSectionType SectionType { get; set; }

    /// <summary>Gets or sets the default SLA day count.</summary>
    public int SlaDays { get; set; } = 7;

    /// <summary>Gets or sets the UTC timestamp the row was created.</summary>
    public DateTime CreatedAt { get; set; }

    /// <summary>Gets or sets the identity of the actor that created the row.</summary>
    public string CreatedBy { get; set; } = string.Empty;

    /// <summary>Gets or sets the UTC timestamp of the most recent update, if any.</summary>
    public DateTime? UpdatedAt { get; set; }

    /// <summary>Gets or sets the id of the user who last updated the default, if known.</summary>
    public int? UpdatedByUserId { get; set; }

    /// <summary>Gets or sets the updating-user navigation property.</summary>
    public User? UpdatedByUser { get; set; }

    /// <summary>Gets or sets the row-version token for optimistic concurrency.</summary>
    public byte[] RowVersion { get; set; } = Array.Empty<byte>();
}
