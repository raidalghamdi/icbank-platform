using Icbank.Platform.Application.Common.Models;
using MediatR;

namespace Icbank.Platform.Application.Shorfah.Commands;

/// <summary>Command for <c>DELETE /shorfah/permissions/{id}</c> (BUSINESS-RULES.md §1.4). Ports <c>shorfah.ts:529-533</c>.</summary>
/// <param name="ActorUserId">The authenticated admin's id.</param>
/// <param name="PermissionId">The permission grant being revoked.</param>
public sealed record RevokeShorfahSectionPermissionCommand(int ActorUserId, int PermissionId) : IRequest<Result<bool>>;
