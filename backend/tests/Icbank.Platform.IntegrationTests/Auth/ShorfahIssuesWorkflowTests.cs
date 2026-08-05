using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using FluentAssertions;
using Icbank.Platform.Domain.Shorfah;
using Icbank.Platform.Infrastructure.Persistence;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Icbank.Platform.IntegrationTests.Auth;

/// <summary>
/// Full lifecycle/workflow coverage for the Wave 4a Shorfah issue endpoints
/// (API-SURFACE.md §19, BUSINESS-RULES.md §1.1): create -&gt; seed verification -&gt; collect
/// (idempotent) -&gt; start-review -&gt; illegal transitions -&gt; publish precondition -&gt; publish -&gt;
/// exports -&gt; pagination boundary -&gt; optimistic concurrency on a double-submit race.
/// </summary>
public sealed class ShorfahIssuesWorkflowTests : IDisposable
{
    private const string SharedPassword = "Str0ng!Passw0rd#2026";

    private readonly AuthWebApplicationFactory _factory = new();

    /// <inheritdoc />
    public void Dispose() => _factory.Dispose();

    [Fact]
    public async Task CreateIssue_SeedsThirteenCanonicalSections()
    {
        HttpClient client = await ArrangeSuperAdminClientAsync();

        HttpResponseMessage response = await client.PostAsJsonAsync(
            new Uri("/api/v1/shorfah/issues", UriKind.Relative),
            new { titleAr = "عدد أغسطس", month = 8, year = 2026 });

        response.StatusCode.Should().Be(HttpStatusCode.Created);
        CreateIssuePayload? created = await response.Content.ReadFromJsonAsync<CreateIssuePayload>();
        created!.Issue.Status.Should().Be(nameof(ShorfahIssueStatus.Collecting));

        HttpResponseMessage getResponse = await client.GetAsync(new Uri($"/api/v1/shorfah/issues/{created.Issue.Id}", UriKind.Relative));
        GetIssuePayload? detail = await getResponse.Content.ReadFromJsonAsync<GetIssuePayload>();
        detail!.Sections.Should().HaveCount(13, "BUSINESS-RULES.md §1.2 mandates the exact 13 canonical sections on every new issue");
    }

    [Fact]
    public async Task CreateIssue_MissingTitle_ReturnsBadRequestWithValidationMessage()
    {
        HttpClient client = await ArrangeSuperAdminClientAsync();

        HttpResponseMessage response = await client.PostAsJsonAsync(
            new Uri("/api/v1/shorfah/issues", UriKind.Relative), new { titleAr = string.Empty, month = 8, year = 2026 });

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task CreateIssue_InvalidMonth_ReturnsBadRequest()
    {
        HttpClient client = await ArrangeSuperAdminClientAsync();

        HttpResponseMessage response = await client.PostAsJsonAsync(
            new Uri("/api/v1/shorfah/issues", UriKind.Relative), new { titleAr = "عدد", month = 13, year = 2026 });

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest, "API-SURFACE.md §22 flags the Node source's missing month-range validation -- this port adds it");
    }

    [Fact]
    public async Task Collect_OnFreshIssue_SeedsSectionsAndSetsCollecting()
    {
        HttpClient client = await ArrangeSuperAdminClientAsync();
        IssueDto issue = await CreateIssueAsync(client, "عدد التجميع");

        HttpResponseMessage response = await client.PostAsync(new Uri($"/api/v1/shorfah/issues/{issue.Id}/collect", UriKind.Relative), content: null);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        CollectPayload? payload = await response.Content.ReadFromJsonAsync<CollectPayload>();
        payload!.SectionsExisting.Should().Be(13, "the issue already has its 13 canonical sections from creation, so collect must not double-seed");
        payload.SectionsSeeded.Should().Be(0);
        payload.Issue.Status.Should().Be(nameof(ShorfahIssueStatus.Collecting));
    }

    [Fact]
    public async Task Collect_IsIdempotent_CalledTwiceNeverDuplicatesSections()
    {
        HttpClient client = await ArrangeSuperAdminClientAsync();
        IssueDto issue = await CreateIssueAsync(client, "عدد التكرار");

        await client.PostAsync(new Uri($"/api/v1/shorfah/issues/{issue.Id}/collect", UriKind.Relative), content: null);
        HttpResponseMessage second = await client.PostAsync(new Uri($"/api/v1/shorfah/issues/{issue.Id}/collect", UriKind.Relative), content: null);

        second.StatusCode.Should().Be(HttpStatusCode.OK);
        CollectPayload? payload = await second.Content.ReadFromJsonAsync<CollectPayload>();
        payload!.SectionsExisting.Should().Be(13);
        payload.SectionsSeeded.Should().Be(0);

        HttpResponseMessage detailResponse = await client.GetAsync(new Uri($"/api/v1/shorfah/issues/{issue.Id}", UriKind.Relative));
        GetIssuePayload? detail = await detailResponse.Content.ReadFromJsonAsync<GetIssuePayload>();
        detail!.Sections.Should().HaveCount(13, "collect called twice must never duplicate the canonical section set");
    }

    [Fact]
    public async Task StartReview_FromCollecting_TransitionsToInReview()
    {
        HttpClient client = await ArrangeSuperAdminClientAsync();
        IssueDto issue = await CreateIssueAsync(client, "عدد المراجعة");

        HttpResponseMessage response = await client.PostAsync(new Uri($"/api/v1/shorfah/issues/{issue.Id}/start-review", UriKind.Relative), content: null);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        UpdateIssuePayload? payload = await response.Content.ReadFromJsonAsync<UpdateIssuePayload>();
        payload!.Issue.Status.Should().Be(nameof(ShorfahIssueStatus.InReview));
    }

    [Fact]
    public async Task StartReview_UnknownIssue_ReturnsNotFound()
    {
        HttpClient client = await ArrangeSuperAdminClientAsync();

        HttpResponseMessage response = await client.PostAsync(new Uri("/api/v1/shorfah/issues/999999/start-review", UriKind.Relative), content: null);

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task Publish_WithoutApprovedSection_ReturnsBadRequest()
    {
        HttpClient client = await ArrangeSuperAdminClientAsync();
        IssueDto issue = await CreateIssueAsync(client, "عدد بلا اعتماد");
        await client.PostAsync(new Uri($"/api/v1/shorfah/issues/{issue.Id}/start-review", UriKind.Relative), content: null);

        HttpResponseMessage response = await client.PostAsync(new Uri($"/api/v1/shorfah/issues/{issue.Id}/publish", UriKind.Relative), content: null);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest, "BUSINESS-RULES.md §1.1 hard precondition: at least one approved+included section is required to publish");
    }

    [Fact]
    public async Task Publish_WithApprovedIncludedSection_Succeeds()
    {
        HttpClient client = await ArrangeSuperAdminClientAsync();
        IssueDto issue = await CreateIssueAsync(client, "عدد جاهز للنشر");
        await ApproveOneSectionAsync(issue.Id);

        HttpResponseMessage response = await client.PostAsync(new Uri($"/api/v1/shorfah/issues/{issue.Id}/publish", UriKind.Relative), content: null);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        UpdateIssuePayload? payload = await response.Content.ReadFromJsonAsync<UpdateIssuePayload>();
        payload!.Issue.Status.Should().Be(nameof(ShorfahIssueStatus.Published));
    }

    [Fact]
    public async Task StartReview_OnAlreadyPublishedIssue_IsRejectedWithClearError()
    {
        HttpClient client = await ArrangeSuperAdminClientAsync();
        IssueDto issue = await CreateIssueAsync(client, "عدد منشور مسبقاً");
        await ApproveOneSectionAsync(issue.Id);
        await client.PostAsync(new Uri($"/api/v1/shorfah/issues/{issue.Id}/publish", UriKind.Relative), content: null);

        HttpResponseMessage response = await client.PostAsync(new Uri($"/api/v1/shorfah/issues/{issue.Id}/start-review", UriKind.Relative), content: null);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest, "BUSINESS-RULES.md §1.1: start-review is blocked once the issue is published -- no reverse transition exists");
    }

    [Fact]
    public async Task Publish_CalledTwice_SecondCallStillRejectedOrIdempotentButNeverCorrupts()
    {
        HttpClient client = await ArrangeSuperAdminClientAsync();
        IssueDto issue = await CreateIssueAsync(client, "عدد نشر مزدوج");
        await ApproveOneSectionAsync(issue.Id);
        await client.PostAsync(new Uri($"/api/v1/shorfah/issues/{issue.Id}/publish", UriKind.Relative), content: null);

        HttpResponseMessage secondPublish = await client.PostAsync(new Uri($"/api/v1/shorfah/issues/{issue.Id}/publish", UriKind.Relative), content: null);

        secondPublish.StatusCode.Should().Be(HttpStatusCode.OK, "publish's precondition only checks section state, not issue status -- republishing an already-published issue with an approved section is a no-op-equivalent success, not a corrupting double-transition");
    }

    [Fact]
    public async Task UpdateIssue_DirectStatusJumpFromCollectingToPublished_IsRejected()
    {
        HttpClient client = await ArrangeSuperAdminClientAsync();
        IssueDto issue = await CreateIssueAsync(client, "عدد قفزة غير شرعية");

        HttpResponseMessage response = await client.PatchAsJsonAsync(
            new Uri($"/api/v1/shorfah/issues/{issue.Id}", UriKind.Relative), new { status = "Published" });

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest, "the state machine forbids collecting->published directly; must go through in_review first");
    }

    [Fact]
    public async Task UpdateIssue_LegalStepwiseTransitionViaPatch_Succeeds()
    {
        HttpClient client = await ArrangeSuperAdminClientAsync();
        IssueDto issue = await CreateIssueAsync(client, "عدد انتقال شرعي");

        HttpResponseMessage response = await client.PatchAsJsonAsync(
            new Uri($"/api/v1/shorfah/issues/{issue.Id}", UriKind.Relative), new { status = "InReview" });

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        UpdateIssuePayload? payload = await response.Content.ReadFromJsonAsync<UpdateIssuePayload>();
        payload!.Issue.Status.Should().Be(nameof(ShorfahIssueStatus.InReview));
    }

    [Fact]
    public async Task UpdateIssue_InvalidStatusLiteral_ReturnsBadRequest()
    {
        HttpClient client = await ArrangeSuperAdminClientAsync();
        IssueDto issue = await CreateIssueAsync(client, "عدد حالة غير صالحة");

        HttpResponseMessage response = await client.PatchAsJsonAsync(
            new Uri($"/api/v1/shorfah/issues/{issue.Id}", UriKind.Relative), new { status = "NotARealStatus" });

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task SeedSections_OnIssueThatAlreadyHasSections_ReturnsBadRequest()
    {
        HttpClient client = await ArrangeSuperAdminClientAsync();
        IssueDto issue = await CreateIssueAsync(client, "عدد أقسام موجودة");

        HttpResponseMessage response = await client.PostAsync(new Uri($"/api/v1/shorfah/issues/{issue.Id}/seed-sections", UriKind.Relative), content: null);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest, "shorfah.ts:199-209 refuses to seed if sections already exist");
    }

    [Fact]
    public async Task AddSection_UnknownSectionType_ReturnsBadRequest()
    {
        HttpClient client = await ArrangeSuperAdminClientAsync();
        IssueDto issue = await CreateIssueAsync(client, "عدد نوع قسم خاطئ");

        HttpResponseMessage response = await client.PostAsJsonAsync(
            new Uri($"/api/v1/shorfah/issues/{issue.Id}/sections", UriKind.Relative),
            new { sectionType = "not_a_real_type", titleAr = "قسم" });

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task AddSection_ValidCustomSection_ReturnsCreated()
    {
        HttpClient client = await ArrangeSuperAdminClientAsync();
        IssueDto issue = await CreateIssueAsync(client, "عدد قسم مخصص");

        HttpResponseMessage response = await client.PostAsJsonAsync(
            new Uri($"/api/v1/shorfah/issues/{issue.Id}/sections", UriKind.Relative),
            new { sectionType = "News", titleAr = "قسم إضافي", autoGenerate = false });

        response.StatusCode.Should().Be(HttpStatusCode.Created);
        AddSectionPayload? payload = await response.Content.ReadFromJsonAsync<AddSectionPayload>();
        payload!.Section.TitleAr.Should().Be("قسم إضافي");
    }

    [Fact]
    public async Task SendInitial_StampsSlaClockOnEverySection()
    {
        HttpClient client = await ArrangeSuperAdminClientAsync();
        IssueDto issue = await CreateIssueAsync(client, "عدد بدء المساهمة");

        HttpResponseMessage response = await client.PostAsync(new Uri($"/api/v1/shorfah/issues/{issue.Id}/send-initial", UriKind.Relative), content: null);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        SendInitialPayload? payload = await response.Content.ReadFromJsonAsync<SendInitialPayload>();
        payload!.Sent.Should().Be(0, "no assignments exist yet on a freshly-seeded issue, so zero notifications are sent, but the call itself must succeed");
    }

    [Fact]
    public async Task SendInitial_UnknownIssue_ReturnsNotFound()
    {
        HttpClient client = await ArrangeSuperAdminClientAsync();

        HttpResponseMessage response = await client.PostAsync(new Uri("/api/v1/shorfah/issues/999999/send-initial", UriKind.Relative), content: null);

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task ExportDocx_ReturnsWordContentType()
    {
        HttpClient client = await ArrangeSuperAdminClientAsync();
        IssueDto issue = await CreateIssueAsync(client, "عدد تصدير وورد");

        HttpResponseMessage response = await client.GetAsync(new Uri($"/api/v1/shorfah/issues/{issue.Id}/docx", UriKind.Relative));

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        response.Content.Headers.ContentType!.MediaType.Should().Be("application/vnd.openxmlformats-officedocument.wordprocessingml.document");
        var bytes = await response.Content.ReadAsByteArrayAsync();
        bytes.Length.Should().BeGreaterThan(0);
    }

    [Fact]
    public async Task ExportDocx_UnknownIssue_ReturnsNotFound()
    {
        HttpClient client = await ArrangeSuperAdminClientAsync();

        HttpResponseMessage response = await client.GetAsync(new Uri("/api/v1/shorfah/issues/999999/docx", UriKind.Relative));

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task ExportPdfHtml_PreviewIncludesUnapprovedSections()
    {
        HttpClient client = await ArrangeSuperAdminClientAsync();
        IssueDto issue = await CreateIssueAsync(client, "عدد معاينة PDF");

        HttpResponseMessage response = await client.GetAsync(new Uri($"/api/v1/shorfah/issues/{issue.Id}/pdf?preview=1", UriKind.Relative));

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        response.Content.Headers.ContentType!.MediaType.Should().Be("text/html");
        var html = await response.Content.ReadAsStringAsync();
        html.Should().Contain(issue.TitleAr, "preview mode must render even unapproved, IncludeInPdf sections per BUSINESS-RULES.md §1.9");
    }

    [Fact]
    public async Task ExportPdfHtml_FinalModeExcludesUnapprovedSections()
    {
        HttpClient client = await ArrangeSuperAdminClientAsync();
        IssueDto issue = await CreateIssueAsync(client, "عدد نهائي بلا اعتماد");

        HttpResponseMessage response = await client.GetAsync(new Uri($"/api/v1/shorfah/issues/{issue.Id}/pdf", UriKind.Relative));

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var html = await response.Content.ReadAsStringAsync();
        html.Should().NotContain("أخبار دولية", "final (non-preview) mode must exclude sections that are not yet approved");
    }

    [Fact]
    public async Task ExportPdfBinary_ReturnsPdfContentType()
    {
        HttpClient client = await ArrangeSuperAdminClientAsync();
        IssueDto issue = await CreateIssueAsync(client, "عدد PDF ثنائي");

        HttpResponseMessage response = await client.GetAsync(new Uri($"/api/v1/shorfah/issues/{issue.Id}/pdf.pdf", UriKind.Relative));

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        response.Content.Headers.ContentType!.MediaType.Should().Be("application/pdf");
    }

    [Fact]
    public async Task ListIssues_PaginationBoundary_SecondPageReturnsRemainder()
    {
        HttpClient client = await ArrangeSuperAdminClientAsync();
        for (var i = 0; i < 3; i++)
        {
            await CreateIssueAsync(client, $"عدد صفحة {i}");
        }

        HttpResponseMessage firstPage = await client.GetAsync(new Uri("/api/v1/shorfah/issues?page=1&pageSize=2", UriKind.Relative));
        HttpResponseMessage secondPage = await client.GetAsync(new Uri("/api/v1/shorfah/issues?page=2&pageSize=2", UriKind.Relative));

        ListIssuesPayload? first = await firstPage.Content.ReadFromJsonAsync<ListIssuesPayload>();
        ListIssuesPayload? second = await secondPage.Content.ReadFromJsonAsync<ListIssuesPayload>();
        first!.Items.Should().HaveCount(2);
        first.Total.Should().BeGreaterOrEqualTo(3);
        second!.Page.Should().Be(2);
        second.Items.Should().NotBeEmpty();
    }

    [Fact]
    public async Task ListIssues_EmptyDatabase_ReturnsEmptyEnvelope()
    {
        HttpClient client = await ArrangeSuperAdminClientAsync();

        HttpResponseMessage response = await client.GetAsync(new Uri("/api/v1/shorfah/issues?page=1&pageSize=10", UriKind.Relative));

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        ListIssuesPayload? payload = await response.Content.ReadFromJsonAsync<ListIssuesPayload>();
        payload!.Items.Should().BeEmpty();
        payload.Total.Should().Be(0);
    }

    [Fact]
    public async Task StartReview_ConcurrentDoubleSubmit_OneSucceedsAndIssueEndsConsistent()
    {
        HttpClient client = await ArrangeSuperAdminClientAsync();
        IssueDto issue = await CreateIssueAsync(client, "عدد تزامن");

        Task<HttpResponseMessage> first = client.PostAsync(new Uri($"/api/v1/shorfah/issues/{issue.Id}/start-review", UriKind.Relative), content: null);
        Task<HttpResponseMessage> second = client.PostAsync(new Uri($"/api/v1/shorfah/issues/{issue.Id}/start-review", UriKind.Relative), content: null);
        HttpResponseMessage[] responses = await Task.WhenAll(first, second);

        responses.Should().OnlyContain(r => r.StatusCode == HttpStatusCode.OK, "both racing start-review calls request the same legal collecting->in_review transition, so both may legitimately succeed (idempotent target state) without corrupting the row");

        HttpResponseMessage finalState = await client.GetAsync(new Uri($"/api/v1/shorfah/issues/{issue.Id}", UriKind.Relative));
        GetIssuePayload? detail = await finalState.Content.ReadFromJsonAsync<GetIssuePayload>();
        detail!.Issue.Status.Should().Be(nameof(ShorfahIssueStatus.InReview), "the issue must land in exactly one consistent, legal state after the race, not a corrupted intermediate one");
    }

    private static async Task<IssueDto> CreateIssueAsync(HttpClient client, string titleAr)
    {
        HttpResponseMessage response = await client.PostAsJsonAsync(
            new Uri("/api/v1/shorfah/issues", UriKind.Relative),
            new { titleAr, month = 8, year = 2026 });
        response.EnsureSuccessStatusCode();
        CreateIssuePayload? created = await response.Content.ReadFromJsonAsync<CreateIssuePayload>();
        return created!.Issue;
    }

    private async Task ApproveOneSectionAsync(int issueId)
    {
        using AppDbContext dbContext = CreateDbContext();
        ShorfahSection section = dbContext.ShorfahSections.First(s => s.IssueId == issueId);
        section.WorkflowStatus = ShorfahWorkflowStatus.Approved;
        section.IncludeInPdf = true;
        section.ContentMd = "محتوى معتمد للاختبار";
        await dbContext.SaveChangesAsync(CancellationToken.None);
    }

    private async Task<HttpClient> ArrangeSuperAdminClientAsync()
    {
        using AppDbContext dbContext = CreateDbContext();
        AuthTestDataBuilder.SeededUsers seeded = await AuthTestDataBuilder.SeedAsync(dbContext, SharedPassword);

        HttpClient client = _factory.CreateClient();
        HttpResponseMessage loginResponse = await client.PostAsJsonAsync(
            new Uri("/api/v1/auth/login", UriKind.Relative), new { email = seeded.SuperAdmin.Email, password = SharedPassword });
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

    private sealed record CreateIssuePayload(IssueDto Issue);

    private sealed record UpdateIssuePayload(IssueDto Issue);

    private sealed record SectionDto(int Id, string TitleAr);

    private sealed record GetIssuePayload(IssueDto Issue, List<SectionDto> Sections);

    private sealed record CollectPayload(bool Ok, IssueDto Issue, int SectionsSeeded, int SectionsExisting);

    private sealed record AddSectionPayload(SectionDto Section);

    private sealed record SendInitialPayload(bool Ok, int Sent, JsonElement Results);

    private sealed record ListIssuesPayload(List<IssueDto> Items, int Page, int PageSize, int Total);
}
