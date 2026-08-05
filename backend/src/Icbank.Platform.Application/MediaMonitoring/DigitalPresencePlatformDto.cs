namespace Icbank.Platform.Application.MediaMonitoring;

/// <summary>Read model for <see cref="Domain.MediaMonitoring.DigitalPresencePlatform"/>.</summary>
public sealed record DigitalPresencePlatformDto(string Name, int Mentions, int Reposts, int Engagement, string Reach);
