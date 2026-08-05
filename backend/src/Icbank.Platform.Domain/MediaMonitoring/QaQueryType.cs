namespace Icbank.Platform.Domain.MediaMonitoring;

/// <summary>Kind of a QA/search query recorded against final reports (DATA-MODEL.md section 5).</summary>
public enum QaQueryType
{
    /// <summary>A guided wizard query.</summary>
    Wizard = 0,

    /// <summary>A full-text search query.</summary>
    SearchFull = 1,

    /// <summary>An informational search query.</summary>
    SearchInfo = 2,
}
