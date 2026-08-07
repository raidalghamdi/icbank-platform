# Activating CD — the remaining steps

Both pipelines now authenticate with credentials the project can actually issue. This
document is the short version of what changed and the one thing still outstanding.

## Status

Both pipelines are green. Backend CD completed its first successful run on 7 Aug 2026
([run 31175109917](https://github.com/raidalghamdi/icbank-platform/actions/runs/31175109917)) --
`gate -> migrate -> deploy -> smoke-test`, all four jobs, 19 smoke assertions against the live
API. Before that it had never completed a single run.

| Pipeline | State |
|---|---|
| Frontend CD (`frontend-deploy.yml`) | **Green** (run 31156244260) |
| Backend CI (`backend-ci.yml`) | **Green** |
| Backend CD (`backend-deploy.yml`) | **Green** (run 31175109917) |

## What changed, and why

The original design used OIDC federated credentials via `azure/login`. That is the better
pattern and the workflows were written for it, but it needs an Azure AD app registration with a
federated credential, and nobody on the project holds the directory rights to create one. The
secrets were never created, so **backend CD never completed a single run** — it failed at the
login step every time, and the backend was deployed by hand instead.

Both pipelines now use:

- **A publish profile** for the App Service (`azure/webapps-deploy@v3`).
- **The SQL login the running API already uses** for `dotnet ef database update`.

The migration step used to carry a comment saying no SQL credential could exist, because
`sql.bicep` sets `azureADOnlyAuthentication: true`. That is not true of this environment:
`icbank-dev-sql` reports it as **false**, and the live API authenticates with a SQL login today.
The comment described the intended design and the pipeline was built on it rather than on the
server as deployed.

The temporary-firewall-rule steps went with it. They existed to admit the runner, and they
needed an authenticated CLI to open the hole they depended on — circular, given that login was
the failing step. GitHub-hosted runners are Azure-hosted, so the server's existing
`AllowAllWindowsAzureIps` rule covers them. That is an assumption about someone else's
infrastructure, so the job **probes TCP 1433 first** and fails with the runner's egress IP and
what to do about it, rather than surfacing as an opaque timeout inside `dotnet ef`.

### The trade-off, stated plainly

Two long-lived secrets instead of short-lived federated tokens. Both are narrowly scoped — one
App Service, one database login the app already uses — and both are rotatable without any Azure
AD rights: Deployment Center → Manage publish profile → Reset, and a password reset on the SQL
login. If the app registration is ever created, restore `permissions: id-token: write`, put
`azure/login` back, and delete `AZURE_API_PUBLISH_PROFILE` and `SQL_CONNECTION_STRING`.

## Already configured

Repository variables:

| Name | Value |
|---|---|
| `AZURE_RESOURCE_GROUP` | `rg-icbank-dev` |
| `FRONTEND_APP_SERVICE_NAME` | `icbank-dev-frontend` |
| `APP_SERVICE_NAME` | `icbank-dev-api` |
| `SQL_SERVER_FQDN` | `icbank-dev-sql.database.windows.net` |

Repository secrets:

| Name | Purpose |
|---|---|
| `AZURE_WEBAPP_PUBLISH_PROFILE` | Frontend deploy |
| `AZURE_API_PUBLISH_PROFILE` | Backend deploy |
| `SQL_CONNECTION_STRING` | Backend migrations |

## The CI service account

Backend CD's smoke job logs in to the deployed API and asserts real traffic. It authenticates as
`svc-smoke@gac.gov.sa`, held in `SMOKE_SEED_EMAIL` / `SMOKE_SEED_PASSWORD`.

Two things about that account are deliberate and worth not undoing:

**It is past the must-change-password gate.** `CreateUserCommandHandler` sets
`MustChangePassword = true` unconditionally, even when an explicit password is supplied. An
account created through `POST /api/v1/admin/users` therefore sits behind
`MustChangePasswordMiddleware`, and the smoke script would rotate its password on the first run
-- so the second run would fail to log in, because the password in the GitHub secret is no
longer the account's password. Green once, red forever after. The account was provisioned with
the flag already clear, and the smoke script's rotation step is conditional, so it never fires.

**It is least-privileged.** Only `super_admin` and `admin` carry any permissions in this database
(90 each, including delete); the other seven seeded roles are empty. Rather than give a CI
account whose password lives in a GitHub secret 90 permissions, it holds the empty `viewer` role
plus two explicit `user_page_overrides` Allow rows -- `dashboard:view` and `shorfah:view` --
which is exactly what the smoke script reads. It is verified to receive 403 from
`/api/v1/admin/users`.

To rotate its password: reset the hash and keep `must_change_password = 0`, then update
`SMOKE_SEED_PASSWORD`. Do not clear the account through the admin API, which will re-arm the gate.

## Dispatching

```bash
gh workflow run backend-deploy.yml --ref feat/dotnet8-backend-foundation -f environment=dev
```

Jobs run `gate → migrate → deploy → smoke-test`. **Migrations run against the live dev
database.** The gate job is the place to stop it if that is not wanted yet.

> **Do not dispatch either pipeline from `main`.** `main` still has none of this work, so a run
> from `main` publishes an empty site over the live one. It happened once and was recovered.
> Run from `feat/dotnet8-backend-foundation` until PR #1 merges.

## Worth fixing separately

`authCanView()` in the frontend hardcodes `true` for `shorfah`, `media_monitoring`, `libraries`,
`smart_assistant`, `settings`, `gac_library` and `prompts_lib`, bypassing the permission matrix.
Left alone deliberately — it is a behaviour change, not a deployment concern.

The Frutiger font licensing question is still open with GAC. It blocks nothing technically; the
binaries are drop-in replaceable under the same filenames. See
`artifacts/internal-comms/fonts/frutiger/PROVENANCE.md`.
