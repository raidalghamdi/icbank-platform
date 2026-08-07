using Icbank.Platform.Domain.Designs;

namespace Icbank.Platform.Application.Designs.IconEvent.Commands;

/// <summary>
/// The content and rendering context shared by every variant of a single icon-event design
/// request. Introduced so variant assembly does not need to thread fourteen positional
/// parameters through each call.
/// </summary>
internal sealed record IconEventVariantContext
{
    /// <summary>Gets the resolved headline applied to every variant.</summary>
    public required string Headline { get; init; }

    /// <summary>Gets the resolved subtitle, if any.</summary>
    public string? Subtitle { get; init; }

    /// <summary>Gets the owning department, if supplied.</summary>
    public string? Department { get; init; }

    /// <summary>Gets the campaign hashtag, if supplied.</summary>
    public string? Hashtag { get; init; }

    /// <summary>Gets the event date, if supplied.</summary>
    public string? Date { get; init; }

    /// <summary>Gets the event time, if supplied.</summary>
    public string? Time { get; init; }

    /// <summary>Gets the event location, if supplied.</summary>
    public string? Location { get; init; }

    /// <summary>Gets the resolved size preset.</summary>
    public required IconEventSizePreset Size { get; init; }

    /// <summary>Gets the HTML rendering port.</summary>
    public required IIconEventHtmlRenderer HtmlRenderer { get; init; }
}
