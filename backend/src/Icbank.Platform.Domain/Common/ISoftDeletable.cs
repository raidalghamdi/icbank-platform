namespace Icbank.Platform.Domain.Common;

/// <summary>
/// Marks an entity as participating in the soft-delete convention (R-BE-023): rows are never
/// physically removed, only flagged with <see cref="DeletedAt"/>, and hidden from normal
/// queries by a global query filter configured in Infrastructure.
/// </summary>
public interface ISoftDeletable
{
    /// <summary>Gets or sets the UTC timestamp the row was soft-deleted, or <c>null</c> if active.</summary>
    DateTime? DeletedAt { get; set; }
}
