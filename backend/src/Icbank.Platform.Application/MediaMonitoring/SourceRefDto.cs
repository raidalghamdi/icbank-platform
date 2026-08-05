namespace Icbank.Platform.Application.MediaMonitoring;

/// <summary>Read model for <see cref="Domain.MediaMonitoring.SourceRef"/>.</summary>
public sealed record SourceRefDto(string Name, string Url, string? Description);
