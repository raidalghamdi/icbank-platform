using System.Collections.Concurrent;
using Icbank.Platform.Application.Common.Interfaces;
using Icbank.Platform.Application.Shorfah;

namespace Icbank.Platform.Infrastructure.Shorfah;

/// <summary>
/// In-memory <see cref="IShorfahSectionGenerationRateLimiter"/>, following the exact structure of
/// <see cref="InMemoryShorfahSendInitialRateLimiter"/>. Registered as a singleton so window state
/// survives across requests within one process.
/// </summary>
public sealed class InMemoryShorfahSectionGenerationRateLimiter : IShorfahSectionGenerationRateLimiter
{
    private const int MaxGenerationsPerWindow = 10;
    private static readonly TimeSpan WindowDuration = TimeSpan.FromHours(1);

    private readonly ConcurrentDictionary<int, RateLimitEntry> _entries = new();
    private readonly IDateTimeProvider _dateTimeProvider;

    /// <summary>Initializes a new instance of the <see cref="InMemoryShorfahSectionGenerationRateLimiter"/> class.</summary>
    /// <param name="dateTimeProvider">The injectable clock.</param>
    public InMemoryShorfahSectionGenerationRateLimiter(IDateTimeProvider dateTimeProvider)
    {
        _dateTimeProvider = dateTimeProvider;
    }

    /// <inheritdoc />
    public bool TryConsume(int userId)
    {
        DateTimeOffset now = _dateTimeProvider.UtcNow;

        while (true)
        {
            if (!_entries.TryGetValue(userId, out RateLimitEntry? current) || now > current.ResetAt)
            {
                var fresh = new RateLimitEntry(1, now + WindowDuration);
                if (current is null ? _entries.TryAdd(userId, fresh) : _entries.TryUpdate(userId, fresh, current))
                {
                    return true;
                }

                continue;
            }

            if (current.Count >= MaxGenerationsPerWindow)
            {
                return false;
            }

            RateLimitEntry incremented = current with { Count = current.Count + 1 };
            if (_entries.TryUpdate(userId, incremented, current))
            {
                return true;
            }
        }
    }

    private sealed record RateLimitEntry(int Count, DateTimeOffset ResetAt);
}
