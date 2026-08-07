using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using Icbank.Platform.Domain.InternationalDays;
using Icbank.Platform.Domain.Shorfah;
using Icbank.Platform.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Icbank.Platform.IntegrationTests.Auth;

/// <summary>
/// End-to-end coverage for the GAP 2 mint/redeem flow (FRONTEND-WIRING-NOTES.md §4): Shorfah PDF
/// preview/download and the international-days export used to ride session cookies via plain
/// <c>&lt;a href&gt;</c>/<c>window.open</c> navigation and now 401 under bearer-only auth. Verifies
/// the full HTTP surface -- mint requires the normal policy, the <c>via-token</c> routes accept no
/// bearer header at all, and every one of the task's named guarantees (expiry, single use,
/// wrong-resource rejection, tampered-signature rejection, resource authorization still enforced)
/// holds at the controller level, not just inside DownloadTokenService in isolation.
/// </summary>
public sealed class DownloadTokenFlowTests : IDisposable
{
    private const string SharedPassword = "Str0ng!Passw0rd#2026";

    private readonly AuthWebApplicationFactory _factory = new();

    /// <inheritdoc />
    public void Dispose() => _factory.Dispose();

    [Fact]
    public async Task MintShorfahPdfToken_NoBearer_ReturnsUnauthorized()
    {
        using HttpClient client = _factory.CreateClient();

        HttpResponseMessage response = await client.PostAsync(
            new Uri("/api/v1/shorfah/issues/1/pdf/download-token", UriKind.Relative), content: null);

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized, "minting must require the exact same policy as the PDF endpoints themselves");
    }

    [Fact]
    public async Task MintShorfahPdfToken_ThenRedeem_NoBearerHeaderRequired_ReturnsPdfHtml()
    {
        var issueId = await SeedShorfahIssueAsync();
        HttpClient authedClient = await ArrangeAuthenticatedClientAsync(useSuperAdmin: true);

        HttpResponseMessage mintResponse = await authedClient.PostAsync(
            new Uri($"/api/v1/shorfah/issues/{issueId}/pdf/download-token", UriKind.Relative), content: null);
        mintResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        MintPayload? mint = await mintResponse.Content.ReadFromJsonAsync<MintPayload>();
        mint.Should().NotBeNull();

        // Why: this is the entire point of GAP 2 -- a client with no bearer header at all (a plain
        // browser navigation would have none) must still be able to complete the download using
        // only the minted token.
        using HttpClient anonymousClient = _factory.CreateClient();
        HttpResponseMessage redeemResponse = await anonymousClient.GetAsync(
            new Uri($"/api/v1/shorfah/issues/{issueId}/pdf/via-token?token={Uri.EscapeDataString(mint!.Token)}", UriKind.Relative));

        redeemResponse.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task RedeemShorfahPdfToken_SecondUse_ReturnsUnauthorized()
    {
        var issueId = await SeedShorfahIssueAsync();
        HttpClient authedClient = await ArrangeAuthenticatedClientAsync(useSuperAdmin: true);
        MintPayload mint = await MintShorfahTokenAsync(authedClient, issueId);

        using HttpClient anonymousClient = _factory.CreateClient();
        var uri = new Uri($"/api/v1/shorfah/issues/{issueId}/pdf/via-token?token={Uri.EscapeDataString(mint.Token)}", UriKind.Relative);

        HttpResponseMessage first = await anonymousClient.GetAsync(uri);
        HttpResponseMessage second = await anonymousClient.GetAsync(uri);

        first.StatusCode.Should().Be(HttpStatusCode.OK);
        second.StatusCode.Should().Be(HttpStatusCode.Unauthorized, "a redeemed download link must never work twice");
    }

    [Fact]
    public async Task RedeemShorfahPdfToken_AgainstDifferentIssue_ReturnsUnauthorized()
    {
        var issueId = await SeedShorfahIssueAsync();
        var otherIssueId = await SeedShorfahIssueAsync();
        HttpClient authedClient = await ArrangeAuthenticatedClientAsync(useSuperAdmin: true);
        MintPayload mint = await MintShorfahTokenAsync(authedClient, issueId);

        using HttpClient anonymousClient = _factory.CreateClient();
        HttpResponseMessage response = await anonymousClient.GetAsync(
            new Uri($"/api/v1/shorfah/issues/{otherIssueId}/pdf/via-token?token={Uri.EscapeDataString(mint.Token)}", UriKind.Relative));

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized, "a token minted for one issue must never unlock a different issue's PDF");
    }

    [Fact]
    public async Task RedeemShorfahPdfToken_TamperedSignature_ReturnsUnauthorized()
    {
        var issueId = await SeedShorfahIssueAsync();
        HttpClient authedClient = await ArrangeAuthenticatedClientAsync(useSuperAdmin: true);
        MintPayload mint = await MintShorfahTokenAsync(authedClient, issueId);
        var tampered = mint.Token.Length > 1
            ? string.Concat(mint.Token.AsSpan(1), mint.Token.AsSpan(0, 1))
            : mint.Token + "x";

        using HttpClient anonymousClient = _factory.CreateClient();
        HttpResponseMessage response = await anonymousClient.GetAsync(
            new Uri($"/api/v1/shorfah/issues/{issueId}/pdf/via-token?token={Uri.EscapeDataString(tampered)}", UriKind.Relative));

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task RedeemShorfahPdfToken_MissingToken_ReturnsUnauthorized()
    {
        var issueId = await SeedShorfahIssueAsync();

        using HttpClient anonymousClient = _factory.CreateClient();
        HttpResponseMessage response = await anonymousClient.GetAsync(
            new Uri($"/api/v1/shorfah/issues/{issueId}/pdf/via-token", UriKind.Relative));

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task RedeemShorfahPdfToken_IssueDeletedAfterMint_StillEnforcesResourceAuthorization()
    {
        // Why: the task's hardest requirement -- token redemption must never bypass
        // IResourceAuthorizationService. A valid, unexpired, unused token for a resource that no
        // longer exists must still 404 exactly like the bearer-only path would.
        var issueId = await SeedShorfahIssueAsync();
        HttpClient authedClient = await ArrangeAuthenticatedClientAsync(useSuperAdmin: true);
        MintPayload mint = await MintShorfahTokenAsync(authedClient, issueId);

        using (AppDbContext dbContext = CreateDbContext())
        {
            ShorfahIssue issue = await dbContext.ShorfahIssues.FindAsync(issueId) ?? throw new InvalidOperationException("seeded issue missing");
            dbContext.Remove(issue);
            await dbContext.SaveChangesAsync(CancellationToken.None);
        }

        using HttpClient anonymousClient = _factory.CreateClient();
        HttpResponseMessage response = await anonymousClient.GetAsync(
            new Uri($"/api/v1/shorfah/issues/{issueId}/pdf/via-token?token={Uri.EscapeDataString(mint.Token)}", UriKind.Relative));

        response.StatusCode.Should().Be(HttpStatusCode.NotFound, "resource authorization must run even on a perfectly valid token");
    }

    [Fact]
    public async Task RedeemShorfahPdfBinaryToken_ThenRedeem_ReturnsPdfBytes()
    {
        var issueId = await SeedShorfahIssueAsync();
        HttpClient authedClient = await ArrangeAuthenticatedClientAsync(useSuperAdmin: true);
        MintPayload mint = await MintShorfahTokenAsync(authedClient, issueId);

        using HttpClient anonymousClient = _factory.CreateClient();
        HttpResponseMessage response = await anonymousClient.GetAsync(
            new Uri($"/api/v1/shorfah/issues/{issueId}/pdf.pdf/via-token?token={Uri.EscapeDataString(mint.Token)}", UriKind.Relative));

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        response.Content.Headers.ContentType?.MediaType.Should().Be("application/pdf");
    }

    [Fact]
    public async Task MintInternationalDayExportToken_NoBearer_ReturnsUnauthorized()
    {
        using HttpClient client = _factory.CreateClient();

        HttpResponseMessage response = await client.PostAsync(
            new Uri("/api/v1/intl-days/export/1/download-token", UriKind.Relative), content: null);

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task MintInternationalDayExportToken_ThenRedeem_NoBearerHeaderRequired_ReturnsExport()
    {
        var dayId = await SeedInternationalDayAsync();
        HttpClient authedClient = await ArrangeAuthenticatedClientAsync(useSuperAdmin: true);

        HttpResponseMessage mintResponse = await authedClient.PostAsync(
            new Uri($"/api/v1/intl-days/export/{dayId}/download-token", UriKind.Relative), content: null);
        mintResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        MintPayload? mint = await mintResponse.Content.ReadFromJsonAsync<MintPayload>();

        using HttpClient anonymousClient = _factory.CreateClient();
        HttpResponseMessage redeemResponse = await anonymousClient.GetAsync(
            new Uri($"/api/v1/intl-days/export/{dayId}/via-token?token={Uri.EscapeDataString(mint!.Token)}", UriKind.Relative));

        redeemResponse.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task RedeemInternationalDayExportToken_SecondUse_ReturnsUnauthorized()
    {
        var dayId = await SeedInternationalDayAsync();
        HttpClient authedClient = await ArrangeAuthenticatedClientAsync(useSuperAdmin: true);
        HttpResponseMessage mintResponse = await authedClient.PostAsync(
            new Uri($"/api/v1/intl-days/export/{dayId}/download-token", UriKind.Relative), content: null);
        MintPayload mint = (await mintResponse.Content.ReadFromJsonAsync<MintPayload>())!;

        using HttpClient anonymousClient = _factory.CreateClient();
        var uri = new Uri($"/api/v1/intl-days/export/{dayId}/via-token?token={Uri.EscapeDataString(mint.Token)}", UriKind.Relative);

        HttpResponseMessage first = await anonymousClient.GetAsync(uri);
        HttpResponseMessage second = await anonymousClient.GetAsync(uri);

        first.StatusCode.Should().Be(HttpStatusCode.OK);
        second.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task RedeemInternationalDayExportToken_AgainstDifferentDay_ReturnsUnauthorized()
    {
        var dayId = await SeedInternationalDayAsync();
        var otherDayId = await SeedInternationalDayAsync();
        HttpClient authedClient = await ArrangeAuthenticatedClientAsync(useSuperAdmin: true);
        HttpResponseMessage mintResponse = await authedClient.PostAsync(
            new Uri($"/api/v1/intl-days/export/{dayId}/download-token", UriKind.Relative), content: null);
        MintPayload mint = (await mintResponse.Content.ReadFromJsonAsync<MintPayload>())!;

        using HttpClient anonymousClient = _factory.CreateClient();
        HttpResponseMessage response = await anonymousClient.GetAsync(
            new Uri($"/api/v1/intl-days/export/{otherDayId}/via-token?token={Uri.EscapeDataString(mint.Token)}", UriKind.Relative));

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task RedeemInternationalDayExportToken_MintedForShorfahResource_ReturnsUnauthorized()
    {
        // Why: cross-family rejection -- DownloadResourceType is a closed enum specifically so a
        // Shorfah-scoped token can never unlock an international-days export even if the numeric
        // ids happen to collide.
        var issueId = await SeedShorfahIssueAsync();
        HttpClient authedClient = await ArrangeAuthenticatedClientAsync(useSuperAdmin: true);
        MintPayload shorfahMint = await MintShorfahTokenAsync(authedClient, issueId);

        using HttpClient anonymousClient = _factory.CreateClient();
        HttpResponseMessage response = await anonymousClient.GetAsync(
            new Uri($"/api/v1/intl-days/export/{issueId}/via-token?token={Uri.EscapeDataString(shorfahMint.Token)}", UriKind.Relative));

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    private static async Task<MintPayload> MintShorfahTokenAsync(HttpClient authedClient, int issueId)
    {
        HttpResponseMessage mintResponse = await authedClient.PostAsync(
            new Uri($"/api/v1/shorfah/issues/{issueId}/pdf/download-token", UriKind.Relative), content: null);
        mintResponse.EnsureSuccessStatusCode();
        return (await mintResponse.Content.ReadFromJsonAsync<MintPayload>())!;
    }

    /// <summary>
    /// Seeds a Shorfah issue, allocating the next free issue number.
    /// </summary>
    /// <remarks>
    /// IssueNo has to be assigned explicitly. shorfah_issues carries a unique index on
    /// issue_no (ux_shorfah_issues_issue_no), so leaving it at the CLR default of 0 means the
    /// helper can only ever be called once per test database -- the second call collides.
    /// That is a real constraint doing its job, not a schema quirk to work around: issue
    /// numbers are what readers cite, so two issues sharing one is meaningless.
    ///
    /// This only ever surfaced in CI. Locally there is no SQL Server, the suite falls back to
    /// the InMemory provider, and InMemory does not enforce unique indexes at all -- so the
    /// duplicate insert succeeded and the test passed against a database state that SQL Server
    /// would never have allowed.
    /// </remarks>
    private async Task<int> SeedShorfahIssueAsync()
    {
        using AppDbContext dbContext = CreateDbContext();

        // Derived from the table rather than a counter so the value stays correct no matter
        // what else has already seeded an issue (the app's own seeder, or another helper).
        int nextIssueNo = await dbContext.ShorfahIssues
            .Select(existing => (int?)existing.IssueNo)
            .MaxAsync(CancellationToken.None) ?? 0;

        var issue = new ShorfahIssue
        {
            TitleAr = "عدد تجريبي",
            Month = 8,
            Year = 2026,
            IssueNo = nextIssueNo + 1,
            CreatedBy = "test",
        };
        dbContext.Add(issue);
        await dbContext.SaveChangesAsync(CancellationToken.None);
        return issue.Id;
    }

    private async Task<int> SeedInternationalDayAsync()
    {
        using AppDbContext dbContext = CreateDbContext();
        var day = new InternationalDay { DayNameAr = "اليوم العالمي للاختبار", AnnualDate = "01-01", CreatedBy = "test" };
        dbContext.Add(day);
        await dbContext.SaveChangesAsync(CancellationToken.None);
        return day.Id;
    }

    private async Task<HttpClient> ArrangeAuthenticatedClientAsync(bool useSuperAdmin = false, bool useViewer = false)
    {
        using AppDbContext dbContext = CreateDbContext();
        AuthTestDataBuilder.SeededUsers seeded = await AuthTestDataBuilder.SeedAsync(dbContext, SharedPassword);

        var email = useSuperAdmin ? seeded.SuperAdmin.Email : useViewer ? seeded.Viewer.Email : seeded.Admin.Email;

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

    private sealed record MintPayload(string Token, int ExpiresInSeconds);
}
