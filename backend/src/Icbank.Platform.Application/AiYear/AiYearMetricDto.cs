namespace Icbank.Platform.Application.AiYear;

/// <summary>Ports a single row of <c>ai_year_metrics</c> (API-SURFACE.md §13).</summary>
/// <param name="Id">The metric row id.</param>
/// <param name="MetricKey">The metric key.</param>
/// <param name="MetricValue">The metric value, stored as text even when numeric.</param>
public sealed record AiYearMetricDto(int Id, string MetricKey, string? MetricValue);
