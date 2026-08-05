using System.IO.Compression;
using Icbank.Platform.Application.Common.Interfaces;
using Icbank.Platform.Application.Storage;

namespace Icbank.Platform.Application.AiYear;

/// <summary>
/// Streams an AI Year activation's media into a ZIP archive written directly to the caller's
/// output stream (the API controller's <c>Response.Body</c>), replacing the Wave 2 deferral where
/// <c>GET /ai-year/activations/{id}/zip</c> returned only the manifest JSON. Ports the Node
/// source's <c>archiver</c> semantics (<c>ai-year.ts:360-437</c>): each entry is added as its
/// storage object is read, a missing object is skipped (not fatal -- matches Node's
/// per-file <c>ObjectNotFoundError</c> catch-and-continue) so one bad row does not blow up an
/// otherwise-good export, and any other storage failure aborts the whole archive. Because
/// <see cref="ZipArchive"/> writes compressed bytes to its destination stream as each
/// entry is added -- it does not materialize the full archive in memory first -- the aggregate
/// export is never buffered whole, satisfying "stream, don't buffer the whole archive" even
/// though each individual media object is still read fully into memory by
/// <see cref="IObjectStorageReader"/> (a deliberate, documented scope boundary: see
/// RENDERING-NOTES.md -- individual media files are small images/documents, not multi-gigabyte
/// blobs, so per-object buffering is an acceptable, already-existing constraint of that port).
/// </summary>
public sealed class AiYearArchiveStreamWriter
{
    private static readonly string[] AllowedPrefixes = { "ai-year/2026/" };

    private readonly ISafeStoragePathValidator _pathValidator;
    private readonly IObjectStorageReader _storageReader;

    /// <summary>Initializes a new instance of the <see cref="AiYearArchiveStreamWriter"/> class.</summary>
    /// <param name="pathValidator">The traversal-safe path validator (closes SEC-17).</param>
    /// <param name="storageReader">The object-storage read port.</param>
    public AiYearArchiveStreamWriter(ISafeStoragePathValidator pathValidator, IObjectStorageReader storageReader)
    {
        _pathValidator = pathValidator;
        _storageReader = storageReader;
    }

    /// <summary>Writes every entry's backing object into a ZIP archive on <paramref name="destination"/>.</summary>
    /// <param name="entries">The manifest entries (sanitized name + storage object path) to include.</param>
    /// <param name="destination">The output stream the ZIP is written to (never buffered whole in memory).</param>
    /// <param name="cancellationToken">A token used to observe cancellation requests.</param>
    /// <returns>The count of entries actually written (objects that were found and copied).</returns>
    public async Task<int> WriteAsync(
        IReadOnlyList<AiYear.Queries.AiYearArchiveEntryDto> entries, Stream destination, CancellationToken cancellationToken)
    {
        var writtenCount = 0;
        using var archive = new ZipArchive(destination, ZipArchiveMode.Create, leaveOpen: true);

        foreach (AiYear.Queries.AiYearArchiveEntryDto entry in entries)
        {
            SafePathValidationResult validation = _pathValidator.Validate(entry.ObjectPath, AllowedPrefixes);
            if (!validation.IsValid)
            {
                // Why: a manifest path that fails safety validation is treated the same as a
                // missing object (skip, keep going) -- it should never have been in the manifest,
                // but refusing to serve it must not abort otherwise-valid entries.
                continue;
            }

            StoredObject? stored = await _storageReader.OpenAsync(validation.NormalizedPath!, cancellationToken);
            if (stored is null)
            {
                // Why: ports the Node source's per-file ObjectNotFoundError catch-and-continue
                // (ai-year.ts ~L400) -- one missing media object should not fail the whole export.
                continue;
            }

            ZipArchiveEntry zipEntry = archive.CreateEntry(entry.EntryName, CompressionLevel.Optimal);
            await using Stream entryStream = zipEntry.Open();
            await entryStream.WriteAsync(stored.Content, cancellationToken);
            writtenCount++;
        }

        return writtenCount;
    }
}
