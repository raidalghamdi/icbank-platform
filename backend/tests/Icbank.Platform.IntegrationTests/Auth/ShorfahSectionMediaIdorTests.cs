using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using Icbank.Platform.Domain.Identity;
using Icbank.Platform.Infrastructure.Identity;
using Icbank.Platform.Infrastructure.Persistence;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Icbank.Platform.IntegrationTests.Auth;

/// <summary>
/// Cross-user IDOR coverage for Shorfah section media (closes the read-side half of
/// AMBIGUOUS-API-4): a user holding the global <c>shorfah:edit</c>/<c>shorfah:view</c> RBAC
/// permission but with zero <c>shorfah_section_permissions</c> rows on a specific section must be
/// refused on every media operation for that section -- list, upload, patch, and delete alike.
/// The controller-level <c>[Authorize]</c> policy alone cannot express this; only the per-section
/// access-tier check inside each handler can, which is exactly what this class proves.
/// </summary>
public sealed class ShorfahSectionMediaIdorTests : IDisposable
{
    private const string SharedPassword = "Str0ng!Passw0rd#2026";
    private const string TinyPngBase64 = "iVBORw0KGgoAAAANSUhEUgAAAAEAAAABCAQAAAC1HAwCAAAAC0lEQVR42mNk+A8AAQUAAscOaXcAAAAASUVORK5CYII=";

    private readonly AuthWebApplicationFactory _factory = new();

    /// <inheritdoc />
    public void Dispose() => _factory.Dispose();

    [Fact]
    public async Task ListMedia_UserWithNoSectionGrant_ReturnsForbidden()
    {
        HttpClient adminClient = await ArrangeSuperAdminClientAsync();
        SectionDto section = await FirstSectionAsync(adminClient);
        await UploadOneMediaAsync(adminClient, section.Id);
        HttpClient noGrantClient = await ArrangeShorfahEditorClientAsync();

        HttpResponseMessage response = await noGrantClient.GetAsync(new Uri($"/api/v1/shorfah/sections/{section.Id}/media?page=1&pageSize=10", UriKind.Relative));

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden, "a user with shorfah:edit but zero section-level grants must not be able to list this section's media");
    }

    [Fact]
    public async Task UploadMedia_UserWithNoSectionGrant_ReturnsForbidden()
    {
        HttpClient adminClient = await ArrangeSuperAdminClientAsync();
        SectionDto section = await FirstSectionAsync(adminClient);
        HttpClient noGrantClient = await ArrangeShorfahEditorClientAsync();

        HttpResponseMessage response = await noGrantClient.PostAsJsonAsync(
            new Uri($"/api/v1/shorfah/sections/{section.Id}/media", UriKind.Relative),
            new { dataBase64 = TinyPngBase64, contentType = "image/png" });

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task PatchMedia_UserWithNoSectionGrant_ReturnsForbidden()
    {
        HttpClient adminClient = await ArrangeSuperAdminClientAsync();
        SectionDto section = await FirstSectionAsync(adminClient);
        MediaDto media = await UploadOneMediaAsync(adminClient, section.Id);
        HttpClient noGrantClient = await ArrangeShorfahEditorClientAsync();

        HttpResponseMessage response = await noGrantClient.PatchAsJsonAsync(
            new Uri($"/api/v1/shorfah/media/{media.Id}", UriKind.Relative), new { captionAr = "محاولة تعديل" });

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task DeleteMedia_UserWithNoSectionGrant_ReturnsForbidden()
    {
        HttpClient adminClient = await ArrangeSuperAdminClientAsync();
        SectionDto section = await FirstSectionAsync(adminClient);
        MediaDto media = await UploadOneMediaAsync(adminClient, section.Id);
        HttpClient noGrantClient = await ArrangeShorfahEditorClientAsync();

        HttpResponseMessage response = await noGrantClient.DeleteAsync(new Uri($"/api/v1/shorfah/media/{media.Id}", UriKind.Relative));

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);

        // Why: a refused delete must not have removed the row -- verifying via the admin client
        // (which does have access) closes the loop that "Forbidden" really means untouched, not
        // a false-negative status code on top of a real deletion.
        HttpResponseMessage verifyResponse = await adminClient.GetAsync(new Uri($"/api/v1/shorfah/sections/{section.Id}/media?page=1&pageSize=10", UriKind.Relative));
        MediaListPayload? verifyPayload = await verifyResponse.Content.ReadFromJsonAsync<MediaListPayload>();
        verifyPayload!.Media.Should().ContainSingle(m => m.Id == media.Id);
    }

    [Fact]
    public async Task ListMedia_UserWithNoSectionGrant_OnADifferentSectionWithGrant_StillForbiddenOnUngrantedSection()
    {
        // Why: the IDOR-precise proof -- a grant on section X must not leak access to section Y.
        // Holding *some* section-level permission somewhere is not the same as holding it on the
        // specific section being requested.
        HttpClient adminClient = await ArrangeSuperAdminClientAsync();
        SectionDto grantedSection = await FirstSectionAsync(adminClient);
        SectionDto ungrantedSection = await FirstSectionAsync(adminClient);
        User editor = await SeedShorfahEditorAsync();
        await adminClient.PostAsJsonAsync(
            new Uri($"/api/v1/shorfah/sections/{grantedSection.Id}/permissions", UriKind.Relative), new { userId = editor.Id, permission = "View" });
        HttpClient editorClient = await LoginAsync(editor.Email);

        HttpResponseMessage grantedResponse = await editorClient.GetAsync(new Uri($"/api/v1/shorfah/sections/{grantedSection.Id}/media?page=1&pageSize=10", UriKind.Relative));
        HttpResponseMessage ungrantedResponse = await editorClient.GetAsync(new Uri($"/api/v1/shorfah/sections/{ungrantedSection.Id}/media?page=1&pageSize=10", UriKind.Relative));

        grantedResponse.StatusCode.Should().Be(HttpStatusCode.OK, "the section this user was explicitly granted View on must be listable");
        ungrantedResponse.StatusCode.Should().Be(HttpStatusCode.Forbidden, "a grant on one section must never extend to a different, ungranted section");
    }

    [Fact]
    public async Task ListMedia_AfterGrantingViewTier_Succeeds()
    {
        HttpClient adminClient = await ArrangeSuperAdminClientAsync();
        SectionDto section = await FirstSectionAsync(adminClient);
        await UploadOneMediaAsync(adminClient, section.Id);
        User editor = await SeedShorfahEditorAsync();
        HttpClient editorClient = await LoginAsync(editor.Email);

        HttpResponseMessage beforeGrant = await editorClient.GetAsync(new Uri($"/api/v1/shorfah/sections/{section.Id}/media?page=1&pageSize=10", UriKind.Relative));
        beforeGrant.StatusCode.Should().Be(HttpStatusCode.Forbidden);

        await adminClient.PostAsJsonAsync(
            new Uri($"/api/v1/shorfah/sections/{section.Id}/permissions", UriKind.Relative), new { userId = editor.Id, permission = "View" });

        HttpResponseMessage afterGrant = await editorClient.GetAsync(new Uri($"/api/v1/shorfah/sections/{section.Id}/media?page=1&pageSize=10", UriKind.Relative));

        afterGrant.StatusCode.Should().Be(HttpStatusCode.OK, "the View tier alone (not Contribute/Review/Approve) must be sufficient to list media, matching the non-hierarchical tier model");
        MediaListPayload? payload = await afterGrant.Content.ReadFromJsonAsync<MediaListPayload>();
        payload!.Media.Should().ContainSingle();
    }

    private static async Task<MediaDto> UploadOneMediaAsync(HttpClient client, int sectionId)
    {
        HttpResponseMessage response = await client.PostAsJsonAsync(
            new Uri($"/api/v1/shorfah/sections/{sectionId}/media", UriKind.Relative),
            new { dataBase64 = TinyPngBase64, contentType = "image/png" });
        response.EnsureSuccessStatusCode();
        MediaPayload? payload = await response.Content.ReadFromJsonAsync<MediaPayload>();
        return payload!.Media;
    }

    private static async Task<SectionDto> FirstSectionAsync(HttpClient client)
    {
        HttpResponseMessage createResponse = await client.PostAsJsonAsync(
            new Uri("/api/v1/shorfah/issues", UriKind.Relative), new { titleAr = $"عدد اختبار {Guid.NewGuid()}", month = 8, year = 2026 });
        createResponse.EnsureSuccessStatusCode();
        CreateIssuePayload? issue = await createResponse.Content.ReadFromJsonAsync<CreateIssuePayload>();

        HttpResponseMessage detailResponse = await client.GetAsync(new Uri($"/api/v1/shorfah/issues/{issue!.Issue.Id}", UriKind.Relative));
        GetIssuePayload? detail = await detailResponse.Content.ReadFromJsonAsync<GetIssuePayload>();
        return detail!.Sections[0];
    }

    private async Task<HttpClient> ArrangeSuperAdminClientAsync()
    {
        using AppDbContext dbContext = CreateDbContext();
        AuthTestDataBuilder.SeededUsers seeded = await AuthTestDataBuilder.SeedAsync(dbContext, SharedPassword);
        return await LoginAsync(seeded.SuperAdmin.Email);
    }

    private async Task<HttpClient> ArrangeShorfahEditorClientAsync()
    {
        User editor = await SeedShorfahEditorAsync();
        return await LoginAsync(editor.Email);
    }

    /// <summary>
    /// Seeds a user whose only role carries the global shorfah:edit/:view RBAC page permissions
    /// -- no admin_panel, no super_admin, and (critically for this class) zero
    /// shorfah_section_permissions rows on any section -- so the [Authorize] gate is satisfied
    /// and the only remaining boundary under test is the per-section access-tier check.
    /// </summary>
    private async Task<User> SeedShorfahEditorAsync()
    {
        using AppDbContext dbContext = CreateDbContext();

        Role editorRole = new() { Name = $"shorfah_media_editor_{Guid.NewGuid()}", NameAr = "shorfah_editor", CreatedBy = "test" };
        dbContext.Add(editorRole);
        Page shorfahPage = new() { Slug = PageSlugs.Shorfah, NameAr = "shorfah", CreatedBy = "test" };
        Permission viewPermission = new() { Name = "view", NameAr = "view", CreatedBy = "test" };
        Permission editPermission = new() { Name = "edit", NameAr = "edit", CreatedBy = "test" };
        dbContext.AddRange(shorfahPage, viewPermission, editPermission);
        await dbContext.SaveChangesAsync(CancellationToken.None);

        dbContext.RolePermissions.Add(new RolePermission { RoleId = editorRole.Id, PageId = shorfahPage.Id, PermissionId = viewPermission.Id, CreatedBy = "test" });
        dbContext.RolePermissions.Add(new RolePermission { RoleId = editorRole.Id, PageId = shorfahPage.Id, PermissionId = editPermission.Id, CreatedBy = "test" });
        await dbContext.SaveChangesAsync(CancellationToken.None);

        var hasher = new PasswordHasher<PasswordHasherSubject>();
        var user = new User
        {
            Email = $"media-idor-{Guid.NewGuid()}@test.local",
            Name = "Media IDOR Test User",
            PasswordHash = hasher.HashPassword(new PasswordHasherSubject(), SharedPassword),
            IsActive = true,
            CreatedBy = "test",
        };
        dbContext.Add(user);
        await dbContext.SaveChangesAsync(CancellationToken.None);

        dbContext.UserRoles.Add(new UserRole { UserId = user.Id, RoleId = editorRole.Id, AssignedAt = DateTime.UtcNow, CreatedBy = "test" });
        await dbContext.SaveChangesAsync(CancellationToken.None);

        return user;
    }

    private async Task<HttpClient> LoginAsync(string email)
    {
        HttpClient client = _factory.CreateClient();
        HttpResponseMessage loginResponse = await client.PostAsJsonAsync(
            new Uri("/api/v1/auth/login", UriKind.Relative), new { email, password = SharedPassword });
        loginResponse.EnsureSuccessStatusCode();

        LoginResponsePayload? payload = await loginResponse.Content.ReadFromJsonAsync<LoginResponsePayload>();
        client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", payload!.AccessToken);
        return client;
    }

    private AppDbContext CreateDbContext()
    {
        IServiceScope scope = _factory.Services.CreateScope();
        return scope.ServiceProvider.GetRequiredService<AppDbContext>();
    }

    private sealed record LoginResponsePayload(string AccessToken);

    private sealed record IssueDto(int Id, string TitleAr, string Status);

    private sealed record SectionDto(int Id, string TitleAr, string WorkflowStatus);

    private sealed record CreateIssuePayload(IssueDto Issue);

    private sealed record GetIssuePayload(IssueDto Issue, List<SectionDto> Sections);

    private sealed record MediaDto(int Id, int SectionId, string MediaUrl, string MediaType, string? CaptionAr, int? DisplayOrder);

    private sealed record MediaPayload(MediaDto Media);

    private sealed record MediaListPayload(List<MediaDto> Media, int Page, int PageSize, int Total);
}
