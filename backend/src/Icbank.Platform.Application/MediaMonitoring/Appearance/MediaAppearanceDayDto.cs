namespace Icbank.Platform.Application.MediaMonitoring.Appearance;

/// <summary>One Riyadh-local day on the appearance trend line.</summary>
/// <param name="Date">The Riyadh-local day in ISO <c>yyyy-MM-dd</c> form.</param>
/// <param name="Appearances">The number of monitored items published on that day.</param>
public sealed record MediaAppearanceDayDto(string Date, int Appearances);
