namespace Icbank.Platform.Domain.MediaMonitoring;

/// <summary>Typed shape for <c>reports_qa_queries.wizard_answers</c> (DATA-MODEL.md section 6).</summary>
public sealed class WizardAnswers
{
    /// <summary>Gets or sets the requested period, free text.</summary>
    public string? Period { get; set; }

    /// <summary>Gets or sets the requested audience tier, free text.</summary>
    public string? Audience { get; set; }

    /// <summary>Gets or sets the requested source list.</summary>
    public List<string> Sources { get; set; } = new();

    /// <summary>Gets or sets the requested focus topics, free text.</summary>
    public string? FocusTopics { get; set; }

    /// <summary>Gets or sets the requested output language, free text.</summary>
    public string? Language { get; set; }

    /// <summary>Gets or sets the intended recipients, free text.</summary>
    public string? Recipients { get; set; }

    /// <summary>Gets or sets the wizard mode: generate or search.</summary>
    public string? Mode { get; set; }
}
