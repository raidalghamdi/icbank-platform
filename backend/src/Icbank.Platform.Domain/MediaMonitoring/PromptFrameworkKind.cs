namespace Icbank.Platform.Domain.MediaMonitoring;

/// <summary>Whether a prompt framework row is a structural framework or a ready-to-copy template (DATA-MODEL.md section 5).</summary>
public enum PromptFrameworkKind
{
    /// <summary>A structural framework.</summary>
    Framework = 0,

    /// <summary>A ready-to-copy template.</summary>
    Template = 1,
}
