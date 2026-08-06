namespace Icbank.Platform.Application.Admin.Queries;

/// <summary>
/// The result of <see cref="ExportActivityLogQuery"/>: the already-filtered, already-capped,
/// newest-first rows ready for CSV rendering. Deliberately carries rows rather than a
/// pre-rendered byte array — CSV rendering (escaping, BOM, streaming to the response body) is an
/// API-layer concern (the admin controller writes it via a streaming <c>IActionResult</c> so the
/// whole file is never buffered in memory at once).
/// </summary>
/// <param name="Rows">The rows to render, in the exact order they must appear in the file (newest first).</param>
/// <param name="TotalMatched">
/// How many rows matched the filter before the <see cref="ExportActivityLogQueryHandler.MaxRows"/>
/// cap was applied — lets the caller/audit entry distinguish "exported everything" from
/// "truncated at the cap".
/// </param>
public sealed record ActivityLogExportDto(IReadOnlyList<ActivityLogExportRow> Rows, int TotalMatched);
