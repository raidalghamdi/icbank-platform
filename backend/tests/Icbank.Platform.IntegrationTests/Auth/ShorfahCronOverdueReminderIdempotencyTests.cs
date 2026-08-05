using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using FluentAssertions;
using Icbank.Platform.Domain.Identity;
using Icbank.Platform.Domain.Shorfah;
using Icbank.Platform.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Icbank.Platform.IntegrationTests.Auth;

/// <summary>
/// Proves the fix for AMBIGUOUS-BR-2 (BUSINESS-RULES.md §1.6): the Node source's
/// <c>check-overdue</c> cron had no cooldown and could re-notify every assignee on every
/// invocation -- run hourly, that is up to ~168 duplicate reminder emails per section/recipient a
/// week. This port caps it at one <see cref="ShorfahReminderType.Overdue"/> reminder per
/// section/recipient per Riyadh calendar day. Also proves SEC-13: the endpoint is unreachable
/// without the configured cron API key.
/// </summary>
public sealed class ShorfahCronOverdueReminderIdempotencyTests : IDisposable
{
    private const string CronApiKey = "test-cron-key";

    private readonly AuthWebApplicationFactory _factory = new();

    /// <inheritdoc />
    public void Dispose() => _factory.Dispose();

    [Fact]
    public async Task CheckOverdue_CalledTwiceSameRiyadhDay_SecondRunNotifiesZero()
    {
        _factory.Clock.FixedUtcNow = new DateTimeOffset(2026, 8, 5, 6, 0, 0, TimeSpan.Zero);
        (ShorfahSection section, User assignee) = await SeedOverdueSectionWithAssignmentAsync();
        HttpClient client = CronClient();

        HttpResponseMessage firstResponse = await client.PostAsync(new Uri("/api/v1/shorfah/cron/check-overdue", UriKind.Relative), content: null);
        firstResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        CheckOverduePayload? firstPayload = await firstResponse.Content.ReadFromJsonAsync<CheckOverduePayload>();
        firstPayload!.Notified.Should().Be(1, "the first run of the day must send exactly one reminder for the one overdue assignee");

        // Why: same Riyadh calendar day, later the same day -- a real cron scheduled hourly would
        // call this endpoint many more times before Riyadh midnight without the fix.
        _factory.Clock.FixedUtcNow = _factory.Clock.FixedUtcNow.AddHours(4);

        HttpResponseMessage secondResponse = await client.PostAsync(new Uri("/api/v1/shorfah/cron/check-overdue", UriKind.Relative), content: null);

        secondResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        CheckOverduePayload? secondPayload = await secondResponse.Content.ReadFromJsonAsync<CheckOverduePayload>();
        secondPayload!.Notified.Should().Be(0, "a second call the same Riyadh day must send zero new reminders -- this is the AMBIGUOUS-BR-2 regression guard");
        secondPayload.OverdueSections.Should().Be(1, "the section is still overdue; only the *notification* is deduped, not the overdue detection itself");

        (await ReminderCountAsync(section.Id, assignee.Id)).Should().Be(1, "exactly one ShorfahReminder row must exist for this section/recipient after both calls");
        (await NotificationCountAsync(assignee.Id)).Should().Be(1, "exactly one in-app ShorfahNotification row must exist after both calls");
    }

    [Fact]
    public async Task CheckOverdue_CalledAgainNextRiyadhDay_SendsANewReminder()
    {
        _factory.Clock.FixedUtcNow = new DateTimeOffset(2026, 8, 5, 6, 0, 0, TimeSpan.Zero);
        (ShorfahSection section, User assignee) = await SeedOverdueSectionWithAssignmentAsync();
        HttpClient client = CronClient();

        HttpResponseMessage firstResponse = await client.PostAsync(new Uri("/api/v1/shorfah/cron/check-overdue", UriKind.Relative), content: null);
        CheckOverduePayload? firstPayload = await firstResponse.Content.ReadFromJsonAsync<CheckOverduePayload>();
        firstPayload!.Notified.Should().Be(1);

        // Why: crossing the Riyadh midnight boundary is the one case where a *new* reminder is
        // expected -- proves the dedup key is genuinely per-calendar-day, not a permanent
        // one-and-done suppression that would silently stop reminding an overdue contributor
        // forever after the first nudge.
        _factory.Clock.FixedUtcNow = _factory.Clock.FixedUtcNow.AddDays(1);

        HttpResponseMessage secondResponse = await client.PostAsync(new Uri("/api/v1/shorfah/cron/check-overdue", UriKind.Relative), content: null);

        CheckOverduePayload? secondPayload = await secondResponse.Content.ReadFromJsonAsync<CheckOverduePayload>();
        secondPayload!.Notified.Should().Be(1, "a new Riyadh calendar day must allow exactly one fresh reminder");
        (await ReminderCountAsync(section.Id, assignee.Id)).Should().Be(2, "one reminder per day across the two distinct days");
    }

    [Fact]
    public async Task CheckOverdue_NoSectionsOverdue_NotifiesZeroAndIsIdempotentTrivially()
    {
        HttpClient client = CronClient();

        HttpResponseMessage response = await client.PostAsync(new Uri("/api/v1/shorfah/cron/check-overdue", UriKind.Relative), content: null);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        CheckOverduePayload? payload = await response.Content.ReadFromJsonAsync<CheckOverduePayload>();
        payload!.OverdueSections.Should().Be(0);
        payload.Notified.Should().Be(0);
    }

    [Fact]
    public async Task CheckOverdue_WithoutCronApiKey_ReturnsUnauthorized()
    {
        using HttpClient client = _factory.CreateClient();

        HttpResponseMessage response = await client.PostAsync(new Uri("/api/v1/shorfah/cron/check-overdue", UriKind.Relative), content: null);

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized, "SEC-13: the cron endpoint must refuse a caller that does not present the configured cron API key");
    }

    [Fact]
    public async Task CheckOverdue_WithWrongCronApiKey_ReturnsUnauthorized()
    {
        using HttpClient client = _factory.CreateClient();
        client.DefaultRequestHeaders.Add("X-Api-Key", "not-the-real-key");

        HttpResponseMessage response = await client.PostAsync(new Uri("/api/v1/shorfah/cron/check-overdue", UriKind.Relative), content: null);

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task CheckOverdue_WithBearerTokenInsteadOfCronKey_ReturnsUnauthorized()
    {
        // Why: proves the cron route is on its own distinct auth scheme, not merely "any
        // authenticated user" -- a normal user's access token must not substitute for the cron
        // API key, matching SEC-13's service-to-service-only intent.
        using AppDbContext dbContext = CreateDbContext();
        AuthTestDataBuilder.SeededUsers seeded = await AuthTestDataBuilder.SeedAsync(dbContext, "Str0ng!Passw0rd#2026");
        HttpClient client = _factory.CreateClient();
        HttpResponseMessage loginResponse = await client.PostAsJsonAsync(
            new Uri("/api/v1/auth/login", UriKind.Relative), new { email = seeded.SuperAdmin.Email, password = "Str0ng!Passw0rd#2026" });
        loginResponse.EnsureSuccessStatusCode();
        LoginResponsePayload? loginPayload = await loginResponse.Content.ReadFromJsonAsync<LoginResponsePayload>();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", loginPayload!.AccessToken);

        HttpResponseMessage response = await client.PostAsync(new Uri("/api/v1/shorfah/cron/check-overdue", UriKind.Relative), content: null);

        // Why: this request IS authenticated (a valid user bearer token), just not authorized
        // for the cron-only policy -- ASP.NET's convention is 403 here, distinct from the 401
        // an entirely unauthenticated caller gets in the other tests in this class. Both outcomes
        // equally prove SEC-13: a normal user token can never substitute for the cron API key.
        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    private HttpClient CronClient()
    {
        HttpClient client = _factory.CreateClient();
        client.DefaultRequestHeaders.Add("X-Api-Key", CronApiKey);
        return client;
    }

    private async Task<int> ReminderCountAsync(int sectionId, int recipientUserId)
    {
        using AppDbContext dbContext = CreateDbContext();
        return await dbContext.ShorfahReminders.CountAsync(
            r => r.SectionId == sectionId && r.RecipientUserId == recipientUserId && r.ReminderType == ShorfahReminderType.Overdue);
    }

    private async Task<int> NotificationCountAsync(int userId)
    {
        using AppDbContext dbContext = CreateDbContext();
        return await dbContext.ShorfahNotifications.CountAsync(n => n.UserId == userId && n.Type == "reminder_overdue");
    }

    /// <summary>Seeds one issue/section past its SLA deadline (relative to the factory's fake clock), with one assigned contributor.</summary>
    private async Task<(ShorfahSection Section, User Assignee)> SeedOverdueSectionWithAssignmentAsync()
    {
        using AppDbContext dbContext = CreateDbContext();

        var assignee = new User { Email = $"overdue-assignee-{Guid.NewGuid()}@test.local", Name = "Overdue Assignee", IsActive = true, CreatedBy = "test" };
        dbContext.Add(assignee);
        await dbContext.SaveChangesAsync(CancellationToken.None);

        var issue = new ShorfahIssue { TitleAr = "عدد تجريبي", Month = 8, Year = 2026, CreatedBy = "test" };
        dbContext.Add(issue);
        await dbContext.SaveChangesAsync(CancellationToken.None);

        var section = new ShorfahSection
        {
            IssueId = issue.Id,
            SectionType = ShorfahSectionType.News,
            TitleAr = "قسم متأخر",
            WorkflowStatus = ShorfahWorkflowStatus.PendingContribution,
            SlaDays = 3,
            SlaStartsAt = _factory.Clock.FixedUtcNow.AddDays(-10),
            SlaDeadline = _factory.Clock.FixedUtcNow.AddDays(-7),
            CreatedBy = "test",
        };
        dbContext.Add(section);
        await dbContext.SaveChangesAsync(CancellationToken.None);

        dbContext.Add(new ShorfahAssignment { SectionId = section.Id, UserId = assignee.Id, Role = "contributor", CreatedBy = "test" });
        await dbContext.SaveChangesAsync(CancellationToken.None);

        return (section, assignee);
    }

    private AppDbContext CreateDbContext()
    {
        IServiceScope scope = _factory.Services.CreateScope();
        return scope.ServiceProvider.GetRequiredService<AppDbContext>();
    }

    private sealed record LoginResponsePayload(string AccessToken);

    private sealed record CheckOverduePayload(bool Ok, int OverdueSections, int Notified);
}
