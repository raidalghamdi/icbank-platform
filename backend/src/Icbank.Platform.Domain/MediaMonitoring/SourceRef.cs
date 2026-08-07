namespace Icbank.Platform.Domain.MediaMonitoring;

/// <summary>One entry of <c>final_media_reports.sources</c> (DATA-MODEL.md section 6).</summary>
public sealed class SourceRef
{
    /// <summary>Gets or sets the source name.</summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>Gets or sets the source URL.</summary>
    public string Url { get; set; } = string.Empty;

    /// <summary>Gets or sets an optional description.</summary>
    public string? Description { get; set; }
}
