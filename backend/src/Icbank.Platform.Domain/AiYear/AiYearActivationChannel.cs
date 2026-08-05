using Icbank.Platform.Domain.Common;

namespace Icbank.Platform.Domain.AiYear;

/// <summary>
/// One distribution channel for an <see cref="AiYearActivation"/>. Normalizes the source
/// <c>ai_year_activations.channels text[]</c> Postgres array (AMBIGUOUS-2 in DATA-MODEL.md,
/// section 2) into a proper child table, since SQL Server has no native array type.
/// </summary>
public sealed class AiYearActivationChannel : AuditableEntity
{
    /// <summary>Gets or sets the owning activation's id.</summary>
    public int ActivationId { get; set; }

    /// <summary>Gets or sets the activation navigation property.</summary>
    public AiYearActivation Activation { get; set; } = null!;

    /// <summary>Gets or sets the channel name, e.g. <c>linkedin</c>, <c>twitter</c>.</summary>
    public string Channel { get; set; } = string.Empty;
}
