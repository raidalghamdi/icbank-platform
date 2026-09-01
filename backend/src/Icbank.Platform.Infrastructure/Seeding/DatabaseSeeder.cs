using Icbank.Platform.Domain.Identity;
using Icbank.Platform.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Icbank.Platform.Infrastructure.Seeding;

/// <summary>
/// Idempotent reference-data + initial super-admin seeder (task requirement 6). Replaces the old
/// system's unconditional hardcoded-password <c>TEST_USERS</c> seed (DEFECT-LOG.md SEC-14):
/// refuses to run in Production unless <c>Seed:AllowInProduction</c> is explicitly <c>true</c>,
/// generates a random initial super-admin password when none is configured, forces a password
/// change on first login, and never logs the password anywhere — the one-time plaintext value is
/// returned only from <see cref="SeedAsync"/>'s return value for the caller (Program.cs) to
/// surface exactly once via console-only output, never a log sink.
/// </summary>
#pragma warning disable SA1204 // Static members should appear before non-static members — LoggerMessage partials must sit near their call sites; ordering here favors readability over the mechanical rule.
public sealed partial class DatabaseSeeder
{
    private const int GeneratedPasswordLength = 24;

    private readonly AppDbContext _dbContext;
    private readonly IHostEnvironment _environment;
    private readonly SeedOptions _options;
    private readonly Microsoft.AspNetCore.Identity.PasswordHasher<Identity.PasswordHasherSubject> _passwordHasher = new();
    private readonly ILogger<DatabaseSeeder> _logger;

    /// <summary>Initializes a new instance of the <see cref="DatabaseSeeder"/> class.</summary>
    /// <param name="dbContext">The write-model persistence context.</param>
    /// <param name="environment">The hosting environment, used to gate Production seeding.</param>
    /// <param name="options">The bound seed configuration options.</param>
    /// <param name="logger">The structured logger. Never passed the plaintext password (R-BE-054).</param>
    public DatabaseSeeder(AppDbContext dbContext, IHostEnvironment environment, IOptions<SeedOptions> options, ILogger<DatabaseSeeder> logger)
    {
        _dbContext = dbContext;
        _environment = environment;
        _options = options.Value;
        _logger = logger;
    }

    /// <summary>
    /// Seeds the 9 roles, 18 pages, 4 permissions, the default role-permission matrix, and the
    /// initial super-admin account if none exists yet. Returns the one-time plaintext password
    /// only when a brand-new super-admin account was just created; returns <c>null</c> on every
    /// subsequent run (idempotent) or if seeding was skipped.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The one-time generated super-admin password, or <c>null</c>.</returns>
    public async Task<string?> SeedAsync(CancellationToken cancellationToken)
    {
        if (_environment.IsProduction() && !_options.AllowInProduction)
        {
            LogSeedingSkipped(_logger);
            return null;
        }

        await SeedRolesAsync(cancellationToken);
        await SeedPagesAsync(cancellationToken);
        await SeedPermissionsAsync(cancellationToken);
        await SeedDefaultRolePermissionsAsync(cancellationToken);
        await SeedInternationalDaysAsync(cancellationToken);
        await SeedPortfolioProjectsAsync(cancellationToken);
        await SeedCampaignsAsync(cancellationToken);
        await ReconcileShorfahSectionsAsync(cancellationToken);
        return await SeedInitialSuperAdminAsync(cancellationToken);
    }

    // Why: the dashboard's "upcoming events" panel and the الأيام العالمية page both read
    // international_days, and nothing has ever populated it, so every deployment has shown an
    // empty landing page to every user. These are genuine observances with citable sources, not
    // demo scaffolding, so seeding them is safe anywhere and nobody has to clear them out later.
    //
    // Matched on the Arabic name, which is the only stable natural key the table has. Existing
    // rows are left untouched rather than overwritten: once the Authority edits a description or
    // adds suggestions, the seeder must not undo that on the next restart.
    private async Task SeedInternationalDaysAsync(CancellationToken cancellationToken)
    {
        List<string> existing = await _dbContext.InternationalDays
            .Select(d => d.DayNameAr)
            .ToListAsync(cancellationToken);
        var known = new HashSet<string>(existing, StringComparer.Ordinal);

        var added = 0;
        foreach (InternationalDaySeedRow row in InternationalDaySeedCatalog.Rows)
        {
            if (!known.Add(row.NameAr))
            {
                continue;
            }

            _dbContext.InternationalDays.Add(new Domain.InternationalDays.InternationalDay
            {
                DayNameAr = row.NameAr,
                DayNameEn = row.NameEn,
                AnnualDate = row.AnnualDate,
                Category = row.Category,
                OfficialOrganizer = row.Organizer,
                OfficialOrganizerSource = row.OrganizerSource,
                HistorySummary = row.History,
                CreatedBy = "seeder",
            });
            added++;
        }

        if (added > 0)
        {
            await _dbContext.SaveChangesAsync(cancellationToken);
            LogInternationalDaysSeeded(_logger, added);
        }
    }

    // Why: the projects page had no store of its own and could only show whatever an external
    // automation run had pushed, so it rendered an empty state on a fresh environment. The seed
    // catalogue is therefore the source of truth for the portfolio and this reconciles the table
    // against it in both directions — inserting only the missing codes left retired projects
    // visible for ever and let renamed projects keep their old titles in already-seeded
    // environments. Idempotent: a second run finds the same rows and writes nothing new.
    //
    // Matched on the project code, the only stable natural key these rows have. Dates are stored
    // relative to the seed instant so the schedule reads sensibly whenever a run happens.
    private async Task SeedPortfolioProjectsAsync(CancellationToken cancellationToken)
    {
        List<Domain.Projects.PortfolioProject> tracked = await _dbContext.PortfolioProjects
            .Include(project => project.Milestones)
            .Include(project => project.ProgressUpdates)
            .ToListAsync(cancellationToken);

        PortfolioProjectReconciliation plan = PortfolioProjectReconciler.Reconcile(tracked, DateTime.UtcNow);
        if (!plan.HasChanges)
        {
            return;
        }

        // Children go first so the delete order stays valid even where the cascade is enforced by
        // the application rather than the database (e.g. an in-memory provider in tests).
        _dbContext.ProjectProgressUpdates.RemoveRange(plan.RemovedProgressUpdates);
        _dbContext.ProjectMilestones.RemoveRange(plan.RemovedMilestones);
        _dbContext.PortfolioProjects.RemoveRange(plan.Removed);
        _dbContext.PortfolioProjects.AddRange(plan.Added);

        await _dbContext.SaveChangesAsync(cancellationToken);
        LogPortfolioProjectsReconciled(_logger, plan.Added.Count, plan.Updated.Count, plan.Removed.Count);
    }

    // Why: the campaigns pages and the executive dashboard both read this table, and nothing has
    // ever populated it, so both would render an empty state on a fresh environment. Reconciled in
    // both directions against the catalogue for the same reason the portfolio is: inserting only
    // the missing codes leaves retired campaigns visible for ever and lets renamed campaigns keep
    // their old titles. Idempotent: a second run finds the same rows and writes nothing new.
    private async Task SeedCampaignsAsync(CancellationToken cancellationToken)
    {
        List<Domain.Campaigns.Campaign> tracked = await _dbContext.Campaigns
            .Include(campaign => campaign.Deliverables)
            .Include(campaign => campaign.Channels)
            .ToListAsync(cancellationToken);

        CampaignReconciliation plan = CampaignReconciler.Reconcile(tracked, DateTime.UtcNow);
        if (!plan.HasChanges)
        {
            return;
        }

        // Children go first so the delete order stays valid even where the cascade is enforced by
        // the application rather than the database (e.g. an in-memory provider in tests).
        _dbContext.CampaignDeliverables.RemoveRange(plan.RemovedDeliverables);
        _dbContext.CampaignChannels.RemoveRange(plan.RemovedChannels);
        _dbContext.Campaigns.RemoveRange(plan.Removed);
        _dbContext.Campaigns.AddRange(plan.Added);

        await _dbContext.SaveChangesAsync(cancellationToken);
        LogCampaignsReconciled(_logger, plan.Added.Count, plan.Updated.Count, plan.Removed.Count);
    }

    // Why: ShorfahSectionSeeder only runs when an issue is created, so every issue that already
    // existed when the paragraph catalogue was restructured would keep its old Arabic titles and
    // never gain the paragraphs added since — the شرفة page would disagree with the agreed table of
    // contents until someone edited the database by hand. Published issues are left alone: their
    // PDF is already out. Idempotent: a second run finds every paragraph already in line and
    // writes nothing.
    private async Task ReconcileShorfahSectionsAsync(CancellationToken cancellationToken)
    {
        List<Domain.Shorfah.ShorfahSectionSlaDefault> slaDefaults = await _dbContext.ShorfahSectionSlaDefaults.ToListAsync(cancellationToken);
        var slaDefaultsByType = slaDefaults.ToDictionary(slaDefault => slaDefault.SectionType, slaDefault => slaDefault.SlaDays);

        List<Domain.Shorfah.ShorfahIssue> issues = await LoadUnpublishedIssuesAsync(cancellationToken);

        var inserted = 0;
        var updated = 0;
        var removed = 0;
        foreach (Domain.Shorfah.ShorfahIssue issue in issues)
        {
            ShorfahSectionReconciliation plan = ShorfahCanonicalSectionReconciler.Reconcile(issue, slaDefaultsByType);
            if (!plan.HasChanges)
            {
                continue;
            }

            _dbContext.ShorfahSections.RemoveRange(plan.Removed);
            _dbContext.ShorfahSections.AddRange(plan.Inserted);
            inserted += plan.Inserted.Count;
            updated += plan.Updated.Count;
            removed += plan.Removed.Count;
        }

        if (inserted + updated + removed == 0)
        {
            return;
        }

        await _dbContext.SaveChangesAsync(cancellationToken);
        LogShorfahSectionsReconciled(_logger, inserted, updated, removed);
    }

    // Every collection that would be orphaned by deleting a paragraph is loaded, because the
    // reconciler refuses to delete a paragraph that has any dependent row.
    private Task<List<Domain.Shorfah.ShorfahIssue>> LoadUnpublishedIssuesAsync(CancellationToken cancellationToken) =>
        _dbContext.ShorfahIssues
            .Where(issue => issue.Status != Domain.Shorfah.ShorfahIssueStatus.Published)
            .Include(issue => issue.Sections).ThenInclude(section => section.ChildSections)
            .Include(issue => issue.Sections).ThenInclude(section => section.Permissions)
            .Include(issue => issue.Sections).ThenInclude(section => section.Media)
            .Include(issue => issue.Sections).ThenInclude(section => section.WorkflowLogs)
            .Include(issue => issue.Sections).ThenInclude(section => section.Assignments)
            .Include(issue => issue.Sections).ThenInclude(section => section.Reminders)
            .Include(issue => issue.Sections).ThenInclude(section => section.Notifications)
            .ToListAsync(cancellationToken);

    private async Task SeedRolesAsync(CancellationToken cancellationToken)
    {
        foreach (RoleName roleName in Enum.GetValues<RoleName>())
        {
            var machineName = RoleMachineName(roleName);
            var exists = await _dbContext.Roles.AnyAsync(r => r.Name == machineName, cancellationToken);
            if (exists)
            {
                continue;
            }

            _dbContext.Roles.Add(new Role { Name = machineName, NameAr = machineName, IsSystem = true, CreatedBy = "seeder" });
        }

        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    private async Task SeedPagesAsync(CancellationToken cancellationToken)
    {
        var sortOrder = 0;
        foreach (var slug in PageSlugs.All)
        {
            var exists = await _dbContext.Pages.AnyAsync(p => p.Slug == slug, cancellationToken);
            if (!exists)
            {
                _dbContext.Pages.Add(new Page { Slug = slug, NameAr = slug, SortOrder = sortOrder, CreatedBy = "seeder" });
            }

            sortOrder++;
        }

        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    private async Task SeedPermissionsAsync(CancellationToken cancellationToken)
    {
        foreach (PermissionVerbName verbName in Enum.GetValues<PermissionVerbName>())
        {
            var machineName = verbName.ToString().ToLowerInvariant();
            var exists = await _dbContext.Permissions.AnyAsync(p => p.Name == machineName, cancellationToken);
            if (!exists)
            {
                _dbContext.Permissions.Add(new Permission { Name = machineName, NameAr = machineName, CreatedBy = "seeder" });
            }
        }

        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    private async Task SeedDefaultRolePermissionsAsync(CancellationToken cancellationToken)
    {
        // Why: only super_admin gets full grants seeded by default. Every other role starts with
        // zero grants — this is a deliberate behaviour change from the old system, where
        // super_admin/admin/system_admin all got identical {"*", every verb} grants
        // (BUSINESS-RULES.md §10.2's "purely cosmetic three-tier admin" finding). See
        // AUTH-PORT-NOTES.md for the product-facing callout.
        Role? superAdminRole = await _dbContext.Roles.SingleOrDefaultAsync(r => r.Name == RoleMachineName(RoleName.SuperAdmin), cancellationToken);
        if (superAdminRole is null)
        {
            return;
        }

        List<Page> pages = await _dbContext.Pages.ToListAsync(cancellationToken);
        List<Permission> permissions = await _dbContext.Permissions.ToListAsync(cancellationToken);

        foreach (Page page in pages)
        {
            foreach (Permission permission in permissions)
            {
                var exists = await _dbContext.RolePermissions.AnyAsync(
                    rp => rp.RoleId == superAdminRole.Id && rp.PageId == page.Id && rp.PermissionId == permission.Id,
                    cancellationToken);
                if (!exists)
                {
                    _dbContext.RolePermissions.Add(new RolePermission
                    {
                        RoleId = superAdminRole.Id,
                        PageId = page.Id,
                        PermissionId = permission.Id,
                        CreatedBy = "seeder",
                    });
                }
            }
        }

        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    private async Task<string?> SeedInitialSuperAdminAsync(CancellationToken cancellationToken)
    {
        var email = _options.InitialSuperAdminEmail.Trim().ToLowerInvariant();
        if (string.IsNullOrWhiteSpace(email))
        {
            return null;
        }

        Role? superAdminRole = await _dbContext.Roles.SingleOrDefaultAsync(r => r.Name == RoleMachineName(RoleName.SuperAdmin), cancellationToken);
        (User user, var password, var created) = await GetOrCreateInitialSuperAdminAsync(email, cancellationToken);

        // Reconcile a migrated administrator as well as a newly created account. Returning early
        // for an existing email left ccteam234@gmail.com as a plain admin after cutover.
        await EnsureRoleAssignedAsync(user, superAdminRole, cancellationToken);

        if (!created)
        {
            return null;
        }

        // Why: R-BE-054 — the password is never logged. It is only returned once, in memory, to
        // Program.cs, which is responsible for console-only output on a newly created account.
        LogSuperAdminSeeded(_logger, email);
        return string.IsNullOrWhiteSpace(_options.InitialSuperAdminPassword) ? password : null;
    }

    private async Task<(User User, string Password, bool Created)> GetOrCreateInitialSuperAdminAsync(string email, CancellationToken cancellationToken)
    {
        User? existing = await _dbContext.Users.SingleOrDefaultAsync(user => user.Email == email, cancellationToken);
        if (existing is not null)
        {
            return (existing, string.Empty, false);
        }

        var password = string.IsNullOrWhiteSpace(_options.InitialSuperAdminPassword)
            ? GenerateRandomPassword()
            : _options.InitialSuperAdminPassword;
        var created = new User
        {
            Email = email,
            Name = "Initial Super Admin",
            PasswordHash = _passwordHasher.HashPassword(new Identity.PasswordHasherSubject(), password),
            IsActive = true,
            MustChangePassword = true,
            CreatedBy = "seeder",
        };
        _dbContext.Users.Add(created);
        await _dbContext.SaveChangesAsync(cancellationToken);
        return (created, password, true);
    }

    private async Task EnsureRoleAssignedAsync(User user, Role? role, CancellationToken cancellationToken)
    {
        if (role is null || await _dbContext.UserRoles.AnyAsync(
                assignment => assignment.UserId == user.Id && assignment.RoleId == role.Id,
                cancellationToken))
        {
            return;
        }

        _dbContext.UserRoles.Add(new UserRole { UserId = user.Id, RoleId = role.Id, AssignedAt = DateTime.UtcNow, CreatedBy = "seeder" });
        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    [LoggerMessage(Level = LogLevel.Information, Message = "Seeding skipped: Production environment without Seed:AllowInProduction=true.")]
    private static partial void LogSeedingSkipped(ILogger logger);

    [LoggerMessage(Level = LogLevel.Information, Message = "Seeded initial super-admin account {Email}. Password was NOT logged.")]
    private static partial void LogSuperAdminSeeded(ILogger logger, string email);

    [LoggerMessage(Level = LogLevel.Information, Message = "Seeded {Count} international day(s) into an empty or partial catalogue.")]
    private static partial void LogInternationalDaysSeeded(ILogger logger, int count);

    [LoggerMessage(Level = LogLevel.Information, Message = "Reconciled the tracked portfolio against the seed catalogue: {Added} added, {Updated} refreshed, {Removed} removed.")]
    private static partial void LogPortfolioProjectsReconciled(ILogger logger, int added, int updated, int removed);

    [LoggerMessage(Level = LogLevel.Information, Message = "Reconciled the tracked campaign book against the seed catalogue: {Added} added, {Updated} refreshed, {Removed} removed.")]
    private static partial void LogCampaignsReconciled(ILogger logger, int added, int updated, int removed);

    [LoggerMessage(Level = LogLevel.Information, Message = "Reconciled unpublished Shorfah issues against the canonical paragraph catalogue: {Inserted} inserted, {Updated} refreshed, {Removed} removed.")]
    private static partial void LogShorfahSectionsReconciled(ILogger logger, int inserted, int updated, int removed);

    private static string RoleMachineName(RoleName roleName) => roleName switch
    {
        RoleName.SuperAdmin => "super_admin",
        RoleName.Admin => "admin",
        RoleName.SystemAdmin => "system_admin",
        RoleName.ApprovedManager => "approved_manager",
        RoleName.TeamMember => "team_member",
        RoleName.Requester => "requester",
        RoleName.Editor => "editor",
        RoleName.Viewer => "viewer",
        RoleName.Guest => "guest",
        _ => roleName.ToString().ToLowerInvariant(),
    };

    private static string GenerateRandomPassword()
    {
        const string upper = "ABCDEFGHJKLMNPQRSTUVWXYZ";
        const string lower = "abcdefghijkmnopqrstuvwxyz";
        const string digits = "23456789";
        const string special = "!@#$%^&*()-_=+";
        const string alphabet = upper + lower + digits + special;

        var bytes = System.Security.Cryptography.RandomNumberGenerator.GetBytes(GeneratedPasswordLength);
        var chars = new char[GeneratedPasswordLength];
        for (var i = 0; i < GeneratedPasswordLength; i++)
        {
            chars[i] = alphabet[bytes[i] % alphabet.Length];
        }

        return new string(chars);
    }
}
#pragma warning restore SA1204
