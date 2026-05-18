import { Router, type IRouter, type Request, type Response } from "express";
import jwt from "jsonwebtoken";
import bcryptjs from "bcryptjs";
import rateLimit from "express-rate-limit";
import { db } from "@workspace/db";
import {
  usersTable,
  activityLogsTable,
} from "@workspace/db";
import { eq } from "drizzle-orm";
import { authenticate, requireAuth, getUserPermissions } from "../middleware/auth";
import { getSettings } from "../services/settings";

const router: IRouter = Router();

const REFRESH_TOKEN_DAYS = 7;
const REFRESH_TOKEN_EXPIRY_MS = REFRESH_TOKEN_DAYS * 24 * 60 * 60 * 1000;

function getJwtSecret(): string {
  const secret = process.env["SESSION_SECRET"] || process.env["JWT_SECRET"];
  if (!secret) throw new Error("SESSION_SECRET is required");
  return secret;
}

function generateAccessToken(userId: number, email: string, name: string, expiry = "60m"): string {
  // eslint-disable-next-line @typescript-eslint/no-explicit-any
  return jwt.sign({ sub: userId, email, name }, getJwtSecret(), {
    expiresIn: expiry as any,
  });
}

function generateRefreshToken(userId: number): string {
  return jwt.sign({ sub: userId, type: "refresh" }, getJwtSecret(), {
    expiresIn: "7d",
  });
}

const loginLimiter = rateLimit({
  windowMs: 60 * 1000,
  max: 5,
  message: { error: "محاولات كثيرة. انتظر دقيقة وحاول مجدداً.", code: "RATE_LIMITED" },
  standardHeaders: true,
  legacyHeaders: false,
});

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

router.post("/auth/login", loginLimiter, async (req: Request, res: Response) => {
  const { email, password, rememberMe } = req.body as {
    email?: string;
    password?: string;
    rememberMe?: boolean;
  };

  if (!email || !password) {
    res.status(400).json({ error: "البريد الإلكتروني وكلمة المرور مطلوبان", code: "MISSING_FIELDS" });
    return;
  }

  const users = await db
    .select()
    .from(usersTable)
    .where(eq(usersTable.email, email.toLowerCase().trim()))
    .limit(1);

  const user = users[0];

  if (!user) {
    await logActivity(null, "login_failed", { email, reason: "user_not_found" }, req);
    res.status(401).json({ error: "البريد الإلكتروني أو كلمة المرور غير صحيحة", code: "INVALID_CREDENTIALS" });
    return;
  }

  if (!user.isActive) {
    await logActivity(user.id, "login_failed", { reason: "inactive_account" }, req);
    res.status(403).json({ error: "الحساب غير مفعل. تواصل مع المدير.", code: "INACTIVE_ACCOUNT" });
    return;
  }

  if (user.isLocked) {
    await logActivity(user.id, "login_failed", { reason: "locked_account" }, req);
    res.status(403).json({ error: "الحساب مقفل بسبب محاولات فاشلة متعددة. تواصل مع المدير.", code: "LOCKED_ACCOUNT" });
    return;
  }

  const passwordMatch = await bcryptjs.compare(password, user.passwordHash);

  if (!passwordMatch) {
    const newFailedAttempts = user.failedAttempts + 1;
    const shouldLock = newFailedAttempts >= 5;

    await db
      .update(usersTable)
      .set({
        failedAttempts: newFailedAttempts,
        isLocked: shouldLock,
        updatedAt: new Date(),
      })
      .where(eq(usersTable.id, user.id));

    await logActivity(user.id, "login_failed", { reason: "wrong_password", attempt: newFailedAttempts, locked: shouldLock }, req);

    if (shouldLock) {
      res.status(403).json({ error: "تم قفل حسابك بعد 5 محاولات فاشلة. تواصل مع المدير.", code: "LOCKED_ACCOUNT" });
    } else {
      res.status(401).json({
        error: `كلمة المرور غير صحيحة. ${5 - newFailedAttempts} محاولات متبقية.`,
        code: "INVALID_CREDENTIALS",
      });
    }
    return;
  }

  await db
    .update(usersTable)
    .set({ failedAttempts: 0, lastLogin: new Date(), updatedAt: new Date() })
    .where(eq(usersTable.id, user.id));

  const settings = await getSettings();
  const sessionMins = Math.max(5, parseInt(settings["session_duration_minutes"] || "60"));
  const accessToken = generateAccessToken(user.id, user.email, user.name, `${sessionMins}m`);
  const refreshToken = generateRefreshToken(user.id);

  // Refresh token is always 7 days.
  // session_duration_minutes controls the access token lifetime.
  res.cookie("refresh_token", refreshToken, {
    httpOnly: true,
    secure: process.env["NODE_ENV"] === "production",
    sameSite: "lax",
    path: "/api/auth",
    maxAge: REFRESH_TOKEN_EXPIRY_MS,
  });

  const { permissions, role } = await getUserPermissions(user.id);

  await logActivity(user.id, "login_success", { role }, req);

  res.json({
    accessToken,
    user: {
      id: user.id,
      email: user.email,
      name: user.name,
      title: user.title,
      department: user.department,
      role,
      permissions,
    },
  });
});

router.post("/auth/logout", authenticate, async (req: Request, res: Response) => {
  if (req.user) {
    await logActivity(req.user.id, "logout", {}, req);
  }
  res.clearCookie("refresh_token", { path: "/api/auth" });
  res.clearCookie("access_token");
  res.json({ success: true });
});

router.post("/auth/refresh", async (req: Request, res: Response) => {
  const refreshToken = req.cookies?.["refresh_token"] as string | undefined;
  if (!refreshToken) {
    res.status(401).json({ error: "لا يوجد رمز تحديث", code: "NO_REFRESH_TOKEN" });
    return;
  }

  try {
    const payload = jwt.verify(refreshToken, getJwtSecret()) as unknown as {
      sub: number;
      type: string;
    };
    if (payload.type !== "refresh") {
      res.status(401).json({ error: "رمز تحديث غير صالح", code: "INVALID_TOKEN" });
      return;
    }

    const users = await db
      .select()
      .from(usersTable)
      .where(eq(usersTable.id, payload.sub))
      .limit(1);

    const user = users[0];
    if (!user || !user.isActive || user.isLocked) {
      res.status(401).json({ error: "المستخدم غير موجود أو محظور", code: "USER_UNAVAILABLE" });
      return;
    }

    const accessToken = generateAccessToken(user.id, user.email, user.name);
    const { permissions, role } = await getUserPermissions(user.id);

    res.json({
      accessToken,
      user: {
        id: user.id,
        email: user.email,
        name: user.name,
        title: user.title,
        department: user.department,
        role,
        permissions,
      },
    });
  } catch {
    res.clearCookie("refresh_token", { path: "/api/auth" });
    res.status(401).json({ error: "رمز التحديث منتهي أو غير صالح", code: "INVALID_TOKEN" });
  }
});

router.get("/auth/me", authenticate, requireAuth, async (req: Request, res: Response) => {
  const user = req.user!;
  res.json({
    id: user.id,
    email: user.email,
    name: user.name,
    role: user.role,
    isSuperAdmin: user.isSuperAdmin,
    permissions: user.permissions,
  });
});

export default router;
