namespace Icbank.Platform.Application.AiYear.Queries;

/// <summary>The archive manifest.</summary>
/// <param name="ActivationTitle">The activation's title, used to build the ZIP's display filename.</param>
/// <param name="Entries">The sanitized entry name to object path map, in display order.</param>
public sealed record AiYearActivationMediaArchiveDto(string ActivationTitle, IReadOnlyList<AiYearArchiveEntryDto> Entries);
