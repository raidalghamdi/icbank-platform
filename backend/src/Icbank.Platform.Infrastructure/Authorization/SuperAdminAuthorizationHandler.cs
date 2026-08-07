using Microsoft.AspNetCore.Authorization;

namespace Icbank.Platform.Infrastructure.Authorization;

/// <summary>
/// Handler for the distinct super-admin capability (closes SEC-01). Unlike
/// <see cref="PermissionAuthorizationHandler"/>, this handler grants no implicit bypass to
/// anything — only a token carrying <c>is_super_admin=true</c> satisfies it, and that claim is
/// only ever set by <c>IPermissionResolver</c> for a user whose role-union includes
/// <c>super_admin</c>. A plain <c>admin</c> role can never produce this claim.
/// </summary>
public sealed class SuperAdminAuthorizationHandler : AuthorizationHandler<SuperAdminRequirement>
{
    private const string SuperAdminClaimType = "is_super_admin";

    /// <inheritdoc />
    protected override Task HandleRequirementAsync(AuthorizationHandlerContext context, SuperAdminRequirement requirement)
    {
        if (context.User.FindFirst(SuperAdminClaimType)?.Value == bool.TrueString)
        {
            context.Succeed(requirement);
        }

        return Task.CompletedTask;
    }
}
