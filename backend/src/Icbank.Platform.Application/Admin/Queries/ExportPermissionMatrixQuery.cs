using Icbank.Platform.Application.Common.Models;
using MediatR;

namespace Icbank.Platform.Application.Admin.Queries;

/// <summary>
/// Exports the full permission matrix (API-SURFACE.md §5 <c>GET /admin/matrix/export</c>). Per
/// DOTNET-CONVENTIONS.md §8's interpretation of R-BE-033 vs. export needs, this is a
/// purpose-built, uncapped export endpoint (not the interactive paginated
/// <see cref="GetEffectivePermissionMatrixQuery"/>) — still authorization-scoped identically
/// (super-admin only) and still closes SEC-16 by construction (it reads every row, so there is no
/// client-suppliable id to misuse).
/// </summary>
/// <param name="Format">Either <c>csv</c> or <c>json</c>.</param>
public sealed record ExportPermissionMatrixQuery(string Format) : IRequest<Result<PermissionMatrixExportDto>>;
