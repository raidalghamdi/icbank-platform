import { Router, type Request, type Response } from "express";
import { db } from "@workspace/db";
import {
  shorfahIssuesTable,
  shorfahSectionsTable,
  shorfahSectionPermissionsTable,
  shorfahSectionMediaTable,
  shorfahWorkflowLogTable,
  shorfahAssignmentsTable,
  shorfahRemindersTable,
  shorfahNotificationsTable,
  shorfahSectionSlaDefaultsTable,
  usersTable,
} from "@workspace/db";
import { eq, desc, asc, and, or, inArray } from "drizzle-orm";
import { requireAuth, requireAdmin } from "../middleware/auth";
import { geminiJSON } from "../lib/aiProviders";
import { buildShorfahPdfHtml } from "./shorfah-pdf";
import { sendNotification, buildPublishEmailHtml, buildOverdueEmailHtml, buildInitialEmailHtml } from "../lib/notify";
import { ObjectStorageService } from "../lib/objectStorage";

const router = Router();

// ── helpers ──────────────────────────────────────────────────────────────
async function canAccessSection(
  userId: number,
  userRole: string,
  sectionId: number,
  perm: "view" | "contribute" | "review" | "approve",
): Promise<boolean> {
  if (userRole === "super_admin" || userRole === "admin") return true;
  const perms = await db
    .select()
    .from(shorfahSectionPermissionsTable)
    .where(eq(shorfahSectionPermissionsTable.sectionId, sectionId));
  return perms.some(
    (p) =>
      (p.userId === userId || p.roleName === userRole) && p.permission === perm,
  );
}

async function logAction(
  sectionId: number,
  actorUserId: number,
  action: string,
  fromStatus: string | null,
  toStatus: string | null,
  notes?: string,
) {
  await db.insert(shorfahWorkflowLogTable).values({
    sectionId,
    actorUserId,
    action,
    fromStatus,
    toStatus,
    notes: notes ?? null,
  });
}

// Fetch all users (for publish fan-out / notifications)
async function getAllUsers(): Promise<Array<{ id: number; email: string; name: string }>> {
  try {
    const users = await db.select({ id: usersTable.id, email: usersTable.email, name: usersTable.name }).from(usersTable);
    return users;
  } catch {
    return [];
  }
}

// Fetch user by ID
async function getUserById(userId: number): Promise<{ id: number; email: string; name: string } | null> {
  try {
    const [user] = await db.select({ id: usersTable.id, email: usersTable.email, name: usersTable.name })
      .from(usersTable)
      .where(eq(usersTable.id, userId))
      .limit(1);
    return user || null;
  } catch {
    return null;
  }
}

// ── issues ───────────────────────────────────────────────────────────────
router.get("/shorfah/issues", requireAuth, async (_req: Request, res: Response) => {
  const issues = await db
    .select()
    .from(shorfahIssuesTable)
    .orderBy(desc(shorfahIssuesTable.year), desc(shorfahIssuesTable.month));
  res.json({ issues });
});

router.get("/shorfah/issues/:id", requireAuth, async (req: Request, res: Response) => {
  const id = Number(req.params.id);
  const [issue] = await db
    .select()
    .from(shorfahIssuesTable)
    .where(eq(shorfahIssuesTable.id, id))
    .limit(1);
  if (!issue) return res.status(404).json({ error: "العدد غير موجود" });
  const sections = await db
    .select()
    .from(shorfahSectionsTable)
    .where(eq(shorfahSectionsTable.issueId, id))
    .orderBy(asc(shorfahSectionsTable.displayOrder));
  // attach media + permissions
  const sectionIds = sections.map((s) => s.id);
  const media = sectionIds.length
    ? await db.select().from(shorfahSectionMediaTable)
    : [];
  const permissions = sectionIds.length
    ? await db.select().from(shorfahSectionPermissionsTable)
    : [];
  const sectionsEnriched = sections.map((s) => ({
    ...s,
    media: media.filter((m) => m.sectionId === s.id),
    permissions: permissions.filter((p) => p.sectionId === s.id),
  }));
  res.json({ issue, sections: sectionsEnriched });
});

// Canonical Shorfah issue template — keep in sync with the published issue 1 structure.
const SHORFAH_DEFAULT_SECTIONS: Array<{
  sectionType: string;
  titleAr: string;
  descriptionAr: string;
  displayOrder: number;
}> = [
  { sectionType: "news", titleAr: "أخبارنا", descriptionAr: "أبرز أخبار الهيئة هذا الشهر", displayOrder: 10 },
  { sectionType: "office_interview", titleAr: "في مكتبهم", descriptionAr: "حوار شهري مع أحد القياديين", displayOrder: 20 },
  { sectionType: "competition_culture", titleAr: "ثقافة المنافسة", descriptionAr: "مفاهيم ومقالات في ثقافة المنافسة", displayOrder: 30 },
  { sectionType: "outside_box", titleAr: "خارج الصندوق", descriptionAr: "مقال شهري من موظف", displayOrder: 40 },
  { sectionType: "events", titleAr: "فعالياتنا", descriptionAr: "فعاليات الشهر", displayOrder: 50 },
  { sectionType: "employee_qa", titleAr: "عطنا علومك", descriptionAr: "ست أسئلة سريعة مع أحد الزملاء", displayOrder: 60 },
];

async function seedShorfahSections(issueId: number) {
  const rows = [] as Array<Record<string, unknown>>;
  for (const t of SHORFAH_DEFAULT_SECTIONS) {
    const slaDays = await getSlaDefaultDays(t.sectionType);
    rows.push({
      issueId,
      sectionType: t.sectionType,
      titleAr: t.titleAr,
      descriptionAr: t.descriptionAr,
      displayOrder: t.displayOrder,
      includeInPdf: true,
      workflowStatus: "pending_contribution",
      slaDays,
    });
  }
  await db.insert(shorfahSectionsTable).values(rows as any);
}

router.post("/shorfah/issues", requireAdmin, async (req: Request, res: Response) => {
  const { issueNo: bodyIssueNo, titleAr, subtitleAr, month, year, contributionsOpenAt, contributionsCloseAt, editorLetter } = req.body || {};
  if (!titleAr || !month || !year) {
    return res.status(400).json({ error: "بيانات ناقصة" });
  }
  // Auto-assign issueNo when not provided: max(issueNo)+1
  let issueNo = Number(bodyIssueNo);
  if (!issueNo || isNaN(issueNo)) {
    const all = await db.select({ n: shorfahIssuesTable.issueNo }).from(shorfahIssuesTable);
    const maxNo = all.reduce((m, r) => Math.max(m, Number(r.n) || 0), 0);
    issueNo = maxNo + 1;
  }
  const [created] = await db
    .insert(shorfahIssuesTable)
    .values({
      issueNo,
      titleAr,
      subtitleAr: subtitleAr ?? null,
      month: Number(month),
      year: Number(year),
      contributionsOpenAt: contributionsOpenAt ? new Date(contributionsOpenAt) : null,
      contributionsCloseAt: contributionsCloseAt ? new Date(contributionsCloseAt) : null,
      editorLetter: editorLetter ?? null,
      status: "collecting",
      createdBy: req.user!.id,
    })
    .returning();
  // Seed canonical sections so the new issue has the same structure as issue 1
  try {
    await seedShorfahSections(created.id);
  } catch (e) {
    // Don't fail issue creation if seeding hits a transient error — just log.
    console.error("[shorfah] seed sections failed for issue", created.id, e);
  }
  res.json({ issue: created });
});

// Backfill: seed canonical sections into an existing issue that has none.
router.post("/shorfah/issues/:id/seed-sections", requireAdmin, async (req: Request, res: Response) => {
  const id = Number(req.params.id);
  const [issue] = await db.select().from(shorfahIssuesTable).where(eq(shorfahIssuesTable.id, id)).limit(1);
  if (!issue) return res.status(404).json({ error: "العدد غير موجود" });
  const existing = await db.select({ id: shorfahSectionsTable.id }).from(shorfahSectionsTable).where(eq(shorfahSectionsTable.issueId, id));
  if (existing.length > 0) return res.status(400).json({ error: "هذا العدد يحتوي على أقسام بالفعل", existing: existing.length });
  await seedShorfahSections(id);
  const rows = await db.select().from(shorfahSectionsTable).where(eq(shorfahSectionsTable.issueId, id));
  res.json({ ok: true, sections: rows.length });
});

router.patch("/shorfah/issues/:id", requireAdmin, async (req: Request, res: Response) => {
  const id = Number(req.params.id);
  const patch: Record<string, unknown> = { updatedAt: new Date() };
  for (const k of ["titleAr", "subtitleAr", "editorLetter", "coverImageUrl", "status"]) {
    if (req.body[k] !== undefined) patch[k] = req.body[k];
  }
  if (req.body.contributionsOpenAt) patch.contributionsOpenAt = new Date(req.body.contributionsOpenAt);
  if (req.body.contributionsCloseAt) patch.contributionsCloseAt = new Date(req.body.contributionsCloseAt);
  const [updated] = await db
    .update(shorfahIssuesTable)
    .set(patch)
    .where(eq(shorfahIssuesTable.id, id))
    .returning();
  res.json({ issue: updated });
});

// Task 2: Start-review transition
router.post("/shorfah/issues/:id/start-review", requireAdmin, async (req: Request, res: Response) => {
  const id = Number(req.params.id);
  const [issue] = await db.select().from(shorfahIssuesTable).where(eq(shorfahIssuesTable.id, id)).limit(1);
  if (!issue) return res.status(404).json({ error: "العدد غير موجود" });
  if (issue.status === "published") return res.status(400).json({ error: "العدد منشور بالفعل" });
  const [updated] = await db.update(shorfahIssuesTable).set({
    status: "in_review",
    updatedAt: new Date(),
  }).where(eq(shorfahIssuesTable.id, id)).returning();
  res.json({ issue: updated });
});

// ── SLA defaults (Round 3 Task 1) ────────────────────────────────────────
// Helper: lookup SLA default for a section type
async function getSlaDefaultDays(sectionType: string): Promise<number> {
  const [row] = await db
    .select()
    .from(shorfahSectionSlaDefaultsTable)
    .where(eq(shorfahSectionSlaDefaultsTable.sectionType, sectionType))
    .limit(1);
  return row?.slaDays ?? 7;
}

router.get("/shorfah/sla-defaults", requireAuth, async (_req: Request, res: Response) => {
  const rows = await db.select().from(shorfahSectionSlaDefaultsTable);
  res.json({ defaults: rows });
});

router.put("/shorfah/sla-defaults", requireAdmin, async (req: Request, res: Response) => {
  const { defaults, propagate } = req.body || {};
  if (!Array.isArray(defaults)) return res.status(400).json({ error: "defaults يجب أن يكون مصفوفة" });
  const userId = req.user!.id;
  const now = new Date();
  // السلوك الافتراضي: نعم، يتم تطبيق التغيير على أقسام الأعداد الموجودة التي لم تبدأ بعد.
  const shouldPropagate = propagate !== false;
  let updatedSections = 0;
  for (const row of defaults) {
    if (!row || !row.sectionType) continue;
    const slaDays = Math.max(1, Math.min(60, Number(row.slaDays) || 7));
    const existing = await db.select().from(shorfahSectionSlaDefaultsTable).where(eq(shorfahSectionSlaDefaultsTable.sectionType, row.sectionType)).limit(1);
    if (existing.length) {
      await db.update(shorfahSectionSlaDefaultsTable)
        .set({ slaDays, updatedAt: now, updatedBy: userId })
        .where(eq(shorfahSectionSlaDefaultsTable.sectionType, row.sectionType));
    } else {
      await db.insert(shorfahSectionSlaDefaultsTable).values({
        sectionType: row.sectionType,
        slaDays,
        updatedAt: now,
        updatedBy: userId,
      });
    }
    // تغيير الأقسام التي لا تزال في حالة pending_contribution أو rejected
    if (shouldPropagate) {
      const updated = await db.update(shorfahSectionsTable)
        .set({ slaDays })
        .where(
          and(
            eq(shorfahSectionsTable.sectionType, row.sectionType),
            inArray(shorfahSectionsTable.workflowStatus, ["pending_contribution", "rejected"]),
          )
        )
        .returning({ id: shorfahSectionsTable.id });
      updatedSections += updated.length;
    }
  }
  const rows = await db.select().from(shorfahSectionSlaDefaultsTable);
  res.json({ defaults: rows, propagatedSections: updatedSections });
});

// ── sections ─────────────────────────────────────────────────────────────
router.post("/shorfah/issues/:id/sections", requireAdmin, async (req: Request, res: Response) => {
  const issueId = Number(req.params.id);
  const { sectionType, titleAr, descriptionAr, displayOrder, ownerUserId, ownerRole, autoGenerate, generationPrompt, parentSectionId, slaDays } = req.body || {};
  if (!sectionType || !titleAr) return res.status(400).json({ error: "بيانات ناقصة" });
  // Round 3 Task 1: pull SLA default from per-section-type template
  const defaultDays = slaDays !== undefined ? Number(slaDays) : await getSlaDefaultDays(sectionType);
  const [created] = await db
    .insert(shorfahSectionsTable)
    .values({
      issueId,
      parentSectionId: parentSectionId ?? null,
      sectionType,
      titleAr,
      descriptionAr: descriptionAr ?? null,
      displayOrder: displayOrder ?? 0,
      ownerUserId: ownerUserId ?? null,
      ownerRole: ownerRole ?? null,
      autoGenerate: !!autoGenerate,
      generationPrompt: generationPrompt ?? null,
      slaDays: defaultDays,
    })
    .returning();
  res.json({ section: created });
});

router.patch("/shorfah/sections/:id", requireAuth, async (req: Request, res: Response) => {
  const id = Number(req.params.id);
  const [section] = await db.select().from(shorfahSectionsTable).where(eq(shorfahSectionsTable.id, id)).limit(1);
  if (!section) return res.status(404).json({ error: "القسم غير موجود" });

  const userId = req.user!.id;
  const userRole = req.user!.role || "viewer";

  // Determine allowed fields by permission level
  const isContributor = await canAccessSection(userId, userRole, id, "contribute");
  const isReviewer = await canAccessSection(userId, userRole, id, "review");
  const isApprover = await canAccessSection(userId, userRole, id, "approve");
  const isAdmin = userRole === "super_admin" || userRole === "admin";

  const patch: Record<string, unknown> = { updatedAt: new Date() };

  // content edits require contribute permission (or higher)
  if (req.body.contentMd !== undefined || req.body.contentHtml !== undefined) {
    if (!isContributor && !isReviewer && !isApprover && !isAdmin) {
      return res.status(403).json({ error: "ليس لديك صلاحية لتحرير المحتوى" });
    }
    if (req.body.contentMd !== undefined) patch.contentMd = req.body.contentMd;
    if (req.body.contentHtml !== undefined) patch.contentHtml = req.body.contentHtml;
  }
  // include_in_pdf toggle: reviewer/approver/admin
  if (req.body.includeInPdf !== undefined) {
    if (!isReviewer && !isApprover && !isAdmin) {
      return res.status(403).json({ error: "ليس لديك صلاحية لتغيير حالة العرض في PDF" });
    }
    patch.includeInPdf = !!req.body.includeInPdf;
    await logAction(id, userId, "toggled_include", null, null,
      `include_in_pdf set to ${patch.includeInPdf}`);
  }
  // metadata (title/order): admin only
  if (req.body.titleAr !== undefined || req.body.displayOrder !== undefined || req.body.descriptionAr !== undefined) {
    if (!isAdmin) return res.status(403).json({ error: "تغيير البيانات الأساسية للقسم يحتاج صلاحية إدارية" });
    if (req.body.titleAr !== undefined) patch.titleAr = req.body.titleAr;
    if (req.body.displayOrder !== undefined) patch.displayOrder = req.body.displayOrder;
    if (req.body.descriptionAr !== undefined) patch.descriptionAr = req.body.descriptionAr;
  }
  // SLA fields: admin only
  if (isAdmin) {
    if (req.body.slaDays !== undefined) patch.slaDays = req.body.slaDays;
    if (req.body.slaStartsAt !== undefined) patch.slaStartsAt = new Date(req.body.slaStartsAt);
    if (req.body.slaDeadline !== undefined) patch.slaDeadline = new Date(req.body.slaDeadline);
  }

  const [updated] = await db
    .update(shorfahSectionsTable)
    .set(patch)
    .where(eq(shorfahSectionsTable.id, id))
    .returning();
  res.json({ section: updated });
});

// ── workflow transitions: submit / review / approve / reject ─────────────
router.post("/shorfah/sections/:id/submit", requireAuth, async (req: Request, res: Response) => {
  const id = Number(req.params.id);
  const [section] = await db.select().from(shorfahSectionsTable).where(eq(shorfahSectionsTable.id, id)).limit(1);
  if (!section) return res.status(404).json({ error: "القسم غير موجود" });
  const userId = req.user!.id;
  const userRole = req.user!.role || "viewer";
  const allowed = await canAccessSection(userId, userRole, id, "contribute");
  if (!allowed) return res.status(403).json({ error: "لست مساهماً في هذا القسم" });
  if (!section.contentMd && !section.contentHtml) return res.status(400).json({ error: "أضف المحتوى قبل التسليم" });

  const [updated] = await db.update(shorfahSectionsTable).set({
    workflowStatus: "submitted",
    contributedBy: userId,
    contributedAt: new Date(),
    updatedAt: new Date(),
  }).where(eq(shorfahSectionsTable.id, id)).returning();
  await logAction(id, userId, "submitted", section.workflowStatus, "submitted", "تسليم المحتوى");
  res.json({ section: updated });
});

router.post("/shorfah/sections/:id/review", requireAuth, async (req: Request, res: Response) => {
  const id = Number(req.params.id);
  const { notes, decision } = req.body || {}; // decision: 'pass' | 'reject'
  const [section] = await db.select().from(shorfahSectionsTable).where(eq(shorfahSectionsTable.id, id)).limit(1);
  if (!section) return res.status(404).json({ error: "القسم غير موجود" });
  const userId = req.user!.id;
  const userRole = req.user!.role || "viewer";
  const allowed = await canAccessSection(userId, userRole, id, "review");
  if (!allowed) return res.status(403).json({ error: "ليس لديك صلاحية مراجعة" });

  if (decision === "reject") {
    const [updated] = await db.update(shorfahSectionsTable).set({
      workflowStatus: "rejected",
      reviewedBy: userId,
      reviewedAt: new Date(),
      rejectionReason: notes ?? null,
      updatedAt: new Date(),
    }).where(eq(shorfahSectionsTable.id, id)).returning();
    await logAction(id, userId, "rejected", section.workflowStatus, "rejected", notes);
    return res.json({ section: updated });
  }
  const [updated] = await db.update(shorfahSectionsTable).set({
    workflowStatus: "in_review",
    reviewedBy: userId,
    reviewedAt: new Date(),
    reviewNotes: notes ?? null,
    updatedAt: new Date(),
  }).where(eq(shorfahSectionsTable.id, id)).returning();
  await logAction(id, userId, "reviewed", section.workflowStatus, "in_review", notes);
  res.json({ section: updated });
});

router.post("/shorfah/sections/:id/approve", requireAuth, async (req: Request, res: Response) => {
  const id = Number(req.params.id);
  const [section] = await db.select().from(shorfahSectionsTable).where(eq(shorfahSectionsTable.id, id)).limit(1);
  if (!section) return res.status(404).json({ error: "القسم غير موجود" });
  const userId = req.user!.id;
  const userRole = req.user!.role || "viewer";
  const allowed = await canAccessSection(userId, userRole, id, "approve");
  if (!allowed) return res.status(403).json({ error: "ليس لديك صلاحية اعتماد" });
  const [updated] = await db.update(shorfahSectionsTable).set({
    workflowStatus: "approved",
    approvedBy: userId,
    approvedAt: new Date(),
    updatedAt: new Date(),
  }).where(eq(shorfahSectionsTable.id, id)).returning();
  await logAction(id, userId, "approved", section.workflowStatus, "approved", req.body?.notes);
  res.json({ section: updated });
});

// ── auto-generate news section via Gemini ────────────────────────────────
router.post("/shorfah/sections/:id/generate", requireAdmin, async (req: Request, res: Response) => {
  const id = Number(req.params.id);
  const [section] = await db.select().from(shorfahSectionsTable).where(eq(shorfahSectionsTable.id, id)).limit(1);
  if (!section) return res.status(404).json({ error: "القسم غير موجود" });

  let prompt: string;
  if (section.generationPrompt) {
    prompt = section.generationPrompt;
  } else {
    const typePrompts: Record<string, string> = {
      news: `اكتب 3-4 فقرات أخبارية عن أبرز فعاليات وأخبار الهيئة هذا الشهر. كل فقرة بعنوان H3 وفقرة وصفية قصيرة (60-100 كلمة). النبرة رسمية وموجزة.`,
      office_interview: `اكتب مسودة حوار مع أحد القياديين التنفيذيين في الهيئة، تظهر رؤيته لإدارته وأهدافها. تتألف من: اسم الضيف، عنوان رئيسي، ثم 4-6 أسئلة بعناوين H3 مع إجابات تفصيلية (80-150 كلمة للإجابة).`,
      competition_culture: `أعد لائحة مؤشرات وأرقام عن جهود نشر ثقافة المنافسة خلال الشهر: عدد اللقاءات، عدد المستفيدين، عدد منشورات التواصل، طلبات الخدمات الإلكترونية، نسبة الرضا، مستفيدين من مركز الاتصال. اجعلها في فقرة افتتاحية قصيرة + قائمة بـ 8-10 مؤشرات.`,
      outside_box: `اكتب مقالاً إبداعياً بقلم موظف خبير في أحد المجالات (على سبيل المثال: الموارد البشرية، المالية، أو التحول الرقمي). البداية باسم الضيف ومنصبه وعنوان جذاب، ثم مقال من 4-5 فقرات (300-450 كلمة).`,
      events: `أعد قائمة فعاليات الهيئة لهذا الشهر بعنوان H3 لكل فعالية + فقرة وصفية قصيرة (40-80 كلمة). العدد من 3 إلى 5 فعاليات.`,
      employee_qa: `اختر أحد الموظفين وأجر معه حواراً سريعاً: اسمه ومنصبه، ثم 6 أسئلة سريعة وإجابات قصيرة (جملة أو جملتين). استخدم صيغة: **س: السؤال** ... **ج: الإجابة**.`,
    };
    const guidance = typePrompts[section.sectionType] || `اكتب محتوى مناسباً للقسم.`;
    prompt = `أنت محرر مجلة \"شُرفة\" الشهرية الداخلية للهيئة العامة للمنافسة السعودية.
القسم: \"${section.titleAr}\"
الوصف: ${section.descriptionAr || ""}
المطلوب: ${guidance}
النبرة: رسمية، احترافية، عربية فصحى واضحة.
أرجع JSON بهذا الشكل فقط: { "content_md": "محتوى ماركداون بالعربية" }`;
  }

  try {
    const out = await geminiJSON(prompt, { maxTokens: 2000 });
    const md: string | undefined = (out as Record<string, string>)?.content_md;
    if (!md) return res.status(500).json({ error: "فشل التوليد - الرد فارغ" });
    const [updated] = await db.update(shorfahSectionsTable).set({
      contentMd: md,
      workflowStatus: "submitted",
      contributedBy: req.user!.id,
      contributedAt: new Date(),
      updatedAt: new Date(),
    }).where(eq(shorfahSectionsTable.id, id)).returning();
    await logAction(id, req.user!.id, "contributed", section.workflowStatus, "submitted", "توليد آلي عبر Gemini");
    res.json({ section: updated });
  } catch (e: unknown) {
    res.status(500).json({ error: "فشل التوليد", details: e instanceof Error ? e.message : "unknown" });
  }
});

// ── permissions management ───────────────────────────────────────────────
router.post("/shorfah/sections/:id/permissions", requireAdmin, async (req: Request, res: Response) => {
  const sectionId = Number(req.params.id);
  const { userId, roleName, permission } = req.body || {};
  if (!permission || (!userId && !roleName)) return res.status(400).json({ error: "بيانات ناقصة" });
  const [created] = await db.insert(shorfahSectionPermissionsTable).values({
    sectionId,
    userId: userId ?? null,
    roleName: roleName ?? null,
    permission,
  }).returning();
  res.json({ permission: created });
});

router.delete("/shorfah/permissions/:id", requireAdmin, async (req: Request, res: Response) => {
  const id = Number(req.params.id);
  await db.delete(shorfahSectionPermissionsTable).where(eq(shorfahSectionPermissionsTable.id, id));
  res.json({ ok: true });
});

// ── workflow log ─────────────────────────────────────────────────────────
router.get("/shorfah/sections/:id/log", requireAuth, async (req: Request, res: Response) => {
  const sectionId = Number(req.params.id);
  const logs = await db
    .select()
    .from(shorfahWorkflowLogTable)
    .where(eq(shorfahWorkflowLogTable.sectionId, sectionId))
    .orderBy(desc(shorfahWorkflowLogTable.createdAt));
  res.json({ logs });
});

// ── Section media (photos) ──────────────────────────────────────────────
// GET list media for a section
router.get("/shorfah/sections/:id/media", requireAuth, async (req: Request, res: Response) => {
  const sectionId = Number(req.params.id);
  const media = await db
    .select()
    .from(shorfahSectionMediaTable)
    .where(eq(shorfahSectionMediaTable.sectionId, sectionId))
    .orderBy(asc(shorfahSectionMediaTable.displayOrder));
  res.json({ media });
});

// POST upload media (base64). Body: { dataBase64, contentType, captionAr?, displayOrder? }
router.post("/shorfah/sections/:id/media", requireAuth, async (req: Request, res: Response) => {
  const sectionId = Number(req.params.id);
  const user = (req as any).user as { id: number; role: string };
  // Reuse section permission helper — contributors/reviewers/approvers/admins allowed
  const allowed =
    user.role === "super_admin" || user.role === "admin" ||
    (await canAccessSection(user.id, user.role, sectionId, "contribute")) ||
    (await canAccessSection(user.id, user.role, sectionId, "review")) ||
    (await canAccessSection(user.id, user.role, sectionId, "approve"));
  if (!allowed) return res.status(403).json({ error: "غير مصرح" });

  const { dataBase64, contentType, captionAr, displayOrder } = req.body ?? {};
  if (!dataBase64 || typeof dataBase64 !== "string") {
    return res.status(400).json({ error: "dataBase64 مطلوب" });
  }
  const ct = String(contentType ?? "image/png");
  // Strip data URI prefix if present
  const b64 = dataBase64.includes(",") ? dataBase64.split(",")[1] : dataBase64;
  let buffer: Buffer;
  try { buffer = Buffer.from(b64, "base64"); } catch { return res.status(400).json({ error: "base64 غير صالح" }); }
  // Reject huge payloads (>8MB)
  if (buffer.byteLength > 8 * 1024 * 1024) {
    return res.status(413).json({ error: "الملف كبير جداً (الحد 8 ميجابايت)" });
  }
  const storage = new ObjectStorageService();
  const objectPath = await storage.saveShorfahMedia(buffer, ct, sectionId);
  const [row] = await db
    .insert(shorfahSectionMediaTable)
    .values({
      sectionId,
      mediaUrl: objectPath,
      mediaType: ct.startsWith("image/") ? "image" : "file",
      captionAr: captionAr ?? null,
      displayOrder: Number(displayOrder ?? 0),
    })
    .returning();
  res.json({ media: row });
});

// PATCH update caption / order
router.patch("/shorfah/media/:id", requireAuth, async (req: Request, res: Response) => {
  const id = Number(req.params.id);
  const { captionAr, displayOrder } = req.body ?? {};
  const updates: Record<string, unknown> = {};
  if (captionAr !== undefined) updates.captionAr = captionAr;
  if (displayOrder !== undefined) updates.displayOrder = Number(displayOrder);
  if (Object.keys(updates).length === 0) return res.json({ ok: true, noop: true });
  const [row] = await db
    .update(shorfahSectionMediaTable)
    .set(updates)
    .where(eq(shorfahSectionMediaTable.id, id))
    .returning();
  res.json({ media: row });
});

// DELETE media
router.delete("/shorfah/media/:id", requireAuth, async (req: Request, res: Response) => {
  const id = Number(req.params.id);
  await db.delete(shorfahSectionMediaTable).where(eq(shorfahSectionMediaTable.id, id));
  res.json({ ok: true });
});

// ── PDF generation: HTML preview ─────────────────────────────────────────
router.get("/shorfah/issues/:id/pdf", requireAuth, async (req: Request, res: Response) => {
  const id = Number(req.params.id);
  const [issue] = await db
    .select()
    .from(shorfahIssuesTable)
    .where(eq(shorfahIssuesTable.id, id))
    .limit(1);
  if (!issue) return res.status(404).json({ error: "العدد غير موجود" });

  const isPreview =
    String(req.query.preview ?? "") === "1" ||
    String(req.query.preview ?? "") === "true";
  const whereClause = isPreview
    ? and(
        eq(shorfahSectionsTable.issueId, id),
        eq(shorfahSectionsTable.includeInPdf, true),
      )
    : and(
        eq(shorfahSectionsTable.issueId, id),
        eq(shorfahSectionsTable.includeInPdf, true),
        eq(shorfahSectionsTable.workflowStatus, "approved"),
      );

  const sections = await db
    .select()
    .from(shorfahSectionsTable)
    .where(whereClause)
    .orderBy(asc(shorfahSectionsTable.displayOrder));

  // Load media for all sections in this issue (single query, then group)
  const sectionIds = sections.map((s) => s.id);
  const mediaRows = sectionIds.length > 0
    ? await db
        .select()
        .from(shorfahSectionMediaTable)
        .where(inArray(shorfahSectionMediaTable.sectionId, sectionIds))
        .orderBy(asc(shorfahSectionMediaTable.displayOrder))
    : [];
  const mediaBySection = new Map<number, Array<{ url: string; caption: string | null }>>();
  // Inline images as base64 data URLs so Playwright/PDF render works without network
  const storageSvc = new ObjectStorageService();
  for (const m of mediaRows) {
    if (!mediaBySection.has(m.sectionId)) mediaBySection.set(m.sectionId, []);
    let url = m.mediaUrl;
    try {
      if (m.mediaUrl.startsWith("/objects/")) {
        const file = await storageSvc.getObjectEntityFile(m.mediaUrl);
        const resp = await storageSvc.downloadObject(file);
        const ct = resp.headers.get("content-type") || "image/png";
        const buf = Buffer.from(await resp.arrayBuffer());
        url = `data:${ct};base64,${buf.toString("base64")}`;
      }
    } catch (err) {
      req.log.warn({ err, mediaId: m.id }, "Failed to inline shorfah media; falling back to URL");
    }
    mediaBySection.get(m.sectionId)!.push({ url, caption: m.captionAr ?? null });
  }

  const html = buildShorfahPdfHtml({
    issue: {
      titleAr: issue.titleAr,
      subtitleAr: issue.subtitleAr,
      editorLetter: issue.editorLetter,
      month: issue.month,
      year: issue.year,
      issueNo: issue.issueNo,
      publishedAt: issue.publishedAt,
    },
    sections: sections.map((s) => ({
      sectionType: s.sectionType,
      titleAr: s.titleAr,
      descriptionAr: s.descriptionAr,
      contentMd: s.contentMd,
      media: mediaBySection.get(s.id) ?? [],
    })),
    baseUrl: `${req.protocol}://${req.get("host")}`,
  });

  res.setHeader("Content-Type", "text/html; charset=utf-8");
  res.send(html);
});

// Task 1: Binary PDF download via Puppeteer
router.get("/shorfah/issues/:id/pdf.pdf", requireAuth, async (req: Request, res: Response) => {
  const id = Number(req.params.id);
  const FRONTEND_BASE = process.env.FRONTEND_URL || "https://icbank-platform-internal-comms.vercel.app";
  const arabicMonths = ["يناير","فبراير","مارس","أبريل","مايو","يونيو",
    "يوليو","أغسطس","سبتمبر","أكتوبر","نوفمبر","ديسمبر"];

  try {
  const [issue] = await db
    .select()
    .from(shorfahIssuesTable)
    .where(eq(shorfahIssuesTable.id, id))
    .limit(1);
  if (!issue) return res.status(404).json({ error: "العدد غير موجود" });

  const isPreview =
    String(req.query.preview ?? "") === "1" ||
    String(req.query.preview ?? "") === "true";
  const whereClause = isPreview
    ? and(
        eq(shorfahSectionsTable.issueId, id),
        eq(shorfahSectionsTable.includeInPdf, true),
      )
    : and(
        eq(shorfahSectionsTable.issueId, id),
        eq(shorfahSectionsTable.includeInPdf, true),
        eq(shorfahSectionsTable.workflowStatus, "approved"),
      );

  const sections = await db
    .select()
    .from(shorfahSectionsTable)
    .where(whereClause)
    .orderBy(asc(shorfahSectionsTable.displayOrder));

  // Use absolute URLs for assets so Puppeteer can load them
  const html = buildShorfahPdfHtml({
    issue: {
      titleAr: issue.titleAr,
      subtitleAr: issue.subtitleAr,
      editorLetter: issue.editorLetter,
      month: issue.month,
      year: issue.year,
      issueNo: issue.issueNo,
      publishedAt: issue.publishedAt,
    },
    sections: sections.map((s) => ({
      sectionType: s.sectionType,
      titleAr: s.titleAr,
      descriptionAr: s.descriptionAr,
      contentMd: s.contentMd,
    })),
    baseUrl: FRONTEND_BASE,
  });

  // Use ASCII-only filename for Content-Disposition header compatibility
  const monthNum = String(issue.month).padStart(2, '0');
  const filename = `shorfah-issue-${issue.issueNo}-${issue.year}-${monthNum}.pdf`;

  try {
    // Attempt Puppeteer PDF generation
    const puppeteer = await import("puppeteer-core");
    const fs = await import("node:fs");
    const systemChromium = process.env.PUPPETEER_EXECUTABLE_PATH;
    let executablePath: string;
    let extraArgs: string[] = [];
    let defaultViewport: any = { width: 1240, height: 1754, deviceScaleFactor: 1 };
    if (systemChromium && fs.existsSync(systemChromium)) {
      executablePath = systemChromium;
    } else {
      const chromium = await import("@sparticuz/chromium-min");
      const CHROMIUM_URL = process.env.CHROMIUM_URL ||
        "https://github.com/Sparticuz/chromium/releases/download/v131.0.1/chromium-v131.0.1-pack.tar";
      executablePath = await chromium.default.executablePath(CHROMIUM_URL);
      extraArgs = chromium.default.args;
      defaultViewport = chromium.default.defaultViewport;
    }
    const browser = await puppeteer.default.launch({
      args: [...extraArgs, "--no-sandbox", "--disable-setuid-sandbox", "--disable-dev-shm-usage"],
      defaultViewport,
      executablePath,
      headless: true,
    });

    const page = await browser.newPage();
    
    // Replace relative /shorfah/ image paths with absolute URLs
    const htmlWithAbsUrls = html.replace(
      /src="\/shorfah\//g,
      `src="${FRONTEND_BASE}/shorfah/`
    );
    
    await page.setContent(htmlWithAbsUrls, { waitUntil: "networkidle2" });
    
    const pdfBuffer = await page.pdf({
      format: "A4",
      printBackground: true,
      margin: { top: "0", right: "0", bottom: "0", left: "0" },
    });

    await browser.close();

    res.setHeader("Content-Type", "application/pdf");
    res.setHeader("Content-Disposition", `attachment; filename="${filename}"`);
    res.send(Buffer.from(pdfBuffer));
  } catch (err) {
    console.error("[pdf.pdf] Puppeteer failed, falling back to HTML:", err);
    // Graceful fallback: serve HTML with print CSS and autoprint
    const htmlWithPrint = html.replace(
      "</body>",
      `<script>window.onload=function(){setTimeout(function(){window.print();},800);}</script></body>`
    ).replace(/src="\/shorfah\//g, `src="${FRONTEND_BASE}/shorfah/`);
    res.setHeader("Content-Type", "text/html; charset=utf-8");
    res.setHeader("Content-Disposition", `inline; filename="${filename}"`);
    res.send(htmlWithPrint);
  }
  } catch (outerErr) {
    console.error("[pdf.pdf] Outer error:", outerErr);
    if (!res.headersSent) {
      res.status(500).json({ error: "فشل توليد PDF", details: outerErr instanceof Error ? outerErr.message : "unknown" });
    }
  }
});

// ── Admin: SLA management ────────────────────────────────────────────────
router.get("/shorfah/issues/:id/admin", requireAdmin, async (req: Request, res: Response) => {
  const issueId = Number(req.params.id);
  const sections = await db
    .select()
    .from(shorfahSectionsTable)
    .where(eq(shorfahSectionsTable.issueId, issueId))
    .orderBy(asc(shorfahSectionsTable.displayOrder));

  const sectionIds = sections.map((s) => s.id);
  
  const assignments = sectionIds.length
    ? await db.select().from(shorfahAssignmentsTable)
        .where(inArray(shorfahAssignmentsTable.sectionId, sectionIds))
    : [];
  
  const reminders = sectionIds.length
    ? await db.select().from(shorfahRemindersTable)
        .where(inArray(shorfahRemindersTable.sectionId, sectionIds))
        .orderBy(desc(shorfahRemindersTable.sentAt))
    : [];

  res.json({ sections, assignments, reminders });
});

// Update SLA for a section
router.patch("/shorfah/sections/:id/sla", requireAdmin, async (req: Request, res: Response) => {
  const id = Number(req.params.id);
  const { slaDays, slaStartsAt } = req.body || {};
  const patch: Record<string, unknown> = { updatedAt: new Date() };
  if (slaDays !== undefined) patch.slaDays = Number(slaDays);
  if (slaStartsAt) {
    const starts = new Date(slaStartsAt);
    patch.slaStartsAt = starts;
    const deadline = new Date(starts);
    deadline.setDate(deadline.getDate() + (Number(slaDays) || 7));
    patch.slaDeadline = deadline;
  }
  const [updated] = await db.update(shorfahSectionsTable).set(patch).where(eq(shorfahSectionsTable.id, id)).returning();
  res.json({ section: updated });
});

// Assign contributor to section
router.post("/shorfah/sections/:id/assign", requireAdmin, async (req: Request, res: Response) => {
  const sectionId = Number(req.params.id);
  const { userId, role } = req.body || {};
  if (!userId) return res.status(400).json({ error: "بيانات ناقصة" });
  const [created] = await db.insert(shorfahAssignmentsTable).values({
    sectionId,
    userId: Number(userId),
    role: role ?? "contributor",
  }).returning();
  res.json({ assignment: created });
});

router.delete("/shorfah/assignments/:id", requireAdmin, async (req: Request, res: Response) => {
  const id = Number(req.params.id);
  await db.delete(shorfahAssignmentsTable).where(eq(shorfahAssignmentsTable.id, id));
  res.json({ ok: true });
});

// Send initial messages (sets sla_starts_at + sends notifications)
router.post("/shorfah/issues/:id/send-initial", requireAdmin, async (req: Request, res: Response) => {
  const issueId = Number(req.params.id);
  const [issue] = await db.select().from(shorfahIssuesTable).where(eq(shorfahIssuesTable.id, issueId)).limit(1);
  if (!issue) return res.status(404).json({ error: "العدد غير موجود" });

  const sections = await db.select().from(shorfahSectionsTable).where(eq(shorfahSectionsTable.issueId, issueId));
  const sectionIds = sections.map((s) => s.id);
  const assignments = sectionIds.length
    ? await db.select().from(shorfahAssignmentsTable).where(inArray(shorfahAssignmentsTable.sectionId, sectionIds))
    : [];

  const now = new Date();
  const results: unknown[] = [];

  for (const section of sections) {
    const slaDays = section.slaDays ?? 7;
    const deadline = new Date(now);
    deadline.setDate(deadline.getDate() + slaDays);
    
    await db.update(shorfahSectionsTable).set({
      slaStartsAt: now,
      slaDeadline: deadline,
      updatedAt: now,
    }).where(eq(shorfahSectionsTable.id, section.id));

    const sectionAssignments = assignments.filter((a) => a.sectionId === section.id);
    for (const assignment of sectionAssignments) {
      // Get user details
      const userRec1 = await getUserById(assignment.userId);
      const userEmail = userRec1?.email ?? null;
      const userName = userRec1?.name ?? "المساهم";

      const arabicMonths = ["يناير","فبراير","مارس","أبريل","مايو","يونيو",
        "يوليو","أغسطس","سبتمبر","أكتوبر","نوفمبر","ديسمبر"];
      const monthName = arabicMonths[issue.month - 1] || String(issue.month);
      const deadlineStr = deadline.toLocaleDateString("ar-SA");
      const url = `/#/shorfah/${issueId}`;

      await sendNotification({
        userId: assignment.userId,
        issueId,
        sectionId: section.id,
        channel: "both",
        type: "initial",
        title: `مطلوب مساهمتك في شُرفة — ${section.titleAr}`,
        body: `تمت دعوتك للمساهمة في قسم "${section.titleAr}" من عدد "${issue.titleAr}" (${monthName} ${issue.year}). آخر موعد: ${deadlineStr}`,
        url,
        recipientEmail: userEmail,
        assignmentId: assignment.id,
        reminderType: "initial",
        emailHtml: buildInitialEmailHtml({
          recipientName: userName,
          sectionTitle: section.titleAr,
          issueTitleAr: issue.titleAr,
          deadline: deadlineStr,
          url: `https://icbank-platform-internal-comms.vercel.app${url}`,
        }),
      });

      results.push({ sectionId: section.id, userId: assignment.userId, status: "sent" });
    }
  }

  res.json({ ok: true, sent: results.length, results });
});

// Send manual reminder for a specific assignment/section
router.post("/shorfah/sections/:id/remind", requireAdmin, async (req: Request, res: Response) => {
  const sectionId = Number(req.params.id);
  const { userId } = req.body || {};
  if (!userId) return res.status(400).json({ error: "userId مطلوب" });

  const [section] = await db.select().from(shorfahSectionsTable).where(eq(shorfahSectionsTable.id, sectionId)).limit(1);
  if (!section) return res.status(404).json({ error: "القسم غير موجود" });

  const [issue] = await db.select().from(shorfahIssuesTable).where(eq(shorfahIssuesTable.id, section.issueId)).limit(1);

  const userRec = await getUserById(Number(userId));
  let userEmail: string | null = userRec?.email ?? null;
  let userName: string = userRec?.name ?? "المساهم";

  const now = new Date();
  const daysOverdue = section.slaDeadline
    ? Math.max(0, Math.floor((now.getTime() - section.slaDeadline.getTime()) / 86400000))
    : 0;

  await sendNotification({
    userId: Number(userId),
    issueId: section.issueId,
    sectionId,
    channel: "both",
    type: "reminder_overdue",
    title: `تذكير: قسم "${section.titleAr}" ${daysOverdue > 0 ? `متأخر ${daysOverdue} يوم` : "قيد التجميع"}`,
    body: `يُرجى تسليم المحتوى الخاص بك لقسم "${section.titleAr}" في أقرب وقت.`,
    url: `/#/shorfah/${section.issueId}`,
    recipientEmail: userEmail,
    reminderType: "pre_due",
    emailHtml: buildOverdueEmailHtml({
      recipientName: userName,
      sectionTitle: section.titleAr,
      issueTitleAr: issue?.titleAr || "شرفة",
      daysOverdue,
      url: `https://icbank-platform-internal-comms.vercel.app/#/shorfah/${section.issueId}`,
    }),
  });

  res.json({ ok: true });
});

// ── Notifications API (Task 4) ────────────────────────────────────────────
router.get("/notifications", requireAuth, async (req: Request, res: Response) => {
  const userId = req.user!.id;
  const notifications = await db
    .select()
    .from(shorfahNotificationsTable)
    .where(eq(shorfahNotificationsTable.userId, userId))
    .orderBy(desc(shorfahNotificationsTable.createdAt))
    .limit(30);
  res.json({ notifications });
});

router.post("/notifications/:id/read", requireAuth, async (req: Request, res: Response) => {
  const notifId = Number(req.params.id);
  const userId = req.user!.id;
  await db.update(shorfahNotificationsTable)
    .set({ isRead: true })
    .where(and(
      eq(shorfahNotificationsTable.id, notifId),
      eq(shorfahNotificationsTable.userId, userId),
    ));
  res.json({ ok: true });
});

router.post("/notifications/read-all", requireAuth, async (req: Request, res: Response) => {
  const userId = req.user!.id;
  await db.update(shorfahNotificationsTable)
    .set({ isRead: true })
    .where(eq(shorfahNotificationsTable.userId, userId));
  res.json({ ok: true });
});

// ── Publish (Task 7: fan-out notifications) ───────────────────────────────
router.post("/shorfah/issues/:id/publish", requireAdmin, async (req: Request, res: Response) => {
  const id = Number(req.params.id);
  // ensure at least one approved+included section
  const sections = await db.select().from(shorfahSectionsTable).where(and(
    eq(shorfahSectionsTable.issueId, id),
    eq(shorfahSectionsTable.workflowStatus, "approved"),
    eq(shorfahSectionsTable.includeInPdf, true),
  ));
  if (!sections.length) return res.status(400).json({ error: "لا يوجد أقسام معتمدة ومُفعّلة للنشر" });
  
  const [updated] = await db.update(shorfahIssuesTable).set({
    status: "published",
    publishedAt: new Date(),
    publishedPdfUrl: `/api/shorfah/issues/${id}/pdf.pdf`,
    updatedAt: new Date(),
  }).where(eq(shorfahIssuesTable.id, id)).returning();

  // Task 7: Fan-out notifications to all users
  try {
    const arabicMonths = ["يناير","فبراير","مارس","أبريل","مايو","يونيو",
      "يوليو","أغسطس","سبتمبر","أكتوبر","نوفمبر","ديسمبر"];
    const monthName = arabicMonths[updated.month - 1] || String(updated.month);
    const issueUrl = `/#/shorfah/${id}`;
    const pdfUrl = `/api/shorfah/issues/${id}/pdf.pdf`;

    const allUsers = await getAllUsers();
    
    for (const user of allUsers) {
      const emailHtml = buildPublishEmailHtml({
        issueTitleAr: updated.titleAr,
        month: monthName,
        year: updated.year,
        issueNo: updated.issueNo,
        url: `https://icbank-platform-internal-comms.vercel.app${issueUrl}`,
        pdfUrl: `https://workspaceapi-server-production-9087.up.railway.app${pdfUrl}`,
      });

      await sendNotification({
        userId: user.id,
        issueId: id,
        sectionId: null,
        channel: user.email ? "both" : "in_app",
        type: "published",
        title: "عدد جديد من شُرفة متوفر الآن",
        body: `تفضل بقراءة العدد ${updated.issueNo} — ${monthName} ${updated.year}`,
        url: issueUrl,
        recipientEmail: user.email,
        emailHtml,
      });
    }
    console.log(`[publish] Fan-out sent to ${allUsers.length} users`);
  } catch (err) {
    console.error("[publish] Fan-out error:", err);
    // Don't fail the publish response if fan-out errors
  }

  res.json({ issue: updated });
});

// ────────────────────────────────────────────────────────────────────────
// B1: Word export (DOCX) — alternative to PDF, no Chromium needed
// ────────────────────────────────────────────────────────────────────────
router.get("/shorfah/issues/:id/docx", requireAuth, async (req: Request, res: Response) => {
  const id = Number(req.params.id);
  const arabicMonths = ["يناير","فبراير","مارس","أبريل","مايو","يونيو",
    "يوليو","أغسطس","سبتمبر","أكتوبر","نوفمبر","ديسمبر"];

  try {
    const [issue] = await db
      .select()
      .from(shorfahIssuesTable)
      .where(eq(shorfahIssuesTable.id, id))
      .limit(1);
    if (!issue) return res.status(404).json({ error: "العدد غير موجود" });

    const isPreview =
      String(req.query.preview ?? "") === "1" ||
      String(req.query.preview ?? "") === "true";
    const whereClause = isPreview
      ? and(
          eq(shorfahSectionsTable.issueId, id),
          eq(shorfahSectionsTable.includeInPdf, true),
        )
      : and(
          eq(shorfahSectionsTable.issueId, id),
          eq(shorfahSectionsTable.includeInPdf, true),
          eq(shorfahSectionsTable.workflowStatus, "approved"),
        );

    const sections = await db
      .select()
      .from(shorfahSectionsTable)
      .where(whereClause)
      .orderBy(asc(shorfahSectionsTable.displayOrder));

    const { Document, Packer, Paragraph, TextRun, HeadingLevel, AlignmentType } = await import("docx");

    // Helper: strip markdown to plain text
    const stripMd = (md: string | null | undefined): string => {
      if (!md) return "";
      return String(md)
        .replace(/!\[[^\]]*\]\([^)]+\)/g, "")  // images
        .replace(/\[([^\]]+)\]\([^)]+\)/g, "$1")  // links
        .replace(/[*_`~#>]/g, "")
        .replace(/\n{3,}/g, "\n\n")
        .trim();
    };

    const docChildren: any[] = [];

    // Title page
    docChildren.push(
      new Paragraph({
        children: [new TextRun({ text: issue.titleAr || "شُرفة", bold: true, size: 48, font: "Cairo" })],
        alignment: AlignmentType.CENTER,
        spacing: { before: 400, after: 200 },
        bidirectional: true,
      }),
    );
    if (issue.subtitleAr) {
      docChildren.push(
        new Paragraph({
          children: [new TextRun({ text: issue.subtitleAr, size: 28, font: "Cairo", color: "555555" })],
          alignment: AlignmentType.CENTER,
          spacing: { after: 200 },
          bidirectional: true,
        }),
      );
    }
    docChildren.push(
      new Paragraph({
        children: [new TextRun({
          text: `العدد ${issue.issueNo} · ${arabicMonths[(issue.month || 1) - 1]} ${issue.year}`,
          size: 24, font: "Cairo", color: "888888",
        })],
        alignment: AlignmentType.CENTER,
        spacing: { after: 600 },
        bidirectional: true,
      }),
    );

    // Editor letter
    if (issue.editorLetter) {
      docChildren.push(
        new Paragraph({
          children: [new TextRun({ text: "رسالة رئيس التحرير", bold: true, size: 32, font: "Cairo", color: "0069A7" })],
          heading: HeadingLevel.HEADING_1,
          spacing: { before: 400, after: 200 },
          bidirectional: true,
        }),
      );
      stripMd(issue.editorLetter).split(/\n\n+/).forEach((para) => {
        if (para.trim()) {
          docChildren.push(
            new Paragraph({
              children: [new TextRun({ text: para.trim(), size: 24, font: "Cairo" })],
              spacing: { after: 160, line: 360 },
              bidirectional: true,
            }),
          );
        }
      });
    }

    // Sections
    for (const s of sections) {
      docChildren.push(
        new Paragraph({
          children: [new TextRun({ text: s.titleAr || "(بلا عنوان)", bold: true, size: 32, font: "Cairo", color: "0069A7" })],
          heading: HeadingLevel.HEADING_1,
          spacing: { before: 360, after: 160 },
          bidirectional: true,
        }),
      );
      if (s.descriptionAr) {
        docChildren.push(
          new Paragraph({
            children: [new TextRun({ text: s.descriptionAr, italics: true, size: 22, font: "Cairo", color: "666666" })],
            spacing: { after: 200 },
            bidirectional: true,
          }),
        );
      }
      if (s.contentMd) {
        stripMd(s.contentMd).split(/\n\n+/).forEach((para) => {
          if (para.trim()) {
            docChildren.push(
              new Paragraph({
                children: [new TextRun({ text: para.trim(), size: 24, font: "Cairo" })],
                spacing: { after: 160, line: 360 },
                bidirectional: true,
              }),
            );
          }
        });
      }
    }

    // Footer page
    docChildren.push(
      new Paragraph({
        children: [new TextRun({ text: "· · ·", size: 32, color: "888888" })],
        alignment: AlignmentType.CENTER,
        spacing: { before: 600, after: 200 },
      }),
    );
    docChildren.push(
      new Paragraph({
        children: [new TextRun({
          text: `شُرفة · العدد ${issue.issueNo} · ${arabicMonths[(issue.month || 1) - 1]} ${issue.year}`,
          size: 20, font: "Cairo", color: "888888",
        })],
        alignment: AlignmentType.CENTER,
        bidirectional: true,
      }),
    );

    const doc = new Document({
      creator: "GAC",
      title: issue.titleAr || "شُرفة",
      description: `العدد ${issue.issueNo} — ${arabicMonths[(issue.month || 1) - 1]} ${issue.year}`,
      styles: {
        default: {
          document: { run: { font: "Cairo", size: 24 } },
        },
      },
      sections: [{
        properties: { page: { margin: { top: 1000, right: 1200, bottom: 1000, left: 1200 } } },
        children: docChildren,
      }],
    });

    const buffer = await Packer.toBuffer(doc);
    const monthNum = String(issue.month).padStart(2, "0");
    const filename = `shorfah-issue-${issue.issueNo}-${issue.year}-${monthNum}.docx`;
    res.setHeader("Content-Type", "application/vnd.openxmlformats-officedocument.wordprocessingml.document");
    res.setHeader("Content-Disposition", `attachment; filename="${filename}"`);
    res.setHeader("Content-Length", String(buffer.length));
    res.end(buffer);
  } catch (err: any) {
    console.error("[shorfah/docx] error:", err);
    res.status(500).json({ error: err?.message || "تعذّر توليد ملف Word" });
  }
});

export default router;
