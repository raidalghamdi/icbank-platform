import { Request, Response, NextFunction } from "express";
import jwt from "jsonwebtoken";
import { db } from "@workspace/db";
import {
  usersTable,
  userRolesTable,
  rolesTable,
  rolePermissionsTable,
  pagesTable,
  permissionsTable,
  userPageOverridesTable,
} from "@workspace/db";
import { eq, and } from "drizzle-orm";

export interface AuthUser {
  id: number;
  email: string;
  name: string;
  role: string;
  roleId: number;
  isSuperAdmin: boolean;
  permissions: Record<string, string[]>;
}

declare global {
  namespace Express {
    interface Request {
      user?: AuthUser | null;
    }
  }
}

function getJwtSecret(): string {
  const secret = process.env["SESSION_SECRET"] || process.env["JWT_SECRET"];
  if (!secret) throw new Error("SESSION_SECRET is required");
  return secret;
}

export function extractToken(req: Request): string | null {
  const authHeader = req.headers["authorization"];
  if (authHeader?.startsWith("Bearer ")) {
    return authHeader.slice(7);
  }
  const cookieToken = req.cookies?.["access_token"] as string | undefined;
  if (cookieToken) return cookieToken;
  return null;
}

export async function getUserPermissions(userId: number): Promise<{ role: string; roleId: number; permissions: Record<string, string[]> }> {
  const userRoleRows = await db
    .select({ roleId: userRolesTable.roleId, roleName: rolesTable.name })
    .from(userRolesTable)
    .innerJoin(rolesTable, eq(userRolesTable.roleId, rolesTable.id))
    .where(eq(userRolesTable.userId, userId))
    .limit(1);

  if (userRoleRows.length === 0) {
    return { role: "guest", roleId: 0, permissions: {} };
  }

  const { roleId, roleName } = userRoleRows[0]!;

  const rpRows = await db
    .select({
      pageSlug: pagesTable.slug,
      permName: permissionsTable.name,
    })
    .from(rolePermissionsTable)
    .innerJoin(pagesTable, eq(rolePermissionsTable.pageId, pagesTable.id))
    .innerJoin(permissionsTable, eq(rolePermissionsTable.permissionId, permissionsTable.id))
    .where(eq(rolePermissionsTable.roleId, roleId));

  const permissions: Record<string, string[]> = {};
  for (const row of rpRows) {
    if (!permissions[row.pageSlug]) permissions[row.pageSlug] = [];
    permissions[row.pageSlug]!.push(row.permName);
  }

  const overrides = await db
    .select({
      pageSlug: pagesTable.slug,
      permName: permissionsTable.name,
      grantType: userPageOverridesTable.grantType,
    })
    .from(userPageOverridesTable)
    .innerJoin(pagesTable, eq(userPageOverridesTable.pageId, pagesTable.id))
    .innerJoin(permissionsTable, eq(userPageOverridesTable.permissionId, permissionsTable.id))
    .where(eq(userPageOverridesTable.userId, userId));

  for (const ov of overrides) {
    if (ov.grantType === "allow") {
      if (!permissions[ov.pageSlug]) permissions[ov.pageSlug] = [];
      if (!permissions[ov.pageSlug]!.includes(ov.permName)) {
        permissions[ov.pageSlug]!.push(ov.permName);
      }
    } else if (ov.grantType === "deny") {
      if (permissions[ov.pageSlug]) {
        permissions[ov.pageSlug] = permissions[ov.pageSlug]!.filter(p => p !== ov.permName);
      }
    }
  }

  return { role: roleName, roleId, permissions };
}

export async function authenticate(req: Request, res: Response, next: NextFunction) {
  const token = extractToken(req);
  if (!token) {
    req.user = null;
    return next();
  }

  try {
    const payload = jwt.verify(token, getJwtSecret()) as unknown as { sub: number; email: string; name: string };
    const users = await db
      .select()
      .from(usersTable)
      .where(eq(usersTable.id, payload.sub))
      .limit(1);

    const user = users[0];
    if (!user || !user.isActive || user.isLocked) {
      req.user = null;
      return next();
    }

    const { role, roleId, permissions } = await getUserPermissions(user.id);
    req.user = {
      id: user.id,
      email: user.email,
      name: user.name,
      role,
      roleId,
      isSuperAdmin: role === "super_admin",
      permissions,
    };
    return next();
  } catch {
    req.user = null;
    return next();
  }
}

export function requireAuth(req: Request, res: Response, next: NextFunction) {
  if (!req.user) {
    res.status(401).json({ error: "غير مصرح", code: "UNAUTHORIZED" });
    return;
  }
  next();
}

export function requireAdmin(req: Request, res: Response, next: NextFunction) {
  if (!req.user) {
    res.status(401).json({ error: "غير مصرح", code: "UNAUTHORIZED" });
    return;
  }
  if (req.user.role !== "super_admin" && req.user.role !== "admin") {
    res.status(403).json({ error: "ليس لديك صلاحية", code: "FORBIDDEN" });
    return;
  }
  next();
}

export function requirePermission(pageSlug: string, permission: string) {
  return (req: Request, res: Response, next: NextFunction) => {
    if (!req.user) {
      res.status(401).json({ error: "غير مصرح", code: "UNAUTHORIZED" });
      return;
    }
    // super_admin and admin bypass all page-level permission checks
    if (req.user.isSuperAdmin || req.user.role === "admin") return next();
    const pagePerms = req.user.permissions[pageSlug] || [];
    if (!pagePerms.includes(permission)) {
      res.status(403).json({ error: "ليس لديك صلاحية للوصول لهذه الصفحة", code: "FORBIDDEN" });
      return;
    }
    next();
  };
}

/**
 * requirePageAccess(pageSlug) — method-aware permission middleware.
 *
 * Maps the HTTP method to the required permission automatically:
 *   GET / HEAD        → "view"
 *   POST              → "create"
 *   PUT / PATCH       → "edit"
 *   DELETE            → "delete"
 *
 * super_admin and admin always bypass these checks.
 * Use this instead of requirePermission(<page>, "view") on route prefixes
 * so that mutating endpoints (POST/PUT/PATCH/DELETE) require the appropriate
 * elevated permission and a Viewer with only "view" cannot write.
 */
export function requirePageAccess(pageSlug: string) {
  return (req: Request, res: Response, next: NextFunction) => {
    if (!req.user) {
      res.status(401).json({ error: "غير مصرح", code: "UNAUTHORIZED" });
      return;
    }
    // super_admin and admin bypass all page-level permission checks
    if (req.user.isSuperAdmin || req.user.role === "admin") return next();

    const method = req.method.toUpperCase();
    let required: string;
    if (method === "GET" || method === "HEAD") {
      required = "view";
    } else if (method === "POST") {
      required = "create";
    } else if (method === "PUT" || method === "PATCH") {
      required = "edit";
    } else if (method === "DELETE") {
      required = "delete";
    } else {
      // OPTIONS, etc. — only require view
      required = "view";
    }

    const pagePerms = req.user.permissions[pageSlug] || [];
    if (!pagePerms.includes(required)) {
      res.status(403).json({
        error: "ليس لديك صلاحية لتنفيذ هذه العملية",
        code: "FORBIDDEN",
        required,
        page: pageSlug,
      });
      return;
    }
    next();
  };
}
