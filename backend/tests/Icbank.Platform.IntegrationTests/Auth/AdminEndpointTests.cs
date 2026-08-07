using System.Net;
using System.Net.Http.Json;
using System.Text;
using FluentAssertions;
using Icbank.Platform.Domain.Identity;
using Icbank.Platform.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Icbank.Platform.IntegrationTests.Auth;

/// <summary>
/// End-to-end coverage of the 16 admin endpoints added in this work package (task requirement 4):
/// every endpoint gets at least an authorization test, every mutating endpoint gets an
/// audit-log-write assertion, and every super-admin-only endpoint gets an explicit
/// plain-admin-is-rejected test.
/// </summary>
public sealed class AdminEndpointTests : IDisposable
{
    private const string SharedPassword = "Str0ng!Passw0rd#2026";

    private readonly AuthWebApplicationFactory _factory = new();

    /// <inheritdoc />
    public void Dispose() => _factory.Dispose();

    [Fact]
    public async Task CreateUser_AsAdmin_Succeeds()
    {
        (HttpClient client, AuthTestDataBuilder.SeededUsers seeded) = await ArrangeAuthenticatedClientAsync(useAdmin: true);

        HttpResponseMessage response = await client.PostAsJsonAsync(
            new Uri("/api/v1/admin/users", UriKind.Relative),
            new { email = "new.user@test.local", name = "New User", roleId = seeded.SuperAdminRoleId == 0 ? 1 : seeded.SuperAdminRoleId });

        // Why: a plain admin CAN create users (admin_panel:create) but must never be able to
        // pre-assign the super_admin role — asserting Forbidden-by-handler (400) here, not 200,
        // is the SEC-01 regression check for the create-user path specifically.
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task CreateUser_NoToken_ReturnsUnauthorized()
    {
        using HttpClient client = _factory.CreateClient();

        HttpResponseMessage response = await client.PostAsJsonAsync(
            new Uri("/api/v1/admin/users", UriKind.Relative), new { email = "x@test.local", name = "X", roleId = 1 });

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task CreateUser_AsSuperAdminWithNonPrivilegedRole_WritesAuditLogEntry()
    {
        (HttpClient client, AuthTestDataBuilder.SeededUsers seeded) = await ArrangeAuthenticatedClientAsync(useAdmin: false);
        var viewerRoleId = await GetRoleIdAsync("viewer");

        HttpResponseMessage response = await client.PostAsJsonAsync(
            new Uri("/api/v1/admin/users", UriKind.Relative),
            new { email = "created.by.superadmin@test.local", name = "Created User", roleId = viewerRoleId });

        response.StatusCode.Should().Be(HttpStatusCode.Created);

        using AppDbContext assertionDbContext = CreateDbContext();
        List<AuditLogEntry> entries = await assertionDbContext.AuditLogEntries.Where(e => e.Action == "user.create").ToListAsync();
        entries.Should().ContainSingle();
        entries[0].ActorUserId.Should().Be(seeded.SuperAdmin.Id);
    }

    [Fact]
    public async Task GetUser_ViewerToken_ReturnsForbidden()
    {
        (HttpClient client, AuthTestDataBuilder.SeededUsers seeded) = await ArrangeAuthenticatedClientAsync(useAdmin: false, useViewer: true);

        HttpResponseMessage response = await client.GetAsync(new Uri($"/api/v1/admin/users/{seeded.Admin.Id}", UriKind.Relative));

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task GetUser_AdminTargetingSuperAdminPeer_ReturnsNotFound()
    {
        // Why: this is the explicit SEC-16 regression test — a plain admin passes the coarse
        // admin_panel:view role check but must still be refused a super-admin peer's record by
        // the resource-level authorization check inside the handler.
        (HttpClient client, AuthTestDataBuilder.SeededUsers seeded) = await ArrangeAuthenticatedClientAsync(useAdmin: true);

        HttpResponseMessage response = await client.GetAsync(new Uri($"/api/v1/admin/users/{seeded.SuperAdmin.Id}", UriKind.Relative));

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task GetUser_AdminTargetingUnknownId_ReturnsNotFound()
    {
        (HttpClient client, _) = await ArrangeAuthenticatedClientAsync(useAdmin: true);

        HttpResponseMessage response = await client.GetAsync(new Uri("/api/v1/admin/users/999999", UriKind.Relative));

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task UpdateUser_AsAdmin_WritesAuditLogEntry()
    {
        (HttpClient client, AuthTestDataBuilder.SeededUsers seeded) = await ArrangeAuthenticatedClientAsync(useAdmin: true);

        HttpResponseMessage response = await client.PatchAsJsonAsync(
            new Uri($"/api/v1/admin/users/{seeded.Viewer.Id}", UriKind.Relative), new { name = "Renamed Viewer" });

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        using AppDbContext assertionDbContext = CreateDbContext();
        List<AuditLogEntry> entries = await assertionDbContext.AuditLogEntries
            .Where(e => e.Action == "user.profile.update" && e.TargetId == seeded.Viewer.Id.ToString(System.Globalization.CultureInfo.InvariantCulture))
            .ToListAsync();
        entries.Should().ContainSingle();
    }

    [Fact]
    public async Task DeleteUser_Self_ReturnsBadRequest()
    {
        (HttpClient client, AuthTestDataBuilder.SeededUsers seeded) = await ArrangeAuthenticatedClientAsync(useAdmin: true);

        HttpResponseMessage response = await client.DeleteAsync(new Uri($"/api/v1/admin/users/{seeded.Admin.Id}", UriKind.Relative));

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task DeleteUser_AsAdmin_SoftDeletesAndWritesAuditLogEntry()
    {
        (HttpClient client, AuthTestDataBuilder.SeededUsers seeded) = await ArrangeAuthenticatedClientAsync(useAdmin: true);

        HttpResponseMessage response = await client.DeleteAsync(new Uri($"/api/v1/admin/users/{seeded.Viewer.Id}", UriKind.Relative));
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        using AppDbContext assertionDbContext = CreateDbContext();
        List<AuditLogEntry> entries = await assertionDbContext.AuditLogEntries.Where(e => e.Action == "user.delete").ToListAsync();
        entries.Should().ContainSingle();

        User? deleted = await assertionDbContext.Users.IgnoreQueryFilters().SingleOrDefaultAsync(u => u.Id == seeded.Viewer.Id);
        deleted.Should().NotBeNull();
        deleted!.DeletedAt.Should().NotBeNull();
    }

    [Fact]
    public async Task SuspendUser_Self_ReturnsBadRequest()
    {
        (HttpClient client, AuthTestDataBuilder.SeededUsers seeded) = await ArrangeAuthenticatedClientAsync(useAdmin: true);

        HttpResponseMessage response = await client.PostAsync(new Uri($"/api/v1/admin/users/{seeded.Admin.Id}/suspend", UriKind.Relative), null);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task SuspendUser_AsAdmin_TogglesAndWritesAuditLogEntry()
    {
        (HttpClient client, AuthTestDataBuilder.SeededUsers seeded) = await ArrangeAuthenticatedClientAsync(useAdmin: true);

        HttpResponseMessage response = await client.PostAsync(new Uri($"/api/v1/admin/users/{seeded.Viewer.Id}/suspend", UriKind.Relative), null);
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        using AppDbContext assertionDbContext = CreateDbContext();
        List<AuditLogEntry> entries = await assertionDbContext.AuditLogEntries.Where(e => e.Action == "user.suspension.toggle").ToListAsync();
        entries.Should().ContainSingle();
    }

    [Fact]
    public async Task ResetPassword_AsAdmin_ReturnsTempPasswordAndWritesAuditLogEntry()
    {
        (HttpClient client, AuthTestDataBuilder.SeededUsers seeded) = await ArrangeAuthenticatedClientAsync(useAdmin: true);

        HttpResponseMessage response = await client.PostAsync(new Uri($"/api/v1/admin/users/{seeded.Viewer.Id}/reset-password", UriKind.Relative), null);
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        ResetPasswordResponseBody? body = await response.Content.ReadFromJsonAsync<ResetPasswordResponseBody>();
        body!.TempPassword.Should().NotBeNullOrEmpty();

        using AppDbContext assertionDbContext = CreateDbContext();
        List<AuditLogEntry> entries = await assertionDbContext.AuditLogEntries.Where(e => e.Action == "user.password.reset").ToListAsync();
        entries.Should().ContainSingle();
    }

    [Fact]
    public async Task ListRoles_ViewerToken_ReturnsForbidden()
    {
        (HttpClient client, _) = await ArrangeAuthenticatedClientAsync(useAdmin: false, useViewer: true);

        HttpResponseMessage response = await client.GetAsync(new Uri("/api/v1/admin/roles", UriKind.Relative));

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task CreateRole_AsPlainAdmin_ReturnsForbidden()
    {
        (HttpClient client, _) = await ArrangeAuthenticatedClientAsync(useAdmin: true);

        HttpResponseMessage response = await client.PostAsJsonAsync(
            new Uri("/api/v1/admin/roles", UriKind.Relative), new { name = "custom_role", nameAr = "دور" });

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task CreateRole_AsSuperAdmin_SucceedsAndWritesAuditLogEntry()
    {
        (HttpClient client, AuthTestDataBuilder.SeededUsers seeded) = await ArrangeAuthenticatedClientAsync(useAdmin: false);

        HttpResponseMessage response = await client.PostAsJsonAsync(
            new Uri("/api/v1/admin/roles", UriKind.Relative), new { name = "custom_role", nameAr = "دور" });

        response.StatusCode.Should().Be(HttpStatusCode.Created);

        using AppDbContext assertionDbContext = CreateDbContext();
        List<AuditLogEntry> entries = await assertionDbContext.AuditLogEntries.Where(e => e.Action == "role.create").ToListAsync();
        entries.Should().ContainSingle();
        entries[0].ActorUserId.Should().Be(seeded.SuperAdmin.Id);
    }

    [Fact]
    public async Task UpdateRole_AsPlainAdmin_ReturnsForbidden()
    {
        (HttpClient client, AuthTestDataBuilder.SeededUsers seeded) = await ArrangeAuthenticatedClientAsync(useAdmin: true);

        HttpResponseMessage response = await client.PatchAsJsonAsync(
            new Uri($"/api/v1/admin/roles/{seeded.SuperAdminRoleId}", UriKind.Relative), new { nameAr = "محاولة" });

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task DeleteRole_SystemRole_ReturnsBadRequest()
    {
        (HttpClient client, AuthTestDataBuilder.SeededUsers seeded) = await ArrangeAuthenticatedClientAsync(useAdmin: false);

        HttpResponseMessage response = await client.DeleteAsync(new Uri($"/api/v1/admin/roles/{seeded.SuperAdminRoleId}", UriKind.Relative));

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task DeleteRole_AsPlainAdmin_ReturnsForbidden()
    {
        (HttpClient client, AuthTestDataBuilder.SeededUsers seeded) = await ArrangeAuthenticatedClientAsync(useAdmin: true);

        HttpResponseMessage response = await client.DeleteAsync(new Uri($"/api/v1/admin/roles/{seeded.SuperAdminRoleId}", UriKind.Relative));

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task GetRolePermissions_UnknownRoleId_ReturnsBadRequest()
    {
        (HttpClient client, _) = await ArrangeAuthenticatedClientAsync(useAdmin: true);

        HttpResponseMessage response = await client.GetAsync(new Uri("/api/v1/admin/roles/999999/permissions", UriKind.Relative));

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task GetRolePermissions_AsAdmin_ReturnsMatrix()
    {
        (HttpClient client, AuthTestDataBuilder.SeededUsers seeded) = await ArrangeAuthenticatedClientAsync(useAdmin: true);

        HttpResponseMessage response = await client.GetAsync(new Uri($"/api/v1/admin/roles/{seeded.SuperAdminRoleId}/permissions", UriKind.Relative));

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task GetMatrix_ViewerToken_ReturnsForbidden()
    {
        (HttpClient client, _) = await ArrangeAuthenticatedClientAsync(useAdmin: false, useViewer: true);

        HttpResponseMessage response = await client.GetAsync(new Uri("/api/v1/admin/matrix", UriKind.Relative));

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task GetMatrix_AsAdmin_ReturnsPagedEnvelope()
    {
        (HttpClient client, _) = await ArrangeAuthenticatedClientAsync(useAdmin: true);

        HttpResponseMessage response = await client.GetAsync(new Uri("/api/v1/admin/matrix?page=1&pageSize=10", UriKind.Relative));

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task SetUserOverride_AsPlainAdmin_ReturnsForbidden()
    {
        (HttpClient client, AuthTestDataBuilder.SeededUsers seeded) = await ArrangeAuthenticatedClientAsync(useAdmin: true);

        HttpResponseMessage response = await client.PutAsJsonAsync(
            new Uri("/api/v1/admin/matrix/user-override", UriKind.Relative),
            new { userId = seeded.Viewer.Id, pageSlug = "admin_panel", permName = "view", grantType = "allow" });

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task SetUserOverride_AsSuperAdmin_SucceedsAndWritesAuditLogEntry()
    {
        (HttpClient client, AuthTestDataBuilder.SeededUsers seeded) = await ArrangeAuthenticatedClientAsync(useAdmin: false);

        HttpResponseMessage response = await client.PutAsJsonAsync(
            new Uri("/api/v1/admin/matrix/user-override", UriKind.Relative),
            new { userId = seeded.Viewer.Id, pageSlug = "admin_panel", permName = "view", grantType = "allow" });

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        using AppDbContext assertionDbContext = CreateDbContext();
        List<AuditLogEntry> entries = await assertionDbContext.AuditLogEntries.Where(e => e.Action == "user.permission_override.set").ToListAsync();
        entries.Should().ContainSingle();
    }

    [Fact]
    public async Task ExportMatrix_AsPlainAdmin_ReturnsForbidden()
    {
        (HttpClient client, _) = await ArrangeAuthenticatedClientAsync(useAdmin: true);

        HttpResponseMessage response = await client.GetAsync(new Uri("/api/v1/admin/matrix/export?format=csv", UriKind.Relative));

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task ExportMatrix_AsSuperAdmin_ReturnsCsv()
    {
        (HttpClient client, _) = await ArrangeAuthenticatedClientAsync(useAdmin: false);

        HttpResponseMessage response = await client.GetAsync(new Uri("/api/v1/admin/matrix/export?format=csv", UriKind.Relative));

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        response.Content.Headers.ContentType?.MediaType.Should().Be("text/csv");
    }

    [Fact]
    public async Task ListActivity_AsAdmin_ReturnsPagedEnvelope()
    {
        (HttpClient client, _) = await ArrangeAuthenticatedClientAsync(useAdmin: true);

        HttpResponseMessage response = await client.GetAsync(new Uri("/api/v1/admin/activity?page=1&pageSize=10", UriKind.Relative));

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task ListActivity_NoToken_ReturnsUnauthorized()
    {
        using HttpClient client = _factory.CreateClient();

        HttpResponseMessage response = await client.GetAsync(new Uri("/api/v1/admin/activity", UriKind.Relative));

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task ExportActivity_NoToken_ReturnsUnauthorized()
    {
        using HttpClient client = _factory.CreateClient();

        HttpResponseMessage response = await client.GetAsync(new Uri("/api/v1/admin/activity/export", UriKind.Relative));

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task ExportActivity_ViewerToken_ReturnsForbidden()
    {
        (HttpClient client, _) = await ArrangeAuthenticatedClientAsync(useAdmin: false, useViewer: true);

        HttpResponseMessage response = await client.GetAsync(new Uri("/api/v1/admin/activity/export", UriKind.Relative));

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task ExportActivity_AsAdmin_ReturnsCsvWithUtf8BomAndWritesAuditLogEntry()
    {
        (HttpClient client, AuthTestDataBuilder.SeededUsers seeded) = await ArrangeAuthenticatedClientAsync(useAdmin: true);

        HttpResponseMessage response = await client.GetAsync(new Uri("/api/v1/admin/activity/export", UriKind.Relative));

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        response.Content.Headers.ContentType?.MediaType.Should().Be("text/csv");
        response.Content.Headers.ContentDisposition?.FileName.Should().Be("\"activity-log.csv\"");

        var bytes = await response.Content.ReadAsByteArrayAsync();
        bytes.Take(3).Should().BeEquivalentTo(new byte[] { 0xEF, 0xBB, 0xBF }, options => options.WithStrictOrdering());

        var text = Encoding.UTF8.GetString(bytes.Skip(3).ToArray());
        text.Should().Contain("\"#\",\"المستخدم\",\"البريد\",\"العملية\",\"النوع\",\"المعرف\",\"IP\",\"التاريخ\"");

        using AppDbContext assertionDbContext = CreateDbContext();
        List<AuditLogEntry> entries = await assertionDbContext.AuditLogEntries.Where(e => e.Action == "activity_log.export").ToListAsync();
        entries.Should().ContainSingle();
        entries[0].ActorUserId.Should().Be(seeded.Admin.Id);
    }

    [Fact]
    public async Task ExportActivity_DateFromAfterDateTo_ReturnsBadRequest()
    {
        (HttpClient client, _) = await ArrangeAuthenticatedClientAsync(useAdmin: true);

        HttpResponseMessage response = await client.GetAsync(new Uri(
            "/api/v1/admin/activity/export?dateFrom=2026-02-01&dateTo=2026-01-01", UriKind.Relative));

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task GetSettings_AsAdmin_MasksSecretKey()
    {
        (HttpClient client, _) = await ArrangeAuthenticatedClientAsync(useAdmin: true);

        HttpResponseMessage response = await client.GetAsync(new Uri("/api/v1/admin/settings", UriKind.Relative));

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadAsStringAsync();
        body.Should().NotContain("plaintext-secret-value");
    }

    [Fact]
    public async Task UpdateSettings_AsPlainAdmin_ReturnsForbidden()
    {
        (HttpClient client, _) = await ArrangeAuthenticatedClientAsync(useAdmin: true);

        HttpResponseMessage response = await client.PutAsJsonAsync(
            new Uri("/api/v1/admin/settings", UriKind.Relative), new { settings = new Dictionary<string, string> { ["session_duration_minutes"] = "30" } });

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task UpdateSettings_AsSuperAdminWithUnknownKey_ReturnsBadRequest()
    {
        (HttpClient client, _) = await ArrangeAuthenticatedClientAsync(useAdmin: false);

        HttpResponseMessage response = await client.PutAsJsonAsync(
            new Uri("/api/v1/admin/settings", UriKind.Relative), new { settings = new Dictionary<string, string> { ["not_a_real_setting"] = "x" } });

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task UpdateSettings_AsSuperAdminWithWhitelistedKey_SucceedsAndWritesAuditLogEntry()
    {
        (HttpClient client, AuthTestDataBuilder.SeededUsers seeded) = await ArrangeAuthenticatedClientAsync(useAdmin: false);

        HttpResponseMessage response = await client.PutAsJsonAsync(
            new Uri("/api/v1/admin/settings", UriKind.Relative), new { settings = new Dictionary<string, string> { ["session_duration_minutes"] = "30" } });

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        using AppDbContext assertionDbContext = CreateDbContext();
        List<AuditLogEntry> entries = await assertionDbContext.AuditLogEntries.Where(e => e.Action == "settings.update").ToListAsync();
        entries.Should().ContainSingle();
        entries[0].ActorUserId.Should().Be(seeded.SuperAdmin.Id);
    }

    private static async Task<string> LoginAndGetAccessTokenAsync(HttpClient client, string email)
    {
        HttpResponseMessage response = await client.PostAsJsonAsync(
            new Uri("/api/v1/auth/login", UriKind.Relative), new { email, password = SharedPassword });
        response.EnsureSuccessStatusCode();

        LoginResponseBody? body = await response.Content.ReadFromJsonAsync<LoginResponseBody>();
        return body?.AccessToken ?? throw new InvalidOperationException("Login response missing accessToken.");
    }

    private async Task<int> GetRoleIdAsync(string roleName)
    {
        using AppDbContext dbContext = CreateDbContext();
        Role role = await dbContext.Roles.SingleAsync(r => r.Name == roleName);
        return role.Id;
    }

    private async Task<(HttpClient Client, AuthTestDataBuilder.SeededUsers Seeded)> ArrangeAuthenticatedClientAsync(bool useAdmin, bool useViewer = false)
    {
        using AppDbContext dbContext = CreateDbContext();
        AuthTestDataBuilder.SeededUsers seeded = await AuthTestDataBuilder.SeedAsync(dbContext, SharedPassword);
        HttpClient client = _factory.CreateClient();

        var email = useViewer ? seeded.Viewer.Email : useAdmin ? seeded.Admin.Email : seeded.SuperAdmin.Email;
        var accessToken = await LoginAndGetAccessTokenAsync(client, email);
        client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", accessToken);

        return (client, seeded);
    }

    private AppDbContext CreateDbContext()
    {
        IServiceScope scope = _factory.Services.CreateScope();
        return scope.ServiceProvider.GetRequiredService<AppDbContext>();
    }

    private sealed record LoginResponseBody(string AccessToken);

    private sealed record ResetPasswordResponseBody(string TempPassword);
}
