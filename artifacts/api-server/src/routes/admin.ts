import { Router, type Request, type Response } from "express";
import { randomBytes } from "crypto";
import bcryptjs from "bcryptjs";
import { db } from "@workspace/db";
import {
  usersTable,
  rolesTable,
  pagesTable,
  permissionsTable,
  rolePermissionsTable,
  userRolesTable,
  userPageOverridesTable,
  activityLogsTable,
  systemSettingsTable,
} from "@workspace/db";
import { eq, desc, ilike, or, and, count, sql, asc } from "drizzle-orm";
import { requireAdmin } from "../middleware/auth";
import { getSettings, validatePassword, invalidateSettingsCache } from "../services/settings";

const router = Router();

// All /admin/* routes require admin role
router.use(requireAdmin);

// ── Helpers ───────────────────────────────────────────────────────────────────

function genTempPassword(): string {
  const chars = "ABCDEFGHJKMNPQRSTUVWXYZabcdefghjkmnpqrstuvwxyz23456789!@#$";
  const bytes = randomBytes(12);
  let pw = "";
  for (let i = 0; i < 12; i++) pw += chars[bytes[i]! % chars.length];
  return pw;
}

async function logAdminAction(
  req: Request,
  action: string,
  entityType: string,
  entityId: string | number,
  details: Record<string, unknown> = {},
) {
  try {
    await db.insert(activityLogsTable).values({
      userId: req.user!.id,
      action,
      entityType,
      entityId: String(entityId),
      details,
      ipAddress: (req.headers["x-forwarded-for"] as string) || req.ip || null,
      userAgent: req.headers["user-agent"] || null,
    });
  } catch { /* non-critical */ }
}

// ── Users ─────────────────────────────────────────────────────────────────────

router.get("/admin/users", async (req: Request, res: Response) => {
  const { search = "", role = "", page = "1", limit = "50" } = req.query as Record<string, string>;
  const pageNum = Math.max(1, parseInt(page));
  const lim = Math.min(200, parseInt(limit) || 50);
  const off = (pageNum - 1) * lim;

  const searchCond = search
    ? or(
        ilike(usersTable.name, `%${search}%`),
        ilike(usersTable.email, `%${search}%`),
        ilike(usersTable.department, `%${search}%`),
      )
    : undefined;

  const roleCond = role ? ilike(rolesTable.name, `%${role}%`) : undefined;
  const whereCond = searchCond && roleCond ? and(searchCond, roleCond) : (searchCond ?? roleCond);

  const users = await db
    .select({
      id: usersTable.id,
      email: usersTable.email,
      name: usersTable.name,
      title: usersTable.title,
      department: usersTable.department,
      isActive: usersTable.isActive,
      isLocked: usersTable.isLocked,
      failedAttempts: usersTable.failedAttempts,
      lastLogin: usersTable.lastLogin,
      createdAt: usersTable.createdAt,
      roleId: userRolesTable.roleId,
      roleName: rolesTable.name,
      roleNameAr: rolesTable.nameAr,
    })
    .from(usersTable)
    .leftJoin(userRolesTable, eq(userRolesTable.userId, usersTable.id))
    .leftJoin(rolesTable, eq(rolesTable.id, userRolesTable.roleId))
    .where(whereCond)
    .orderBy(desc(usersTable.createdAt))
    .limit(lim)
    .offset(off);

  const [{ total }] = await db
    .select({ total: count() })
    .from(usersTable)
    .leftJoin(userRolesTable, eq(userRolesTable.userId, usersTable.id))
    .leftJoin(rolesTable, eq(rolesTable.id, userRolesTable.roleId))
    .where(whereCond);

  res.json({ users, total, page: pageNum, pages: Math.ceil(total / lim) });
});

router.post("/admin/users", async (req: Request, res: Response) => {
  const { email, name, title, department, roleId, password } = req.body as {
    email?: string;
    name?: string;
    title?: string;
    department?: string;
    roleId?: number;
    password?: string;
  };

  if (!email || !name || !roleId) {
    res.status(400).json({ error: "email و name و roleId مطلوبة" });
    return;
  }

  const existing = await db
    .select({ id: usersTable.id })
    .from(usersTable)
    .where(eq(usersTable.email, email.toLowerCase().trim()))
    .limit(1);

  if (existing.length > 0) {
    res.status(400).json({ error: "البريد الإلكتروني مستخدم بالفعل" });
    return;
  }

  const tempPassword = password || genTempPassword();

  if (password) {
    const settings = await getSettings();
    const pwError = validatePassword(password, settings);
    if (pwError) { res.status(400).json({ error: pwError }); return; }
  }

  const hash = await bcryptjs.hash(tempPassword, 12);
  const now = new Date();

  const [user] = await db
    .insert(usersTable)
    .values({ email: email.toLowerCase().trim(), name, title, department, passwordHash: hash, passwordChangedAt: now })
    .returning();

  await db.insert(userRolesTable).values({
    userId: user!.id,
    roleId: Number(roleId),
    assignedBy: req.user!.id,
  });

  await logAdminAction(req, "user_created", "user", user!.id, { email, name });
  res.json({ user, tempPassword });
});

router.patch("/admin/users/:id", async (req: Request, res: Response) => {
  const id = parseInt(req.params["id"] as string);
  const { name, title, department, email, roleId } = req.body as {
    name?: string;
    title?: string;
    department?: string;
    email?: string;
    roleId?: number;
  };

  await db
    .update(usersTable)
    .set({ name, title, department, email: email?.toLowerCase().trim(), updatedAt: new Date() })
    .where(eq(usersTable.id, id));

  if (roleId) {
    await db.delete(userRolesTable).where(eq(userRolesTable.userId, id));
    await db.insert(userRolesTable).values({
      userId: id,
      roleId: Number(roleId),
      assignedBy: req.user!.id,
    });
  }

  await logAdminAction(req, "user_updated", "user", id, { name, roleId });
  res.json({ ok: true });
});

router.post("/admin/users/:id/suspend", async (req: Request, res: Response) => {
  const id = parseInt(req.params["id"] as string);
  if (id === req.user!.id) {
    res.status(400).json({ error: "لا يمكنك تعليق حسابك الخاص" });
    return;
  }
  const [user] = await db
    .select({ isActive: usersTable.isActive })
    .from(usersTable)
    .where(eq(usersTable.id, id))
    .limit(1);
  if (!user) { res.status(404).json({ error: "المستخدم غير موجود" }); return; }

  await db
    .update(usersTable)
    .set({ isActive: !user.isActive, updatedAt: new Date() })
    .where(eq(usersTable.id, id));

  await logAdminAction(req, user.isActive ? "user_suspended" : "user_activated", "user", id);
  res.json({ ok: true, isActive: !user.isActive });
});

router.post("/admin/users/:id/unlock", async (req: Request, res: Response) => {
  const id = parseInt(req.params["id"] as string);
  await db
    .update(usersTable)
    .set({ isLocked: false, failedAttempts: 0, updatedAt: new Date() })
    .where(eq(usersTable.id, id));
  await logAdminAction(req, "user_unlocked", "user", id);
  res.json({ ok: true });
});

router.post("/admin/users/:id/reset-password", async (req: Request, res: Response) => {
  const id = parseInt(req.params["id"] as string);
  const tempPassword = genTempPassword();
  const hash = await bcryptjs.hash(tempPassword, 12);
  const now = new Date();
  await db
    .update(usersTable)
    .set({ passwordHash: hash, passwordChangedAt: now, isLocked: false, failedAttempts: 0, updatedAt: now })
    .where(eq(usersTable.id, id));
  await logAdminAction(req, "password_reset", "user", id);
  res.json({ ok: true, tempPassword });
});

router.get("/admin/users/:id", async (req: Request, res: Response) => {
  const id = parseInt(req.params["id"] as string);
  const [user] = await db
    .select({
      id: usersTable.id,
      email: usersTable.email,
      name: usersTable.name,
      title: usersTable.title,
      department: usersTable.department,
      isActive: usersTable.isActive,
      isLocked: usersTable.isLocked,
      lastLogin: usersTable.lastLogin,
      passwordChangedAt: usersTable.passwordChangedAt,
      createdAt: usersTable.createdAt,
      roleId: userRolesTable.roleId,
      roleName: rolesTable.name,
      roleNameAr: rolesTable.nameAr,
    })
    .from(usersTable)
    .leftJoin(userRolesTable, eq(userRolesTable.userId, usersTable.id))
    .leftJoin(rolesTable, eq(rolesTable.id, userRolesTable.roleId))
    .where(eq(usersTable.id, id))
    .limit(1);

  if (!user) { res.status(404).json({ error: "المستخدم غير موجود" }); return; }
  res.json({ user });
});

router.delete("/admin/users/:id", async (req: Request, res: Response) => {
  const id = parseInt(req.params["id"] as string);
  if (id === req.user!.id) {
    res.status(400).json({ error: "لا يمكنك حذف حسابك الخاص" });
    return;
  }
  await db.delete(usersTable).where(eq(usersTable.id, id));
  await logAdminAction(req, "user_deleted", "user", id);
  res.json({ ok: true });
});

// ── Roles ─────────────────────────────────────────────────────────────────────

router.get("/admin/roles", async (_req: Request, res: Response) => {
  const roles = await db
    .select({
      id: rolesTable.id,
      name: rolesTable.name,
      nameAr: rolesTable.nameAr,
      description: rolesTable.description,
      isSystem: rolesTable.isSystem,
      createdAt: rolesTable.createdAt,
      userCount: count(userRolesTable.userId),
    })
    .from(rolesTable)
    .leftJoin(userRolesTable, eq(userRolesTable.roleId, rolesTable.id))
    .groupBy(rolesTable.id)
    .orderBy(asc(rolesTable.id));

  res.json({ roles });
});

router.post("/admin/roles", async (req: Request, res: Response) => {
  const { name, nameAr, description } = req.body as {
    name?: string;
    nameAr?: string;
    description?: string;
  };
  if (!name || !nameAr) {
    res.status(400).json({ error: "name و nameAr مطلوبان" });
    return;
  }
  const [role] = await db
    .insert(rolesTable)
    .values({ name, nameAr, description, isSystem: false })
    .returning();
  await logAdminAction(req, "role_created", "role", role!.id, { name });
  res.json({ role });
});

router.patch("/admin/roles/:id", async (req: Request, res: Response) => {
  const id = parseInt(req.params["id"] as string);
  const { nameAr, description } = req.body as { nameAr?: string; description?: string };
  await db.update(rolesTable).set({ nameAr, description }).where(eq(rolesTable.id, id));
  await logAdminAction(req, "role_updated", "role", id);
  res.json({ ok: true });
});

router.delete("/admin/roles/:id", async (req: Request, res: Response) => {
  const id = parseInt(req.params["id"] as string);
  const [role] = await db.select().from(rolesTable).where(eq(rolesTable.id, id)).limit(1);
  if (!role) { res.status(404).json({ error: "الدور غير موجود" }); return; }
  if (role.isSystem) {
    res.status(400).json({ error: "لا يمكن حذف الأدوار الجوهرية للنظام" });
    return;
  }
  await db.delete(rolesTable).where(eq(rolesTable.id, id));
  await logAdminAction(req, "role_deleted", "role", id, { name: role.name });
  res.json({ ok: true });
});

router.get("/admin/roles/:id/permissions", async (req: Request, res: Response) => {
  const roleId = parseInt(req.params["id"] as string);

  const pages = await db
    .select()
    .from(pagesTable)
    .where(eq(pagesTable.isActive, true))
    .orderBy(asc(pagesTable.sortOrder));
  const perms = await db.select().from(permissionsTable).orderBy(asc(permissionsTable.id));

  const rpRows = await db
    .select({ pageId: rolePermissionsTable.pageId, permissionId: rolePermissionsTable.permissionId })
    .from(rolePermissionsTable)
    .where(eq(rolePermissionsTable.roleId, roleId));

  const granted = new Set(rpRows.map((r) => `${r.pageId}:${r.permissionId}`));
  const matrix: Record<string, string[]> = {};
  for (const page of pages) {
    matrix[page.slug] = perms.filter((p) => granted.has(`${page.id}:${p.id}`)).map((p) => p.name);
  }

  res.json({ pages, permissions: perms, matrix });
});

router.put("/admin/roles/:id/permissions", async (req: Request, res: Response) => {
  const roleId = parseInt(req.params["id"] as string);
  const { permissions } = req.body as { permissions?: Record<string, string[]> };
  if (!permissions) {
    res.status(400).json({ error: "permissions مطلوب" });
    return;
  }

  const pages = await db.select().from(pagesTable);
  const allPerms = await db.select().from(permissionsTable);
  const pageMap = new Map(pages.map((p) => [p.slug, p.id]));
  const permMap = new Map(allPerms.map((p) => [p.name, p.id]));

  await db.delete(rolePermissionsTable).where(eq(rolePermissionsTable.roleId, roleId));

  const rows: { roleId: number; pageId: number; permissionId: number }[] = [];
  for (const [pageSlug, permNames] of Object.entries(permissions)) {
    const pageId = pageMap.get(pageSlug);
    if (!pageId) continue;
    for (const permName of permNames) {
      const permId = permMap.get(permName);
      if (!permId) continue;
      rows.push({ roleId, pageId, permissionId: permId });
    }
  }

  if (rows.length > 0) await db.insert(rolePermissionsTable).values(rows);

  await logAdminAction(req, "role_permissions_updated", "role", roleId);
  res.json({ ok: true });
});

// ── Permission Matrix ─────────────────────────────────────────────────────────

router.get("/admin/matrix", async (_req: Request, res: Response) => {
  const pages = await db
    .select()
    .from(pagesTable)
    .where(eq(pagesTable.isActive, true))
    .orderBy(asc(pagesTable.sortOrder));
  const allPerms = await db.select().from(permissionsTable);

  const users = await db
    .select({
      id: usersTable.id,
      name: usersTable.name,
      email: usersTable.email,
      isActive: usersTable.isActive,
      roleId: userRolesTable.roleId,
      roleName: rolesTable.name,
      roleNameAr: rolesTable.nameAr,
    })
    .from(usersTable)
    .leftJoin(userRolesTable, eq(userRolesTable.userId, usersTable.id))
    .leftJoin(rolesTable, eq(rolesTable.id, userRolesTable.roleId))
    .orderBy(asc(usersTable.id));

  const rpRows = await db
    .select({
      roleId: rolePermissionsTable.roleId,
      pageId: rolePermissionsTable.pageId,
      permName: permissionsTable.name,
    })
    .from(rolePermissionsTable)
    .innerJoin(permissionsTable, eq(permissionsTable.id, rolePermissionsTable.permissionId));

  const rolePermsMap: Record<number, Record<number, string[]>> = {};
  for (const row of rpRows) {
    if (!rolePermsMap[row.roleId]) rolePermsMap[row.roleId] = {};
    if (!rolePermsMap[row.roleId]![row.pageId]) rolePermsMap[row.roleId]![row.pageId] = [];
    rolePermsMap[row.roleId]![row.pageId]!.push(row.permName);
  }

  const overrides = await db
    .select({
      userId: userPageOverridesTable.userId,
      pageId: userPageOverridesTable.pageId,
      permName: permissionsTable.name,
      grantType: userPageOverridesTable.grantType,
    })
    .from(userPageOverridesTable)
    .innerJoin(permissionsTable, eq(permissionsTable.id, userPageOverridesTable.permissionId));

  const userOvsMap: Record<number, Record<number, { perm: string; type: string }[]>> = {};
  for (const ov of overrides) {
    if (!userOvsMap[ov.userId]) userOvsMap[ov.userId] = {};
    if (!userOvsMap[ov.userId]![ov.pageId]) userOvsMap[ov.userId]![ov.pageId] = [];
    userOvsMap[ov.userId]![ov.pageId]!.push({ perm: ov.permName, type: ov.grantType });
  }

  const matrixUsers = users.map((u) => {
    const rolePerms = u.roleId ? (rolePermsMap[u.roleId] || {}) : {};
    const effectivePerms: Record<string, string[]> = {};
    for (const page of pages) {
      const base = new Set(rolePerms[page.id] || []);
      const ovs = (userOvsMap[u.id] || {})[page.id] || [];
      for (const ov of ovs) {
        if (ov.type === "allow") base.add(ov.perm);
        else if (ov.type === "deny") base.delete(ov.perm);
      }
      effectivePerms[page.slug] = [...base];
    }
    return { ...u, permissions: effectivePerms };
  });

  res.json({ pages, permissions: allPerms, users: matrixUsers });
});

router.put("/admin/matrix/user-override", async (req: Request, res: Response) => {
  const { userId, pageSlug, permName, grantType } = req.body as {
    userId?: number;
    pageSlug?: string;
    permName?: string;
    grantType?: string | null;
  };

  if (!userId || !pageSlug || !permName) {
    res.status(400).json({ error: "userId, pageSlug, permName مطلوبة" });
    return;
  }

  const [page] = await db.select().from(pagesTable).where(eq(pagesTable.slug, pageSlug)).limit(1);
  const [perm] = await db.select().from(permissionsTable).where(eq(permissionsTable.name, permName)).limit(1);
  if (!page || !perm) { res.status(400).json({ error: "صفحة أو صلاحية غير صحيحة" }); return; }

  await db.delete(userPageOverridesTable).where(
    and(
      eq(userPageOverridesTable.userId, userId),
      eq(userPageOverridesTable.pageId, page.id),
      eq(userPageOverridesTable.permissionId, perm.id),
    ),
  );

  if (grantType === "allow" || grantType === "deny") {
    await db.insert(userPageOverridesTable).values({
      userId,
      pageId: page.id,
      permissionId: perm.id,
      grantType,
      createdBy: req.user!.id,
    });
  }

  await logAdminAction(req, "matrix_override", "user", userId, { pageSlug, permName, grantType });
  res.json({ ok: true });
});

router.get("/admin/matrix/export", async (req: Request, res: Response) => {
  const format = (req.query["format"] as string | undefined) || "csv";
  const pages = await db
    .select()
    .from(pagesTable)
    .where(eq(pagesTable.isActive, true))
    .orderBy(asc(pagesTable.sortOrder));

  const users = await db
    .select({
      id: usersTable.id,
      name: usersTable.name,
      email: usersTable.email,
      roleId: userRolesTable.roleId,
      roleNameAr: rolesTable.nameAr,
    })
    .from(usersTable)
    .leftJoin(userRolesTable, eq(userRolesTable.userId, usersTable.id))
    .leftJoin(rolesTable, eq(rolesTable.id, userRolesTable.roleId))
    .orderBy(asc(usersTable.id));

  const rpRows = await db
    .select({ roleId: rolePermissionsTable.roleId, pageId: rolePermissionsTable.pageId, permName: permissionsTable.name })
    .from(rolePermissionsTable)
    .innerJoin(permissionsTable, eq(permissionsTable.id, rolePermissionsTable.permissionId));

  const rolePermsMap: Record<number, Record<number, string[]>> = {};
  for (const row of rpRows) {
    if (!rolePermsMap[row.roleId]) rolePermsMap[row.roleId] = {};
    if (!rolePermsMap[row.roleId]![row.pageId]) rolePermsMap[row.roleId]![row.pageId] = [];
    rolePermsMap[row.roleId]![row.pageId]!.push(row.permName);
  }

  const overrides = await db
    .select({ userId: userPageOverridesTable.userId, pageId: userPageOverridesTable.pageId, permName: permissionsTable.name, grantType: userPageOverridesTable.grantType })
    .from(userPageOverridesTable)
    .innerJoin(permissionsTable, eq(permissionsTable.id, userPageOverridesTable.permissionId));

  const userOvsMap: Record<number, Record<number, { perm: string; type: string }[]>> = {};
  for (const ov of overrides) {
    if (!userOvsMap[ov.userId]) userOvsMap[ov.userId] = {};
    if (!userOvsMap[ov.userId]![ov.pageId]) userOvsMap[ov.userId]![ov.pageId] = [];
    userOvsMap[ov.userId]![ov.pageId]!.push({ perm: ov.permName, type: ov.grantType });
  }

  type MatrixRow = { userId: number; name: string; email: string; role: string; pages: Record<string, string[]> };
  const matrixData: MatrixRow[] = [];

  for (const u of users) {
    const rolePerms = u.roleId ? (rolePermsMap[u.roleId] || {}) : {};
    const pagePerms: Record<string, string[]> = {};
    for (const pg of pages) {
      const base = new Set(rolePerms[pg.id] || []);
      const ovs = (userOvsMap[u.id] || {})[pg.id] || [];
      for (const ov of ovs) {
        if (ov.type === "allow") base.add(ov.perm);
        else if (ov.type === "deny") base.delete(ov.perm);
      }
      pagePerms[pg.slug] = [...base];
    }
    matrixData.push({ userId: u.id, name: u.name, email: u.email, role: u.roleNameAr || "—", pages: pagePerms });
  }

  if (format === "json") {
    res.json({ pages: pages.map((p) => ({ slug: p.slug, nameAr: p.nameAr })), matrix: matrixData });
    return;
  }

  // Default: CSV (Excel-compatible with BOM)
  const levelOf = (perms: string[]) => {
    if (perms.includes("delete") || perms.includes("export")) return "★ كامل";
    if (perms.includes("create") || perms.includes("edit")) return "✏ تعديل";
    if (perms.includes("view")) return "✓ عرض";
    return "✗";
  };

  const header = ["المستخدم", "البريد", "الدور", ...pages.map((p) => p.nameAr || p.slug)];
  const csvRows = [header, ...matrixData.map((u) => [
    u.name, u.email, u.role,
    ...pages.map((pg) => levelOf(u.pages[pg.slug] || [])),
  ])];

  const csv = csvRows.map((r) => r.map((v) => `"${String(v).replace(/"/g, '""')}"`).join(",")).join("\n");
  res.setHeader("Content-Type", "text/csv; charset=utf-8");
  res.setHeader("Content-Disposition", 'attachment; filename="permissions-matrix.csv"');
  res.send("\ufeff" + csv);
});

// ── Activity Logs ─────────────────────────────────────────────────────────────

const buildLogConditions = (q: Record<string, string>) => {
  const conds = [];
  if (q["userId"]) conds.push(eq(activityLogsTable.userId, parseInt(q["userId"]!)));
  if (q["action"]) conds.push(ilike(activityLogsTable.action, `%${q["action"]}%`));
  if (q["dateFrom"]) conds.push(sql`${activityLogsTable.createdAt} >= ${new Date(q["dateFrom"]!)}`);
  if (q["dateTo"]) conds.push(sql`${activityLogsTable.createdAt} <= ${new Date(q["dateTo"]! + "T23:59:59")}`);
  return conds.length > 0 ? and(...conds) : undefined;
};

router.get("/admin/activity", async (req: Request, res: Response) => {
  const q = req.query as Record<string, string>;
  const pageNum = Math.max(1, parseInt(q["page"] || "1"));
  const lim = Math.min(200, parseInt(q["limit"] || "50") || 50);
  const off = (pageNum - 1) * lim;
  const where = buildLogConditions(q);

  const logs = await db
    .select({
      id: activityLogsTable.id,
      userId: activityLogsTable.userId,
      action: activityLogsTable.action,
      entityType: activityLogsTable.entityType,
      entityId: activityLogsTable.entityId,
      details: activityLogsTable.details,
      ipAddress: activityLogsTable.ipAddress,
      createdAt: activityLogsTable.createdAt,
      userName: usersTable.name,
      userEmail: usersTable.email,
    })
    .from(activityLogsTable)
    .leftJoin(usersTable, eq(usersTable.id, activityLogsTable.userId))
    .where(where)
    .orderBy(desc(activityLogsTable.createdAt))
    .limit(lim)
    .offset(off);

  const [{ total }] = await db
    .select({ total: count() })
    .from(activityLogsTable)
    .where(where);

  res.json({ logs, total, page: pageNum, pages: Math.ceil(total / lim) });
});

router.get("/admin/activity/export", async (req: Request, res: Response) => {
  const q = req.query as Record<string, string>;
  const where = buildLogConditions(q);

  const logs = await db
    .select({
      id: activityLogsTable.id,
      userId: activityLogsTable.userId,
      action: activityLogsTable.action,
      entityType: activityLogsTable.entityType,
      entityId: activityLogsTable.entityId,
      details: activityLogsTable.details,
      ipAddress: activityLogsTable.ipAddress,
      createdAt: activityLogsTable.createdAt,
      userName: usersTable.name,
      userEmail: usersTable.email,
    })
    .from(activityLogsTable)
    .leftJoin(usersTable, eq(usersTable.id, activityLogsTable.userId))
    .where(where)
    .orderBy(desc(activityLogsTable.createdAt))
    .limit(5000);

  const rows = [
    ["#", "المستخدم", "البريد", "العملية", "النوع", "المعرف", "IP", "التاريخ"],
    ...logs.map((l) => [
      l.id,
      l.userName || "—",
      l.userEmail || "—",
      l.action,
      l.entityType || "—",
      l.entityId || "—",
      l.ipAddress || "—",
      l.createdAt ? new Date(l.createdAt).toISOString() : "—",
    ]),
  ];

  const csv = rows.map((r) => r.map((v) => `"${String(v).replace(/"/g, '""')}"`).join(",")).join("\n");
  res.setHeader("Content-Type", "text/csv; charset=utf-8");
  res.setHeader("Content-Disposition", 'attachment; filename="activity-log.csv"');
  res.send("\ufeff" + csv);
});

// ── System Settings ───────────────────────────────────────────────────────────

const DEFAULT_SETTINGS: Record<string, string> = {
  password_min_length: "8",
  password_require_uppercase: "true",
  password_require_symbols: "true",
  password_expiry_days: "90",
  session_duration_minutes: "60",
  auto_logout_minutes: "30",
};

router.get("/admin/settings", async (_req: Request, res: Response) => {
  const rows = await db.select().from(systemSettingsTable);
  const settings: Record<string, string> = { ...DEFAULT_SETTINGS };
  for (const row of rows) settings[row.key] = row.value;
  res.json({ settings });
});

const SETTINGS_SCHEMA: Record<string, { type: "int" | "bool"; min?: number; max?: number }> = {
  password_min_length:         { type: "int",  min: 4,   max: 100  },
  password_expiry_days:        { type: "int",  min: 0,   max: 365  },
  session_duration_minutes:    { type: "int",  min: 5,   max: 1440 },
  auto_logout_minutes:         { type: "int",  min: 1,   max: 480  },
  password_require_uppercase:  { type: "bool" },
  password_require_symbols:    { type: "bool" },
};

router.put("/admin/settings", async (req: Request, res: Response) => {
  const { settings } = req.body as { settings?: Record<string, string> };
  if (!settings) { res.status(400).json({ error: "settings مطلوب" }); return; }

  const errors: string[] = [];
  const validated: Record<string, string> = {};

  for (const [key, value] of Object.entries(settings)) {
    const schema = SETTINGS_SCHEMA[key];
    if (!schema) { errors.push(`مفتاح غير مسموح: ${key}`); continue; }

    if (schema.type === "int") {
      const num = parseInt(String(value), 10);
      if (isNaN(num)) { errors.push(`${key}: يجب أن يكون رقماً صحيحاً`); continue; }
      if (schema.min !== undefined && num < schema.min) { errors.push(`${key}: الحد الأدنى ${schema.min}`); continue; }
      if (schema.max !== undefined && num > schema.max) { errors.push(`${key}: الحد الأقصى ${schema.max}`); continue; }
      validated[key] = String(num);
    } else {
      if (value !== "true" && value !== "false") { errors.push(`${key}: يجب أن تكون القيمة true أو false`); continue; }
      validated[key] = value;
    }
  }

  if (errors.length > 0) { res.status(400).json({ error: errors.join("; ") }); return; }

  for (const [key, value] of Object.entries(validated)) {
    await db
      .insert(systemSettingsTable)
      .values({ key, value, updatedAt: new Date() })
      .onConflictDoUpdate({
        target: systemSettingsTable.key,
        set: { value, updatedAt: new Date() },
      });
  }

  invalidateSettingsCache();
  await logAdminAction(req, "settings_updated", "system", "settings", { keys: Object.keys(validated) });
  res.json({ ok: true });
});

export default router;
