namespace Icbank.Platform.Application.MediaMonitoring;

/// <summary>Read model for <see cref="Domain.MediaMonitoring.DigitalPresenceHashtag"/>.</summary>
public sealed record DigitalPresenceHashtagDto(string Tag, int Uses, string Trend);
