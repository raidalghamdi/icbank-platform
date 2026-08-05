using Icbank.Platform.Domain.Common;

namespace Icbank.Platform.Domain.AiYear;

/// <summary>Free-form key/value performance metric for an activation (DATA-MODEL.md section 3.2 <c>ai_year_metrics</c>).</summary>
public sealed class AiYearMetric : AuditableEntity
{
    /// <summary>Gets or sets the owning activation's id.</summary>
    public int ActivationId { get; set; }

    /// <summary>Gets or sets the activation navigation property.</summary>
    public AiYearActivation Activation { get; set; } = null!;

    /// <summary>Gets or sets the metric key.</summary>
    public string MetricKey { get; set; } = string.Empty;

    /// <summary>Gets or sets the metric value, stored as text even when numeric (matches source fidelity).</summary>
    public string? MetricValue { get; set; }
}
