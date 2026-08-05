# Data migration runbook: Postgres (Node/Drizzle) → SQL Server (.NET/EF Core)

This is the operational runbook for `Icbank.Platform.DataMigration`
(`backend/tools/Icbank.Platform.DataMigration/`). Read the two findings below before planning a
cutover date — they change what must be communicated to users and stakeholders, and when.

---

## Finding 1 (read first): password hashes are NOT portable — every user must reset their password

The source Node application hashes passwords with **bcrypt** (`users.password_hash`, `text`,
nullable). The destination .NET application hashes passwords with **ASP.NET Core Identity's
built-in `PasswordHasher<TUser>`, which is PBKDF2-HMAC-SHA256** (see
[`PasswordHasher.cs`](../backend/src/Icbank.Platform.Infrastructure/Identity/PasswordHasher.cs)).
These two schemes are mutually incompatible — there is no way to convert a bcrypt hash into a
PBKDF2 hash without the plaintext password, which the migration never has access to.

The migration tool already encodes this decision explicitly
(see [`UserTransformer.cs`](../backend/tools/Icbank.Platform.DataMigration/Mapping/Transformers/UserTransformer.cs)
and [`PasswordMigrationOutcome.cs`](../backend/tools/Icbank.Platform.DataMigration/Mapping/Dtos/PasswordMigrationOutcome.cs)):

- Every migrated user is written with `PasswordHash = null` and `MustChangePassword = true`.
- Users with `password_hash IS NULL` in the source (SSO-only via Azure AD) are recorded as
  `PasswordMigrationOutcome.SsoOnlyNoPasswordToMigrate` — nothing to reset, they keep signing in
  via Azure AD exactly as before.
- Users with a non-null source `password_hash` are recorded as
  `PasswordMigrationOutcome.BcryptHashNotPortableMustReset` — **every one of these must reset
  their password before or immediately after cutover.**

**Operator action required:** before cutover, export the count and (if your comms tooling allows
it) the email list of `BcryptHashNotPortableMustReset` users from the migration report's
per-table notes for `users`, and notify them in advance — "your password will not carry over;
you will be asked to set a new one on first login after `<cutover date>`." Telling users *after*
cutover, when they discover it via a failed login, is the failure mode this finding exists to
prevent. If the destination login flow does not yet surface `MustChangePassword` as a forced
reset step, that must be confirmed working *before* cutover, not after.

---

## Finding 2 (read first): the multi-role permission model changed — some users gain permissions at cutover

The source Node app resolves a user's role via
[`getUserPermissions()`](../artifacts/api-server/src/middleware/auth.ts) with:

```ts
const userRoleRows = await db.select(...).from(userRolesTable)...where(eq(userRolesTable.userId, userId)).limit(1);
```

This takes only the **first** `user_roles` row per user, with **no `ORDER BY`** — for a user with
more than one role assigned, which role "wins" is whatever order Postgres happens to return rows
in (not guaranteed stable across queries, reloads, or Postgres versions).

The .NET port's [`PermissionResolver.cs`](../backend/src/Icbank.Platform.Infrastructure/Identity/PermissionResolver.cs)
deliberately fixes this by **unioning every role a user holds** rather than picking one. This is
documented in the port as an intentional bug fix, but it is also a **behavior change**: any user
who has more than one `user_roles` row in the source will have a broader effective permission set
after cutover than the Node app ever actually granted them (the Node UI only ever exercised
whichever single role `.limit(1)` happened to return).

**Who is affected:** the migration tool computes this at run time. `UserRoleTableMigrator`
(see [`UserRoleTableMigrator.cs`](../backend/tools/Icbank.Platform.DataMigration/Migration/Migrators/UserRoleTableMigrator.cs))
counts, per run, how many distinct users have more than one `user_roles` row and adds a note to
the migration report, e.g.:

> "`N` user(s) have more than one `user_roles` row. All rows were migrated (multi-role union, not
> Node's first-role-only `.limit(1)` behavior) — their effective permission set after cutover may
> be broader than what the old Node UI ever surfaced. Flagged for product review."

**This document cannot name the affected users**, because no live Postgres instance and no
`user_roles` sample data were available in the environment this migration tool was built and
verified in — `supabase/schema.sql` (the only Postgres artifact present, read-only) contains only
RBAC lookup-table seed rows (`permissions`, `roles`, `pages`), never real `users`/`user_roles`
data. **Operator action required:** before cutover, run the tool in `validate` mode (below)
against the real source database, read the `user_roles` table note in the resulting report for
the exact count of affected users, and — if a named list is required for the security/product
review the note recommends — extend `UserRoleTableMigrator` to also log the affected source
`user_id` values (currently it logs only the count), or query
`SELECT user_id, COUNT(*) FROM user_roles GROUP BY user_id HAVING COUNT(*) > 1;` directly against
the source before cutover.

---

## Prerequisites

- Read access to the source Postgres database (`Migration:SourceConnectionString`).
- Write access to a destination SQL Server database that already has every EF Core migration
  applied (`Migration:DestinationConnectionString`) — the tool never runs migrations itself.
- `.NET 8` runtime.
- The two connection strings are read only from the `Migration` configuration section
  (`appsettings.json` or environment variables) — **never from command-line arguments** (avoids
  leaking credentials into shell history/process lists).

## Recommended cutover window

This tool is **not** designed for live/concurrent dual-write operation — it is a one-shot batch
copy intended for a **read-only or full downtime window** on the source application:

1. Put the Node application into read-only mode (or take it fully offline) before starting
   `migrate` mode. If any write lands in Postgres after the migration run starts, that row will
   silently not exist in the destination (the tool takes one snapshot per table via
   `ReadTableAsync`, it does not tail changes).
2. Recommended window length: proportional to total row count across all 42 covered tables (no
   volume/throughput benchmark exists yet from this session — see "Unresolved questions" in
   `spec/DATA-MIGRATION-NOTES.md`). Budget extra time for the manual password-reset communication
   in Finding 1 and the multi-role review in Finding 2, both of which should happen **before**
   the window opens, not during it.
3. Do not point the destination application at users until `reconcile` mode (below) reports a
   clean match.

## Dry-run: `validate` mode

Run pre-flight, **read-only** checks against the source (and, once implemented further,
destination) with no writes at all:

```bash
dotnet run --project backend/tools/Icbank.Platform.DataMigration -- validate
```

This is the safe, repeatable way to rehearse a cutover: run it as many times as needed against a
production-equivalent source snapshot without any risk of double-writing or partial-writing the
destination. Review the emitted report (`<ReportDirectory>/migration-report-<timestamp>.json` and
`.txt`, plus `migration-<date>.log`) before ever running `migrate` for real.

## Running the actual migration: `migrate` mode

```bash
dotnet run --project backend/tools/Icbank.Platform.DataMigration -- migrate
```

This runs every registered `ITableMigrator` in the fixed FK-safe order defined in
[`TableMigratorRegistry.cs`](../backend/tools/Icbank.Platform.DataMigration/Migration/TableMigratorRegistry.cs)
(RBAC roots first, then each feature domain, with each domain's own parent-before-children
ordering). Every table's outcome (rows read/inserted/skipped, and free-text notes such as the two
findings above) is accumulated into one `MigrationReport` and written out at the end, plus echoed
to the console.

The overall exit code is `0` only if `report.OverallPass` is `true`, i.e. **no table reported any
row skipped for a data-quality reason** (duplicate keys, orphaned FKs). A `1` exit code means the
migration ran to completion but at least one table has rows the operator must review in the
report before considering cutover complete; a `2` exit code means the tool itself crashed
(logged, and always a bug or environment problem to fix before retrying, not a "just re-run it").

## Aborting mid-run

Press `Ctrl+C` (or send `SIGINT`) at any point during `migrate` mode. The tool's `Console.CancelKeyPress`
handler triggers cooperative cancellation of the current table's async row loop. Because every
migrator commits each row via `SaveChangesAsync` immediately and records its source→destination id
mapping via `IIdMappingStore.RecordAsync` in the same iteration (not batched at the end), an abort
leaves the destination in a **consistent partial state**: every row committed before the abort
stays committed and mapped; nothing is left half-written mid-row. No explicit "abort" flag or
command exists beyond the OS-level interrupt — there is no separate resume command either, because
none is needed (see below).

## Resuming after an abort or a failure

Simply re-run `migrate` mode again with the same source/destination connection strings. Every
migrator checks `IIdMappingStore.TryGetDestinationIdAsync` before writing each row and skips (as
`RowsSkippedAlreadyMigrated`) any source row already recorded from a prior run — this is what
makes the tool idempotent and safe to re-run after a partial run, a crash, or a manual abort. Do
**not** truncate the destination tables and start over unless you intend a full rollback (below);
just re-invoking `migrate` picks up exactly where the previous run left off.

## Rolling back

This tool has no built-in "undo" command — a migration run only ever inserts rows (never updates
or deletes), so rollback is a database-administration action, not a tool feature:

1. **Best option — restore from backup.** Take (or already have) a SQL Server backup of the
   destination database from immediately before the `migrate` run started, and restore it. This
   is the only rollback path that is guaranteed clean regardless of how far the run progressed.
2. **If no backup exists and the run is known to have failed early:** the id-mapping store
   (`IdMappingStore`, a table in the destination database — see
   [`IdMappingStore.cs`](../backend/tools/Icbank.Platform.DataMigration/Migration/IdMappingStore.cs))
   records exactly which destination rows came from this migration tool, keyed by
   `(sourceTable, sourceId) → destinationId`. In principle this table is precise enough to drive a
   scripted delete of every migrated row across all 42 covered destination tables, in reverse
   FK-safe order — but no such rollback script was written or tested in this engagement. Treat
   this as a fallback path requiring careful, table-by-table manual verification, not a one-command
   operation.
3. In either case, re-open the source application for writes only after confirming the rollback
   is complete and the destination is not partially populated — a half-rolled-back destination is
   worse than a fully-migrated one for a second attempt.

## Post-migration verification: `reconcile` mode

```bash
dotnet run --project backend/tools/Icbank.Platform.DataMigration -- reconcile
```

Runs a post-migration source-vs-destination comparison (see
[`ReconciliationRunner.cs`](../backend/tools/Icbank.Platform.DataMigration/Reconciliation/ReconciliationRunner.cs)).
Run this after `migrate` completes and before declaring cutover done, and again after any
rollback-and-retry cycle.

## Known risks and limitations

1. **Password hashes are not portable — Finding 1 above.** Plan user communication before
   cutover, not after.
2. **Multi-role permission union changes some users' effective access — Finding 2 above.** Run
   `validate` against production data ahead of the cutover window specifically to get the
   affected-user count (and, if the tool is extended per the note above, the affected user list)
   for a security/product sign-off before go-live.
3. **`final_media_reports` is NOT migrated.** This table (an immutable, 8-section GAC report
   entity with heavy nested `jsonb`) was deliberately deferred for JSON-nesting complexity — see
   `spec/DATA-MIGRATION-NOTES.md` for the full inventory. Any historical final reports will not
   exist in the destination after cutover. `reports_qa_queries.final_report_id` will resolve to
   `null` for every migrated row as a direct consequence (already noted in
   [`ReportsQaQueryTableMigrator.cs`](../backend/tools/Icbank.Platform.DataMigration/Migration/Migrators/ReportsQaQueryTableMigrator.cs)).
   Communicate this gap explicitly to anyone relying on historical final reports before cutover.
4. **The Postgres read half is unverified against a real instance.** No live Postgres server was
   reachable in the environment this tool was built and tested in — `IPostgresDataSource`'s
   production implementation (`NpgsqlDataSource`) has never been executed against a real
   database this session; only the pure transform functions and an in-memory fake source have
   been exercised. Run `validate` mode against a real staging copy of the source database well
   before the cutover window, specifically to catch anything a live Npgsql connection surfaces
   that the in-memory fixtures could not (unexpected column types, encoding issues, row counts
   large enough to matter for the downtime-window estimate).
5. **The destination EF write half has real integration test coverage, but it has never
   actually executed** in this environment either, for the same reason: no SQL Server instance
   was reachable (no Docker daemon, no local SQL Server, `ICBANK_TEST_SQL_CONNECTION` unset). See
   `backend/tests/Icbank.Platform.DataMigration.Tests/Migration/DailyReportTableMigratorSqlServerTests.cs`
   — written and gated on `ICBANK_TEST_SQL_CONNECTION` the same way
   `Icbank.Platform.IntegrationTests.Auth.AuthWebApplicationFactory` already is, but it has only
   ever run its no-op/early-return branch. It must be run for real, in an environment with SQL
   Server, before this tool is trusted for a production cutover.
6. **No throughput/volume benchmark exists.** The recommended downtime window length above is
   qualitative, not measured — no timing data exists from a real run against realistic row
   counts.
7. **No automated rollback script.** See "Rolling back" above — restoring from a pre-migration
   backup is the only tested-in-concept path; a scripted per-table delete via the id-mapping store
   was designed but not built or tested.
8. **Duplicate-key handling always keeps the earliest-created row and discards the rest**, for
   both `gac_social_posts` (platform, external_id) and `daily_reports` (report_date), which are
   the two tables where the destination adds a new unique index the source never enforced. Confirm
   with the data owner that "earliest wins" is the correct tie-break before cutover — it was
   chosen as a reasonable default, not confirmed against a business rule.
