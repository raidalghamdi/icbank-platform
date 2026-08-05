namespace Icbank.Platform.Api.Controllers;

/// <summary>Request body for <c>POST /week-start/generate</c>.</summary>
public sealed class GenerateWeekStartMessagesRequest
{
    /// <summary>Gets or sets the message topic.</summary>
    public string Topic { get; set; } = string.Empty;

    /// <summary>Gets or sets the occasion, if any.</summary>
    public string? Occasion { get; set; }

    /// <summary>Gets or sets the target audience, if any.</summary>
    public string? Audience { get; set; }

    /// <summary>Gets or sets the desired tone.</summary>
    public string? Tone { get; set; }

    /// <summary>Gets or sets the desired length option.</summary>
    public string? Length { get; set; }
}
