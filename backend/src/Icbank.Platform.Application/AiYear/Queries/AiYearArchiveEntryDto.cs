namespace Icbank.Platform.Application.AiYear.Queries;

/// <summary>One ZIP entry.</summary>
/// <param name="EntryName">The sanitized in-archive file name (<c>[\w.\-]</c> only, ported verbatim from the Node source's sanitization rule).</param>
/// <param name="ObjectPath">The backing storage object path.</param>
public sealed record AiYearArchiveEntryDto(string EntryName, string ObjectPath);
