using Icbank.Platform.Infrastructure.Gemini;

namespace Icbank.Platform.UnitTests.Infrastructure.Gemini;

/// <summary>
/// Records every <see cref="IGeminiDelay.Wait"/> call's exact requested <see cref="TimeSpan"/>
/// without actually waiting, so tests can assert on <see cref="GeminiClient"/>'s backoff timing in
/// milliseconds without a single test taking real wall-clock time.
/// </summary>
public sealed class FakeGeminiDelay : IGeminiDelay
{
    /// <summary>Gets the list of every delay duration requested, in call order.</summary>
    public List<TimeSpan> Requested { get; } = [];

    /// <inheritdoc />
    public Task Wait(TimeSpan delay, CancellationToken cancellationToken)
    {
        Requested.Add(delay);
        return Task.CompletedTask;
    }
}
