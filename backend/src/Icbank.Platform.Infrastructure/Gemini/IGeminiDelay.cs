namespace Icbank.Platform.Infrastructure.Gemini;

/// <summary>
/// Injectable sleep seam so unit tests can assert on the exact backoff durations the retry loop
/// computes without a single test actually waiting in real time. Production registers a
/// <see cref="Task.Delay(TimeSpan,CancellationToken)"/>-backed implementation; tests register a
/// recording fake.
/// </summary>
public interface IGeminiDelay
{
    /// <summary>Delays for the given duration.</summary>
    /// <param name="delay">The duration to wait.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    Task Wait(TimeSpan delay, CancellationToken cancellationToken);
}

/// <summary>Real-time <see cref="IGeminiDelay"/> used in production.</summary>
public sealed class TaskDelayGeminiDelay : IGeminiDelay
{
    /// <inheritdoc />
    public Task Wait(TimeSpan delay, CancellationToken cancellationToken) => Task.Delay(delay, cancellationToken);
}
