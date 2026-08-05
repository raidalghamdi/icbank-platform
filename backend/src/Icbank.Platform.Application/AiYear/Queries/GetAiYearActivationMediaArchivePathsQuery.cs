using Icbank.Platform.Application.Common.Models;
using MediatR;

namespace Icbank.Platform.Application.AiYear.Queries;

/// <summary>
/// Ports the data-assembly half of <c>GET /ai-year/activations/:id/zip</c> (API-SURFACE.md §13):
/// looks up the activation and its media rows and returns the object paths a caller would need to
/// stream into a ZIP archive. The actual ZIP streaming (Node source used <c>archiver</c> piping
/// directly from Supabase Storage) is deferred -- no ZIP-streaming or real object-storage
/// dependency exists in <c>backend/</c> yet (see WAVE2-PORT-NOTES.md, following the Wave 1
/// storage-deferral pattern). This query still fully exercises the lookup/404 semantics and
/// filename-sanitization rule so the endpoint's business logic is provable today.
/// </summary>
/// <param name="ActivationId">The activation id.</param>
public sealed record GetAiYearActivationMediaArchivePathsQuery(int ActivationId) : IRequest<Result<AiYearActivationMediaArchiveDto>>;
