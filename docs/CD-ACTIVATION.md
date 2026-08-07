# Activating CD — the remaining steps

Both pipelines now authenticate with credentials the project can actually issue. This
document is the short version of what changed and the one thing still outstanding.

## Status

All three pipelines are green from `main`. PR #1 is merged, so `main` now carries the work.

| Pipeline | State (run from `main`) |
|---|---|
| Frontend CD (`frontend-deploy.yml`) | **Green** — [run 31180332097](https://github.com/raidalghamdi/icbank-platform/actions/runs/31180332097) |
| Backend CI (`backend-ci.yml`) | **Green** — [run 31180323282](https://github.com/raidalghamdi/icbank-platform/actions/runs/31180323282) |
| Backend CD (`backend-deploy.yml`) | **Green** — [run 31182004647](https://github.com/raidalghamdi/icbank-platform/actions/runs/31182004647), all five jobs |

Backend CD runs `preflight -> gate -> migrate -> deploy -> smoke-test`.

### Two races fixed along the way

Both pipelines went red on deployments that were actually fine, because each asserted before
the platform had finished swapping. Neither was a code problem, and both would have recurred.

- **Frontend.** The readiness wait polled `login.html` only. That file changes rarely, so on any
  release that did not touch it the first poll matched the *old* build, the wait returned at
  once, and the byte check then ran mid-swap and failed on `index.html`. It now waits on every
  file the check verifies.
- **Backend.** The smoke script began asserting the moment the deploy step returned, and got 500
  on everything — including assertions that only expect a 404. `/health/ready` was already
  answering 200 at that point, so it is not a sufficient signal; the job now also waits for an
  unknown route to return a clean 404, which only happens once routing and the middleware chain
  are live.

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
gh workflow run backend-deploy.yml --ref main -f environment=dev
```

Jobs run `preflight → gate → migrate → deploy → smoke-test`. **Migrations run against the live
dev database.** The gate job is the place to stop it if that is not wanted yet.

PR #1 is merged, so `main` now holds this work and both pipelines have run green from it. The
earlier warning about never dispatching from `main` no longer applies.

## Adding staging or production

`dev` is the only option the dispatch form offers, and the only environment that exists.

The input used to offer `dev`, `staging` and `prod`. That was not a harmless placeholder.
GitHub resolves `vars.X` from the environment first and the repository second, and it does not
warn when it falls through — and every repository-level variable here names a dev resource. A
run targeting `prod` would have deployed the API to `icbank-dev-api`, migrated `icbank-dev-sql`,
smoke-tested dev, and gone green while reporting a production deploy. The frontend equivalent
would have republished the dev site under the same claim.

So adding a target is now a three-part operation, and the `preflight` job checks you did all of
it:

1. Create the Azure resources, named with the environment token: `icbank-<env>-api`,
   `icbank-<env>-frontend`, `icbank-<env>-sql`, `rg-icbank-<env>`.
2. Create the GitHub Environment and give it **its own** `APP_SERVICE_NAME`,
   `FRONTEND_APP_SERVICE_NAME`, `SQL_SERVER_FQDN` and `AZURE_RESOURCE_GROUP` variables, plus its
   own `AZURE_API_PUBLISH_PROFILE`, `AZURE_WEBAPP_PUBLISH_PROFILE` and `SQL_CONNECTION_STRING`
   secrets. For production, add required reviewers here too.
3. Add the name to the `options:` list in both `backend-deploy.yml` and `frontend-deploy.yml`.

`preflight` asserts every resolved resource name carries the target's own token. Miss a variable
in step 2 and the run stops with an explicit error instead of quietly retargeting dev.

Note that environment names are case-sensitive: the existing `Production` environment is not
`prod`. Two unused environments (`Preview`, `prolific-spontaneity / production`) are also present
and appear to be left over from another integration.

## Still open

None of these block a deployment.

- **Frutiger font licensing**, open with GAC. The files carry `alfont_com` filename prefixes,
  Monotype/Linotype trademarks and `OS/2.fsType = 4`. Nothing breaks technically and the
  binaries are drop-in replaceable under the same filenames once a licensed set arrives. See
  `artifacts/internal-comms/fonts/frutiger/PROVENANCE.md`.
- **A stale SQL firewall rule**, `sandbox-migration-rehearsal` (54.237.68.156) on
  `icbank-dev-sql`. Left over from a migration rehearsal and should be removed: SQL server ->
  Networking -> delete the rule.
- **Two unused GitHub environments**, `Preview` and `prolific-spontaneity / production`. They
  appear to be left over from another integration and are unrelated to these pipelines.
- **Empty KPI tiles.** Activation and archive counters read real tables and read zero, because
  the Authority has not entered that content yet. This is deliberate — seeding invented numbers
  would make the dashboard look like it were measuring something. Observance dates are seeded
  because they are public facts, not measurements.
