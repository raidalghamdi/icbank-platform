namespace Icbank.Platform.Infrastructure.Rendering;

/// <summary>
/// Thrown when a rendering/extraction input or output fails a validation guard (oversized content,
/// unsupported format, or a timeout) -- always caught by the calling Application-layer handler and
/// translated into a <c>Result&lt;T&gt;.Failure</c>, never allowed to surface as an unhandled 500.
/// </summary>
public sealed class RenderingValidationException : Exception
{
    /// <summary>Initializes a new instance of the <see cref="RenderingValidationException"/> class.</summary>
    public RenderingValidationException()
    {
    }

    /// <summary>Initializes a new instance of the <see cref="RenderingValidationException"/> class.</summary>
    /// <param name="message">A human-readable description of the validation failure.</param>
    public RenderingValidationException(string message)
        : base(message)
    {
    }

    /// <summary>Initializes a new instance of the <see cref="RenderingValidationException"/> class.</summary>
    /// <param name="message">A human-readable description of the validation failure.</param>
    /// <param name="innerException">The exception that caused this validation failure.</param>
    public RenderingValidationException(string message, Exception innerException)
        : base(message, innerException)
    {
    }
}
