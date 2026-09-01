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
    // automation run had pushed, so it rendered an empty state on a fresh environment. Seeding a
    // starter portfolio gives the page something to track from the first deployment.
    //
    // Matched on the project code, the only stable natural key these rows have. Existing rows are
    // never overwritten: once someone edits progress or a checkpoint, a restart must not undo it.
    // Dates are stored relative to the seed instant so the schedule reads sensibly whenever an
    // environment is first brought up.
    private async Task SeedPortfolioProjectsAsync(CancellationToken cancellationToken)
    {
        List<string> existing = await _dbContext.PortfolioProjects.Select(p => p.Code).ToListAsync(cancellationToken);
        var known = new HashSet<string>(existing, StringComparer.Ordinal);
        DateTime seededAt = DateTime.UtcNow;

        var added = 0;
        foreach (PortfolioProjectSeedRow row in PortfolioProjectSeedCatalog.Rows)
        {
            if (!known.Add(row.Code))
            {
                continue;
            }

            _dbContext.PortfolioProjects.Add(BuildProject(row, seededAt));
            added++;
        }

        if (added > 0)
        {
            await _dbContext.SaveChangesAsync(cancellationToken);
            LogPortfolioProjectsSeeded(_logger, added);
        }
    }

    private static Domain.Projects.PortfolioProject BuildProject(PortfolioProjectSeedRow row, DateTime seededAt)
    {
        var project = new Domain.Projects.PortfolioProject
        {
            Code = row.Code,
            Name = row.Name,
            Description = row.Description,
            Category = row.Category,
            Stage = row.Stage,
            Owner = row.Owner,
            Department = row.Department,
            ProgressPercent = row.ProgressPercent,
            TeamSize = row.TeamSize,
            StartDate = seededAt.AddDays(row.StartOffsetDays).Date,
            DueDate = seededAt.AddDays(row.DueOffsetDays).Date,
            LatestUpdate = row.LatestUpdate,
            SortOrder = row.SortOrder,
            IsActive = true,
            CreatedBy = "seeder",
        };

        var order = 1;
        foreach (PortfolioMilestoneSeedRow milestone in row.Milestones)
        {
            project.Milestones.Add(new Domain.Projects.ProjectMilestone
            {
                Title = milestone.Title,
                DueDate = seededAt.AddDays(milestone.DueOffsetDays).Date,
                IsCompleted = milestone.IsCompleted,
                SortOrder = order++,
                CreatedBy = "seeder",
            });
        }

        return project;
    }

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

    [LoggerMessage(Level = LogLevel.Information, Message = "Seeded {Count} portfolio project(s) into an empty or partial portfolio.")]
    private static partial void LogPortfolioProjectsSeeded(ILogger logger, int count);

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
