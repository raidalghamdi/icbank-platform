namespace Icbank.Platform.Application.AiYear.Commands;

/// <summary>One metric item supplied when creating/updating an activation.</summary>
/// <param name="MetricKey">The metric key.</param>
/// <param name="MetricValue">The metric value, stored as text even when numeric.</param>
public sealed record CreateAiYearActivationMetricItem(string MetricKey, string? MetricValue);
