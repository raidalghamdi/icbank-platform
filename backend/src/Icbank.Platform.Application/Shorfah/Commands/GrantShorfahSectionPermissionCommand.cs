using Icbank.Platform.Application.Common.Models;
using MediatR;

namespace Icbank.Platform.Application.Shorfah.Commands;

/// <summary>Command for <c>POST /shorfah/sections/{id}/permissions</c> (BUSINESS-RULES.md §1.4). Ports <c>shorfah.ts:516-528</c>.</summary>
/// <param name="ActorUserId">The authenticated admin's id.</param>
/// <param name="SectionId">The section being granted access to.</param>
/// <param name="UserId">The granted user's id, mutually exclusive with <paramref name="RoleName"/>.</param>
/// <param name="RoleName">The granted role name, mutually exclusive with <paramref name="UserId"/>.</param>
/// <param name="Permission">The permission verb being granted.</param>
public sealed record GrantShorfahSectionPermissionCommand(
    int ActorUserId, int SectionId, int? UserId, string? RoleName, string Permission) : IRequest<Result<ShorfahSectionPermissionDto>>;
