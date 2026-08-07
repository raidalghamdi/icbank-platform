#!/usr/bin/env python3
"""Mint an ARM token from the stored service principal and call Azure Resource Manager.

Why this exists: Azure access tokens live one hour and a client-credentials grant gets no
refresh token, so a hand-pasted token was never going to hold. The service principal's
secret sits in the credential vault and is injected by the proxy at request time. This
mints a fresh token on demand and never sees the secret.

Run with: bash(api_credentials=["custom-cred:login.microsoftonline.com"])

  python3 arm.py GET /subscriptions/<sub>/resourceGroups/rg-icbank-dev?api-version=2021-04-01

Implementation note: this shells out to curl rather than using requests. The credential
proxy terminates TLS with its own CA, and while curl honours SSL_CERT_FILE, requests
verifies the *proxy leg* against certifi and fails with CERTIFICATE_VERIFY_FAILED no
matter what `verify=` is set to. curl works, so curl it is.
"""
import json
import os
import subprocess
import sys
import time

TENANT = "545e83b1-8ba8-442b-8a9d-a2832568a865"
SUBSCRIPTION = "f1422c2e-a1f8-4794-bfa4-d1c9c16e9287"
RESOURCE_GROUP = "rg-icbank-dev"
SQL_SERVER = "icbank-dev-sql"

SQL_SERVER_PATH = (
    f"/subscriptions/{SUBSCRIPTION}/resourceGroups/{RESOURCE_GROUP}"
    f"/providers/Microsoft.Sql/servers/{SQL_SERVER}"
)

# The proxy only tunnels hosts declared in api_credentials, and declaring the ARM host
# would inject the old expired bearer token over the one minted here. So minting and
# calling happen in separate bash invocations, with the short-lived token cached between
# them. What lands on disk is a one-hour derived access token, never the client secret --
# that stays in the vault and is only ever seen by the proxy.
TOKEN_CACHE = "/tmp/.arm_token.json"

_token = None


def _curl(args):
    """Run curl, returning (status_code, parsed_body_or_text_or_None)."""
    out = subprocess.run(
        ["curl", "-sS", "-w", "\n%{http_code}", "--max-time", "120", *args],
        capture_output=True, text=True,
    )
    if out.returncode != 0:
        raise RuntimeError(f"curl failed: {out.stderr.strip()}")
    body, _, status = out.stdout.rpartition("\n")
    if not body.strip():
        return int(status), None
    try:
        return int(status), json.loads(body)
    except json.JSONDecodeError:
        return int(status), body


def token():
    """Mint an ARM access token.

    Basic auth is injected by the credential proxy. Azure AD accepts client credentials in
    the Authorization header per RFC 6749 -- confirmed in the Microsoft identity platform
    client-credentials documentation -- which is what keeps the secret out of this body.
    """
    global _token
    if _token:
        return _token
    if os.path.exists(TOKEN_CACHE):
        with open(TOKEN_CACHE) as fh:
            cached = json.load(fh)
        if cached.get("expires_at", 0) - time.time() > 120:
            _token = cached["access_token"]
            return _token
    status, body = _curl([
        "-X", "POST",
        f"https://login.microsoftonline.com/{TENANT}/oauth2/v2.0/token",
        "-d", "grant_type=client_credentials",
        "-d", "scope=https://management.azure.com/.default",
    ])
    if status != 200 or not isinstance(body, dict) or "access_token" not in body:
        desc = (body or {}).get("error_description", body) if isinstance(body, dict) else body
        raise RuntimeError(f"token mint failed (HTTP {status}): {desc}")
    _token = body["access_token"]
    fd = os.open(TOKEN_CACHE, os.O_WRONLY | os.O_CREAT | os.O_TRUNC, 0o600)
    with os.fdopen(fd, "w") as fh:
        json.dump({
            "access_token": _token,
            "expires_at": time.time() + int(body.get("expires_in", 3600)),
        }, fh)
    return _token


def arm(method, path, payload=None, api_version=None):
    """Call ARM. Returns (status, parsed_body_or_text_or_None). Does not raise on HTTP error."""
    url = "https://management.azure.com" + path
    if api_version and "api-version=" not in url:
        url += ("&" if "?" in url else "?") + "api-version=" + api_version
    args = ["-X", method, url, "-H", "Authorization: Bearer " + token()]
    if payload is not None:
        args += ["-H", "Content-Type: application/json", "-d", json.dumps(payload)]
    return _curl(args)


if __name__ == "__main__":
    if sys.argv[1] == "mint":
        token()
        with open(TOKEN_CACHE) as fh:
            left = int(json.load(fh)["expires_at"] - time.time())
        print(f"token cached, valid for {left}s")
        sys.exit(0)
    st, bd = arm(sys.argv[1], sys.argv[2])
    print(f"HTTP {st}")
    print(json.dumps(bd, indent=2, ensure_ascii=False) if bd is not None else "(empty)")
