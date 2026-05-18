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
    const existingUsers = await db.select().from(usersTable).limit(1);
    if (existingUsers.length > 0) {
      logger.info("Seed already ran — skipping");
      return;
    }

    logger.info("Running initial seed...");

    const insertedRoles = await db.insert(rolesTable).values(ROLES.map(r => ({ ...r }))).returning();
    const roleMap = new Map(insertedRoles.map(r => [r.name, r.id]));

    const insertedPages = await db.insert(pagesTable).values(PAGES.map(p => ({ ...p }))).returning();
    const pageMap = new Map(insertedPages.map(p => [p.slug, p.id]));

    const insertedPerms = await db.insert(permissionsTable).values(PERMISSIONS.map(p => ({ ...p }))).returning();
    const permMap = new Map(insertedPerms.map(p => [p.name, p.id]));

    const allPageIds = insertedPages.map(p => p.id);
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

    const superAdminRoleId = roleMap.get("super_admin");
    if (superAdmin && superAdminRoleId) {
      await db.insert(userRolesTable).values({
        userId: superAdmin.id,
        roleId: superAdminRoleId,
      });
    }

    for (const u of TEST_USERS) {
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

      const roleId = roleMap.get(u.role);
      if (user && roleId) {
        await db.insert(userRolesTable).values({
          userId: user.id,
          roleId,
          assignedBy: superAdmin?.id,
        });
      }
    }

    logger.info("Seed completed successfully");
  } catch (err) {
    logger.error({ err }, "Seed failed");
  }
}
