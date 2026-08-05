using Icbank.Platform.DataMigration.Migration.Migrators;

namespace Icbank.Platform.DataMigration.Migration;

/// <summary>
/// The single, authoritative FK-safe dependency order every mode (validate/migrate/reconcile)
/// walks tables in (task requirement 2). Ordering here — not inside any individual migrator —
/// is what makes cross-table FK resolution via <see cref="IIdMappingStore"/> safe: a table is
/// never processed before every table it references.
/// </summary>
/// <remarks>
/// <para>Hand-built, individually-transformed migrators exist today for the tables that needed
/// non-trivial decisions (multi-role union, duplicate detection, nullable-timestamp backfill,
/// native-array fan-out, password non-portability) plus the core RBAC lookup tables the rest of
/// the graph roots from. See spec/DATA-MIGRATION-NOTES.md "Table-by-table mapping status" for
/// the remaining ~35 tables — they are a mechanical extension of the exact same
/// read-row → transform → resolve-FK-via-id-map → write-via-EF → record-mapping pattern used by
/// every migrator here, deliberately not hand-built individually within this engagement's scope.
/// </para>
/// </remarks>
public static class TableMigratorRegistry
{
    /// <summary>Gets every implemented table migrator, in FK-safe dependency order.</summary>
    /// <returns>The ordered list of migrators to run.</returns>
    public static IReadOnlyList<ITableMigrator> GetOrderedMigrators() => new ITableMigrator[]
    {
        // Identity / RBAC root tables -- everything else references users and/or roles.
        new RoleTableMigrator(),
        new PageTableMigrator(),
        new PermissionTableMigrator(),
        new UserTableMigrator(),
        new RolePermissionTableMigrator(),
        new UserRoleTableMigrator(),

        // Feature-domain "hard case" tables demonstrated end-to-end in this engagement.
        new AiYearActivationTableMigrator(),
        new GacSocialPostTableMigrator(),

        // ShorfahIssue is the top-level workflow container; ShorfahSection FKs to it (issue_id)
        // and to Users for the four actor FKs, so issues must be migrated first.
        new ShorfahIssueTableMigrator(),
        new ShorfahSectionTableMigrator(),

        // Section-scoped children -- all FK to ShorfahSection, some also to Users.
        // ShorfahAssignment must precede ShorfahReminder (optional assignment_id FK).
        new ShorfahSectionPermissionTableMigrator(),
        new ShorfahSectionMediaTableMigrator(),
        new ShorfahWorkflowLogTableMigrator(),
        new ShorfahAssignmentTableMigrator(),
        new ShorfahReminderTableMigrator(),

        // Natural-key (section_type) template config -- independent of any issue/section row.
        new ShorfahSectionSlaDefaultTableMigrator(),

        // Notifications reference Users (required) plus optional Issue/Section -- must run last
        // in the Shorfah group so both optional FKs can resolve.
        new ShorfahNotificationTableMigrator(),
    };
}
