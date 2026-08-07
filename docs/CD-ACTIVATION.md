# Activating CD — the remaining steps

`docs/DEPLOYMENT.md` is the full runbook. This file is the short list of what is *still
outstanding* for `raidalghamdi/icbank-platform`, with the values already substituted, so it
can be pasted straight into [Azure Cloud Shell](https://shell.azure.com) (Bash).

The Azure infrastructure already exists — resource group `icbank-dev-rg`, API
`icbank-dev-api`, frontend `icbank-dev-frontend`, SQL `icbank-dev-sql`, in subscription
`f1422c2e-a1f8-4794-bfa4-d1c9c16e9287`. What does not exist is the **identity GitHub
Actions authenticates as**, which is why `Frontend CD` fails at the `Azure login` step with:

```
Login failed with Error: Using auth-type: SERVICE_PRINCIPAL. Not all values are present.
Ensure 'client-id' and 'tenant-id' are supplied.
```

That message means the `AZURE_CLIENT_ID` / `AZURE_TENANT_ID` secrets resolved to empty
strings. The repo currently has **no secrets at all** and no `dev` environment.

## Already done

- Repository variable `FRONTEND_APP_SERVICE_NAME` = `icbank-dev-frontend`.
  It was originally saved with a trailing U+2009 THIN SPACE (a paste artefact), which would
  have made `az webapp deploy` fail with a "not found" against a name that looks correct in
  the UI. It has been reset to clean ASCII.
- The workflow files now exist on `main`. GitHub only exposes a `workflow_dispatch` workflow
  in the Actions tab if its file is on the default branch, so before this neither CD pipeline
  had a Run workflow button.

## Step A — create the CD identity (Cloud Shell)

```bash
az account set --subscription f1422c2e-a1f8-4794-bfa4-d1c9c16e9287

APP_ID=$(az ad app create --display-name icbank-platform-cd --query appId -o tsv)
az ad sp create --id "$APP_ID"
SP_OBJECT_ID=$(az ad sp show --id "$APP_ID" --query id -o tsv)

# One federated credential per GitHub Environment. The OIDC token's subject claim encodes
# which Environment the run used, so dev/staging/prod each need their own trust entry.
for ENV in dev staging prod; do
  az ad app federated-credential create --id "$APP_ID" --parameters '{
    "name": "gh-actions-'"$ENV"'",
    "issuer": "https://token.actions.githubusercontent.com",
    "subject": "repo:raidalghamdi/icbank-platform:environment:'"$ENV"'",
    "audiences": ["api://AzureADTokenExchange"]
  }'
done

# Contributor scoped to the resource group is enough for both CD pipelines. Subscription
# scope is only needed if you re-run the Bicep that creates the resource group itself.
az role assignment create --assignee "$SP_OBJECT_ID" --role Contributor \
  --scope "/subscriptions/f1422c2e-a1f8-4794-bfa4-d1c9c16e9287/resourceGroups/icbank-dev-rg"

echo "AZURE_CLIENT_ID=$APP_ID"
echo "AZURE_TENANT_ID=$(az account show --query tenantId -o tsv)"
echo "AZURE_SUBSCRIPTION_ID=f1422c2e-a1f8-4794-bfa4-d1c9c16e9287"
```

## Step B — configure GitHub

**Settings → Environments → New environment → `dev`.** The name must match the federated
credential subject exactly; `frontend-deploy.yml` pins `environment: dev` when you pick `dev`
at dispatch, and the OIDC subject will not match otherwise.

Then **Settings → Secrets and variables → Actions**:

| Tab | Name | Value |
|---|---|---|
| Secrets | `AZURE_CLIENT_ID` | from Step A |
| Secrets | `AZURE_TENANT_ID` | from Step A |
| Secrets | `AZURE_SUBSCRIPTION_ID` | `f1422c2e-a1f8-4794-bfa4-d1c9c16e9287` |
| Variables | `AZURE_RESOURCE_GROUP` | `icbank-dev-rg` |
| Variables | `FRONTEND_APP_SERVICE_NAME` | `icbank-dev-frontend` (already set) |

Paste into a plain text editor first if you are copying from a chat window or a rendered
document — that is where the thin space above came from.

`Backend CD` additionally needs `SMOKE_SEED_EMAIL` / `SMOKE_SEED_PASSWORD` and the SQL
variables; see Step 6 of `DEPLOYMENT.md`. `Frontend CD` does not.

## Step C — dispatch

**Actions → Frontend CD → Run workflow.**

**Choose the branch deliberately.** The branch selector decides which commit is packaged.
`main` still carries an older `artifacts/internal-comms`, so running against `main` publishes
that, not the current work. For the Frutiger build, select
`feat/dotnet8-backend-foundation`, then `dev`.

The run packages the site, checks that `server.mjs` and every inline script block in
`index.html` and `login.html` parse, deploys the zip, and then `verify-live-bytes` compares
the sha256 of the served HTML against the committed file — so a stale container still serving
the previous build fails the run rather than passing a status-code check.

## Worth fixing separately

`backend-ci.yml` requests `dotnet-version: '8.0.x'`, which floats to whatever 8.0 patch the
runner image happens to ship. That is how a style-gate failure reached CI while
`dotnet format --verify-no-changes` passed locally on 8.0.423: the newer SDK's IDE0008
flagged a `var` the older one accepted. Pinning the SDK in a `global.json` would make the
gate reproducible off CI.
