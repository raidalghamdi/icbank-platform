namespace Icbank.Platform.Domain.MediaMonitoring;

/// <summary>Category of a prompt framework (DATA-MODEL.md section 5).</summary>
public enum PromptFrameworkCategory
{
    /// <summary>Media-report generation prompts.</summary>
    MediaReport = 0,

    /// <summary>Content-creation prompts.</summary>
    ContentCreation = 1,

    /// <summary>Analysis prompts.</summary>
    Analysis = 2,

    /// <summary>Summarization prompts.</summary>
    Summarization = 3,

    /// <summary>Rewriting prompts.</summary>
    Rewriting = 4,

    /// <summary>Insights prompts.</summary>
    Insights = 5,
}
