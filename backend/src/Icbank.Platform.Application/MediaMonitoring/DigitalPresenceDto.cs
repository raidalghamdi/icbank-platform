namespace Icbank.Platform.Application.MediaMonitoring;

/// <summary>Read model for <see cref="Domain.MediaMonitoring.DigitalPresence"/>.</summary>
public sealed record DigitalPresenceDto(IReadOnlyList<DigitalPresencePlatformDto> Platforms, IReadOnlyList<DigitalPresenceHashtagDto> Hashtags);
