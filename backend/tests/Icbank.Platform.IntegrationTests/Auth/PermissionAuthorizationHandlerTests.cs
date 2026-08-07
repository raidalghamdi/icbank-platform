using System.Security.Claims;
using FluentAssertions;
using Icbank.Platform.Domain.Identity;
using Icbank.Platform.Infrastructure.Authorization;
using Microsoft.AspNetCore.Authorization;
using Xunit;

namespace Icbank.Platform.IntegrationTests.Auth;

/// <summary>
/// Unit-style tests for <see cref="PermissionAuthorizationHandler"/> and
/// <see cref="SuperAdminAuthorizationHandler"/> (task requirement 8: "unit tests for ... the
/// policy handler", plus the explicit SEC-01 regression test). Lives in the integration-test
/// project because these handlers are Infrastructure types.
/// </summary>
public sealed class PermissionAuthorizationHandlerTests
{
    private static readonly string[] ShorfahEditClaim = { "shorfah:edit" };
    private static readonly string[] ShorfahViewClaim = { "shorfah:view" };

    [Fact]
    public async Task HandleRequirementAsync_UserWithMatchingPermissionClaim_Succeeds()
    {
        var handler = new PermissionAuthorizationHandler();
        var requirement = new PermissionRequirement(PageSlugs.Shorfah, PermissionVerb.Edit);
        ClaimsPrincipal principal = BuildPrincipal(isSuperAdmin: false, permissionClaims: ShorfahEditClaim);
        AuthorizationHandlerContext context = new(new[] { requirement }, principal, null);

        await handler.HandleAsync(context);

        context.HasSucceeded.Should().BeTrue();
    }

    [Fact]
    public async Task HandleRequirementAsync_UserWithoutMatchingPermissionClaim_DoesNotSucceed()
    {
        var handler = new PermissionAuthorizationHandler();
        var requirement = new PermissionRequirement(PageSlugs.Shorfah, PermissionVerb.Delete);
        ClaimsPrincipal principal = BuildPrincipal(isSuperAdmin: false, permissionClaims: ShorfahViewClaim);
        AuthorizationHandlerContext context = new(new[] { requirement }, principal, null);

        await handler.HandleAsync(context);

        context.HasSucceeded.Should().BeFalse();
    }

    [Fact]
    public async Task HandleRequirementAsync_SuperAdminClaim_BypassesEveryPermissionCheck()
    {
        var handler = new PermissionAuthorizationHandler();
        var requirement = new PermissionRequirement(PageSlugs.AdminPanel, PermissionVerb.Delete);
        ClaimsPrincipal principal = BuildPrincipal(isSuperAdmin: true, permissionClaims: Array.Empty<string>());
        AuthorizationHandlerContext context = new(new[] { requirement }, principal, null);

        await handler.HandleAsync(context);

        context.HasSucceeded.Should().BeTrue();
    }

    [Fact]
    public async Task HandleRequirementAsync_PlainAdminWithoutSuperAdminClaim_DoesNotBypassPermissionCheck()
    {
        // Why: this is the explicit SEC-01 regression test at the authorization-handler layer —
        // a plain admin role claim, with no permission grant and no super-admin claim, must not
        // be treated as an implicit bypass the way the old system's `isSuperAdmin || role ===
        // "admin"` short-circuit did.
        var handler = new PermissionAuthorizationHandler();
        var requirement = new PermissionRequirement(PageSlugs.AdminPanel, PermissionVerb.Delete);
        ClaimsPrincipal principal = BuildPrincipal(isSuperAdmin: false, permissionClaims: Array.Empty<string>(), roleClaim: "admin");
        AuthorizationHandlerContext context = new(new[] { requirement }, principal, null);

        await handler.HandleAsync(context);

        context.HasSucceeded.Should().BeFalse();
    }

    [Fact]
    public async Task SuperAdminHandler_UserWithoutSuperAdminClaim_DoesNotSucceed()
    {
        // Why: the second half of the SEC-01 regression proof — the distinct super-admin policy
        // itself must reject a plain admin, since this is the policy gating role assignment and
        // permission-matrix edits.
        var handler = new SuperAdminAuthorizationHandler();
        AuthorizationHandlerContext context = new(
            new[] { SuperAdminRequirement.Instance }, BuildPrincipal(isSuperAdmin: false, permissionClaims: Array.Empty<string>(), roleClaim: "admin"), null);

        await handler.HandleAsync(context);

        context.HasSucceeded.Should().BeFalse();
    }

    [Fact]
    public async Task SuperAdminHandler_UserWithSuperAdminClaim_Succeeds()
    {
        var handler = new SuperAdminAuthorizationHandler();
        AuthorizationHandlerContext context = new(
            new[] { SuperAdminRequirement.Instance }, BuildPrincipal(isSuperAdmin: true, permissionClaims: Array.Empty<string>()), null);

        await handler.HandleAsync(context);

        context.HasSucceeded.Should().BeTrue();
    }

    private static ClaimsPrincipal BuildPrincipal(bool isSuperAdmin, IEnumerable<string> permissionClaims, string? roleClaim = null)
    {
        List<Claim> claims = new() { new Claim("is_super_admin", isSuperAdmin ? bool.TrueString : bool.FalseString) };
        claims.AddRange(permissionClaims.Select(permission => new Claim("permission", permission)));
        if (roleClaim is not null)
        {
            claims.Add(new Claim(ClaimTypes.Role, roleClaim));
        }

        var identity = new ClaimsIdentity(claims, "TestAuth");
        return new ClaimsPrincipal(identity);
    }
}
