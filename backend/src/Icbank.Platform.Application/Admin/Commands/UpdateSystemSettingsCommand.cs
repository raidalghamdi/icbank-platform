using Icbank.Platform.Application.Common.Models;
using MediatR;

namespace Icbank.Platform.Application.Admin.Commands;

/// <summary>
/// Updates system settings (API-SURFACE.md §5 <c>PUT /admin/settings</c>). Every key is validated
/// against <see cref="SystemSettingsSchema.AllKeys"/> before any write — an unrecognized key fails
/// the whole request rather than silently writing an arbitrary row.
/// </summary>
/// <param name="ActorUserId">The id of the super-admin performing the change (for audit).</param>
/// <param name="Settings">The key/value pairs to upsert.</param>
public sealed record UpdateSystemSettingsCommand(int ActorUserId, IReadOnlyDictionary<string, string> Settings) : IRequest<Result<bool>>;
