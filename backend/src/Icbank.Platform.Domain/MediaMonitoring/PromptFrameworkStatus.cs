namespace Icbank.Platform.Domain.MediaMonitoring;

/// <summary>
/// Lifecycle status of a prompt framework (DATA-MODEL.md section 5). The source system only
/// ever sets <see cref="Active"/> today; the other values are reserved for future use.
/// </summary>
public enum PromptFrameworkStatus
{
    /// <summary>Active and usable.</summary>
    Active = 0,

    /// <summary>Retired, no longer offered.</summary>
    Retired = 1,
}
