namespace Icbank.Platform.Application.MediaMonitoring;

/// <summary>Read model for <see cref="Domain.MediaMonitoring.Recommendation"/>.</summary>
public sealed record RecommendationDto(string Title, string Description, string Priority, string Responsible, string Kpi, string Deadline, string Dependencies);
