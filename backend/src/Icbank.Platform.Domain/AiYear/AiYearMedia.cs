using Icbank.Platform.Domain.Common;

namespace Icbank.Platform.Domain.AiYear;

/// <summary>Uploaded media attached to an activation (DATA-MODEL.md section 3.2 <c>ai_year_media</c>).</summary>
public sealed class AiYearMedia : AuditableEntity
{
    /// <summary>Gets or sets the owning activation's id.</summary>
    public int ActivationId { get; set; }

    /// <summary>Gets or sets the activation navigation property.</summary>
    public AiYearActivation Activation { get; set; } = null!;

    /// <summary>Gets or sets the storage object path.</summary>
    public string ObjectPath { get; set; } = string.Empty;

    /// <summary>Gets or sets the original file name, if known.</summary>
    public string? FileName { get; set; }

    /// <summary>Gets or sets the MIME content type, if known.</summary>
    public string? ContentType { get; set; }

    /// <summary>Gets or sets the display sort order.</summary>
    public int SortOrder { get; set; }
}
