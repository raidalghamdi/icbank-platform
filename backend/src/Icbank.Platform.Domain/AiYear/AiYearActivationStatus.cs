namespace Icbank.Platform.Domain.AiYear;

/// <summary>
/// Lifecycle status of an AI Year activation (DATA-MODEL.md section 5). The source system only
/// ever sets <see cref="Published"/> today; the other values are reserved for future use.
/// </summary>
public enum AiYearActivationStatus
{
    /// <summary>The activation is live/published.</summary>
    Published = 0,

    /// <summary>The activation is a draft, not yet published.</summary>
    Draft = 1,

    /// <summary>The activation has been archived.</summary>
    Archived = 2,
}
