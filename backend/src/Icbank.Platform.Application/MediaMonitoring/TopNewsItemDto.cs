namespace Icbank.Platform.Application.MediaMonitoring;

/// <summary>Read model for <see cref="Domain.MediaMonitoring.TopNewsItem"/>.</summary>
public sealed record TopNewsItemDto(string Date, string Tone, string Headline, IReadOnlyList<string> Details, string Source);
