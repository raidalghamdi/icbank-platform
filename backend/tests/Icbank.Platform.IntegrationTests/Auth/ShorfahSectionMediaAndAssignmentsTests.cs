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
/// Coverage for the Wave 4b Shorfah media, assignments, permissions, and SLA endpoints
/// (API-SURFACE.md §19, BUSINESS-RULES.md §1.4/§1.5): upload/patch/delete media with the
/// AMBIGUOUS-API-4 permission gap closed, assignment/permission grant-and-revoke, and the SLA
/// defaults bulk-update with propagation.
/// </summary>
public sealed class ShorfahSectionMediaAndAssignmentsTests : IDisposable
{
    private const string SharedPassword = "Str0ng!Passw0rd#2026";
    private const string TinyPngBase64 = "iVBORw0KGgoAAAANSUhEUgAAAAEAAAABCAQAAAC1HAwCAAAAC0lEQVR42mNk+A8AAQUAAscOaXcAAAAASUVORK5CYII=";

    private readonly AuthWebApplicationFactory _factory = new();

    // Why: this factory's InMemory database is shared by every Arrange* call within a single
    // test, and AuthTestDataBuilder.SeedAsync is not idempotent -- calling it twice inserts a
    // second "viewer@test.local" row, and the login handler's SingleOrDefaultAsync-by-email then
    // throws ("Sequence contains more than one element") instead of the intended 500-free flow.
    // SeedOnceAsync guards against that by seeding at most once per factory instance.
    private AuthTestDataBuilder.SeededUsers? _seededUsers;

    /// <inheritdoc />
    public void Dispose() => _factory.Dispose();

    [Fact]
    public async Task UploadMedia_AsAdmin_Succeeds()
    {
        HttpClient client = await ArrangeSuperAdminClientAsync();
        SectionDto section = await FirstSectionAsync(client);

        HttpResponseMessage response = await client.PostAsJsonAsync(
            new Uri($"/api/v1/shorfah/sections/{section.Id}/media", UriKind.Relative),
            new { dataBase64 = TinyPngBase64, contentType = "image/png", captionAr = "صورة" });

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        MediaPayload? payload = await response.Content.ReadFromJsonAsync<MediaPayload>();
        payload!.Media.MediaType.Should().Be("Image");
        payload.Media.CaptionAr.Should().Be("صورة");
    }

    [Fact]
    public async Task UploadMedia_InvalidBase64_ReturnsBadRequest()
    {
        HttpClient client = await ArrangeSuperAdminClientAsync();
        SectionDto section = await FirstSectionAsync(client);

        HttpResponseMessage response = await client.PostAsJsonAsync(
            new Uri($"/api/v1/shorfah/sections/{section.Id}/media", UriKind.Relative),
            new { dataBase64 = "not-valid-base64!!", contentType = "image/png" });

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task UploadMedia_TooLarge_ReturnsPayloadTooLarge()
    {
        HttpClient client = await ArrangeSuperAdminClientAsync();
        SectionDto section = await FirstSectionAsync(client);
        var oversized = Convert.ToBase64String(new byte[9 * 1024 * 1024]);

        HttpResponseMessage response = await client.PostAsJsonAsync(
            new Uri($"/api/v1/shorfah/sections/{section.Id}/media", UriKind.Relative),
            new { dataBase64 = oversized, contentType = "image/png" });

        response.StatusCode.Should().Be(HttpStatusCode.RequestEntityTooLarge);
    }

    [Fact]
    public async Task UploadMedia_DisallowedContentType_ReturnsBadRequest()
    {
        HttpClient client = await ArrangeSuperAdminClientAsync();
        SectionDto section = await FirstSectionAsync(client);

        HttpResponseMessage response = await client.PostAsJsonAsync(
            new Uri($"/api/v1/shorfah/sections/{section.Id}/media", UriKind.Relative),
            new { dataBase64 = TinyPngBase64, contentType = "application/x-msdownload" });

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest, "SEC-17 class: content-type is allowlisted, unlike the Node source (API-SURFACE.md §22)");
    }

    [Fact]
    public async Task UploadMedia_ViewerWithNoSectionGrant_ReturnsForbidden()
    {
        HttpClient client = await ArrangeSuperAdminClientAsync();
        SectionDto section = await FirstSectionAsync(client);
        HttpClient viewerClient = await ArrangeViewerClientAsync();

        HttpResponseMessage response = await viewerClient.PostAsJsonAsync(
            new Uri($"/api/v1/shorfah/sections/{section.Id}/media", UriKind.Relative),
            new { dataBase64 = TinyPngBase64, contentType = "image/png" });

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden, "closes AMBIGUOUS-API-4: uploading media requires a qualifying section permission tier");
    }

    [Fact]
    public async Task GetMedia_Paginated_ReturnsEnvelope()
    {
        HttpClient client = await ArrangeSuperAdminClientAsync();
        SectionDto section = await FirstSectionAsync(client);
        await UploadOneMediaAsync(client, section.Id);

        HttpResponseMessage response = await client.GetAsync(new Uri($"/api/v1/shorfah/sections/{section.Id}/media?page=1&pageSize=10", UriKind.Relative));

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        MediaListPayload? payload = await response.Content.ReadFromJsonAsync<MediaListPayload>();
        payload!.Media.Should().ContainSingle();
        payload.Total.Should().Be(1);
    }

    [Fact]
    public async Task PatchMedia_ByAdmin_UpdatesCaption()
    {
        HttpClient client = await ArrangeSuperAdminClientAsync();
        SectionDto section = await FirstSectionAsync(client);
        MediaDto media = await UploadOneMediaAsync(client, section.Id);

        HttpResponseMessage response = await client.PatchAsJsonAsync(
            new Uri($"/api/v1/shorfah/media/{media.Id}", UriKind.Relative), new { captionAr = "تعليق محدث" });

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        MediaPayload? payload = await response.Content.ReadFromJsonAsync<MediaPayload>();
        payload!.Media.CaptionAr.Should().Be("تعليق محدث");
    }

    [Fact]
    public async Task PatchMedia_ViewerWithNoGrant_ReturnsForbidden()
    {
        HttpClient client = await ArrangeSuperAdminClientAsync();
        SectionDto section = await FirstSectionAsync(client);
        MediaDto media = await UploadOneMediaAsync(client, section.Id);
        HttpClient viewerClient = await ArrangeViewerClientAsync();

        HttpResponseMessage response = await viewerClient.PatchAsJsonAsync(
            new Uri($"/api/v1/shorfah/media/{media.Id}", UriKind.Relative), new { captionAr = "x" });

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden, "closes AMBIGUOUS-API-4 for PATCH");
    }

    [Fact]
    public async Task PatchMedia_UnknownId_ReturnsNotFound()
    {
        HttpClient client = await ArrangeSuperAdminClientAsync();

        HttpResponseMessage response = await client.PatchAsJsonAsync(new Uri("/api/v1/shorfah/media/999999", UriKind.Relative), new { captionAr = "x" });

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task DeleteMedia_ByAdmin_Succeeds()
    {
        HttpClient client = await ArrangeSuperAdminClientAsync();
        SectionDto section = await FirstSectionAsync(client);
        MediaDto media = await UploadOneMediaAsync(client, section.Id);

        HttpResponseMessage response = await client.DeleteAsync(new Uri($"/api/v1/shorfah/media/{media.Id}", UriKind.Relative));

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task DeleteMedia_ViewerWithNoGrant_ReturnsForbidden()
    {
        HttpClient client = await ArrangeSuperAdminClientAsync();
        SectionDto section = await FirstSectionAsync(client);
        MediaDto media = await UploadOneMediaAsync(client, section.Id);
        HttpClient viewerClient = await ArrangeViewerClientAsync();

        HttpResponseMessage response = await viewerClient.DeleteAsync(new Uri($"/api/v1/shorfah/media/{media.Id}", UriKind.Relative));

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task AssignSection_ThenRemove_Succeeds()
    {
        HttpClient client = await ArrangeSuperAdminClientAsync();
        SectionDto section = await FirstSectionAsync(client);
        AuthTestDataBuilder.SeededUsers seeded = await SeedOnceAsync();

        HttpResponseMessage assignResponse = await client.PostAsJsonAsync(
            new Uri($"/api/v1/shorfah/sections/{section.Id}/assign", UriKind.Relative), new { userId = seeded.Viewer.Id, role = "contributor" });

        assignResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        AssignmentPayload? assignPayload = await assignResponse.Content.ReadFromJsonAsync<AssignmentPayload>();

        HttpResponseMessage removeResponse = await client.DeleteAsync(new Uri($"/api/v1/shorfah/assignments/{assignPayload!.Assignment.Id}", UriKind.Relative));

        removeResponse.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task AssignSection_MissingUserId_ReturnsBadRequest()
    {
        HttpClient client = await ArrangeSuperAdminClientAsync();
        SectionDto section = await FirstSectionAsync(client);

        HttpResponseMessage response = await client.PostAsJsonAsync(
            new Uri($"/api/v1/shorfah/sections/{section.Id}/assign", UriKind.Relative), new { userId = 0 });

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task RemoveAssignment_UnknownId_ReturnsNotFound()
    {
        HttpClient client = await ArrangeSuperAdminClientAsync();

        HttpResponseMessage response = await client.DeleteAsync(new Uri("/api/v1/shorfah/assignments/999999", UriKind.Relative));

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task GrantPermission_ThenRevoke_Succeeds()
    {
        HttpClient client = await ArrangeSuperAdminClientAsync();
        SectionDto section = await FirstSectionAsync(client);
        AuthTestDataBuilder.SeededUsers seeded = await SeedOnceAsync();

        HttpResponseMessage grantResponse = await client.PostAsJsonAsync(
            new Uri($"/api/v1/shorfah/sections/{section.Id}/permissions", UriKind.Relative), new { userId = seeded.Viewer.Id, permission = "Contribute" });

        grantResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        PermissionPayload? grantPayload = await grantResponse.Content.ReadFromJsonAsync<PermissionPayload>();

        HttpResponseMessage revokeResponse = await client.DeleteAsync(new Uri($"/api/v1/shorfah/permissions/{grantPayload!.Permission.Id}", UriKind.Relative));

        revokeResponse.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task GrantPermission_NeitherUserNorRole_ReturnsBadRequest()
    {
        HttpClient client = await ArrangeSuperAdminClientAsync();
        SectionDto section = await FirstSectionAsync(client);

        HttpResponseMessage response = await client.PostAsJsonAsync(
            new Uri($"/api/v1/shorfah/sections/{section.Id}/permissions", UriKind.Relative), new { permission = "Contribute" });

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task RevokePermission_UnknownId_ReturnsNotFound()
    {
        HttpClient client = await ArrangeSuperAdminClientAsync();

        HttpResponseMessage response = await client.DeleteAsync(new Uri("/api/v1/shorfah/permissions/999999", UriKind.Relative));

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task GrantedPermission_ThenViewerCanUploadMedia()
    {
        // Why: the controller-level [Authorize(Policy = "shorfah:edit")] gate and the
        // per-section ShorfahSectionPermission grant are two independent layers -- a user needs
        // the global shorfah:edit RBAC permission just to reach the handler, AND (if not an
        // access-service admin) a qualifying section-level tier once inside it. This arranges a
        // dedicated "shorfah contributor" role carrying only shorfah:edit/:view (no admin_panel,
        // no super_admin) so the section-level grant-and-revoke below is what actually flips the
        // outcome, not a page-level admin bypass.
        HttpClient client = await ArrangeSuperAdminClientAsync();
        SectionDto section = await FirstSectionAsync(client);
        User shorfahEditorUser = await SeedShorfahEditorAsync();
        HttpClient preGrantClient = await ArrangeExistingUserClientAsync(shorfahEditorUser.Email);

        HttpResponseMessage beforeGrantResponse = await preGrantClient.PostAsJsonAsync(
            new Uri($"/api/v1/shorfah/sections/{section.Id}/media", UriKind.Relative),
            new { dataBase64 = TinyPngBase64, contentType = "image/png" });
        beforeGrantResponse.StatusCode.Should().Be(HttpStatusCode.Forbidden, "shorfah:edit alone is not a qualifying section-level tier");

        await client.PostAsJsonAsync(
            new Uri($"/api/v1/shorfah/sections/{section.Id}/permissions", UriKind.Relative), new { userId = shorfahEditorUser.Id, permission = "Contribute" });
        HttpClient viewerClient = await ArrangeExistingUserClientAsync(shorfahEditorUser.Email);

        HttpResponseMessage response = await viewerClient.PostAsJsonAsync(
            new Uri($"/api/v1/shorfah/sections/{section.Id}/media", UriKind.Relative),
            new { dataBase64 = TinyPngBase64, contentType = "image/png" });

        response.StatusCode.Should().Be(HttpStatusCode.OK, "once granted Contribute on this section, the shorfah:edit-holding user must now pass the upload permission check");
    }

    [Fact]
    public async Task UpdateSlaDefaults_ClampsOutOfBoundsAndPropagates()
    {
        HttpClient client = await ArrangeSuperAdminClientAsync();
        await FirstSectionAsync(client);

        HttpResponseMessage response = await client.PutAsJsonAsync(
            new Uri("/api/v1/shorfah/sla-defaults", UriKind.Relative),
            new { defaults = new[] { new { sectionType = "News", slaDays = 999 } }, propagate = true });

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        SlaDefaultsPayload? payload = await response.Content.ReadFromJsonAsync<SlaDefaultsPayload>();
        payload!.Defaults.Should().Contain(d => d.SectionType == "News" && d.SlaDays == 60, "BUSINESS-RULES.md §1.5 clamps slaDays to [1, 60]");
    }

    [Fact]
    public async Task GetSlaDefaults_ReturnsArray()
    {
        HttpClient client = await ArrangeSuperAdminClientAsync();

        HttpResponseMessage response = await client.GetAsync(new Uri("/api/v1/shorfah/sla-defaults", UriKind.Relative));

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task UpdateSectionSla_RecomputesDeadline()
    {
        HttpClient client = await ArrangeSuperAdminClientAsync();
        SectionDto section = await FirstSectionAsync(client);
        var startsAt = new DateTimeOffset(2026, 1, 1, 0, 0, 0, TimeSpan.Zero);

        HttpResponseMessage response = await client.PatchAsJsonAsync(
            new Uri($"/api/v1/shorfah/sections/{section.Id}/sla", UriKind.Relative), new { slaDays = 5, slaStartsAt = startsAt });

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        SectionPayload? payload = await response.Content.ReadFromJsonAsync<SectionPayload>();
        payload!.Section.SlaDeadline.Should().Be(startsAt.AddDays(5));
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
        AuthTestDataBuilder.SeededUsers seeded = await SeedOnceAsync();
        return await LoginAsync(seeded.SuperAdmin.Email);
    }

    private async Task<HttpClient> ArrangeViewerClientAsync()
    {
        AuthTestDataBuilder.SeededUsers seeded = await SeedOnceAsync();
        return await LoginAsync(seeded.Viewer.Email);
    }

    private async Task<AuthTestDataBuilder.SeededUsers> SeedOnceAsync()
    {
        if (_seededUsers is not null)
        {
            return _seededUsers;
        }

        using AppDbContext dbContext = CreateDbContext();
        _seededUsers = await AuthTestDataBuilder.SeedAsync(dbContext, SharedPassword);
        return _seededUsers;
    }

    /// <summary>
    /// Seeds a user whose only role carries the shorfah:edit and shorfah:view RBAC page
    /// permissions -- no admin_panel, no super_admin -- so tests can prove the section-level
    /// permission grant is the thing unlocking access, not a page-level admin bypass.
    /// </summary>
    private async Task<User> SeedShorfahEditorAsync()
    {
        await SeedOnceAsync();
        using AppDbContext dbContext = CreateDbContext();

        // roles.name is unique, so the role name is per-call unique to keep this helper safe to
        // invoke more than once against the same database.
        Role shorfahEditorRole = new() { Name = $"shorfah_editor_{Guid.NewGuid()}", NameAr = "shorfah_editor", CreatedBy = "test" };
        dbContext.Add(shorfahEditorRole);

        Page shorfahPage = await AuthTestDataBuilder.EnsurePageAsync(dbContext, PageSlugs.Shorfah);
        Permission viewPermission = await AuthTestDataBuilder.EnsurePermissionAsync(dbContext, "view");
        Permission editPermission = await AuthTestDataBuilder.EnsurePermissionAsync(dbContext, "edit");
        await dbContext.SaveChangesAsync(CancellationToken.None);

        dbContext.RolePermissions.Add(new RolePermission { RoleId = shorfahEditorRole.Id, PageId = shorfahPage.Id, PermissionId = viewPermission.Id, CreatedBy = "test" });
        dbContext.RolePermissions.Add(new RolePermission { RoleId = shorfahEditorRole.Id, PageId = shorfahPage.Id, PermissionId = editPermission.Id, CreatedBy = "test" });
        await dbContext.SaveChangesAsync(CancellationToken.None);

        var hasher = new PasswordHasher<PasswordHasherSubject>();
        var user = new User
        {
            Email = $"shorfah-editor-{Guid.NewGuid()}@test.local",
            Name = "Shorfah Editor",
            PasswordHash = hasher.HashPassword(new PasswordHasherSubject(), SharedPassword),
            IsActive = true,
            CreatedBy = "test",
        };
        dbContext.Add(user);
        await dbContext.SaveChangesAsync(CancellationToken.None);

        dbContext.UserRoles.Add(new UserRole { UserId = user.Id, RoleId = shorfahEditorRole.Id, AssignedAt = DateTime.UtcNow, CreatedBy = "test" });
        await dbContext.SaveChangesAsync(CancellationToken.None);

        return user;
    }

    private Task<HttpClient> ArrangeExistingUserClientAsync(string email) => LoginAsync(email);

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

    private sealed record SectionDto(int Id, string TitleAr, string WorkflowStatus, DateTimeOffset? SlaDeadline);

    private sealed record CreateIssuePayload(IssueDto Issue);

    private sealed record GetIssuePayload(IssueDto Issue, List<SectionDto> Sections);

    private sealed record SectionPayload(SectionDto Section);

    private sealed record MediaDto(int Id, int SectionId, string MediaUrl, string MediaType, string? CaptionAr, int? DisplayOrder);

    private sealed record MediaPayload(MediaDto Media);

    private sealed record MediaListPayload(List<MediaDto> Media, int Page, int PageSize, int Total);

    private sealed record AssignmentDto(int Id, int SectionId, int UserId, string? Role);

    private sealed record AssignmentPayload(AssignmentDto Assignment);

    private sealed record PermissionDto(int Id, int SectionId, int? UserId, string? RoleName, string Permission);

    private sealed record PermissionPayload(PermissionDto Permission);

    private sealed record SlaDefaultDto(string SectionType, int SlaDays);

    private sealed record SlaDefaultsPayload(List<SlaDefaultDto> Defaults, int PropagatedSections);
}
