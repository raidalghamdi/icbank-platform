namespace Icbank.Platform.Application.Ping;

/// <summary>Response DTO for <see cref="GetPingQuery"/> (R-BE-096: DTOs are records).</summary>
/// <param name="Message">A static acknowledgement message.</param>
/// <param name="ServerTimeUtc">The server's current UTC time, proving the handler actually ran.</param>
public sealed record PingResponse(string Message, DateTime ServerTimeUtc);
