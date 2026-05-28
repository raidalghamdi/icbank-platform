import { Router, type Request, type Response } from "express";
import { db } from "@workspace/db";
import {
  shorfahIssuesTable,
  shorfahSectionsTable,
  shorfahSectionPermissionsTable,
  shorfahSectionMediaTable,
  shorfahWorkflowLogTable,
} from "@workspace/db";
import { eq, desc, asc, and, or } from "drizzle-orm";
import { requireAuth, requireAdmin } from "../middleware/auth";
import { geminiJSON } from "../lib/aiProviders";
import { buildShorfahPdfHtml } from "./shorfah-pdf";

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

router.post("/shorfah/issues", requireAdmin, async (req: Request, res: Response) => {
  const { issueNo, titleAr, subtitleAr, month, year, contributionsOpenAt, contributionsCloseAt, editorLetter } = req.body || {};
  if (!issueNo || !titleAr || !month || !year) {
    return res.status(400).json({ error: "بيانات ناقصة" });
  }
  const [created] = await db
    .insert(shorfahIssuesTable)
    .values({
      issueNo,
      titleAr,
      subtitleAr: subtitleAr ?? null,
      month,
      year,
      contributionsOpenAt: contributionsOpenAt ? new Date(contributionsOpenAt) : null,
      contributionsCloseAt: contributionsCloseAt ? new Date(contributionsCloseAt) : null,
      editorLetter: editorLetter ?? null,
      createdBy: req.user!.id,
    })
    .returning();
  res.json({ issue: created });
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

// ── sections ─────────────────────────────────────────────────────────────
router.post("/shorfah/issues/:id/sections", requireAdmin, async (req: Request, res: Response) => {
  const issueId = Number(req.params.id);
  const { sectionType, titleAr, descriptionAr, displayOrder, ownerUserId, ownerRole, autoGenerate, generationPrompt, parentSectionId } = req.body || {};
  if (!sectionType || !titleAr) return res.status(400).json({ error: "بيانات ناقصة" });
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
      local_news: `اكتب قسماً يغطي أبرز أخبار وقرارات هيئات المنافسة محلياً داخل المملكة.`,
      regional_news: `اكتب قسماً يغطي أبرز أخبار هيئات المنافسة خليجياً وعربياً.`,
      global_news: `اكتب قسماً يغطي أبرز أخبار وقرارات هيئات المنافسة عالمياً.`,
    };
    const guidance = typePrompts[section.sectionType] || `اكتب محتوى مناسباً للقسم.`;
    prompt = `أنت محرر مجلة "شُرفة" الشهرية الداخلية للهيئة العامة للمنافسة السعودية.
القسم: "${section.titleAr}"
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

// ── PDF generation — branded HTML matching the printed sample ────────────
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
  });

  res.setHeader("Content-Type", "text/html; charset=utf-8");
  res.send(html);
});

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
    publishedPdfUrl: `/api/shorfah/issues/${id}/pdf`,
    updatedAt: new Date(),
  }).where(eq(shorfahIssuesTable.id, id)).returning();
  res.json({ issue: updated });
});

export default router;
