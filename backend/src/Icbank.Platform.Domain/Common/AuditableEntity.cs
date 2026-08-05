namespace Icbank.Platform.Domain.Common;

/// <summary>
/// Base class for every business entity. Carries the audit columns mandated by R-BE-022
/// (created/updated at + by) and the soft-delete marker required by R-BE-023.
/// </summary>
public abstract class AuditableEntity : ISoftDeletable
{
    /// <summary>Gets the entity's primary key. All source tables use an integer identity column.</summary>
    public int Id { get; init; }

    /// <summary>Gets or sets the UTC timestamp the row was created — R-BE-026 (datetime2(3), UTC only).</summary>
    public DateTime CreatedAt { get; set; }

    /// <summary>Gets or sets the identity of the actor that created the row.</summary>
    public string CreatedBy { get; set; } = string.Empty;

    /// <summary>Gets or sets the UTC timestamp of the most recent update, if any.</summary>
    public DateTime? UpdatedAt { get; set; }

    /// <summary>Gets or sets the identity of the actor that performed the most recent update.</summary>
    public string? UpdatedBy { get; set; }

    /// <summary>Gets or sets the UTC timestamp the row was soft-deleted, if any — R-BE-023.</summary>
    public DateTime? DeletedAt { get; set; }

    /// <summary>
    /// Gets or sets the row-version token EF Core maps to SQL Server's <c>rowversion</c> column,
    /// enabling optimistic concurrency detection on every update.
    /// </summary>
    public byte[] RowVersion { get; set; } = Array.Empty<byte>();
}
