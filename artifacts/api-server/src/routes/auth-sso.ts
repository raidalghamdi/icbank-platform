import { Router, type Request, type Response } from "express";
import { randomBytes, createHash } from "crypto";
import jwt from "jsonwebtoken";
import { db } from "@workspace/db";
import { usersTable, activityLogsTable, userRolesTable, rolesTable } from "@workspace/db";
import { eq } from "drizzle-orm";
import { getSettings } from "../services/settings";
import { getUserPermissions } from "../middleware/auth";

const router = Router();

const REFRESH_TOKEN_EXPIRY_MS = 7 * 24 * 60 * 60 * 1000;
const SSO_STATE_TTL_MS = 10 * 60 * 1000;

interface PkceState {
  codeVerifier: string;
  expiresAt: number;
  redirectAfter: string;
}

const pendingStates = new Map<string, PkceState>();

setInterval(() => {
  const now = Date.now();
  for (const [key, val] of pendingStates.entries()) {
    if (val.expiresAt < now) pendingStates.delete(key);
  }
}, 60_000);

function getJwtSecret(): string {
  const secret = process.env["SESSION_SECRET"] || process.env["JWT_SECRET"];
  if (!secret) throw new Error("SESSION_SECRET is required");
  return secret;
}

function generateAccessToken(userId: number, email: string, name: string, expirySeconds = 3600): string {
  return jwt.sign({ sub: userId, email, name }, getJwtSecret(), { expiresIn: expirySeconds });
}

function generateRefreshToken(userId: number): string {
  return jwt.sign({ sub: userId, type: "refresh" }, getJwtSecret(), { expiresIn: "7d" });
}

function base64urlEncode(buf: Buffer): string {
  return buf.toString("base64").replace(/\+/g, "-").replace(/\//g, "_").replace(/=/g, "");
}

function generateCodeVerifier(): string {
  return base64urlEncode(randomBytes(32));
}

function generateCodeChallenge(verifier: string): string {
  const hash = createHash("sha256").update(verifier).digest();
  return base64urlEncode(hash);
}

async function logActivity(
  userId: number | null,
  action: string,
  details: Record<string, unknown>,
  req: Request,
) {
  try {
    await db.insert(activityLogsTable).values({
      userId,
      action,
      details,
      ipAddress: (req.headers["x-forwarded-for"] as string) || req.ip || null,
      userAgent: req.headers["user-agent"] || null,
    });
  } catch {
    // non-critical
  }
}

function getBaseUrl(req: Request): string {
  const domains = process.env["REPLIT_DOMAINS"];
  if (domains) {
    const primary = domains.split(",")[0]!.trim();
    return `https://${primary}`;
  }
  const host = req.headers["host"] || "localhost";
  const proto = req.headers["x-forwarded-proto"] || "http";
  return `${proto}://${host}`;
}

router.get("/auth/sso/config", async (_req: Request, res: Response) => {
  const settings = await getSettings();
  const enabled = settings["azure_ad_enabled"] === "true";
  const domain = settings["azure_ad_domain"] || "";
  res.json({ enabled, domain });
});

router.get("/auth/sso/azure/start", async (req: Request, res: Response) => {
  const settings = await getSettings();

  if (settings["azure_ad_enabled"] !== "true") {
    res.status(400).json({ error: "تسجيل الدخول بحساب المؤسسة غير مفعّل" });
    return;
  }

  const tenantId = settings["azure_ad_tenant_id"];
  const clientId = settings["azure_ad_client_id"];

  if (!tenantId || !clientId) {
    res.status(500).json({ error: "إعدادات Azure AD غير مكتملة. تواصل مع المدير." });
    return;
  }

  const codeVerifier = generateCodeVerifier();
  const codeChallenge = generateCodeChallenge(codeVerifier);
  const state = base64urlEncode(randomBytes(16));
  const redirectAfter = (req.query["redirect"] as string | undefined) || "/";

  pendingStates.set(state, {
    codeVerifier,
    expiresAt: Date.now() + SSO_STATE_TTL_MS,
    redirectAfter,
  });

  const baseUrl = getBaseUrl(req);
  const redirectUri = `${baseUrl}/api/auth/sso/azure/callback`;

  const params = new URLSearchParams({
    client_id: clientId,
    response_type: "code",
    redirect_uri: redirectUri,
    response_mode: "query",
    scope: "openid profile email",
    state,
    code_challenge: codeChallenge,
    code_challenge_method: "S256",
  });

  const authUrl = `https://login.microsoftonline.com/${tenantId}/oauth2/v2.0/authorize?${params.toString()}`;
  res.redirect(authUrl);
});

router.get("/auth/sso/azure/callback", async (req: Request, res: Response) => {
  const { code, state, error: oauthError, error_description } = req.query as Record<string, string>;

  const frontendBase = "/";

  if (oauthError) {
    await logActivity(null, "sso_failed", { error: oauthError, description: error_description }, req);
    res.redirect(`/login.html?sso_error=${encodeURIComponent(error_description || oauthError)}`);
    return;
  }

  if (!code || !state) {
    res.redirect("/login.html?sso_error=" + encodeURIComponent("استجابة غير صالحة من مزود الهوية"));
    return;
  }

  const pendingState = pendingStates.get(state);
  if (!pendingState || pendingState.expiresAt < Date.now()) {
    pendingStates.delete(state);
    res.redirect("/login.html?sso_error=" + encodeURIComponent("انتهت صلاحية طلب تسجيل الدخول. حاول مجدداً."));
    return;
  }

  pendingStates.delete(state);

  const settings = await getSettings();
  const tenantId = settings["azure_ad_tenant_id"];
  const clientId = settings["azure_ad_client_id"];
  const clientSecret = settings["azure_ad_client_secret"];
  const allowedDomain = settings["azure_ad_domain"] || "";

  if (!tenantId || !clientId) {
    res.redirect("/login.html?sso_error=" + encodeURIComponent("إعدادات Azure AD غير مكتملة"));
    return;
  }

  const baseUrl = getBaseUrl(req);
  const redirectUri = `${baseUrl}/api/auth/sso/azure/callback`;

  try {
    const tokenBody = new URLSearchParams({
      client_id: clientId,
      grant_type: "authorization_code",
      code,
      redirect_uri: redirectUri,
      code_verifier: pendingState.codeVerifier,
    });

    if (clientSecret) {
      tokenBody.set("client_secret", clientSecret);
    }

    const tokenResp = await fetch(
      `https://login.microsoftonline.com/${tenantId}/oauth2/v2.0/token`,
      {
        method: "POST",
        headers: { "Content-Type": "application/x-www-form-urlencoded" },
        body: tokenBody.toString(),
      },
    );

    if (!tokenResp.ok) {
      const errBody = await tokenResp.text();
      await logActivity(null, "sso_token_exchange_failed", { status: tokenResp.status, body: errBody }, req);
      res.redirect("/login.html?sso_error=" + encodeURIComponent("فشل في التحقق من الهوية. حاول مجدداً."));
      return;
    }

    const tokenData = (await tokenResp.json()) as {
      id_token?: string;
      access_token?: string;
    };

    if (!tokenData.id_token) {
      res.redirect("/login.html?sso_error=" + encodeURIComponent("لم يتم إرجاع رمز الهوية من Azure AD"));
      return;
    }

    const idTokenPayload = JSON.parse(
      Buffer.from(tokenData.id_token.split(".")[1]!, "base64url").toString("utf-8"),
    ) as {
      oid?: string;
      sub?: string;
      email?: string;
      preferred_username?: string;
      name?: string;
      given_name?: string;
      family_name?: string;
    };

    const oid = idTokenPayload.oid || idTokenPayload.sub;
    const email = (idTokenPayload.email || idTokenPayload.preferred_username || "").toLowerCase().trim();
    const displayName = idTokenPayload.name ||
      [idTokenPayload.given_name, idTokenPayload.family_name].filter(Boolean).join(" ") ||
      email.split("@")[0] ||
      "مستخدم";

    if (!email || !oid) {
      await logActivity(null, "sso_missing_claims", { idTokenPayload }, req);
      res.redirect("/login.html?sso_error=" + encodeURIComponent("لم يتم الحصول على بيانات البريد الإلكتروني من حساب المؤسسة"));
      return;
    }

    if (allowedDomain && !email.endsWith(`@${allowedDomain}`)) {
      await logActivity(null, "sso_domain_rejected", { email, allowedDomain }, req);
      res.redirect("/login.html?sso_error=" + encodeURIComponent(`البريد الإلكتروني يجب أن ينتمي إلى نطاق ${allowedDomain}`));
      return;
    }

    let user = (
      await db.select().from(usersTable).where(eq(usersTable.azureOid, oid)).limit(1)
    )[0];

    if (!user) {
      user = (
        await db.select().from(usersTable).where(eq(usersTable.email, email)).limit(1)
      )[0];

      if (user) {
        await db.update(usersTable)
          .set({ azureOid: oid, updatedAt: new Date() })
          .where(eq(usersTable.id, user.id));
        user = { ...user, azureOid: oid };
      }
    }

    if (!user) {
      const defaultRole = await db
        .select()
        .from(rolesTable)
        .where(eq(rolesTable.name, "viewer"))
        .limit(1);

      const [newUser] = await db.insert(usersTable).values({
        email,
        name: displayName,
        azureOid: oid,
        isActive: true,
        failedAttempts: 0,
      }).returning();

      user = newUser!;

      if (defaultRole[0]) {
        await db.insert(userRolesTable).values({
          userId: user.id,
          roleId: defaultRole[0].id,
          assignedBy: null,
        });
      }

      await logActivity(user.id, "sso_user_created", { email, oid }, req);
    }

    if (!user.isActive) {
      await logActivity(user.id, "sso_login_failed", { reason: "inactive_account" }, req);
      res.redirect("/login.html?sso_error=" + encodeURIComponent("الحساب غير مفعّل. تواصل مع المدير."));
      return;
    }

    if (user.isLocked) {
      await logActivity(user.id, "sso_login_failed", { reason: "locked_account" }, req);
      res.redirect("/login.html?sso_error=" + encodeURIComponent("الحساب مقفل. تواصل مع المدير."));
      return;
    }

    await db.update(usersTable)
      .set({ lastLogin: new Date(), failedAttempts: 0, updatedAt: new Date() })
      .where(eq(usersTable.id, user.id));

    const sessionSettings = await getSettings();
    const sessionMins = Math.max(5, parseInt(sessionSettings["session_duration_minutes"] || "60"));
    const accessToken = generateAccessToken(user.id, user.email, user.name, sessionMins * 60);
    const refreshToken = generateRefreshToken(user.id);

    const { role } = await getUserPermissions(user.id);
    await logActivity(user.id, "sso_login_success", { email, role }, req);

    res.cookie("refresh_token", refreshToken, {
      httpOnly: true,
      secure: process.env["NODE_ENV"] === "production",
      sameSite: "lax",
      path: "/api/auth",
      maxAge: REFRESH_TOKEN_EXPIRY_MS,
    });

    const encodedToken = encodeURIComponent(accessToken);
    const userName = encodeURIComponent(
      JSON.stringify({ id: user.id, email: user.email, name: user.name, role }),
    );

    const redirectTo = pendingState.redirectAfter || "/";

    res.send(`<!DOCTYPE html><html><head><meta charset="UTF-8"></head><body>
<script>
(function() {
  var token = decodeURIComponent("${encodedToken}");
  var user = JSON.parse(decodeURIComponent("${userName}"));
  localStorage.setItem('access_token', token);
  localStorage.setItem('user_info', JSON.stringify(user));
  document.cookie = 'access_token=' + encodeURIComponent(token) + '; path=/; SameSite=Strict; max-age=900';
  document.cookie = 'has_session=1; path=/; SameSite=Strict; max-age=604800';
  window.location.href = ${JSON.stringify(redirectTo)};
})();
</script>
</body></html>`);
  } catch (err) {
    await logActivity(null, "sso_error", { error: String(err) }, req);
    res.redirect("/login.html?sso_error=" + encodeURIComponent("حدث خطأ أثناء تسجيل الدخول. حاول مجدداً."));
  }
});

export default router;
