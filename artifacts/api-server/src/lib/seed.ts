import bcryptjs from "bcryptjs";
import { db } from "@workspace/db";
import {
  usersTable,
  rolesTable,
  pagesTable,
  permissionsTable,
  rolePermissionsTable,
  userRolesTable,
} from "@workspace/db";
import { eq } from "drizzle-orm";
import { logger } from "./logger";

const ROLES = [
  { name: "super_admin", nameAr: "مدير النظام", description: "صلاحيات كاملة غير قابلة للتقييد", isSystem: true },
  { name: "admin", nameAr: "مدير", description: "إدارة المستخدمين والمحتوى", isSystem: true },
  { name: "editor", nameAr: "محرر", description: "إنشاء وتعديل المحتوى", isSystem: false },
  { name: "viewer", nameAr: "مشاهد", description: "عرض المحتوى فقط", isSystem: false },
  { name: "guest", nameAr: "ضيف", description: "وصول محدود", isSystem: false },
] as const;

// Exactly 8 RBAC-controlled pages as specified in the task contract
const PAGES = [
  { slug: "dashboard",          nameAr: "الأداء التنفيذي",       icon: "i-dashboard", sortOrder: 1 },
  { slug: "weekend",            nameAr: "نهاية الأسبوع",         icon: "i-calendar",  sortOrder: 2 },
  { slug: "world_news",         nameAr: "الأخبار العالمية",      icon: "i-news",      sortOrder: 3 },
  { slug: "initiatives",        nameAr: "المبادرات",             icon: "i-bulb",      sortOrder: 4 },
  { slug: "international_days", nameAr: "الأيام العالمية",       icon: "i-globe",     sortOrder: 5 },
  { slug: "ai_year",            nameAr: "عام الذكاء الاصطناعي", icon: "i-bot",       sortOrder: 6 },
  { slug: "smart_assistant",    nameAr: "المساعد الذكي",         icon: "i-sparkle",   sortOrder: 7 },
  { slug: "settings",           nameAr: "الإعدادات",             icon: "i-settings",  sortOrder: 8 },
] as const;

const PERMISSIONS = [
  { name: "view", nameAr: "عرض" },
  { name: "create", nameAr: "إنشاء" },
  { name: "edit", nameAr: "تعديل" },
  { name: "delete", nameAr: "حذف" },
  { name: "export", nameAr: "تصدير" },
] as const;

const ROLE_PERMISSIONS: Record<string, { pages: string[]; perms: string[] }[]> = {
  super_admin: [
    { pages: ["*"], perms: ["view", "create", "edit", "delete", "export"] },
  ],
  admin: [
    { pages: ["*"], perms: ["view", "create", "edit", "delete", "export"] },
  ],
  editor: [
    {
      pages: ["dashboard", "weekend", "world_news", "initiatives", "international_days", "ai_year", "smart_assistant"],
      perms: ["view", "create", "edit", "export"],
    },
  ],
  viewer: [
    {
      pages: ["dashboard", "weekend", "world_news"],
      perms: ["view"],
    },
  ],
  guest: [
    { pages: ["dashboard"], perms: ["view"] },
  ],
};

const TEST_USERS = [
  {
    email: "editor@internal.sa",
    name: "محرر المحتوى",
    title: "محرر أول",
    department: "التواصل الداخلي",
    password: "Editor@2026",
    role: "editor",
  },
  {
    email: "viewer@internal.sa",
    name: "مراجع البوابة",
    title: "موظف",
    department: "الموارد البشرية",
    password: "Viewer@2026",
    role: "viewer",
  },
  {
    email: "manager@internal.sa",
    name: "مدير الإدارة",
    title: "مدير",
    department: "التخطيط والتطوير",
    password: "Manager@2026",
    role: "admin",
  },
];

export async function runSeedIfNeeded() {
  try {
    logger.info("Running seed (idempotent)...");

    // ── Roles ────────────────────────────────────────────────────────────────
    // onConflictDoNothing ensures partial failures on prior runs are safe
    await db.insert(rolesTable).values(ROLES.map(r => ({ ...r }))).onConflictDoNothing();
    const allRoles = await db.select().from(rolesTable);
    const roleMap = new Map(allRoles.map(r => [r.name, r.id]));

    // ── Pages ─────────────────────────────────────────────────────────────────
    await db.insert(pagesTable).values(PAGES.map(p => ({ ...p }))).onConflictDoNothing();
    const allPages = await db.select().from(pagesTable);
    const pageMap = new Map(allPages.map(p => [p.slug, p.id]));

    // ── Permissions ───────────────────────────────────────────────────────────
    await db.insert(permissionsTable).values(PERMISSIONS.map(p => ({ ...p }))).onConflictDoNothing();
    const allPerms = await db.select().from(permissionsTable);
    const permMap = new Map(allPerms.map(p => [p.name, p.id]));

    // ── Role ↔ Permission matrix ───────────────────────────────────────────────
    const allPageIds = allPages.map(p => p.id);
    const rpRows: { roleId: number; pageId: number; permissionId: number }[] = [];

    for (const [roleName, grants] of Object.entries(ROLE_PERMISSIONS)) {
      const roleId = roleMap.get(roleName);
      if (!roleId) continue;

      for (const grant of grants) {
        const targetPageIds =
          grant.pages[0] === "*"
            ? allPageIds
            : grant.pages.map(slug => pageMap.get(slug)).filter((id): id is number => id !== undefined);

        for (const pageId of targetPageIds) {
          for (const permName of grant.perms) {
            const permId = permMap.get(permName);
            if (!permId) continue;
            rpRows.push({ roleId, pageId, permissionId: permId });
          }
        }
      }
    }

    if (rpRows.length > 0) {
      await db.insert(rolePermissionsTable).values(rpRows).onConflictDoNothing();
    }

    // ── Admin user ────────────────────────────────────────────────────────────
    const existingAdmin = await db
      .select()
      .from(usersTable)
      .where(eq(usersTable.email, "admin@internal.sa"))
      .limit(1);

    let superAdminId: number | undefined;
    if (existingAdmin.length === 0) {
      const superAdminHash = await bcryptjs.hash("Admin@2026", 12);
      const [superAdmin] = await db
        .insert(usersTable)
        .values({
          email: "admin@internal.sa",
          name: "مدير النظام",
          title: "مدير البوابة الداخلية",
          department: "تقنية المعلومات",
          passwordHash: superAdminHash,
        })
        .returning();
      superAdminId = superAdmin?.id;
    } else {
      superAdminId = existingAdmin[0]!.id;
    }

    if (superAdminId !== undefined) {
      const superAdminRoleId = roleMap.get("super_admin");
      if (superAdminRoleId) {
        await db
          .insert(userRolesTable)
          .values({ userId: superAdminId, roleId: superAdminRoleId })
          .onConflictDoNothing();
      }
    }

    // ── Test users ────────────────────────────────────────────────────────────
    for (const u of TEST_USERS) {
      const existing = await db
        .select()
        .from(usersTable)
        .where(eq(usersTable.email, u.email))
        .limit(1);

      let userId: number | undefined;
      if (existing.length === 0) {
        const hash = await bcryptjs.hash(u.password, 12);
        const [user] = await db
          .insert(usersTable)
          .values({
            email: u.email,
            name: u.name,
            title: u.title,
            department: u.department,
            passwordHash: hash,
          })
          .returning();
        userId = user?.id;
      } else {
        userId = existing[0]!.id;
      }

      if (userId !== undefined) {
        const roleId = roleMap.get(u.role);
        if (roleId) {
          await db
            .insert(userRolesTable)
            .values({ userId, roleId, assignedBy: superAdminId })
            .onConflictDoNothing();
        }
      }
    }

    logger.info("Seed completed successfully");
  } catch (err) {
    logger.error({ err }, "Seed failed");
  }
}
