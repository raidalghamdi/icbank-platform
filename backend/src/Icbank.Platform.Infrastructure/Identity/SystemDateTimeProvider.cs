using Icbank.Platform.Application.Common.Interfaces;

namespace Icbank.Platform.Infrastructure.Identity;

/// <summary>
/// Default <see cref="IDateTimeProvider"/> implementation backed by the system clock. Resolves
/// the Asia/Riyadh timezone once and reuses it — Riyadh has no daylight-saving transitions
/// (UTC+3 year-round), so a cached <see cref="TimeZoneInfo"/> lookup is always correct.
/// </summary>
public sealed class SystemDateTimeProvider : IDateTimeProvider
{
    private const string IanaRiyadhId = "Asia/Riyadh";
    private const string WindowsRiyadhId = "Arab Standard Time";

    private static readonly TimeZoneInfo RiyadhTimeZone = ResolveRiyadhTimeZone();

    /// <inheritdoc />
    public DateTimeOffset UtcNow => DateTimeOffset.UtcNow;

    /// <inheritdoc />
    public DateTimeOffset RiyadhNow => TimeZoneInfo.ConvertTime(DateTimeOffset.UtcNow, RiyadhTimeZone);

    /// <summary>
    /// Resolves the Riyadh timezone by IANA id first (Linux/macOS containers), falling back to
    /// the Windows id (BUSINESS-RULES.md §2.1's fix recommendation names both explicitly).
    /// </summary>
    /// <returns>The resolved <see cref="TimeZoneInfo"/> for Asia/Riyadh.</returns>
    private static TimeZoneInfo ResolveRiyadhTimeZone()
    {
        try
        {
            return TimeZoneInfo.FindSystemTimeZoneById(IanaRiyadhId);
        }
        catch (TimeZoneNotFoundException)
        {
            return TimeZoneInfo.FindSystemTimeZoneById(WindowsRiyadhId);
        }
    }
}
