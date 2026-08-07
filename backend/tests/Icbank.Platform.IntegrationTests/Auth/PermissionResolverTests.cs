using FluentAssertions;
using Icbank.Platform.Application.Common.Interfaces;
using Icbank.Platform.Domain.Identity;
using Icbank.Platform.Infrastructure.Identity;
using Icbank.Platform.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace Icbank.Platform.IntegrationTests.Auth;

/// <summary>
/// Unit-style tests for <see cref="PermissionResolver"/> against an EF Core InMemory provider
/// (task requirement 8: "unit tests for the permission resolver ... including multi-role
/// union"). Lives in the integration-test project because <c>PermissionResolver</c> is an
/// Infrastructure type (R-BE-002 forbids UnitTests referencing Infrastructure).
/// </summary>
public sealed class PermissionResolverTests
{
    [Fact]
    public async Task ResolveAsync_UserWithTwoRoles_ReturnsUnionOfBothRolesPermissions()
    {
        using AppDbContext dbContext = CreateInMemoryContext(nameof(ResolveAsync_UserWithTwoRoles_ReturnsUnionOfBothRolesPermissions));
        Page shorfahPage = new() { Slug = "shorfah", NameAr = "shorfah", CreatedBy = "test" };
        Page mediaPage = new() { Slug = "media_monitoring", NameAr = "media_monitoring", CreatedBy = "test" };
        Permission viewPermission = new() { Name = "view", NameAr = "view", CreatedBy = "test" };
        Permission editPermission = new() { Name = "edit", NameAr = "edit", CreatedBy = "test" };
        Role editorRole = new() { Name = "editor", NameAr = "editor", CreatedBy = "test" };
        Role viewerRole = new() { Name = "viewer", NameAr = "viewer", CreatedBy = "test" };
        var user = new User { Email = "multi@example.com", Name = "Multi Role", CreatedBy = "test" };

        dbContext.AddRange(shorfahPage, mediaPage, viewPermission, editPermission, editorRole, viewerRole, user);
        await dbContext.SaveChangesAsync(CancellationToken.None);

        dbContext.RolePermissions.Add(new RolePermission { RoleId = editorRole.Id, PageId = shorfahPage.Id, PermissionId = editPermission.Id, CreatedBy = "test" });
        dbContext.RolePermissions.Add(new RolePermission { RoleId = viewerRole.Id, PageId = mediaPage.Id, PermissionId = viewPermission.Id, CreatedBy = "test" });
        dbContext.UserRoles.Add(new UserRole { UserId = user.Id, RoleId = editorRole.Id, AssignedAt = DateTime.UtcNow, CreatedBy = "test" });
        dbContext.UserRoles.Add(new UserRole { UserId = user.Id, RoleId = viewerRole.Id, AssignedAt = DateTime.UtcNow, CreatedBy = "test" });
        await dbContext.SaveChangesAsync(CancellationToken.None);

        var resolver = new PermissionResolver(dbContext);

        PermissionResolution resolution = await resolver.ResolveAsync(user.Id, CancellationToken.None);

        // Why: this is the explicit regression proof for the old system's `.limit(1)` bug
        // (DOMAIN-PORT-NOTES.md) — both roles' grants must be present, not just one.
        resolution.RoleNames.Should().BeEquivalentTo("editor", "viewer");
        resolution.Permissions.Should().Contain("shorfah:edit");
        resolution.Permissions.Should().Contain("media_monitoring:view");
        resolution.IsSuperAdmin.Should().BeFalse();
    }

    [Fact]
    public async Task ResolveAsync_UserWithSuperAdminRole_SetsIsSuperAdminTrue()
    {
        using AppDbContext dbContext = CreateInMemoryContext(nameof(ResolveAsync_UserWithSuperAdminRole_SetsIsSuperAdminTrue));
        Role superAdminRole = new() { Name = "super_admin", NameAr = "super_admin", CreatedBy = "test" };
        var user = new User { Email = "super@example.com", Name = "Super Admin", CreatedBy = "test" };
        dbContext.AddRange(superAdminRole, user);
        await dbContext.SaveChangesAsync(CancellationToken.None);

        dbContext.UserRoles.Add(new UserRole { UserId = user.Id, RoleId = superAdminRole.Id, AssignedAt = DateTime.UtcNow, CreatedBy = "test" });
        await dbContext.SaveChangesAsync(CancellationToken.None);

        var resolver = new PermissionResolver(dbContext);
        PermissionResolution resolution = await resolver.ResolveAsync(user.Id, CancellationToken.None);

        resolution.IsSuperAdmin.Should().BeTrue();
    }

    [Fact]
    public async Task ResolveAsync_UserWithAdminRoleOnly_DoesNotSetIsSuperAdmin()
    {
        using AppDbContext dbContext = CreateInMemoryContext(nameof(ResolveAsync_UserWithAdminRoleOnly_DoesNotSetIsSuperAdmin));
        Role adminRole = new() { Name = "admin", NameAr = "admin", CreatedBy = "test" };
        var user = new User { Email = "admin@example.com", Name = "Plain Admin", CreatedBy = "test" };
        dbContext.AddRange(adminRole, user);
        await dbContext.SaveChangesAsync(CancellationToken.None);

        dbContext.UserRoles.Add(new UserRole { UserId = user.Id, RoleId = adminRole.Id, AssignedAt = DateTime.UtcNow, CreatedBy = "test" });
        await dbContext.SaveChangesAsync(CancellationToken.None);

        var resolver = new PermissionResolver(dbContext);
        PermissionResolution resolution = await resolver.ResolveAsync(user.Id, CancellationToken.None);

        // Why: this is the core SEC-01 regression proof at the resolver layer — a plain admin
        // must never be treated as super-admin.
        resolution.IsSuperAdmin.Should().BeFalse();
    }

    [Fact]
    public async Task ResolveAsync_UserWithDenyOverride_RemovesRoleGrantedPermission()
    {
        using AppDbContext dbContext = CreateInMemoryContext(nameof(ResolveAsync_UserWithDenyOverride_RemovesRoleGrantedPermission));
        Page page = new() { Slug = "shorfah", NameAr = "shorfah", CreatedBy = "test" };
        Permission editPermission = new() { Name = "edit", NameAr = "edit", CreatedBy = "test" };
        Role editorRole = new() { Name = "editor", NameAr = "editor", CreatedBy = "test" };
        var user = new User { Email = "denied@example.com", Name = "Denied User", CreatedBy = "test" };
        dbContext.AddRange(page, editPermission, editorRole, user);
        await dbContext.SaveChangesAsync(CancellationToken.None);

        dbContext.RolePermissions.Add(new RolePermission { RoleId = editorRole.Id, PageId = page.Id, PermissionId = editPermission.Id, CreatedBy = "test" });
        dbContext.UserRoles.Add(new UserRole { UserId = user.Id, RoleId = editorRole.Id, AssignedAt = DateTime.UtcNow, CreatedBy = "test" });
        dbContext.UserPageOverrides.Add(new UserPageOverride
        {
            UserId = user.Id,
            PageId = page.Id,
            PermissionId = editPermission.Id,
            GrantType = OverrideGrantType.Deny,
            CreatedBy = "test",
        });
        await dbContext.SaveChangesAsync(CancellationToken.None);

        var resolver = new PermissionResolver(dbContext);
        PermissionResolution resolution = await resolver.ResolveAsync(user.Id, CancellationToken.None);

        resolution.Permissions.Should().NotContain("shorfah:edit");
    }

    [Fact]
    public async Task ResolveAsync_ContradictoryOverrides_AppliesThemInAscendingIdOrder()
    {
        using AppDbContext dbContext = CreateInMemoryContext(nameof(ResolveAsync_ContradictoryOverrides_AppliesThemInAscendingIdOrder));
        Page page = new() { Slug = "shorfah", NameAr = "shorfah", CreatedBy = "test" };
        Permission editPermission = new() { Name = "edit", NameAr = "edit", CreatedBy = "test" };
        var user = new User { Email = "ordered-overrides@example.com", Name = "Ordered Overrides", CreatedBy = "test" };
        dbContext.AddRange(page, editPermission, user);
        await dbContext.SaveChangesAsync(CancellationToken.None);

        // Add the high id first to ensure the test does not accidentally pass due to provider
        // insertion order. The later id is the authoritative last write and must therefore win.
        dbContext.UserPageOverrides.Add(new UserPageOverride
        {
            Id = 200,
            UserId = user.Id,
            PageId = page.Id,
            PermissionId = editPermission.Id,
            GrantType = OverrideGrantType.Allow,
            CreatedBy = "test",
        });
        dbContext.UserPageOverrides.Add(new UserPageOverride
        {
            Id = 100,
            UserId = user.Id,
            PageId = page.Id,
            PermissionId = editPermission.Id,
            GrantType = OverrideGrantType.Deny,
            CreatedBy = "test",
        });
        await dbContext.SaveChangesAsync(CancellationToken.None);

        var resolver = new PermissionResolver(dbContext);
        PermissionResolution resolution = await resolver.ResolveAsync(user.Id, CancellationToken.None);

        resolution.Permissions.Should().Contain("shorfah:edit");
    }

    [Fact]
    public async Task ResolveAsync_UserWithZeroRoles_DefaultsToGuest()
    {
        using AppDbContext dbContext = CreateInMemoryContext(nameof(ResolveAsync_UserWithZeroRoles_DefaultsToGuest));
        var user = new User { Email = "noroles@example.com", Name = "No Roles", CreatedBy = "test" };
        dbContext.Add(user);
        await dbContext.SaveChangesAsync(CancellationToken.None);

        var resolver = new PermissionResolver(dbContext);
        PermissionResolution resolution = await resolver.ResolveAsync(user.Id, CancellationToken.None);

        resolution.RoleNames.Should().Contain("guest");
        resolution.Permissions.Should().BeEmpty();
        resolution.AccessGrantedBy.Should().BeNull("nobody has tailored this user's access individually");
    }

    [Fact]
    public async Task ResolveAsync_OverridesFromSeveralAdmins_ReportsTheMostRecentOneAsAccessGrantedBy()
    {
        using AppDbContext dbContext = CreateInMemoryContext(nameof(ResolveAsync_OverridesFromSeveralAdmins_ReportsTheMostRecentOneAsAccessGrantedBy));
        Page page = new() { Slug = "shorfah", NameAr = "shorfah", CreatedBy = "test" };
        Permission viewPermission = new() { Name = "view", NameAr = "view", CreatedBy = "test" };
        var user = new User { Email = "granted@example.com", Name = "Granted User", CreatedBy = "test" };
        var firstAdmin = new User { Email = "first-admin@example.com", Name = "سارة الأحمد", CreatedBy = "test" };
        var latestAdmin = new User { Email = "latest-admin@example.com", Name = "فهد العتيبي", CreatedBy = "test" };
        dbContext.AddRange(page, viewPermission, user, firstAdmin, latestAdmin);
        await dbContext.SaveChangesAsync(CancellationToken.None);

        // Insert the newer row (higher id) first so the assertion cannot pass merely because the
        // provider happened to return rows in insertion order.
        dbContext.UserPageOverrides.Add(new UserPageOverride
        {
            Id = 220,
            UserId = user.Id,
            PageId = page.Id,
            PermissionId = viewPermission.Id,
            GrantType = OverrideGrantType.Allow,
            CreatedByUserId = latestAdmin.Id,
            CreatedBy = "test",
        });
        dbContext.UserPageOverrides.Add(new UserPageOverride
        {
            Id = 110,
            UserId = user.Id,
            PageId = page.Id,
            PermissionId = viewPermission.Id,
            GrantType = OverrideGrantType.Allow,
            CreatedByUserId = firstAdmin.Id,
            CreatedBy = "test",
        });
        await dbContext.SaveChangesAsync(CancellationToken.None);

        var resolver = new PermissionResolver(dbContext);
        PermissionResolution resolution = await resolver.ResolveAsync(user.Id, CancellationToken.None);

        resolution.AccessGrantedBy.Should().Be("فهد العتيبي");
        resolution.Permissions.Should().Contain("shorfah:view");
    }

    [Fact]
    public async Task ResolveAsync_OverrideWithNoRecordedAuthor_LeavesAccessGrantedByNull()
    {
        using AppDbContext dbContext = CreateInMemoryContext(nameof(ResolveAsync_OverrideWithNoRecordedAuthor_LeavesAccessGrantedByNull));
        Page page = new() { Slug = "shorfah", NameAr = "shorfah", CreatedBy = "test" };
        Permission viewPermission = new() { Name = "view", NameAr = "view", CreatedBy = "test" };
        var user = new User { Email = "legacy-override@example.com", Name = "Legacy Override", CreatedBy = "test" };
        dbContext.AddRange(page, viewPermission, user);
        await dbContext.SaveChangesAsync(CancellationToken.None);

        // Rows migrated from the old system have no CreatedByUserId. The resolver must degrade to
        // null rather than throwing, because the UI treats null as "we don't know who to name".
        dbContext.UserPageOverrides.Add(new UserPageOverride
        {
            UserId = user.Id,
            PageId = page.Id,
            PermissionId = viewPermission.Id,
            GrantType = OverrideGrantType.Allow,
            CreatedByUserId = null,
            CreatedBy = "test",
        });
        await dbContext.SaveChangesAsync(CancellationToken.None);

        var resolver = new PermissionResolver(dbContext);
        PermissionResolution resolution = await resolver.ResolveAsync(user.Id, CancellationToken.None);

        resolution.AccessGrantedBy.Should().BeNull();
        resolution.Permissions.Should().Contain("shorfah:view");
    }

    private static AppDbContext CreateInMemoryContext(string dbName)
    {
        DbContextOptions<AppDbContext> options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(dbName)
            .Options;
        return new AppDbContext(options);
    }
}
