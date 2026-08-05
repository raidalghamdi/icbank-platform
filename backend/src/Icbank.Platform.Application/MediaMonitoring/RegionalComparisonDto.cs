namespace Icbank.Platform.Application.MediaMonitoring;

/// <summary>Read model for <see cref="Domain.MediaMonitoring.RegionalComparison"/>.</summary>
public sealed record RegionalComparisonDto(string Authority, string Country, int Mentions, string Tone, string Highlights);
