using Icbank.Platform.Application.Common.Models;
using MediatR;

namespace Icbank.Platform.Application.Gac.Commands;

/// <summary>
/// Ports <c>POST /gac/publications/reseed</c> (API-SURFACE.md §12). Admin-only. The Node source
/// reads bundled PDFs from local disk (<c>assets/gac-publications</c>) and uploads each to
/// Supabase Storage; this port does not re-implement bundled-asset file I/O or real object
/// storage (neither exists in <c>backend/</c> — see WAVE2-PORT-NOTES.md), so the caller supplies
/// the already-known publication metadata (including a pre-issued <c>fileUrl</c>) directly. The
/// idempotency-by-<c>titleAr</c> rule is preserved exactly.
/// </summary>
/// <param name="ActorUserId">The admin performing the reseed, for the audit-log write.</param>
/// <param name="Publications">The publication metadata rows to idempotently insert.</param>
public sealed record ReseedGacPublicationsCommand(int ActorUserId, IReadOnlyList<ReseedGacPublicationItem> Publications)
    : IRequest<Result<ReseedGacPublicationsResult>>;
