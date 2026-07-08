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

// Phase 8 — Roles include original 5 + 4 new user types aligned with the login form
const ROLES = [
  { name: "super_admin",       nameAr: "مدير النظام",              description: "صلاحيات كاملة غير قابلة للتقييد", isSystem: true },
  { name: "admin",             nameAr: "مدير",                    description: "إدارة المستخدمين والمحتوى", isSystem: true },
  // — Phase 6 login user types —
  { name: "system_admin",      nameAr: "مسؤول النظام",              description: "مسؤول تقني يدير المستخدمين والأدوار والصلاحيات", isSystem: true },
  { name: "approved_manager",  nameAr: "مدير معتمد",               description: "يعتمد الطلبات والحملات ويراجع المحتوى", isSystem: false },
  { name: "team_member",       nameAr: "عضو فريق التواصل المؤسسي", description: "فريق التواصل المؤسسي — ينشئ ويحرر ويتابع الحملات والطلبات", isSystem: false },
  { name: "requester",         nameAr: "موظف مقدم طلب",             description: "يقدم طلبات داخلية/خارجية ويتابع حالتها", isSystem: false },
  // — Legacy —
  { name: "editor",            nameAr: "محرر",                    description: "إنشاء وتعديل المحتوى", isSystem: false },
  { name: "viewer",            nameAr: "مشاهد",                   description: "عرض المحتوى فقط", isSystem: false },
  { name: "guest",             nameAr: "ضيف",                     description: "وصول محدود", isSystem: false },
] as const;

// Phase 8 — RBAC-controlled pages aligned with the 6 sidebar sections + shorfah + admin
const PAGES = [
  { slug: "dashboard",             nameAr: "الأداء التنفيذي",           icon: "i-dashboard", sortOrder: 1 },
  // تواصل داخلي
  { slug: "internal_requests",     nameAr: "الطلبات الداخلية",         icon: "i-inbox",     sortOrder: 2 },
  { slug: "internal_campaigns",    nameAr: "الحملات الداخلية",         icon: "i-megaphone", sortOrder: 3 },
  { slug: "weekend",               nameAr: "نهاية الأسبوع",             icon: "i-calendar",  sortOrder: 4 },
  { slug: "weekstart",             nameAr: "بداية الأسبوع",             icon: "i-calendar",  sortOrder: 5 },
  { slug: "international_days",    nameAr: "الأيام العالمية",           icon: "i-globe",     sortOrder: 6 },
  // تواصل خارجي
  { slug: "external_requests",     nameAr: "الطلبات الخارجية",         icon: "i-inbox",     sortOrder: 7 },
  { slug: "external_campaigns",    nameAr: "الحملات الخارجية",         icon: "i-megaphone", sortOrder: 8 },
  { slug: "shorfah",               nameAr: "نشرة شُرفة",               icon: "i-newspaper", sortOrder: 9 },
  // الرصد الإعلامي
  { slug: "media_monitoring",      nameAr: "الرصد الإعلامي",           icon: "i-eye",       sortOrder: 10 },
  { slug: "world_news",            nameAr: "الأخبار العالمية",           icon: "i-news",      sortOrder: 11 },
  // تقارير الأداء
  { slug: "performance_reports",   nameAr: "تقارير الأداء",             icon: "i-file-text", sortOrder: 12 },
  // المبادرات
  { slug: "initiatives",           nameAr: "المبادرات",                 icon: "i-bulb",      sortOrder: 13 },
  { slug: "ai_year",               nameAr: "عام الذكاء الاصطناعي",     icon: "i-bot",       sortOrder: 14 },
  // ستوديو المحتوى
  { slug: "design_studio",         nameAr: "ستوديو المحتوى",             icon: "i-image",     sortOrder: 15 },
  { slug: "smart_assistant",       nameAr: "المساعد الذكي",             icon: "i-sparkle",   sortOrder: 16 },
  // الإعدادات ولوحة التحكم
  { slug: "admin_panel",           nameAr: "لوحة التحكم",                icon: "i-shield",    sortOrder: 17 },
  { slug: "settings",              nameAr: "الإعدادات",                 icon: "i-settings",  sortOrder: 18 },
] as const;

const PERMISSIONS = [
  { name: "view", nameAr: "عرض" },
  { name: "create", nameAr: "إنشاء" },
  { name: "edit", nameAr: "تعديل" },
  { name: "delete", nameAr: "حذف" },
  { name: "export", nameAr: "تصدير" },
] as const;

// Phase 8 — Role Matrix aligned with expanded page list and new user-type roles
const ROLE_PERMISSIONS: Record<string, { pages: string[]; perms: string[] }[]> = {
  super_admin: [
    { pages: ["*"], perms: ["view", "create", "edit", "delete", "export"] },
  ],
  admin: [
    { pages: ["*"], perms: ["view", "create", "edit", "delete", "export"] },
  ],
  // مسؤول النظام — رمزياً أقوى دور: إدارة المستخدمين والأدوار والصلاحيات
  system_admin: [
    { pages: ["*"], perms: ["view", "create", "edit", "delete", "export"] },
  ],
  // مدير معتمد — يراجع/يعتمد المحتوى ويرى كل شيء ما عدا لوحة التحكم
  approved_manager: [
    {
      pages: [
        "dashboard","internal_requests","internal_campaigns","weekend","weekstart","international_days",
        "external_requests","external_campaigns","shorfah","media_monitoring","world_news",
        "performance_reports","initiatives","ai_year","design_studio","smart_assistant","settings"
      ],
      perms: ["view", "edit", "export"],
    },
  ],
  // عضو فريق التواصل — ينشئ ويحرر المحتوى
  team_member: [
    {
      pages: [
        "dashboard","internal_requests","internal_campaigns","weekend","weekstart","international_days",
        "external_requests","external_campaigns","shorfah","media_monitoring","world_news",
        "performance_reports","initiatives","ai_year","design_studio","smart_assistant"
      ],
      perms: ["view", "create", "edit", "export"],
    },
  ],
  // موظف مقدم طلب — يرى طلباته فقط وينشئها
  requester: [
    {
      pages: ["dashboard","internal_requests","external_requests","international_days","smart_assistant"],
      perms: ["view", "create"],
    },
  ],
  // — Legacy —
  editor: [
    {
      pages: [
        "dashboard","internal_requests","internal_campaigns","weekend","weekstart",
        "international_days","external_requests","external_campaigns","shorfah",
        "world_news","initiatives","ai_year","design_studio","smart_assistant"
      ],
      perms: ["view", "create", "edit", "export"],
    },
  ],
  viewer: [
    { pages: ["dashboard","weekend","world_news","shorfah"], perms: ["view"] },
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
  // — Phase 8 test users for the 4 login user types —
  {
    email: "team@internal.sa",
    name: "عضو الفريق",
    title: "أخصائي تواصل مؤسسي",
    department: "التواصل المؤسسي",
    password: "Team@2026",
    role: "team_member",
  },
  {
    email: "approver@internal.sa",
    name: "مدير معتمد",
    title: "مدير إدارة",
    department: "التواصل المؤسسي",
    password: "Approver@2026",
    role: "approved_manager",
  },
  {
    email: "sysadmin@internal.sa",
    name: "مسؤول النظام",
    title: "مدير نظم وتقنية",
    department: "تقنية المعلومات",
    password: "SysAdmin@2026",
    role: "system_admin",
  },
  {
    email: "requester@internal.sa",
    name: "موظف مقدم طلب",
    title: "موظف",
    department: "الخدمات المساندة",
    password: "Request@2026",
    role: "requester",
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
