namespace Icbank.Platform.Application.Designs.IconEvent;

/// <summary>
/// The typed shape the AI extraction provider must return (H-2: AI JSON always deserializes into
/// a typed DTO and passes FluentValidation before use -- never raw <c>dynamic</c>/<c>JsonElement</c>
/// access). Mirrors the <c>extracted</c> object in the icon-event prompt's required JSON response
/// shape (BUSINESS-RULES.md §7.4).
/// </summary>
/// <param name="Headline">The AI-extracted headline (2-6 words).</param>
/// <param name="Subtitle">The AI-extracted subtitle, copied verbatim from the source per the prompt's text-preservation rules.</param>
/// <param name="Department">The AI-extracted department name, or empty string if not present in the input.</param>
/// <param name="Hashtag">The AI-extracted hashtag, or empty string if not present in the input.</param>
/// <param name="ContactEmail">The AI-extracted contact email, or empty string if not present.</param>
/// <param name="ContactPhone">The AI-extracted contact phone, or empty string if not present.</param>
/// <param name="Stats">The AI-extracted statistic chips (0-3 entries).</param>
public sealed record IconEventExtractedDataDto(
    string Headline,
    string Subtitle,
    string Department,
    string Hashtag,
    string ContactEmail,
    string ContactPhone,
    IReadOnlyList<IconEventStatDto> Stats);
