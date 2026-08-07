namespace Icbank.Platform.Application.Gac.Commands;

/// <summary>The news ingest outcome summary.</summary>
/// <param name="Inserted">The number of newly inserted items.</param>
/// <param name="Updated">The number of existing items refreshed by URL match.</param>
/// <param name="Skipped">The number of items dropped as duplicates within the submitted batch itself.</param>
public sealed record IngestGacNewsItemsResult(int Inserted, int Updated, int Skipped);
