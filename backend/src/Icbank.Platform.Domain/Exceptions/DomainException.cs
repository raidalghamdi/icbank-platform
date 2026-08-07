namespace Icbank.Platform.Domain.Exceptions;

/// <summary>
/// Base type for every domain-level exception (R-BE-090: intent-revealing failure types,
/// never a bare <see cref="Exception"/>). Concrete subclasses are named <c>{Reason}Exception</c>.
/// </summary>
public abstract class DomainException : Exception
{
    /// <summary>Initializes a new instance of the <see cref="DomainException"/> class.</summary>
    /// <param name="message">A human-readable description of the domain rule that was violated.</param>
    protected DomainException(string message)
        : base(message)
    {
    }
}
