using Icbank.Platform.Domain.Identity;
using Icbank.Platform.Infrastructure.Identity;
using Icbank.Platform.Infrastructure.Persistence;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace Icbank.Platform.IntegrationTests.Auth;

/// <summary>Builds the minimal roles/pages/permissions/users fixture shared by the end-to-end auth tests.</summary>
public static class AuthTestDataBuilder
{
    private static readonly PasswordHasherSubject Subject = new();
    private static readonly PasswordHasher<PasswordHasherSubject> Hasher = new();

    /// <summary>Seeds a viewer, an admin, and a super-admin account, all with the given password, plus the admin_panel page/permission rows.</summary>
    /// <param name="dbContext">The database context to seed.</param>
    /// <param name="password">The shared plaintext password for every seeded account.</param>
    /// <returns>The seeded users, keyed by role.</returns>
    public static async Task<SeededUsers> SeedAsync(AppDbContext dbContext, string password)
    {
        Role viewerRole = new() { Name = "viewer", NameAr = "viewer", CreatedBy = "test" };
        Role adminRole = new() { Name = "admin", NameAr = "admin", CreatedBy = "test" };
        Role superAdminRole = new() { Name = "super_admin", NameAr = "super_admin", CreatedBy = "test" };
        dbContext.AddRange(viewerRole, adminRole, superAdminRole);

        Page adminPanelPage = await EnsurePageAsync(dbContext, "admin_panel");
        Page settingsPage = await EnsurePageAsync(dbContext, "settings");
        Permission viewPermission = await EnsurePermissionAsync(dbContext, "view");
        Permission createPermission = await EnsurePermissionAsync(dbContext, "create");
        Permission editPermission = await EnsurePermissionAsync(dbContext, "edit");
        Permission deletePermission = await EnsurePermissionAsync(dbContext, "delete");
        await dbContext.SaveChangesAsync(CancellationToken.None);

        // Why: a plain admin gets every admin_panel:* verb plus settings:view, so authz tests can
        // distinguish "admin lacks this specific verb" failures from "admin lacks admin_panel
        // entirely" — but never gets the super-admin policy, which is the actual SEC-01 boundary
        // this fixture exists to exercise.
        foreach (Permission permission in new[] { viewPermission, createPermission, editPermission, deletePermission })
        {
            dbContext.RolePermissions.Add(new RolePermission { RoleId = adminRole.Id, PageId = adminPanelPage.Id, PermissionId = permission.Id, CreatedBy = "test" });
        }

        dbContext.RolePermissions.Add(new RolePermission { RoleId = adminRole.Id, PageId = settingsPage.Id, PermissionId = viewPermission.Id, CreatedBy = "test" });
        await dbContext.SaveChangesAsync(CancellationToken.None);

        var hashedPassword = Hasher.HashPassword(Subject, password);

        var viewerUser = new User { Email = "viewer@test.local", Name = "Viewer", PasswordHash = hashedPassword, IsActive = true, CreatedBy = "test" };
        var adminUser = new User { Email = "admin@test.local", Name = "Admin", PasswordHash = hashedPassword, IsActive = true, CreatedBy = "test" };
        var superAdminUser = new User { Email = "superadmin@test.local", Name = "Super Admin", PasswordHash = hashedPassword, IsActive = true, CreatedBy = "test" };
        dbContext.AddRange(viewerUser, adminUser, superAdminUser);
        await dbContext.SaveChangesAsync(CancellationToken.None);

        dbContext.UserRoles.Add(new UserRole { UserId = viewerUser.Id, RoleId = viewerRole.Id, AssignedAt = DateTime.UtcNow, CreatedBy = "test" });
        dbContext.UserRoles.Add(new UserRole { UserId = adminUser.Id, RoleId = adminRole.Id, AssignedAt = DateTime.UtcNow, CreatedBy = "test" });
        dbContext.UserRoles.Add(new UserRole { UserId = superAdminUser.Id, RoleId = superAdminRole.Id, AssignedAt = DateTime.UtcNow, CreatedBy = "test" });
        await dbContext.SaveChangesAsync(CancellationToken.None);

        return new SeededUsers(viewerUser, adminUser, superAdminUser, superAdminRole.Id);
    }

    /// <summary>
    /// Returns the permission row with the given verb name, inserting it only if it is absent.
    /// <para>
    /// The permission catalogue is global: <c>permissions.name</c> carries a unique index
    /// (<c>ux_permissions_name</c>), so a verb like <c>view</c> exists at most once per database
    /// and is shared by every page grant. Test setup must therefore claim it rather than insert
    /// it. Blind inserts were invisible under the EF Core InMemory provider, which enforces no
    /// unique indexes, and failed immediately once the suite ran against real SQL Server.
    /// </para>
    /// </summary>
    /// <param name="dbContext">The database context to read and seed.</param>
    /// <param name="name">The verb name, one of <see cref="PermissionVerbName"/>.</param>
    /// <returns>The existing or newly inserted permission.</returns>
    public static async Task<Permission> EnsurePermissionAsync(AppDbContext dbContext, string name)
    {
        Permission? existing = dbContext.ChangeTracker.Entries<Permission>()
            .Select(entry => entry.Entity)
            .FirstOrDefault(permission => permission.Name == name)
            ?? await dbContext.Permissions.FirstOrDefaultAsync(
                permission => permission.Name == name,
                CancellationToken.None);

        if (existing is not null)
        {
            return existing;
        }

        Permission created = new() { Name = name, NameAr = name, CreatedBy = "test" };
        dbContext.Add(created);
        await dbContext.SaveChangesAsync(CancellationToken.None);
        return created;
    }

    /// <summary>
    /// Returns the page row with the given slug, inserting it only if it is absent.
    /// <c>pages.slug</c> is likewise globally unique, so two test fixtures both needing the same
    /// page must share one row.
    /// </summary>
    /// <param name="dbContext">The database context to read and seed.</param>
    /// <param name="slug">The page slug, one of <see cref="PageSlugs"/>.</param>
    /// <returns>The existing or newly inserted page.</returns>
    public static async Task<Page> EnsurePageAsync(AppDbContext dbContext, string slug)
    {
        Page? existing = dbContext.ChangeTracker.Entries<Page>()
            .Select(entry => entry.Entity)
            .FirstOrDefault(page => page.Slug == slug)
            ?? await dbContext.Pages.FirstOrDefaultAsync(page => page.Slug == slug, CancellationToken.None);

        if (existing is not null)
        {
            return existing;
        }

        Page created = new() { Slug = slug, NameAr = slug, CreatedBy = "test" };
        dbContext.Add(created);
        await dbContext.SaveChangesAsync(CancellationToken.None);
        return created;
    }

    /// <summary>The seeded fixture users and the super-admin role's id (for escalation-attempt tests).</summary>
    /// <param name="Viewer">The seeded viewer account.</param>
    /// <param name="Admin">The seeded plain-admin account.</param>
    /// <param name="SuperAdmin">The seeded super-admin account.</param>
    /// <param name="SuperAdminRoleId">The super-admin role's id.</param>
    public sealed record SeededUsers(User Viewer, User Admin, User SuperAdmin, int SuperAdminRoleId);
}
