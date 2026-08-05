namespace Icbank.Platform.Application.MediaMonitoring;

/// <summary>Read model for <see cref="Domain.MediaMonitoring.AlertItem"/>.</summary>
public sealed record AlertItemDto(string Alert, string SuggestedPosition);
