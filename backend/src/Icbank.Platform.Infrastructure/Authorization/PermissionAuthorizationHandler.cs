using System.Security.Claims;
using Icbank.Platform.Domain.Identity;
using Microsoft.AspNetCore.Authorization;

namespace Icbank.Platform.Infrastructure.Authorization;

/// <summary>
/// The single handler backing every generated <c>{pageSlug}:{verb}</c> policy
/// (DOTNET-CONVENTIONS.md §5.4). Reads the effective permission set directly from the access
/// token's <c>permission</c> claims — computed once at login/refresh time by
/// <c>IPermissionResolver</c> — rather than re-querying the database on every authorized request.
/// <c>super_admin</c> (and only <c>super_admin</c>) implicitly satisfies every page/verb
/// requirement, matching BUSINESS-RULES.md §10.1's "full, non-restrictable privileges" intent —
/// but a plain <c>admin</c> does NOT get this bypass, which is the fix for SEC-01.
/// </summary>
public sealed class PermissionAuthorizationHandler : AuthorizationHandler<PermissionRequirement>
{
    private const string PermissionClaimType = "permission";
    private const string SuperAdminClaimType = "is_super_admin";

    /// <inheritdoc />
    protected override Task HandleRequirementAsync(AuthorizationHandlerContext context, PermissionRequirement requirement)
    {
        if (IsSuperAdmin(context.User))
        {
            context.Succeed(requirement);
            return Task.CompletedTask;
        }

        var policyKey = PermissionRequirementFactory.BuildPolicyName(requirement.PageSlug, requirement.Verb);
        var granted = context.User.FindAll(PermissionClaimType).Any(claim => claim.Value == policyKey);

        if (granted)
        {
            context.Succeed(requirement);
        }

        return Task.CompletedTask;
    }

    private static bool IsSuperAdmin(ClaimsPrincipal user) =>
        user.FindFirst(SuperAdminClaimType)?.Value == bool.TrueString;
}
