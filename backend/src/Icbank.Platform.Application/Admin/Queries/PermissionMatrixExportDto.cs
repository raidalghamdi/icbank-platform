namespace Icbank.Platform.Application.Admin.Queries;

/// <summary>The rendered export payload.</summary>
/// <param name="ContentType">The MIME type of <paramref name="Content"/>.</param>
/// <param name="FileName">The suggested download file name.</param>
/// <param name="Content">The rendered bytes (UTF-8 with BOM for CSV, matching the old system's Excel-compatible export).</param>
public sealed record PermissionMatrixExportDto(string ContentType, string FileName, byte[] Content);
