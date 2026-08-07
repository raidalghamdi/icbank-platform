# ICBank Platform — .NET Backend

.NET 8 Clean Architecture skeleton for the ICBank Platform backend rewrite, built per
[`spec/DOTNET-CONVENTIONS.md`](../../spec/DOTNET-CONVENTIONS.md) and
[`PLATFORM_RULES.md`](../../uploaded_attachments/b1131df2de4a43cc845a6852a6473faf/PLATFORM_RULES.md).
This directory coexists with the current Node backend (`artifacts/`, `lib/`, `supabase/` at the
repo root) — nothing outside `backend/` is touched by this work.

## Solution layout

```
backend/
├── Icbank.Platform.sln
├── Directory.Build.props        # shared MSBuild settings: nullable, TreatWarningsAsErrors, analyzers
├── Directory.Packages.props     # central package management — every version pinned here
├── .editorconfig                 # naming/style rules enforced as build errors/warnings
├── stylecop.json
├── src/
│   ├── Icbank.Platform.Domain/          # entities, value objects, domain exceptions — zero NuGet packages, zero project refs
│   ├── Icbank.Platform.Application/     # use cases (MediatR), Result<T>, PagedResult/PagedQuery, validation pipeline — depends on Domain only
│   ├── Icbank.Platform.Infrastructure/  # EF Core, audit interceptor, soft-delete, resilient HttpClient — depends on Domain + Application
│   └── Icbank.Platform.Api/             # composition root, controllers, middleware — depends on Application + Infrastructure
└── tests/
    ├── Icbank.Platform.UnitTests/       # depends on Domain + Application only
    └── Icbank.Platform.IntegrationTests/# depends on Api + Infrastructure, uses WebApplicationFactory<Program>
```

Dependency direction is enforced by project references alone (no analyzer needed): `Domain` has no
references, `Application → Domain`, `Infrastructure → Domain, Application`, `Api → Application,
Infrastructure`.

## Prerequisites

- .NET 8 SDK (developed/tested against `8.0.423`).
- SQL Server reachable via the `ConnectionStrings__Default` environment variable (LocalDB, a
  container, or Azure SQL). **No live database is required to build, run the unit tests, or run
  the integration tests** — the integration suite only exercises endpoints that do not touch
  `AppDbContext` (see "Testing" below).

## Configuration — no secrets in source control

`appsettings.json` ships with an **empty** `ConnectionStrings:Default` and an **empty**
`Cors:AllowedOrigins` array. Both must be supplied per environment:

| Setting | Environment variable override | Example |
|---|---|---|
| `ConnectionStrings:Default` | `ConnectionStrings__Default` | `Server=localhost;Database=IcbankPlatform;User Id=...;Password=...;TrustServerCertificate=True;` |
| `Cors:AllowedOrigins` | `Cors__AllowedOrigins__0`, `Cors__AllowedOrigins__1`, … | `https://app.icbank.example` |

`appsettings.Development.json` contains a LocalDB connection string with Windows/Trusted
authentication (no password) purely for local convenience — this contains no credential and is
safe to commit.

CORS is an explicit allow-list read from configuration (`Cors:AllowedOrigins`) — the API never
calls `AllowAnyOrigin()`, which was the old Node backend's vulnerability this rewrite must not
repeat.

## Running the API

```bash
export PATH=$PATH:/home/user/.dotnet   # if the SDK isn't already on PATH
cd backend
export ConnectionStrings__Default="Server=localhost;Database=IcbankPlatform;Trusted_Connection=True;TrustServerCertificate=True;"
export Cors__AllowedOrigins__0="http://localhost:5173"
dotnet run --project src/Icbank.Platform.Api
```

Verify the pipeline end-to-end:

```bash
curl http://localhost:5233/health/live     # 200 — process is up, no dependency checks
curl http://localhost:5233/health/ready    # 200 once SQL Server is reachable; 503 otherwise
curl http://localhost:5233/api/v1/ping     # {"message":"pong","serverTimeUtc":"..."}
```

(Port comes from `src/Icbank.Platform.Api/Properties/launchSettings.json`; override with
`ASPNETCORE_URLS` if needed.)

## Building

```bash
dotnet build     # zero warnings, zero errors — TreatWarningsAsErrors is enabled solution-wide
```

## Testing

```bash
dotnet test
```

- **Unit tests** (`Icbank.Platform.UnitTests`) cover `PagedQuery` clamping and `GetPingQueryHandler` — pure, no host, no I/O.
- **Integration tests** (`Icbank.Platform.IntegrationTests`) boot the real `Program` via
  `WebApplicationFactory<Program>` and hit `/health/live` and `/api/v1/ping` — endpoints that never
  touch `AppDbContext`. The factory injects a syntactically valid connection string purely so the
  DI container can construct `AppDbContext`; **no SQL Server needs to be running** for this suite
  to pass. `/health/ready` (which does need SQL Server) is intentionally not asserted against in
  the automated suite; it is verified manually against a running instance instead — see the
  running-API smoke test above.

## Formatting

```bash
dotnet format --verify-no-changes
```

## What's here vs. what's next

This scaffold delivers the cross-cutting skeleton only. `AppDbContext` currently has one
placeholder entity (`PingRecord`) purely to prove the audit interceptor and soft-delete query
filter compile and function. The next work package ports the real 43 entities — see
[`spec/SCAFFOLD-NOTES.md`](../../spec/SCAFFOLD-NOTES.md) for exactly what that work package needs
to know (in particular: **every new `IEntityTypeConfiguration<T>` must call
`.HasQueryFilter(e => e.DeletedAt == null)` itself** — there is no auto-applied global filter).

Hangfire packages (`Hangfire.AspNetCore`, `Hangfire.SqlServer`) are referenced per the conventions
doc's package list but are **not yet wired into DI** — no background job exists yet in this
scaffold to justify starting a Hangfire server against a database that doesn't have its schema.
See `SCAFFOLD-NOTES.md` for the follow-up.
