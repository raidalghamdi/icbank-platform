namespace Icbank.Platform.Infrastructure.Designs;

/// <summary>One ordered piece of parsed body content before it is HTML-encoded for rendering.</summary>
internal sealed record IconEventParagraphBlock(string Kind, string Content, IReadOnlyList<string>? Items = null);
