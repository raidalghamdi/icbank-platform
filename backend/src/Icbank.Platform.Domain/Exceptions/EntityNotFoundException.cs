namespace Icbank.Platform.Domain.Exceptions;

/// <summary>
/// Raised when a lookup by identifier finds no matching, non-deleted row.
/// </summary>
public sealed class EntityNotFoundException : DomainException
{
    /// <summary>Initializes a new instance of the <see cref="EntityNotFoundException"/> class.</summary>
    /// <param name="entityName">The display name of the entity type that was not found.</param>
    /// <param name="entityId">The identifier that was searched for.</param>
    public EntityNotFoundException(string entityName, Guid entityId)
        : base($"{entityName} with id '{entityId}' was not found.")
    {
    }
}
