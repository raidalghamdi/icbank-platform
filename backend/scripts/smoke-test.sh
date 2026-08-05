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
# Requires: ICBANK_SMOKE_CONNECTION pointing at a reachable SQL Server.
set -uo pipefail

BACKEND_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
cd "$BACKEND_DIR"

PORT="${ICBANK_SMOKE_PORT:-5080}"
BASE="http://127.0.0.1:${PORT}"
LOG_FILE="$(mktemp)"
SEED_EMAIL="smoke-superadmin@icbank.local"
SEED_PASSWORD='Sm0ke!Test-Password_2026'
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

if [[ -z "${ICBANK_SMOKE_CONNECTION:-}" ]]; then
  echo "ICBANK_SMOKE_CONNECTION is not set. It must point at a reachable SQL Server." >&2
  exit 1
fi

# ── Asserts an endpoint returns an expected status code ─────────────────────────
# usage: assert_status <description> <expected> <method> <path> [curl args...]
assert_status() {
  local description="$1" expected="$2" method="$3" path="$4"
  shift 4
  local actual
  actual=$(curl -sS -o /dev/null -w '%{http_code}' -X "$method" "${BASE}${path}" "$@" 2>/dev/null)
  if [[ "$actual" == "$expected" ]]; then
    pass "$description (${actual})"
  else
    fail "$description — expected ${expected}, got ${actual}"
  fi
}

# ── 1. Migrations ──────────────────────────────────────────────────────────────
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

# ── 2. Boot ────────────────────────────────────────────────────────────────────
# Staging rather than Development: Development would enable Swagger and skip HSTS,
# so it would not exercise the pipeline the platform actually deploys.
#
# --no-launch-profile matters. Without it dotnet run applies
# src/Icbank.Platform.Api/Properties/launchSettings.json, whose applicationUrl overrides
# ASPNETCORE_URLS - the app boots and seeds successfully but listens on a different
# address, so every assertion below fails against a port nothing is bound to.
# launchSettings is a local-development convenience and has no business in CI.
log "2. Starting the API as a real process"
ASPNETCORE_ENVIRONMENT=Staging \
ASPNETCORE_URLS="$BASE" \
ConnectionStrings__Default="$ICBANK_SMOKE_CONNECTION" \
Jwt__SigningKey='smoke-test-signing-key-not-for-production-use-32bytes' \
Jwt__Issuer='icbank-platform' \
Jwt__Audience='icbank-platform-clients' \
Cors__AllowedOrigins__0='http://127.0.0.1:3000' \
Cron__ApiKey='smoke-cron-key' \
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
LOGIN_BODY=$(curl -sS -X POST "${BASE}/api/v1/auth/login" \
  -H 'Content-Type: application/json' \
  -d "{\"email\":\"${SEED_EMAIL}\",\"password\":\"${SEED_PASSWORD}\"}" 2>/dev/null)
TOKEN=$(printf '%s' "$LOGIN_BODY" | python3 -c 'import json,sys; print(json.load(sys.stdin).get("accessToken",""))' 2>/dev/null)

if [[ -n "$TOKEN" ]]; then
  pass "login issued an access token"
else
  fail "login did not return an access token"
  echo "  response: ${LOGIN_BODY:0:400}"
fi

AUTH_HEADER="Authorization: Bearer ${TOKEN}"

# ── 6. Authorized traffic ──────────────────────────────────────────────────────
log "6. Authorized requests"
assert_status "/auth/me returns the caller's profile"  200 GET /api/v1/auth/me   -H "$AUTH_HEADER"
assert_status "dashboard summary is served"            200 GET /api/v1/dashboard/summary -H "$AUTH_HEADER"
assert_status "shorfah issue list is served"           200 GET /api/v1/shorfah/issues    -H "$AUTH_HEADER"

log "7. Pagination envelope on a list endpoint"
ENVELOPE=$(curl -sS "${BASE}/api/v1/shorfah/issues" -H "$AUTH_HEADER" 2>/dev/null)
if printf '%s' "$ENVELOPE" | python3 -c '
import json,sys
body = json.load(sys.stdin)
missing = [k for k in ("items","page","pageSize","totalCount") if k not in body]
sys.exit(1 if missing else 0)
' 2>/dev/null; then
  pass "list response carries items/page/pageSize/totalCount"
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
NOT_FOUND_TYPE=$(curl -sS -o /dev/null -w '%{content_type}' "${BASE}/api/v1/no-such-route" 2>/dev/null)
if [[ "$NOT_FOUND_TYPE" == *"problem+json"* ]]; then
  pass "unknown routes return application/problem+json"
else
  fail "unknown routes returned content type '${NOT_FOUND_TYPE}', expected problem+json"
fi

HEADERS=$(curl -sS -D - -o /dev/null "${BASE}/health/live" 2>/dev/null)
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
log "10. No credentials in the logs (R-BE-054)"
if grep -qF "$SEED_PASSWORD" "$LOG_FILE"; then
  fail "the seeded password appears in the application log"
else
  pass "no seeded password in the application log"
fi

log "Result"
if [[ "$FAILURES" -eq 0 ]]; then
  printf '\033[32mAll smoke assertions passed. The API boots and serves live traffic.\033[0m\n'
  exit 0
fi

printf '\033[31m%s smoke assertion(s) failed.\033[0m\n' "$FAILURES"
echo "── application log (tail) ──"
tail -60 "$LOG_FILE"
exit 1
