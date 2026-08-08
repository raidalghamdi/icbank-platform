namespace Icbank.Platform.Domain.Designs;

/// <summary>The shape of a piece of source copy, independent of how it will be laid out.</summary>
/// <param name="Lead">The opening copy that precedes any list or labelled section.</param>
/// <param name="Bullets">The list items, in source order.</param>
/// <param name="Sections">The labelled blocks, in source order.</param>
/// <param name="ClosingNote">A short closing line that reads as a sign-off rather than content.</param>
public sealed record IconEventTextStructure(
    string? Lead,
    IReadOnlyList<string> Bullets,
    IReadOnlyList<IconEventTextSection> Sections,
    string? ClosingNote)
{
    /// <summary>Gets the structure produced by empty input.</summary>
    public static IconEventTextStructure Empty { get; } =
        new(null, Array.Empty<string>(), Array.Empty<IconEventTextSection>(), null);

    /// <summary>Gets a value indicating whether the copy carried any list or section structure.</summary>
    public bool IsStructured => Bullets.Count > 0 || Sections.Count > 0;
}
