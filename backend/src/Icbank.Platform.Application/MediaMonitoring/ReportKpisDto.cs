namespace Icbank.Platform.Application.MediaMonitoring;

/// <summary>Read model for <see cref="Domain.MediaMonitoring.ReportKpis"/>.</summary>
/// <param name="TotalNews">The total news item count, if known.</param>
/// <param name="PositivePercent">The positive-sentiment percentage, if known.</param>
/// <param name="MediaOutlets">The distinct media outlet count, if known.</param>
/// <param name="KeyTopics">The distinct key-topic count, if known.</param>
/// <param name="Reach">The free-text reach figure.</param>
/// <param name="AlertsCount">The alert count, if known.</param>
public sealed record ReportKpisDto(int? TotalNews, int? PositivePercent, int? MediaOutlets, int? KeyTopics, string? Reach, int? AlertsCount);
