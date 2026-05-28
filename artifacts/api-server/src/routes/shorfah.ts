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
  usersTable,
} from "@workspace/db";
import { eq, desc, asc, and, or, inArray } from "drizzle-orm";
import { requireAuth, requireAdmin } from "../middleware/auth";
import { geminiJSON } from "../lib/aiProviders";
import { buildShorfahPdfHtml } from "./shorfah-pdf";
import { sendNotification, buildPublishEmailHtml, buildOverdueEmailHtml, buildInitialEmailHtml } from "../lib/notify";

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
      status: "collecting",
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
    const chromium = await import("@sparticuz/chromium-min");
    const puppeteer = await import("puppeteer-core");

    // Use a public CDN for the chromium binary to avoid large bundle issues on Railway
    const CHROMIUM_URL = process.env.CHROMIUM_URL || 
      "https://github.com/Sparticuz/chromium/releases/download/v131.0.1/chromium-v131.0.1-pack.tar";
    
    const executablePath = await chromium.default.executablePath(CHROMIUM_URL);
    
    const browser = await puppeteer.default.launch({
      args: [...chromium.default.args, "--no-sandbox", "--disable-setuid-sandbox"],
      defaultViewport: chromium.default.defaultViewport,
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

export default router;
