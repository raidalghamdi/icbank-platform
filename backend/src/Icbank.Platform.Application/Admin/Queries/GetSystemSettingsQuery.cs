using Icbank.Platform.Application.Common.Models;
using MediatR;

namespace Icbank.Platform.Application.Admin.Queries;

/// <summary>
/// Reads system settings (API-SURFACE.md §5 <c>GET /admin/settings</c>). Closes the old system's
/// plaintext-secret-exposure gap ("Returns <c>azure_ad_client_secret</c> in plaintext to any
/// admin") — <see cref="SystemSettingsSchema.SecretKeys"/> are always masked in the response, this
/// port never has an unmasked read path at all.
/// </summary>
public sealed record GetSystemSettingsQuery : IRequest<Result<IReadOnlyDictionary<string, string>>>;
