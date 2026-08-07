using Icbank.Platform.Application.Common.Models;
using MediatR;

namespace Icbank.Platform.Application.Weekend.Commands;

/// <summary>
/// Ports <c>POST /week-start/upload</c> (API-SURFACE.md §8, BUSINESS-RULES.md §2.5). Extracts
/// text per file and archives it immediately; the style-profile recompute runs synchronously
/// after the archive writes complete (the Node source deferred this via a non-awaited
/// <c>setImmediate</c> — R-BE-060 forbids that fire-and-forget pattern, so this port awaits it
/// inline as a deliberate reliability improvement). Embedding backfill is deferred — no
/// embedding provider is wired in this port, see WAVE1-PORT-NOTES.md.
/// </summary>
/// <param name="ActorUserId">The uploading user's id.</param>
/// <param name="Files">The uploaded files.</param>
public sealed record UploadArchiveDocumentsCommand(int ActorUserId, IReadOnlyList<UploadedDocument> Files)
    : IRequest<Result<UploadArchiveDocumentsResultDto>>;
