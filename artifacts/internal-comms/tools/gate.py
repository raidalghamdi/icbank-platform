"""Every asset the signed-out login page needs must be reachable while signed out.

This runs against the real server.mjs, not proxy.mjs. The Playwright harness talks to the
proxy, which has no auth gate, so it cannot see this class of bug at all: the deployed site
answered 302 -> /login for /css/dga.css and both .woff2 files, and the sign-in screen rendered
in a system fallback face while every harness check still passed.
"""
import os
import re
import shutil
import subprocess
import sys
import time
import urllib.request

ROOT = "/home/user/workspace/repo/artifacts/internal-comms"
PORT = 3399
BASE = f"http://127.0.0.1:{PORT}"

failures = []


def check(name, ok, detail=""):
    print(f"  {'ok  ' if ok else 'FAIL'}  {name}{'' if ok else f'  <- {detail}'}")
    if not ok:
        failures.append(name)


def fetch(path):
    """Return (status, content_type, body_len) without following redirects."""
    req = urllib.request.Request(BASE + path, headers={"User-Agent": "gate"})
    opener = urllib.request.build_opener(NoRedirect)
    try:
        r = opener.open(req, timeout=20)
        return r.status, r.headers.get("Content-Type", ""), len(r.read())
    except urllib.error.HTTPError as e:
        return e.code, e.headers.get("Location", ""), 0


class NoRedirect(urllib.request.HTTPRedirectHandler):
    def redirect_request(self, *_a, **_k):
        return None


srv = subprocess.Popen(
    [shutil.which("node") or "node", "server.mjs"], cwd=ROOT,
    env={**os.environ, "PORT": str(PORT)},
    stdout=subprocess.DEVNULL, stderr=subprocess.PIPE,
)
try:
    for _ in range(40):
        try:
            urllib.request.urlopen(BASE + "/login", timeout=2).read()
            break
        except Exception:
            time.sleep(0.25)
    else:
        print("server did not start"); sys.exit(1)

    html = urllib.request.urlopen(BASE + "/login", timeout=20).read().decode("utf-8", "replace")

    # Everything login.html pulls by absolute path, minus the API (which is proxied elsewhere).
    assets = sorted({
        m for m in re.findall(r'(?:href|src)="(/[^"]+)"', html)
        if not m.startswith("/api/")
    })
    check("login.html references assets", len(assets) >= 3, assets)

    for path in assets:
        status, extra, size = fetch(path)
        check(f"signed-out GET {path} -> 200", status == 200, f"{status} {extra}")

    # The fonts specifically: a 200 that is secretly an HTML error page is the failure mode
    # that shipped a corrupt woff2 before (c2cb260), so assert the payload really is a font.
    for weight in ("Roman", "Bold"):
        path = f"/fonts/frutiger/FrutigerLTArabic-{weight}.woff2"
        status, ctype, size = fetch(path)
        check(f"{weight} served as a real font", status == 200 and size > 20000,
              f"status={status} size={size}")
        if status == 200:
            body = urllib.request.urlopen(BASE + path, timeout=20).read()
            check(f"{weight} has wOF2 signature", body[:4] == b"wOF2", body[:4])

    # And the gate still has to actually gate: the app shell must not be public.
    status, loc, _ = fetch("/index.html")
    check("signed-out /index.html still redirects", status == 302, f"got {status}")

    # A wrong content type is invisible to a status-code check but fatal in a browser: an
    # unmapped extension fell through to application/octet-stream, so following a direct link
    # to /index.html downloaded the page instead of rendering it.
    for path_, expect in (("/index.html", "text/html"), ("/login.html", "text/html"),
                          ("/css/dga.css", "text/css"),
                          ("/fonts/frutiger/FrutigerLTArabic-Roman.woff2", "font/woff2")):
        req = urllib.request.Request(BASE + path_, headers={"Cookie": "has_session=1"})
        got = urllib.request.urlopen(req, timeout=20).headers.get("Content-Type", "")
        check(f"{path_} is {expect}", got.startswith(expect), got)
finally:
    srv.terminate()
    srv.wait(timeout=10)

print(f"\nTOTAL {len(failures)} FAILED")
sys.exit(1 if failures else 0)
