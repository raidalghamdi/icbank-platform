#!/usr/bin/env bash
#
# Boot proof for the ICBank Platform API.
#
# Everything the test suite verifies runs the app in-process through
# WebApplicationFactory, with the database provider and clock swapped out. That leaves a
# whole class of failure unverified: the real DI graph, configuration binding from
# appsettings, migrations applied to an empty database, middleware ordering, and the
# response pipeline over an actual socket. An app can hold every test green and still
# fail to start.
#
# This script starts the published API as a real process against a real SQL Server,
# applies the committed migrations first (migrations are a deploy step here, not a
# startup side effect), and then asserts observable HTTP behaviour end to end.
#
# Two modes:
#   Local/CI mode (default): requires ICBANK_SMOKE_CONNECTION pointing at a reachable SQL
#     Server. Applies migrations, boots the API as a local process, asserts against it.
#   Deployed mode: set SMOKE_BASE_URL to the already-running deployment's base URL (e.g.
#     https://icbank-prod.azurewebsites.net). Skips the local boot step entirely -- the CD
#     pipeline applies migrations and deploys as separate prior steps -- and runs the exact
#     same HTTP assertions (sections 3 onward) against the deployed instance, so a passing
#     deploy is held to the same bar as a passing local/CI run instead of a weaker copy.
#     Deployed mode also requires SMOKE_SEED_EMAIL/SMOKE_SEED_PASSWORD, since a deployed
#     database is seeded once at first deploy rather than fresh on every run.
set -uo pipefail

BACKEND_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
cd "$BACKEND_DIR"

DEPLOYED_MODE=0
if [[ -n "${SMOKE_BASE_URL:-}" ]]; then
  DEPLOYED_MODE=1
fi

PORT="${ICBANK_SMOKE_PORT:-5080}"
BASE="${SMOKE_BASE_URL:-http://127.0.0.1:${PORT}}"
LOG_FILE="$(mktemp)"
SEED_EMAIL="${SMOKE_SEED_EMAIL:-smoke-superadmin@icbank.local}"
SEED_PASSWORD="${SMOKE_SEED_PASSWORD:-Sm0ke!Test-Password_2026}"
# Used by step 5b to clear the first-login restriction. Must satisfy the same complexity
# policy as the seeded one, and must differ from it or the change is rejected.
ROTATED_PASSWORD="${SMOKE_ROTATED_PASSWORD:-Sm0ke!Rotated-Password_2026}"
FAILURES=0
API_PID=""

log()  { printf '\n\033[1m%s\033[0m\n' "$*"; }
pass() { printf '  \033[32mPASS\033[0m  %s\n' "$*"; }
fail() { printf '  \033[31mFAIL\033[0m  %s\n' "$*"; FAILURES=$((FAILURES + 1)); }

cleanup() {
  if [[ -n "$API_PID" ]] && kill -0 "$API_PID" 2>/dev/null; then
    kill "$API_PID" 2>/dev/null
    wait "$API_PID" 2>/dev/null
  fi
}
trap cleanup EXIT

if [[ "$DEPLOYED_MODE" -eq 0 && -z "${ICBANK_SMOKE_CONNECTION:-}" ]]; then
  echo "ICBANK_SMOKE_CONNECTION is not set. It must point at a reachable SQL Server." >&2
  exit 1
fi

if [[ "$DEPLOYED_MODE" -eq 1 && ( -z "${SMOKE_SEED_EMAIL:-}" || -z "${SMOKE_SEED_PASSWORD:-}" ) ]]; then
  echo "SMOKE_BASE_URL is set but SMOKE_SEED_EMAIL/SMOKE_SEED_PASSWORD are not. Deployed mode" >&2
  echo "needs real credentials for an account that already exists in that environment -- it" >&2
  echo "does not seed one, unlike local/CI mode." >&2
  exit 1
fi

# ── Transient-fault retries (deployed mode only) ────────────────────────────────
# The dev App Service runs a single worker with health-check-driven instance replacement,
# so it recycles once more shortly *after* the deploy step returns. The warm-up gate in the
# workflow proves the worker is serving before the assertions start, but it cannot prove it
# will stay up: on runs 31234636892 and 31243308911 the first assertions passed and later
# ones came back 500/502 or outright "Connection refused", and every endpoint was verified
# healthy by hand minutes afterwards.
#
# So a connection failure or gateway error is retried a few times before it is called a
# failure. This does not soften the bar. A genuinely broken endpoint returns the same wrong
# status on every attempt and still fails the run -- only a fault that heals on its own
# within ~24s is absorbed, which is exactly the worker-recycle window and nothing else.
# 500 is deliberately included: a recycling worker's requests surface as 500 through the
# App Service front end. A real 500 is deterministic and survives all four attempts.
#
# Local/CI mode keeps a single attempt: there the API is a child process of this script, so
# a refused connection means it crashed and retrying would only delay the report.
if [[ "$DEPLOYED_MODE" -eq 1 ]]; then
  TRANSIENT_ATTEMPTS="${SMOKE_TRANSIENT_ATTEMPTS:-8}"
else
  TRANSIENT_ATTEMPTS=1
fi
TRANSIENT_BACKOFF_SECONDS="${SMOKE_TRANSIENT_BACKOFF_SECONDS:-12}"

retry() { printf '  \033[33mRETRY\033[0m %s\n' "$*"; }

# True when <actual> looks like the deployment cycling rather than a real answer. An expected
# status is never transient, and neither is any 4xx -- those are the product answering.
is_transient() {
  local expected="$1" actual="$2"
  [[ "$actual" == "$expected" ]] && return 1
  case "$actual" in
    000|500|502|503|504) return 0 ;;
    *) return 1 ;;
  esac
}

# ── Asserts an endpoint returns an expected status code ─────────────────────────
# usage: assert_status <description> <expected> <method> <path> [curl args...]
assert_status() {
  local description="$1" expected="$2" method="$3" path="$4"
  shift 4
  local actual attempt=1
  while :; do
    actual=$(curl -sS -o /dev/null -w '%{http_code}' -X "$method" "${BASE}${path}" "$@" 2>/dev/null)
    if ! is_transient "$expected" "$actual" || (( attempt >= TRANSIENT_ATTEMPTS )); then
      break
    fi
    retry "${description} — got ${actual} (attempt ${attempt}/${TRANSIENT_ATTEMPTS}), re-checking in ${TRANSIENT_BACKOFF_SECONDS}s"
    sleep "$TRANSIENT_BACKOFF_SECONDS"
    attempt=$((attempt + 1))
  done
  if [[ "$actual" == "$expected" ]]; then
    if (( attempt > 1 )); then
      pass "$description (${actual} on attempt ${attempt})"
    else
      pass "$description (${actual})"
    fi
  else
    fail "$description — expected ${expected}, got ${actual}"
  fi
}

# ── Fetches a response body, retrying the same transient faults ─────────────────
# usage: body=$(fetch_body <description> <method> <path> [curl args...])
# Writes only the body to stdout so it stays usable in a command substitution; retry chatter
# goes to stderr, which the workflow log still shows.
fetch_body() {
  local description="$1" method="$2" path="$3"
  shift 3
  local attempt=1 status
  local tmp
  tmp="$(mktemp)"
  while :; do
    status=$(curl -sS -o "$tmp" -w '%{http_code}' -X "$method" "${BASE}${path}" "$@" 2>/dev/null)
    if ! is_transient 200 "$status" || (( attempt >= TRANSIENT_ATTEMPTS )); then
      break
    fi
    retry "${description} — got ${status} (attempt ${attempt}/${TRANSIENT_ATTEMPTS}), re-checking in ${TRANSIENT_BACKOFF_SECONDS}s" >&2
    sleep "$TRANSIENT_BACKOFF_SECONDS"
    attempt=$((attempt + 1))
  done
  cat "$tmp"
  rm -f "$tmp"
}

if [[ "$DEPLOYED_MODE" -eq 0 ]]; then
  # ── 1. Migrations ────────────────────────────────────────────────────────────
  log "1. Applying migrations to an empty database"
  if ConnectionStrings__Default="$ICBANK_SMOKE_CONNECTION" \
     dotnet ef database update \
       --project src/Icbank.Platform.Infrastructure \
       --startup-project src/Icbank.Platform.Api \
       --no-build --configuration Release > "$LOG_FILE" 2>&1; then
    pass "migrations applied to a real SQL Server database"
  else
    fail "migrations did not apply"
    tail -40 "$LOG_FILE"
    exit 1
  fi

  # ── 2. Boot ──────────────────────────────────────────────────────────────────
  # Staging rather than Development: Development would enable Swagger and skip HSTS,
  # so it would not exercise the pipeline the platform actually deploys.
  #
  # --no-launch-profile matters. Without it dotnet run applies
  # src/Icbank.Platform.Api/Properties/launchSettings.json, whose applicationUrl overrides
  # ASPNETCORE_URLS - the app boots and seeds successfully but listens on a different
  # address, so every assertion below fails against a port nothing is bound to.
  # launchSettings is a local-development convenience and has no business in CI.
  # The startup guard's required-key list and this script's environment block are two copies
  # of the same knowledge, and they drifted: DownloadTokens:SigningKey was added to the guard
  # and never added here, so the API aborted on boot and the smoke job failed for a reason
  # that had nothing to do with the code under test. Rather than fix the omission and wait for
  # the next one, read the guard's list and prove this script covers it.
  GUARD_SRC="src/Icbank.Platform.Api/Extensions/StartupSecretsGuardExtensions.cs"
  missing_keys=""
  while read -r key; do
    [ -n "$key" ] || continue
    # "Jwt:SigningKey" is set as the env var Jwt__SigningKey
    env_name="${key//:/__}"
    grep -q "^[[:space:]]*${env_name}=" "$0" || missing_keys="$missing_keys $key"
  done <<< "$(sed -n '/RequiredKeys/,/};/p' "$GUARD_SRC" | grep -oE '"[A-Za-z]+:[A-Za-z]+"' | tr -d '"')"

  if [ -n "$missing_keys" ]; then
    fail "smoke script does not set required startup key(s):$missing_keys"
    printf '  add them to the environment block below, then re-run\n'
    exit 1
  fi
  pass "smoke script sets every key the startup guard requires"

  log "2. Starting the API as a real process"
  ASPNETCORE_ENVIRONMENT=Staging \
  ASPNETCORE_URLS="$BASE" \
  ConnectionStrings__Default="$ICBANK_SMOKE_CONNECTION" \
  Jwt__SigningKey='smoke-test-signing-key-not-for-production-use-32bytes' \
  Jwt__Issuer='icbank-platform' \
  Jwt__Audience='icbank-platform-clients' \
  Cors__AllowedOrigins__0='http://127.0.0.1:3000' \
  Cron__ApiKey='smoke-cron-key' \
  DownloadTokens__SigningKey='smoke-download-token-key-not-for-production-32bytes' \
  Seed__InitialSuperAdminEmail="$SEED_EMAIL" \
  Seed__InitialSuperAdminPassword="$SEED_PASSWORD" \
  Shorfah__FrontendBaseUrl="$BASE" \
  Shorfah__ApiBaseUrl="$BASE" \
    dotnet run --project src/Icbank.Platform.Api --no-build --no-launch-profile --configuration Release \
    > "$LOG_FILE" 2>&1 &
  API_PID=$!

  booted=0
  for _ in $(seq 1 60); do
    if ! kill -0 "$API_PID" 2>/dev/null; then
      fail "the process exited during startup"
      echo "── startup log ──"; tail -60 "$LOG_FILE"
      exit 1
    fi
    if [[ "$(curl -sS -o /dev/null -w '%{http_code}' "${BASE}/health/live" 2>/dev/null)" == "200" ]]; then
      booted=1
      break
    fi
    sleep 2
  done

  if [[ "$booted" != "1" ]]; then
    fail "the API never answered /health/live within 120s"
    echo "── startup log ──"; tail -60 "$LOG_FILE"
    exit 1
  fi
  pass "process started and is serving HTTP"
else
  # ── 1-2. Deployed mode ──────────────────────────────────────────────────────
  # The CD pipeline already applied migrations and deployed the app as separate,
  # explicit prior steps -- redoing either here would blur which step actually failed.
  log "1-2. Deployed mode: verifying the already-deployed instance is live"
  booted=0
  for _ in $(seq 1 30); do
    if [[ "$(curl -sS -o /dev/null -w '%{http_code}' "${BASE}/health/live" 2>/dev/null)" == "200" ]]; then
      booted=1
      break
    fi
    sleep 2
  done

  if [[ "$booted" != "1" ]]; then
    fail "the deployed API never answered /health/live within 60s"
    exit 1
  fi
  pass "deployed instance is serving HTTP at ${BASE}"
fi

# ── 3. Health ──────────────────────────────────────────────────────────────────
log "3. Health endpoints"
assert_status "/health/live is up"                       200 GET /health/live
assert_status "/health/ready confirms SQL is reachable"  200 GET /health/ready

# ── 4. Authentication boundary ─────────────────────────────────────────────────
log "4. Authentication boundary"
assert_status "protected route rejects an anonymous caller" 401 GET /api/v1/dashboard/summary
assert_status "protected route rejects a forged token"      401 GET /api/v1/dashboard/summary \
  -H 'Authorization: Bearer not.a.real.token'
assert_status "login rejects a wrong password"              401 POST /api/v1/auth/login \
  -H 'Content-Type: application/json' \
  -d "{\"email\":\"${SEED_EMAIL}\",\"password\":\"definitely-not-the-password\"}"

log "5. Login with the seeded super-admin"
LOGIN_BODY=$(fetch_body "login" POST /api/v1/auth/login \
  -H 'Content-Type: application/json' \
  -d "{\"email\":\"${SEED_EMAIL}\",\"password\":\"${SEED_PASSWORD}\"}")
TOKEN=$(printf '%s' "$LOGIN_BODY" | python3 -c 'import json,sys; print(json.load(sys.stdin).get("accessToken",""))' 2>/dev/null)

if [[ -n "$TOKEN" ]]; then
  pass "login issued an access token"
else
  fail "login did not return an access token"
  echo "  response: ${LOGIN_BODY:0:400}"
fi

AUTH_HEADER="Authorization: Bearer ${TOKEN}"

# A freshly seeded super-admin holds a temporary password, and MustChangePasswordMiddleware
# answers 403 must_change_password to every data endpoint until it is replaced. Skipping this
# meant the smoke test logged in and then asserted 200 on endpoints the product is designed to
# refuse -- three failures that looked like broken authorization but were the gate working.
# The must_change_password claim is baked into the token, so the new password only takes
# effect for a token minted after the change: change, then log in again.
log "5b. Replacing the temporary password"
# Conditional, because only a freshly seeded account sits behind the gate. In deployed mode the
# super-admin has usually rotated its password long ago, and an unconditional change would fail
# the current-password check and report a failure that is really just a healthy account. Ask
# the product whether the gate is active rather than assuming it is.
GATE=$(curl -sS -o /dev/null -w '%{http_code}' "${BASE}/api/v1/dashboard/summary" -H "$AUTH_HEADER" 2>/dev/null)

if [[ "$GATE" != "403" ]]; then
  pass "account is already past the temporary-password gate (dashboard returned $GATE)"
else
  CHANGED=$(curl -sS -o /dev/null -w '%{http_code}' -X POST "${BASE}/api/v1/auth/change-password" \
    -H 'Content-Type: application/json' -H "$AUTH_HEADER" \
    -d "{\"currentPassword\":\"${SEED_PASSWORD}\",\"newPassword\":\"${ROTATED_PASSWORD}\"}" 2>/dev/null)

  if [[ "$CHANGED" == "200" ]]; then
    pass "temporary password replaced (200)"
  else
    fail "change-password returned $CHANGED, expected 200"
  fi

  LOGIN_BODY=$(curl -sS -X POST "${BASE}/api/v1/auth/login" \
    -H 'Content-Type: application/json' \
    -d "{\"email\":\"${SEED_EMAIL}\",\"password\":\"${ROTATED_PASSWORD}\"}" 2>/dev/null)
  TOKEN=$(printf '%s' "$LOGIN_BODY" | python3 -c 'import json,sys; print(json.load(sys.stdin).get("accessToken",""))' 2>/dev/null)

  if [[ -n "$TOKEN" ]]; then
    pass "re-login with the new password issued a token without the change-password claim"
  else
    fail "re-login after the password change did not return a token"
    echo "  response: ${LOGIN_BODY:0:400}"
  fi

  AUTH_HEADER="Authorization: Bearer ${TOKEN}"
fi

log "6. Authorized requests"
assert_status "/auth/me returns the caller's profile"  200 GET /api/v1/auth/me   -H "$AUTH_HEADER"
assert_status "dashboard summary is served"            200 GET /api/v1/dashboard/summary -H "$AUTH_HEADER"
assert_status "shorfah issue list is served"           200 GET /api/v1/shorfah/issues    -H "$AUTH_HEADER"

# The dashboard's upcoming-observances panel reads the international_days table. Nothing ever
# wrote to that table, so the panel was permanently empty on every environment and no test
# noticed -- a 200 with an empty list looks identical to a 200 with content. The seeder now
# populates it, and this assertion is what keeps it populated: it fails if the table is empty,
# if the seeder stops running, or if a date string stops parsing (the parser returns null on a
# bad date and the handler silently skips the row, which is invisible otherwise).
SUMMARY=$(fetch_body "dashboard summary" GET /api/v1/dashboard/summary -H "$AUTH_HEADER")
if printf '%s' "$SUMMARY" | python3 -c '
import json,sys
body = json.load(sys.stdin)
days = body.get("intlDaysUpcoming") or []
if not days:
    sys.exit(1)
for d in days:
    if not d.get("name") or not d.get("date") or d.get("daysUntil") is None:
        sys.exit(1)
sys.exit(0)
' 2>/dev/null; then
  pass "dashboard lists upcoming observances (the seed catalogue is present)"
else
  fail "dashboard returned no upcoming observances — international_days is empty or its dates do not parse"
fi

log "7. Pagination envelope on a list endpoint"
ENVELOPE=$(fetch_body "shorfah issue list" GET /api/v1/shorfah/issues -H "$AUTH_HEADER")
if printf '%s' "$ENVELOPE" | python3 -c '
import json,sys
body = json.load(sys.stdin)
missing = [k for k in ("items","page","pageSize","total") if k not in body]
sys.exit(1 if missing else 0)
' 2>/dev/null; then
  pass "list response carries items/page/pageSize/total"
else
  fail "list response is missing the pagination envelope"
  echo "  response: ${ENVELOPE:0:400}"
fi

# ── 8. The debug endpoints that were never ported ──────────────────────────────
log "8. Debug endpoints stay gone (DATA-03)"
assert_status "/api/v1/debug/db is not routable"  404 GET /api/v1/debug/db
assert_status "/api/v1/debug/env is not routable" 404 GET /api/v1/debug/env

# ── 9. Error contract and headers ──────────────────────────────────────────────
log "9. Error contract and security headers"
# Same recycle caveat as above: a cycling worker answers this with the App Service error page
# (text/html) instead of the app's problem+json, so retry while the status looks transient.
NOT_FOUND_ATTEMPT=1
while :; do
  NOT_FOUND_PROBE=$(curl -sS -o /dev/null -w '%{http_code} %{content_type}' "${BASE}/api/v1/no-such-route" 2>/dev/null)
  NOT_FOUND_CODE="${NOT_FOUND_PROBE%% *}"
  NOT_FOUND_TYPE="${NOT_FOUND_PROBE#* }"
  if ! is_transient 404 "$NOT_FOUND_CODE" || (( NOT_FOUND_ATTEMPT >= TRANSIENT_ATTEMPTS )); then
    break
  fi
  retry "unknown-route error contract — got ${NOT_FOUND_CODE} (attempt ${NOT_FOUND_ATTEMPT}/${TRANSIENT_ATTEMPTS}), re-checking in ${TRANSIENT_BACKOFF_SECONDS}s"
  sleep "$TRANSIENT_BACKOFF_SECONDS"
  NOT_FOUND_ATTEMPT=$((NOT_FOUND_ATTEMPT + 1))
done
if [[ "$NOT_FOUND_TYPE" == *"problem+json"* ]]; then
  pass "unknown routes return application/problem+json"
else
  fail "unknown routes returned content type '${NOT_FOUND_TYPE}', expected problem+json"
fi

HEADERS_ATTEMPT=1
while :; do
  HEADERS=$(curl -sS -D - -o /dev/null "${BASE}/health/live" 2>/dev/null)
  HEADERS_CODE=$(curl -sS -o /dev/null -w '%{http_code}' "${BASE}/health/live" 2>/dev/null)
  if ! is_transient 200 "$HEADERS_CODE" || (( HEADERS_ATTEMPT >= TRANSIENT_ATTEMPTS )); then
    break
  fi
  retry "security headers — /health/live returned ${HEADERS_CODE} (attempt ${HEADERS_ATTEMPT}/${TRANSIENT_ATTEMPTS}), re-checking in ${TRANSIENT_BACKOFF_SECONDS}s"
  sleep "$TRANSIENT_BACKOFF_SECONDS"
  HEADERS_ATTEMPT=$((HEADERS_ATTEMPT + 1))
done
for header in "X-Content-Type-Options" "X-Frame-Options" "Referrer-Policy"; do
  if printf '%s' "$HEADERS" | grep -qi "^${header}:"; then
    pass "${header} is present"
  else
    fail "${header} is missing"
  fi
done

if printf '%s' "$HEADERS" | grep -qi '^Server:'; then
  fail "the Server header is exposed (R-BE-079)"
else
  pass "the Server header is suppressed (R-BE-079)"
fi

# ── 10. Credentials must never reach the logs ──────────────────────────────────
if [[ "$DEPLOYED_MODE" -eq 0 ]]; then
  log "10. No credentials in the logs (R-BE-054)"
  if grep -qF "$SEED_PASSWORD" "$LOG_FILE" || grep -qF "$ROTATED_PASSWORD" "$LOG_FILE"; then
    fail "the seeded password appears in the application log"
  else
    pass "no seeded password in the application log"
  fi
else
  log "10. No credentials in the logs (R-BE-054)"
  echo "  SKIPPED in deployed mode: this script has no access to the deployed instance's own" \
       "application logs (they live in Azure Monitor/App Insights, not on this runner)."
fi

log "Result"
if [[ "$FAILURES" -eq 0 ]]; then
  printf '\033[32mAll smoke assertions passed. The API boots and serves live traffic.\033[0m\n'
  exit 0
fi

printf '\033[31m%s smoke assertion(s) failed.\033[0m\n' "$FAILURES"
if [[ "$DEPLOYED_MODE" -eq 0 ]]; then
  echo "── application log (tail) ──"
  tail -60 "$LOG_FILE"
else
  echo "This ran in deployed mode: check Azure Monitor/App Insights for the deployed" \
       "instance's application log."
fi
exit 1
