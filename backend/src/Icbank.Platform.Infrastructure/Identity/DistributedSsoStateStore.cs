using System.Text.Json;
using Icbank.Platform.Application.Auth;
using Microsoft.Extensions.Caching.Distributed;

namespace Icbank.Platform.Infrastructure.Identity;

/// <summary>
/// <see cref="IDistributedCache"/>-backed PKCE state store (BUSINESS-RULES.md §11.2 flags the old
/// system's in-memory, non-distributed <c>Map</c> as a real horizontal-scaling constraint —
/// AMBIGUOUS-BR-9). Registering <c>AddDistributedMemoryCache()</c> keeps single-instance
/// deployments working out of the box; swapping in a Redis-backed <c>IDistributedCache</c> for a
/// multi-instance deployment requires only a DI registration change, not a code change — see
/// AUTH-PORT-NOTES.md.
/// </summary>
public sealed class DistributedSsoStateStore : ISsoStateStore
{
    private const string KeyPrefix = "sso-state:";
    private static readonly TimeSpan StateTtl = TimeSpan.FromMinutes(10);

    private readonly IDistributedCache _cache;

    /// <summary>Initializes a new instance of the <see cref="DistributedSsoStateStore"/> class.</summary>
    /// <param name="cache">The distributed cache backing store.</param>
    public DistributedSsoStateStore(IDistributedCache cache)
    {
        _cache = cache;
    }

    /// <inheritdoc />
    public async Task SaveAsync(string state, string codeVerifier, string redirectTarget, CancellationToken cancellationToken)
    {
        var entry = new StateEntry(codeVerifier, redirectTarget);
        var json = JsonSerializer.Serialize(entry);

        await _cache.SetStringAsync(
            KeyPrefix + state,
            json,
            new DistributedCacheEntryOptions { AbsoluteExpirationRelativeToNow = StateTtl },
            cancellationToken);
    }

    /// <inheritdoc />
    public async Task<(string CodeVerifier, string RedirectTarget)?> ConsumeAsync(string state, CancellationToken cancellationToken)
    {
        var key = KeyPrefix + state;
        var json = await _cache.GetStringAsync(key, cancellationToken);
        if (json is null)
        {
            return null;
        }

        await _cache.RemoveAsync(key, cancellationToken);

        StateEntry? entry = JsonSerializer.Deserialize<StateEntry>(json);
        return entry is null ? null : (entry.CodeVerifier, entry.RedirectTarget);
    }

    private sealed record StateEntry(string CodeVerifier, string RedirectTarget);
}
