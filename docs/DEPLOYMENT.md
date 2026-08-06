# Deployment runbook

This is the end-to-end runbook for standing up the Icbank Platform backend on Azure from
nothing. It assumes the reader has an Azure subscription and access to
[Azure Cloud Shell](https://shell.azure.com) (Bash) and nothing else prepared — no resource
group, no app registration, no CLI configured locally.

**This runbook was originally written and reviewed without any live Azure subscription
available to the author.** On 2026-08-06 a real dev environment was provisioned in
`uaenorth`/`westeurope` (subscription `f1422c2e-a1f8-4794-bfa4-d1c9c16e9287`) using the ARM REST
API directly (no `az` CLI was available in that environment, so the exact `az` commands below
were not run verbatim, but the equivalent ARM template/parameter payloads were, and every Bicep
module deployed successfully). That first run found and fixed three real bugs (see the
`DOTNETCORE|8.0` correction below and the Communication Services / Serilog notes in Step 5) but
left the app crash-looping at every startup, because the manual SQL step in Step 4 below had never
actually been carried out (that sandbox had no Azure credential capable of a `database.windows.net`
data-plane call, which Step 4 as originally written requires), and the app seeds roles into the
database *eagerly at startup*, not lazily — so skipping Step 4 is not a "the app will just have
missing data" failure, it is a "the app will never start" failure.

**A second session later the same day took a different path around the exact same blocker for the
dev environment specifically: SQL authentication instead of Entra-ID-only.** It disabled
`azureADOnlyAuthentication` on the SQL server, set a SQL admin password, pointed the App Service's
connection string at SQL auth instead of `Authentication=Active Directory Managed Identity`, then
successfully ran `dotnet ef database update` against the real database (the schema exists now) and
restarted the app. **The dev app is now live and passed all 17 of the deployed-mode smoke test's
assertions.** This is a **dev-only** deviation from this runbook's Entra-ID-only design — see the
corrected [Step 4](#step-4--the-manual-sql-step-bicep-cannot-do) below for exactly what changed and
why, and the important caveat about Bicep drift it creates. Full details of both sessions,
including every assertion of the smoke test and its result, are recorded in
`spec/DEV-DEPLOYMENT-NOTES.md`. The remaining unverified items are listed in
[`Unverified without a live subscription`](#unverified-without-a-live-subscription) below —
read it before you start.

## Contents

1. [What gets provisioned, and why](#what-gets-provisioned-and-why)
2. [Prerequisites](#prerequisites)
3. [Step 1 — clone and open Cloud Shell](#step-1--clone-and-open-cloud-shell)
4. [Step 2 — the app registration and OIDC federation](#step-2--the-app-registration-and-oidc-federation)
5. [Step 3 — the first deployment (single command)](#step-3--the-first-deployment-single-command)
6. [Step 4 — the manual SQL step Bicep cannot do](#step-4--the-manual-sql-step-bicep-cannot-do)
7. [Step 5 — populate Key Vault secrets](#step-5--populate-key-vault-secrets)
8. [Step 6 — GitHub repository configuration](#step-6--github-repository-configuration)
9. [Step 7 — first CD pipeline run](#step-7--first-cd-pipeline-run)
10. [Step 8 — deploying the frontend](#step-8--deploying-the-frontend)
11. [Region fallback](#region-fallback)
12. [Letting the CD pipeline reach SQL](#letting-the-cd-pipeline-reach-sql)
13. [Secret rotation](#secret-rotation)
14. [Rollback](#rollback)
15. [Unverified without a live subscription](#unverified-without-a-live-subscription)

---

## What gets provisioned, and why

Everything below is declared in [`infra/main.bicep`](../infra/main.bicep) and its modules under
[`infra/modules/`](../infra/modules/). Nothing here holds a secret — the design goal throughout
is that the only credential material anywhere is short-lived OIDC/managed-identity tokens, plus
a small number of values that genuinely have no non-secret equivalent (JWT signing key, cron API
key, Communication Services connection string), which live in Key Vault.

| Resource | Bicep module | Why it exists |
|---|---|---|
| Resource Group | (created before Bicep runs — see Step 3) | Deployment scope and cost/lifecycle boundary for one environment. |
| Log Analytics workspace | [`log-analytics.bicep`](../infra/modules/log-analytics.bicep) | Backing store for Application Insights and App Service diagnostics. Workspace-based App Insights is the only mode Microsoft still recommends; classic App Insights is deprecated. |
| Application Insights | [`app-insights.bicep`](../infra/modules/app-insights.bicep) | Request telemetry and structured logs (via the Serilog `ApplicationInsights` sink). Its connection string is not treated as a secret — it only grants write access to telemetry ingestion — so it is passed to the API as a plain app setting, not through Key Vault. |
| Key Vault | [`key-vault.bicep`](../infra/modules/key-vault.bicep) | Holds the handful of values that genuinely are secrets and have no managed-identity alternative: `Jwt:SigningKey`, `Cron:ApiKey`, the Communication Services connection string (if key-based auth is ever used), and any Entra ID SSO client secret. Uses RBAC authorization (not legacy access policies) per current Microsoft guidance. |
| Storage Account (Blob) | [`storage.bicep`](../infra/modules/storage.bicep) | Object storage for uploads/reports, replacing the filesystem-backed adapter used in tests/dev. Creates the five containers the app's `BlobPathResolver` expects: `weekend`, `designs`, `shorfah`, `media-reports`, `ai-year`. The API never holds an account key — it reaches Blob via managed identity, and mints short-lived user-delegation SAS URLs for client uploads. |
| Azure SQL (server + database) | [`sql.bicep`](../infra/modules/sql.bicep) | The application database. Provisioned **Entra-ID-only** (`azureADOnlyAuthentication: true`) — there is no SQL login/password anywhere, ever, for this database. The App Service authenticates via `Authentication=Active Directory Managed Identity`; humans and CI authenticate via their own Entra ID identity. |
| App Service Plan + App Service (Linux) | [`app-service.bicep`](../infra/modules/app-service.bicep) | Hosts the .NET 8 API. Chosen over Container Apps because the API is a single stateless process with no sidecar/multi-container need, App Service's zip-deploy model matches this repo's `dotnet publish` → artifact → deploy pipeline without needing a container registry, and scale-to-zero (Container Apps' main differentiator) is not a requirement for an always-on internal/admin API. Reconsider if the platform grows background workers or event-driven scaling needs. |
| Key Vault role assignment | [`key-vault-access.bicep`](../infra/modules/key-vault-access.bicep) | Grants the App Service's system-assigned managed identity the built-in **Key Vault Secrets User** role (read-only) on the vault. |
| Storage role assignment | [`storage-access.bicep`](../infra/modules/storage-access.bicep) | Grants the same managed identity **Storage Blob Data Contributor** on the storage account — the role needed both for blob read/write and for minting user-delegation SAS tokens. |

**Region**: default `uaenorth` (UAE North). The platform is Arabic/Saudi-facing, so data
residency and latency both favour a Gulf region; `uaenorth` is the closest Azure region with
full first-party availability of every service used here (App Service Linux, Azure SQL, Key
Vault, Storage, Application Insights) — Azure has never operated a region physically inside
Saudi Arabia. See [Region fallback](#region-fallback) if a SKU is unavailable there at deploy
time.

**Environments**: `dev`, `staging`, `prod`, one resource group each, selected via
[`infra/parameters/{dev,staging,prod}.bicepparam`](../infra/parameters/). `prod` differs from
`dev`/`staging` in SKU sizing, Key Vault purge protection (on, irreversible), zone-redundant
storage, and longer log retention — see the parameter files themselves, which are commented in
detail.

**Deliberately not provisioned by this template** (out of scope for "provision the platform's
own resources"; each is a manual, documented step below): the Azure Communication Services
Email resource and its verified sender domain (Step 5), the app registration used for CD OIDC
(Step 2), and the SQL contained-database-user for the managed identity (Step 4).

## Prerequisites

- An Azure subscription, and a role on it sufficient to create resource groups, app
  registrations, and role assignments (Owner, or Contributor + User Access Administrator, at
  the subscription or target resource group scope).
- Access to [Azure Cloud Shell](https://shell.azure.com) — it comes with `az` and `bicep`
  preinstalled and already logged in to your account, so no local tooling is required.
- Permission in your Entra ID tenant to create an app registration and grant it directory
  permissions (Application Administrator or Global Administrator, or ask your tenant admin to
  do Step 2 on your behalf).
- A GitHub repository admin on this repo, to configure environments/secrets/variables (Step 6).
- Decide, before you start: which environment you are deploying (`dev`/`staging`/`prod`), the
  Entra ID group that should administer that environment's SQL server (Step 3 needs its object
  ID), and — if you want email notifications working — that you'll provision an Azure
  Communication Services Email resource and verified sender domain separately (Step 5).

## Step 1 — clone and open Cloud Shell

```bash
# In Cloud Shell (https://shell.azure.com), Bash environment:
git clone <this-repository-url> icbank-platform
cd icbank-platform
az account show   # confirm you're on the right subscription
az account set --subscription "<subscription-id-or-name>"   # if not
```

## Step 2 — the app registration and OIDC federation

This creates the identity the GitHub Actions CD pipeline authenticates as. It uses **workload
identity federation (OIDC)** — GitHub issues a short-lived OIDC token per workflow run, Entra ID
trusts it based on the federated credential's subject claim, and no client secret or publish
profile is ever created or stored anywhere.

```bash
# 1. Create the app registration.
APP_NAME="icbank-platform-cd"
APP_ID=$(az ad app create --display-name "$APP_NAME" --query appId -o tsv)
echo "AZURE_CLIENT_ID=$APP_ID"

# 2. Create the service principal for that app.
az ad sp create --id "$APP_ID"
SP_OBJECT_ID=$(az ad sp show --id "$APP_ID" --query id -o tsv)

# 3. Record your tenant and subscription IDs (you'll need both as GitHub secrets).
az account show --query tenantId -o tsv        # -> AZURE_TENANT_ID
az account show --query id -o tsv              # -> AZURE_SUBSCRIPTION_ID

# 4. Federate it to this GitHub repo, one federated credential per GitHub Environment you will
#    deploy to (dev/staging/prod) — this repo's backend-deploy.yml sets `environment:` per job,
#    and GitHub's OIDC token subject claim encodes which Environment the run used, so each needs
#    its own trust relationship. Replace ORG/REPO with your actual GitHub org/repo.
for ENV in dev staging prod; do
  az ad app federated-credential create \
    --id "$APP_ID" \
    --parameters '{
      "name": "gh-actions-'"$ENV"'",
      "issuer": "https://token.actions.githubusercontent.com",
      "subject": "repo:ORG/REPO:environment:'"$ENV"'",
      "audiences": ["api://AzureADTokenExchange"]
    }'
done

# 5. Grant the app the Azure RBAC it needs to deploy. Contributor at the resource-group scope is
#    sufficient for `az deployment group create`; if you use the subscription-scope command in
#    Step 3 (which also creates the resource group), grant Contributor at the SUBSCRIPTION scope
#    instead, or pre-create the resource group yourself and scope this to it.
RG_NAME="icbank-<env>-rg"   # pick your naming; must match what you use in Step 3
az role assignment create \
  --assignee "$SP_OBJECT_ID" \
  --role "Contributor" \
  --scope "/subscriptions/$(az account show --query id -o tsv)"

# 6. The CD pipeline's migrate job also needs SQL access to run `dotnet ef database update`
#    (Entra-ID-only auth — see Step 4). Add this same service principal, or a group it belongs
#    to, as an additional SQL Entra administrator, or grant it db_ddladmin via the T-SQL in
#    Step 4. Simplest for a first deployment: reuse the same Entra group you pass as
#    sqlEntraAdminObjectId in Step 3.
```

Why Contributor at the subscription scope rather than a narrower custom role: this repo's
`az deployment sub create` command (Step 3) creates the resource group itself as part of the
deployment, which needs subscription-scope write access. Once the resource group exists you can
tighten this to a resource-group-scoped role assignment instead — see
[Secret rotation](#secret-rotation).

## Step 3 — the first deployment (single command)

This is the one command that provisions everything in
[What gets provisioned, and why](#what-gets-provisioned-and-why) from nothing. It targets
subscription scope because it also creates the resource group.

**Before running it**, get the Entra ID object ID of the group (or user) that should administer
SQL for this environment:

```bash
az ad group show --group "icbank-platform-sql-admins" --query id -o tsv
# or, for a user: az ad user show --id "user@yourtenant.com" --query id -o tsv
```

Then, from the repo root in Cloud Shell:

```bash
az deployment sub create \
  --name "icbank-dev-$(date +%Y%m%d%H%M%S)" \
  --location uaenorth \
  --template-file infra/main.bicep \
  --parameters infra/parameters/dev.bicepparam \
  --parameters resourceGroupName=icbank-dev-rg \
  --parameters sqlEntraAdminObjectId="<object-id-from-above>" \
  --parameters sqlEntraAdminLogin="icbank-platform-sql-admins" \
  --parameters sqlEntraAdminTenantId="$(az account show --query tenantId -o tsv)"
```

> **Note on `resourceGroupName`**: `infra/main.bicep` has `targetScope = 'resourceGroup'`, so a
> subscription-scope `az deployment sub create` needs a thin wrapper template (or the
> `--parameters resourceGroupName=...` shown above assumes one exists) that creates the resource
> group and then invokes `main.bicep` as a module scoped into it. **This wrapper does not exist
> in the repository yet** — it is listed explicitly under
> [Unverified without a live subscription](#unverified-without-a-live-subscription) as something
> that could not be authored with confidence and verified without a subscription to test
> `az deployment sub create`'s exact parameter-passing behavior against a real tenant. Until it
> is added, use the two-command resource-group-scope form instead, which needs no wrapper and is
> the form this repo's Bicep files were written and bicep-built against:
>
> ```bash
> az group create --name icbank-dev-rg --location uaenorth
>
> az deployment group create \
>   --name "icbank-dev-$(date +%Y%m%d%H%M%S)" \
>   --resource-group icbank-dev-rg \
>   --template-file infra/main.bicep \
>   --parameters infra/parameters/dev.bicepparam \
>   --parameters sqlEntraAdminObjectId="<object-id-from-above>" \
>   --parameters sqlEntraAdminLogin="icbank-platform-sql-admins" \
>   --parameters sqlEntraAdminTenantId="$(az account show --query tenantId -o tsv)"
> ```
>
> Swap `dev.bicepparam` / `icbank-dev-rg` / `uaenorth` for `staging`/`prod` and your chosen
> region as appropriate. `sqlEntraAdminObjectId/Login/TenantId` have no default in `main.bicep`
> on purpose — there is no safe default for "who administers this environment's database" — so
> they must be supplied on every environment's first deploy this way, once, after which they are
> fixed for that environment.

This single deployment creates the resource group (if using the two-command form, that command
does) and everything in the table above, in dependency order, via Bicep's module graph. Capture
its outputs — you'll need several of them in Steps 5–6:

```bash
az deployment group show \
  --resource-group icbank-dev-rg \
  --name "<the deployment name you used above>" \
  --query properties.outputs
```

## Step 4 — the manual SQL step Bicep cannot do

**Confirmed mandatory and load-bearing by the 2026-08-06 dev deployment — do not skip this
step for staging/prod, or for any dev environment that keeps the default Entra-ID-only design.**

> **Dev-only deviation actually used on 2026-08-06 (second session):** the sandbox running that
> session had an ARM (`management.azure.com`) credential but, like the first session, no
> `database.windows.net`-scoped Entra ID credential of any kind — so the T-SQL statement below
> could still not be executed. Rather than leave the app crash-looping a second time, that session
> took the alternative the original task explicitly allowed for dev: it (1) checked
> `azureADOnlyAuthentications/Default` on `icbank-dev-sql` via ARM and found it `true`, (2) set it
> to `false` via `PUT .../azureADOnlyAuthentications/Default`, (3) set a freshly generated SQL
> admin password via `PATCH .../servers/icbank-dev-sql` (note: the admin **login** itself could not
> be renamed this way — Azure SQL's `administratorLogin` is immutable after server creation; the
> pre-existing Bicep-generated login `CloudSA85825d34` was reused), and (4) pointed
> `ConnectionStrings__Default` at that SQL login instead of
> `Authentication=Active Directory Managed Identity`. `dotnet ef database update` then succeeded
> immediately with a plain SQL-auth connection string — no Entra token needed at all — and the app
> booted cleanly. **This is a deliberate, dev-only compromise, not a correction to this runbook's
> design for staging/prod.** It also means the live `icbank-dev-sql` server has **drifted** from
> `infra/modules/sql.bicep`, which still declares `azureADOnlyAuthentication: true` — anyone
> re-running the Bicep deployment against the dev resource group without accounting for this will
> silently revert dev to Entra-ID-only and reintroduce the original crash-loop. See
> `spec/DEV-DEPLOYMENT-NOTES.md` (session 2) for the full detail, including the EF migration result
> (succeeded, `icbank-dev-db` now has a full schema) and the 17/17 smoke test pass.
>
> The steps below remain the correct, permanent design for any environment that keeps
> Entra-ID-only auth (staging, prod, and dev if it is ever brought back in line) — they were not
> exercised successfully end-to-end in either 2026-08-06 session, because no session had a
> `database.windows.net` credential to run them with.

Skipping it does not degrade gracefully: this
codebase's `Program.cs` calls its database seeder (roles, initial super-admin) synchronously
during host startup, before the app binds to a port. If the managed identity has no SQL login,
that seed query throws `Microsoft.Data.SqlClient.SqlException: Login failed` (Error 18456), the
exception is unhandled, and the whole process aborts — the app crash-loops forever and never
serves a single HTTP request, including `/health/live`. This was reproduced end-to-end: the app
was fully deployed and otherwise correctly configured, but never came up, purely because this
step had not been run. See `spec/DEV-DEPLOYMENT-NOTES.md` for the full failure trace.

Azure Resource Manager / Bicep has no declarative resource for `CREATE USER FROM EXTERNAL
PROVIDER` — granting a managed identity access *inside* a specific database is a T-SQL
operation, not an ARM operation, and must be run once per environment after the SQL server and
database exist. This is a known, documented gap, not an oversight (see the comment at the bottom
of `infra/main.bicep`).

```bash
# Connect as the Entra ID admin you set in Step 3 (needs the SQL admin's own Entra login, e.g.
# via Azure Data Studio, sqlcmd with -G, or the Query Editor in the Azure Portal, which
# supports Entra ID auth directly with no extra setup):
sqlcmd -S <sqlServerFqdn-from-outputs> -d <sqlDatabaseName-from-outputs> -G
```

```sql
-- Run against the application database (not master):
CREATE USER [<appServiceName-from-outputs>] FROM EXTERNAL PROVIDER;
ALTER ROLE db_datareader ADD MEMBER [<appServiceName-from-outputs>];
ALTER ROLE db_datawriter ADD MEMBER [<appServiceName-from-outputs>];
ALTER ROLE db_ddladmin   ADD MEMBER [<appServiceName-from-outputs>];
-- db_ddladmin is required because EF Core migrations run schema DDL. If you would rather the
-- running app have less privilege than the entity that applies migrations, grant db_ddladmin
-- only to the CD pipeline's own Entra identity (Step 2) instead of the App Service's identity,
-- and drop it from the app's grants above once migrations are confirmed working end to end.
```

The App Service's managed identity display name in Entra ID matches its resource name
(`appServiceName` output, e.g. `icbank-dev-api`) — that's the `[...]` name to use above.

## Step 5 — populate Key Vault secrets

Nothing in Bicep writes an actual secret value — only the empty vault and its RBAC wiring exist
after Step 3. Populate the real values once per environment:

```bash
KV_NAME="<keyVaultName-from-outputs>"

# Jwt:SigningKey — must never be empty (StartupSecretsGuardExtensions fails fast if it is).
# Generate a strong random key, don't reuse the one from smoke-test.sh or any lower environment.
az keyvault secret set --vault-name "$KV_NAME" --name "Jwt--SigningKey" \
  --value "$(openssl rand -base64 48)"

# Cron:ApiKey — shared secret for the platform's authenticated cron endpoints.
az keyvault secret set --vault-name "$KV_NAME" --name "Cron--ApiKey" \
  --value "$(openssl rand -base64 32)"

# ConnectionStrings:Default is NOT set here — it is written directly as an app setting by
# app-service.bicep (Authentication=Active Directory Managed Identity, no password), so there is
# nothing secret to store for it.
```

> Naming: App Service's Key Vault configuration provider maps `--` in a secret name to `:` in
> .NET configuration, so `Jwt--SigningKey` in Key Vault becomes `Jwt:SigningKey` in the app —
> matching the keys `StartupSecretsGuardExtensions` checks for.

If you want outbound email working, provision an **Azure Communication Services** resource and a
verified email domain separately (this repo's Bicep does not create one — see
[What gets provisioned, and why](#what-gets-provisioned-and-why)), then set:

```bash
az deployment group create --resource-group icbank-dev-rg \
  --template-file infra/main.bicep --parameters infra/parameters/dev.bicepparam \
  --parameters acsEmailEndpoint="https://<your-acs-resource>.communication.azure.com" \
  --parameters acsEmailSenderAddress="DoNotReply@<your-verified-domain>" \
  --parameters sqlEntraAdminObjectId=... --parameters sqlEntraAdminLogin=... --parameters sqlEntraAdminTenantId=...
  # (re-supply the SQL admin params every time you re-run the deployment — Bicep parameter
  # files are not additive/persistent between separate invocations)
```

This is a Bicep re-deploy (idempotent — it only updates the App Service app setting), not a new
resource group.

## Step 6 — GitHub repository configuration

In the GitHub repo: **Settings → Environments**, create `dev`, `staging`, and `prod` (matching
the federated credentials from Step 2). For `prod`, add required reviewers under
**Deployment protection rules** — this is what makes `environment: prod` in
`backend-deploy.yml` actually gate the job on human approval.

**Repository or Environment secrets** (Settings → Secrets and variables → Actions), per
environment:

| Secret | Value | How to obtain |
|---|---|---|
| `AZURE_CLIENT_ID` | The app registration's Application (client) ID | Output of `az ad app create` in Step 2, or `az ad app list --display-name icbank-platform-cd --query "[0].appId" -o tsv` |
| `AZURE_TENANT_ID` | Your Entra ID tenant ID | `az account show --query tenantId -o tsv` |
| `AZURE_SUBSCRIPTION_ID` | Your subscription ID | `az account show --query id -o tsv` |
| `SMOKE_SEED_EMAIL` | The email of an account that already exists in this environment's database, used by the deployed-mode smoke test to log in | The `Seed:InitialSuperAdminEmail` value you configured for this environment (App Service app setting or Key Vault) |
| `SMOKE_SEED_PASSWORD` | That account's password | Whatever you set `Seed:InitialSuperAdminPassword` to — rotate this immediately if it was ever a placeholder |

**Repository or Environment variables** (same location, "Variables" tab — non-secret, but
environment-specific):

| Variable | Value | How to obtain |
|---|---|---|
| `AZURE_RESOURCE_GROUP` | e.g. `icbank-dev-rg` | Whatever you named it in Step 3 |
| `APP_SERVICE_NAME` | e.g. `icbank-dev-api` | `appServiceName` output from Step 3 |
| `SQL_SERVER_NAME` | e.g. `icbank-dev-sql` | `sqlServerName` output from Step 3 |
| `SQL_SERVER_FQDN` | e.g. `icbank-dev-sql.database.windows.net` | `sqlServerFqdn` output from Step 3 |
| `SQL_DATABASE_NAME` | e.g. `icbank-dev-db` | `sqlDatabaseName` output from Step 3 |
| `FRONTEND_APP_SERVICE_NAME` | e.g. `icbank-dev-frontend` | The App Service hosting the internal-comms UI. Only read by `frontend-deploy.yml`; leave unset if you do not deploy the frontend from CI |

None of the above are secrets in the sense of granting standing access by themselves — resource
names and FQDNs are not credentials — but they're still environment-specific configuration, so
GitHub Variables (not hardcoded YAML) is the right place for them, consistent with deploying the
same workflow file unchanged across dev/staging/prod.

## Step 7 — first CD pipeline run

In the GitHub repo: **Actions → Backend CD → Run workflow**, choose the environment. This runs
`.github/workflows/backend-deploy.yml`: the full gate suite (build, format, lizard, vulnerable
packages, tests+coverage), then an explicit `dotnet ef database update` against the real
database (opening and closing a temporary SQL firewall rule for the runner — see
[Letting the CD pipeline reach SQL](#letting-the-cd-pipeline-reach-sql)), then `az webapp deploy`,
then `backend/scripts/smoke-test.sh` against the live deployed URL. Watch it end to end the
first time — this pipeline has not been run against a live tenant by the author of this runbook
(see [Unverified without a live subscription](#unverified-without-a-live-subscription)).

If everything passes, the app is live at `https://<APP_SERVICE_NAME>.azurewebsites.net`.

## Step 8 — deploying the frontend

The internal-comms UI (`artifacts/internal-comms/`) deploys separately from the API, via
**Actions → Frontend CD → Run workflow**. It reuses the same OIDC federation and the same
`AZURE_CLIENT_ID` / `AZURE_TENANT_ID` / `AZURE_SUBSCRIPTION_ID` secrets as Backend CD, so the
only extra configuration it needs is the `FRONTEND_APP_SERVICE_NAME` variable above. No
publish profile is involved — a publish profile is a long-lived credential with deploy rights
that would sit in GitHub settings indefinitely, whereas the OIDC token is minted per run and
expires with it.

What the workflow does, and why each step is there:

1. **Syntax-checks `server.mjs` and every inline `<script>` block** in `index.html` and
   `login.html`. The site is static HTML with no build step, so nothing else would catch a
   syntax error — and a broken inline script still returns HTTP 200, so a status-code smoke
   test would call it a success while users saw a blank page.
2. **Pins the startup command** to `node server.mjs`. App Service's Node autodetect runs
   `npm start`, which this `package.json` does not define; without the pin the container
   serves the directory statically and every deep link 404s instead of falling through to the
   application shell.
3. **`az webapp deploy`** the zipped directory.
4. **Compares the live bytes to the committed files.** `server.mjs` streams both HTML files
   verbatim (`res.end(data)`, no templating), so the sha256 of the served body must equal the
   sha256 of the file in git. This is the only check that distinguishes a landed deploy from a
   stale container still serving the previous build. `/` sits behind a server-side session
   gate that only tests for the *presence* of a cookie, so the job sends
   `Cookie: has_session=1` — a dummy value, never a real credential in a CI log.
5. **Asserts the release invariants**: six `data-group-key` nav groups, the
   `icbank.nav.groups.v1` persistence key, the `1023.98px` drawer breakpoint, the `tbl-cards`
   responsive-table class, and the *absence* of `right: -300px` on the drawer. That last one
   is a regression guard: parking the off-canvas sidebar at a negative offset enlarges the
   document's scrollable area, which makes mobile Chrome widen the layout viewport (375 → 385)
   and shifts every `position: fixed` overlay off-centre.

Like Backend CD, this is `workflow_dispatch` only and has not been run against a live tenant
(see [Unverified without a live subscription](#unverified-without-a-live-subscription)). Once
you have watched it succeed against dev, adding a `push` trigger filtered to
`artifacts/internal-comms/**` is a one-line change.

## Region fallback

If a specific SKU or service is unavailable in `uaenorth` at deploy time (Azure regions vary in
which SKUs they carry, and this can change), do **not** re-run the whole deployment with a single
global `location=westeurope` override — that silently moves every resource, including the ones
that deployed fine, out of the approved region. **Confirmed in the 2026-08-06 dev deployment:**
`uaenorth` had a hard `SubscriptionIsOverQuotaForSku` block (0-VM quota) for the Basic (B1) Linux
App Service Plan SKU specifically — every other module (Log Analytics, App Insights, Key Vault,
Storage, SQL server/database) deployed to `uaenorth` without any issue. The correct fix is to
redeploy only the failing module standalone with its own `location` parameter override (e.g.
`infra/modules/app-service.bicep` with `--parameters location=westeurope` and the sibling
modules' outputs passed through manually), leaving every other resource's region untouched.
`westeurope` remains the documented fallback because it is a mature, full-featured Azure region
with long-standing availability of every service this stack uses. If keeping the App Service in
`uaenorth` is a hard requirement, request an Azure support quota increase for Basic/B-series
Linux App Service Plan VMs in `uaenorth` for this subscription before deploying.

## Letting the CD pipeline reach SQL

`allowAzureServicesToReachSql` (default `true`) only opens the SQL firewall to the special
`0.0.0.0`–`0.0.0.0` "allow Azure services" rule, which covers Azure-hosted callers like the App
Service itself. **GitHub-hosted Actions runners are not an Azure service** and are not covered
by it — they have rotating public IPs from GitHub's own ranges. Two supported options, in order
of what this repo actually implements:

1. **(Implemented in `backend-deploy.yml`)** The `migrate` job resolves its own runner's public
   IP at run time (`curl ifconfig.me`) and opens a firewall rule scoped to exactly that IP for
   the duration of the job, removing it afterward (`if: always()`, so it's cleaned up even if the
   migration step fails). No standing firewall exposure between deployments.
2. **Not implemented, an alternative for stricter environments:** run a self-hosted runner with
   a static/known IP, and add it once to `sqlFirewallAllowedRanges` in the relevant
   `.bicepparam` file. Preferable if your organization disallows dynamic firewall changes from
   CI, at the cost of operating a self-hosted runner.

## Secret rotation

- **`Jwt:SigningKey`**: rotating this invalidates every currently-issued access and refresh
  token — all users are signed out. Set a new value with `az keyvault secret set` (Step 5); the
  App Service picks up Key Vault references on next restart (or immediately, depending on the
  Key Vault reference cache TTL — restart the App Service to force it:
  `az webapp restart --name <APP_SERVICE_NAME> --resource-group <AZURE_RESOURCE_GROUP>`).
- **`Cron:ApiKey`**: rotate the same way; update whatever external caller (e.g. Azure Scheduler,
  a GitHub Actions cron workflow) invokes the cron endpoints with the old value.
- **OIDC federated credential (`AZURE_CLIENT_ID` / federated trust)**: there is no secret to
  rotate — that is the point of OIDC. If the app registration itself is ever compromised or
  needs replacing, repeat Step 2 end to end with a new app registration and update the
  `AZURE_CLIENT_ID` GitHub secret.
- **`SMOKE_SEED_PASSWORD`**: rotate via whatever mechanism changes that account's password in
  the deployed environment (the platform's own password-change flow, or re-seeding), then update
  the GitHub secret to match.
- **Storage/SQL**: no keys or passwords exist to rotate for either — both are managed-identity
  and Entra-ID-only by design. If shared-key access to Storage is ever needed for emergency
  tooling (`allowSharedKeyAccess: true` is left on for this reason — see `storage.bicep`),
  rotate that account key with `az storage account keys renew`.

## Rollback

- **App Service code**: App Service keeps prior zip-deploy packages accessible via deployment
  history. Fastest rollback: re-run the CD pipeline against a known-good prior commit
  (`workflow_dispatch` from that ref), which re-runs the full gate suite against it too — a
  rollback that skips the gates is not a safety net, it's a second unverified deploy. If you need
  to roll back faster than a full pipeline run, `az webapp deployment list-publishing-credentials`
  + redeploying a previously-downloaded artifact is possible but bypasses gates; only do this
  under active incident pressure and re-run the full pipeline immediately after to re-verify.
- **Database migrations**: EF Core migrations are forward-only by convention in this repo (no
  automatic down-migration on deploy). To roll back a schema change, write and apply a new,
  reviewed migration that reverses it — `dotnet ef database update <previous-migration-name>` is
  possible ad hoc from Cloud Shell (with `dotnet-ef` installed and Entra ID auth to the target
  DB) but should be treated as a break-glass action, not routine practice, since it can be
  destructive if the newer migration already dropped a column.
- **Infrastructure**: Bicep deployments are declarative and idempotent — re-running
  `az deployment group create` with an older commit's `main.bicep`/parameter file converges
  infrastructure back to that state for anything Bicep manages. It will **not** undo the manual
  T-SQL step (Step 4) or Key Vault secret values (Step 5), which are outside Bicep's management
  by design.

## Unverified without a live subscription

This runbook, the Bicep templates, and `backend-deploy.yml` were authored with `bicep build`
as the only available validation (see `spec/AZURE-NOTES.md` for its exact output) — there is no
Azure subscription in the environment they were written in. The following are explicitly
**not** verified against a real tenant and should be treated as the highest-risk parts of a
first deployment:

- **The `az deployment sub create` subscription-scope wrapper described as a note in
  [Step 3](#step-3--the-first-deployment-single-command)** does not exist in this repository.
  `main.bicep` is written with `targetScope = 'resourceGroup'`, which is correct for the
  two-command `az group create` + `az deployment group create` form actually documented and
  used above, but the task that produced this runbook asked for a single subscription-scope
  command; authoring an untested subscription-scope wrapper template and asserting it works
  would have been a bigger risk than documenting the honest two-command alternative. If a true
  one-command flow is required, add a thin `infra/main.subscription.bicep` with
  `targetScope = 'subscription'` that creates the resource group via a `Microsoft.Resources/resourceGroups`
  resource and then invokes today's `main.bicep` as a module — and validate it against a real
  subscription before relying on it.
- **Exact SKU/quota availability in `uaenorth`** for every resource type at deploy time —
  regional SKU availability shifts over time and can only be confirmed with
  `az vm list-skus`/equivalent against a live subscription.
- **The federated credential `subject` claim format** (`repo:ORG/REPO:environment:ENV`) is
  Microsoft's documented format for GitHub Environment-scoped OIDC trust, but the actual
  end-to-end token exchange (GitHub → Entra ID → ARM call) has not been exercised.
- **`Authentication=Active Directory Managed Identity` and `Authentication=Active Directory
  Default`** connection string keywords, used respectively by the deployed App Service
  (`app-service.bicep`) and the CD pipeline's migration step (`backend-deploy.yml`), are
  documented `Microsoft.Data.SqlClient` features (the project's EF Core SQL Server provider pulls
  in a version that supports them) but **still have not been exercised against a real Azure SQL
  server from either context, as of either 2026-08-06 session** — both lacked any
  `database.windows.net`-scoped Entra credential to test them with. What **has** now been verified
  end-to-end against real Azure SQL (second 2026-08-06 session) is plain SQL authentication (a
  `User ID=...;Password=...` connection string) from both `dotnet ef database update` and the
  running App Service, as a dev-only substitute — see the note in Step 4 above. This is a
  different code path from the Entra-ID ones above, not a verification of them.
- **Whether the CD pipeline's federated identity, once granted SQL admin/db_ddladmin per Step
  2/4, can actually run `dotnet ef database update` successfully** end to end — this depends on
  Entra ID token acquisition working correctly for a service principal (not a user), which some
  SQL driver versions have historically had rough edges with.
- **`az webapp deploy --type zip`** itself was not exercised in the 2026-08-06 dev run (no `az`
  CLI available; the equivalent Kudu zip-deploy REST endpoint was used instead and worked without
  issue), but the resulting **app startup was observed, and failed** — see
  `spec/DEV-DEPLOYMENT-NOTES.md` for the full sequence. One real bug in this repository's Bicep
  was found and is now fixed: `linuxFxVersion` must use the **pipe** syntax `DOTNETCORE|8.0`, not
  the colon syntax `DOTNETCORE:8.0` that `infra/main.bicep` and all three `.bicepparam` files
  previously defaulted to. The colon syntax does not error anywhere in the ARM control plane —
  `GET .../config/web` happily echoes back whatever string you sent — but the platform silently
  falls back to its default container image (PHP 8.2-FPM was observed) instead of .NET, and the
  app never runs at all. This has been corrected in `infra/main.bicep` and
  `infra/parameters/{dev,staging,prod}.bicepparam`. Key Vault references and managed-identity
  token acquisition for Blob Storage were **not** confirmed working end-to-end, because the app
  never got that far before crashing on notification-provider configuration and then on a SQL
  login failure for the managed identity (Step 4 had not been completed) — see
  `spec/DEV-DEPLOYMENT-NOTES.md` for both.
- **Leaving `Notifications:Provider=AzureCommunicationServices` (this repo's apparent default
  expectation) without a real Communication Services endpoint configured crashes the app at
  startup** with an unhandled `System.UriFormatException`, not a clean error — the null-check in
  `Icbank.Platform.Infrastructure/DependencyInjection.cs`'s `AddNotificationServices` only guards
  against a missing key, not an empty string. Until a real Communication Services resource and
  verified domain are provisioned for an environment (see Step 5), explicitly set
  `Notifications__Provider=Null` as an App Service app setting for that environment, or the app
  will not start.
- **`backend/scripts/smoke-test.sh`'s deployed mode runs 17 pass/fail assertions, not 20** —
  section 10 ("no credentials in the logs") is explicitly skipped when `SMOKE_BASE_URL` is set,
  because the script has no access to the deployed instance's own application logs (those live in
  Azure Monitor/App Insights). Anyone relying on "20/20" as the expected passing count in deployed
  mode should expect 17/17 instead. **Update:** the second 2026-08-06 session ran this exact
  script against the live `icbank-dev-api` deployment (with the dev-only SQL-auth substitution
  from Step 4 above) and **all 17 assertions passed** — this confirms the script, the deployed
  mode's HTTP-level checks, and the app's real runtime behavior all agree, at least for the SQL-auth
  dev configuration. See `spec/DEV-DEPLOYMENT-NOTES.md` (session 2) for the full per-assertion
  breakdown.
- **Cost estimates** are deliberately not included anywhere in this runbook or
  `spec/AZURE-NOTES.md` — SKU pricing varies by region/time and quoting a number without being
  able to verify it against the Azure Pricing Calculator for the exact `uaenorth` SKUs chosen
  would be a guess presented as fact.
- **The exact Key Vault reference cache TTL** mentioned in [Secret rotation](#secret-rotation) —
  documented by Microsoft as "up to a few hours" historically, but not reconfirmed here.

If you hit a wall on any of the above, the fix is almost always: run the one command that's
failing by itself with `--debug`, read the actual Azure error, and adjust — these are standard,
well-documented Azure mechanisms, just ones this runbook's author could not personally exercise.
