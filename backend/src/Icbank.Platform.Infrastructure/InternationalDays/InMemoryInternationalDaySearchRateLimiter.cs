using System.Collections.Concurrent;
using Icbank.Platform.Application.Common.Interfaces;
using Icbank.Platform.Application.InternationalDays;

namespace Icbank.Platform.Infrastructure.InternationalDays;

/// <summary>
/// In-memory <see cref="IInternationalDaySearchRateLimiter"/>, matching the Node source's actual
/// per-instance behavior (BUSINESS-RULES.md §4.1) via a thread-safe <see cref="ConcurrentDictionary{TKey,TValue}"/>.
/// Registered as a singleton so the window state survives across requests within one process.
/// Does not coordinate across multiple instances -- see the interface XML doc and
/// WAVE2-PORT-NOTES.md (AMBIGUOUS-BR-5) for the distributed-store follow-up.
/// </summary>
public sealed class InMemoryInternationalDaySearchRateLimiter : IInternationalDaySearchRateLimiter
{
    private const int MaxSearchesPerWindow = 10;
    private static readonly TimeSpan WindowDuration = TimeSpan.FromHours(1);

    private readonly ConcurrentDictionary<string, RateLimitEntry> _entries = new(StringComparer.Ordinal);
    private readonly IDateTimeProvider _dateTimeProvider;

    /// <summary>Initializes a new instance of the <see cref="InMemoryInternationalDaySearchRateLimiter"/> class.</summary>
    /// <param name="dateTimeProvider">The injectable clock.</param>
    public InMemoryInternationalDaySearchRateLimiter(IDateTimeProvider dateTimeProvider)
    {
        _dateTimeProvider = dateTimeProvider;
    }

    /// <inheritdoc />
    public bool TryConsume(string ipAddress)
    {
        DateTimeOffset now = _dateTimeProvider.UtcNow;

        while (true)
        {
            if (!_entries.TryGetValue(ipAddress, out RateLimitEntry? current) || now > current.ResetAt)
            {
                var fresh = new RateLimitEntry(1, now + WindowDuration);
                if (current is null ? _entries.TryAdd(ipAddress, fresh) : _entries.TryUpdate(ipAddress, fresh, current))
                {
                    return true;
                }

                continue;
            }

            if (current.Count >= MaxSearchesPerWindow)
            {
                return false;
            }

            RateLimitEntry incremented = current with { Count = current.Count + 1 };
            if (_entries.TryUpdate(ipAddress, incremented, current))
            {
                return true;
            }
        }
    }

    /// <inheritdoc />
    public int GetRemaining(string ipAddress)
    {
        DateTimeOffset now = _dateTimeProvider.UtcNow;
        if (!_entries.TryGetValue(ipAddress, out RateLimitEntry? entry) || now > entry.ResetAt)
        {
            return MaxSearchesPerWindow;
        }

        return Math.Max(0, MaxSearchesPerWindow - entry.Count);
    }

    private sealed record RateLimitEntry(int Count, DateTimeOffset ResetAt);
}
