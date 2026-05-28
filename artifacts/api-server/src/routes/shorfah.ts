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

// ── PDF generation (HTML response that print-friendly) ───────────────────
router.get("/shorfah/issues/:id/pdf", requireAuth, async (req: Request, res: Response) => {
  const id = Number(req.params.id);
  const [issue] = await db.select().from(shorfahIssuesTable).where(eq(shorfahIssuesTable.id, id)).limit(1);
  if (!issue) return res.status(404).json({ error: "العدد غير موجود" });
  const isPreview = String(req.query.preview ?? "") === "1" || String(req.query.preview ?? "") === "true";
  const whereClause = isPreview
    ? and(eq(shorfahSectionsTable.issueId, id), eq(shorfahSectionsTable.includeInPdf, true))
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

  const arabicMonth = ["يناير","فبراير","مارس","أبريل","مايو","يونيو","يوليو","أغسطس","سبتمبر","أكتوبر","نوفمبر","ديسمبر"][issue.month - 1];

  // Convert markdown to simple HTML (minimal — bold/italic/lists/headers)
  function mdToHtml(md: string): string {
    let h = md
      .replace(/&/g, "&amp;").replace(/</g, "&lt;").replace(/>/g, "&gt;");
    h = h.replace(/^### (.+)$/gm, "<h3>$1</h3>")
         .replace(/^## (.+)$/gm, "<h2>$1</h2>")
         .replace(/^# (.+)$/gm, "<h1>$1</h1>")
         .replace(/\*\*(.+?)\*\*/g, "<strong>$1</strong>")
         .replace(/\*(.+?)\*/g, "<em>$1</em>")
         .replace(/^- (.+)$/gm, "<li>$1</li>")
         .replace(/(<li>.*<\/li>\n?)+/g, (m) => `<ul>${m}</ul>`)
         .replace(/\n\n+/g, "</p><p>");
    return `<p>${h}</p>`;
  }

  // Arabic labels for tabs/navigation strip (matches printed magazine)
  const sectionLabels: Record<string, string> = {
    news: "أخبارنا",
    office_interview: "في مكتبهم",
    competition_culture: "ثقافة المنافسة",
    outside_box: "خارج الصندوق",
    events: "فعالياتنا",
    employee_qa: "عطنا علومك",
  };
  const allLabels = Object.values(sectionLabels);
  // Inline SVG decorative icons per section (3D-ish using gradients)
  function sectionIcon(type: string): string {
    const grad = `<defs><linearGradient id="g" x1="0" y1="0" x2="1" y2="1"><stop offset="0" stop-color="#bfe2e6"/><stop offset="1" stop-color="#5fa6ad"/></linearGradient></defs>`;
    switch (type) {
      case "news":
        return `<svg viewBox="0 0 120 120" xmlns="http://www.w3.org/2000/svg">${grad}<rect x="18" y="24" width="84" height="72" rx="6" fill="url(#g)" stroke="#2d6470" stroke-width="2"/><line x1="28" y1="42" x2="68" y2="42" stroke="#2d6470" stroke-width="3"/><line x1="28" y1="54" x2="92" y2="54" stroke="#2d6470" stroke-width="2"/><line x1="28" y1="64" x2="92" y2="64" stroke="#2d6470" stroke-width="2"/><rect x="74" y="36" width="22" height="16" fill="#2d6470"/></svg>`;
      case "office_interview":
        return `<svg viewBox="0 0 120 120" xmlns="http://www.w3.org/2000/svg">${grad}<rect x="24" y="58" width="72" height="40" rx="4" fill="url(#g)" stroke="#2d6470" stroke-width="2"/><circle cx="60" cy="40" r="14" fill="#2d6470"/><path d="M40 64 Q60 80 80 64" fill="none" stroke="#2d6470" stroke-width="2"/></svg>`;
      case "competition_culture":
        return `<svg viewBox="0 0 120 120" xmlns="http://www.w3.org/2000/svg">${grad}<circle cx="60" cy="60" r="40" fill="url(#g)" stroke="#2d6470" stroke-width="2"/><text x="60" y="72" text-anchor="middle" fill="#2d6470" font-size="36" font-weight="900">%</text></svg>`;
      case "outside_box":
        return `<svg viewBox="0 0 120 120" xmlns="http://www.w3.org/2000/svg">${grad}<rect x="30" y="50" width="60" height="45" rx="3" fill="url(#g)" stroke="#2d6470" stroke-width="2"/><path d="M70 50 L70 28 L88 28" fill="none" stroke="#2d6470" stroke-width="3"/><path d="M82 22 L90 28 L82 34" fill="none" stroke="#2d6470" stroke-width="3"/></svg>`;
      case "events":
        return `<svg viewBox="0 0 120 120" xmlns="http://www.w3.org/2000/svg">${grad}<polygon points="20,40 100,40 90,60 30,60" fill="url(#g)" stroke="#2d6470" stroke-width="2"/><circle cx="30" cy="75" r="6" fill="#3ec0d0"/><circle cx="50" cy="82" r="6" fill="#a0d8de"/><circle cx="70" cy="75" r="6" fill="#3ec0d0"/><circle cx="90" cy="82" r="6" fill="#a0d8de"/></svg>`;
      case "employee_qa":
        return `<svg viewBox="0 0 120 120" xmlns="http://www.w3.org/2000/svg">${grad}<rect x="22" y="28" width="76" height="56" rx="10" fill="url(#g)" stroke="#2d6470" stroke-width="2"/><polygon points="50,84 60,96 70,84" fill="url(#g)" stroke="#2d6470" stroke-width="2"/><line x1="34" y1="45" x2="86" y2="45" stroke="#fff" stroke-width="3"/><line x1="34" y1="57" x2="86" y2="57" stroke="#fff" stroke-width="3"/><line x1="34" y1="69" x2="68" y2="69" stroke="#fff" stroke-width="3"/></svg>`;
      default:
        return `<svg viewBox="0 0 120 120" xmlns="http://www.w3.org/2000/svg">${grad}<rect x="24" y="24" width="72" height="72" rx="8" fill="url(#g)"/></svg>`;
    }
  }

  // Nav strip — highlight current section
  function navStrip(currentType?: string): string {
    return `<div class="nav-strip">${allLabels
      .map((label) => {
        const isActive =
          currentType && sectionLabels[currentType] === label;
        return `<span class="nav-tab${isActive ? " active" : ""}">${label}</span>`;
      })
      .join("")}</div>`;
  }

  const sectionsHtml = sections
    .map((s) => {
      const themeClass =
        s.sectionType === "competition_culture"
          ? "theme-dark"
          : s.sectionType === "events"
            ? "theme-teal"
            : s.sectionType === "outside_box"
              ? "theme-navy"
              : "theme-light";
      return `
    <section class="shorfah-section ${themeClass}" data-type="${s.sectionType}">
      ${navStrip(s.sectionType)}
      <div class="section-hero">
        <div class="section-icon">${sectionIcon(s.sectionType)}</div>
        <div class="section-headings">
          <h2 class="section-title">${s.titleAr}</h2>
          ${s.descriptionAr ? `<p class="section-desc">${s.descriptionAr}</p>` : ""}
        </div>
      </div>
      <div class="section-content">${mdToHtml(s.contentMd || "")}</div>
    </section>`;
    })
    .join("");

  const tocHtml = sections
    .map(
      (s, idx) => `
    <div class="toc-row"><span class="toc-num">${idx + 1}</span><span class="toc-title">${s.titleAr}</span><span class="toc-dots"></span><span class="toc-type">${sectionLabels[s.sectionType] || ""}</span></div>
  `,
    )
    .join("");

  const html = `<!DOCTYPE html>
<html dir="rtl" lang="ar">
<head>
<meta charset="UTF-8">
<title>${issue.titleAr} — ${arabicMonth} ${issue.year}</title>
<link rel="preconnect" href="https://fonts.googleapis.com">
<link href="https://fonts.googleapis.com/css2?family=Tajawal:wght@400;500;700;900&family=Noto+Sans+Arabic:wght@400;700;900&display=swap" rel="stylesheet">
<style>
  @page { size: A4; margin: 0; }
  * { box-sizing: border-box; }
  body { font-family: "Tajawal", "Noto Sans Arabic", system-ui, sans-serif; color: #0e3b4a; line-height: 1.8; margin: 0; background: #fff; }
  /* Cover — teal gradient matching sample */
  .cover { min-height: 100vh; position: relative; background: linear-gradient(135deg, #1a6e7a 0%, #2a8d9b 60%, #3ec0d0 100%); color: #fff; padding: 50px 60px; page-break-after: always; overflow: hidden; display: flex; flex-direction: column; }
  .cover-top { display: flex; justify-content: space-between; align-items: flex-start; }
  .cover-date { background: #295a72; color: #fff; padding: 14px 22px; border-radius: 0 0 12px 12px; font-size: 28px; font-weight: 900; line-height: 1.1; text-align: center; min-width: 130px; box-shadow: 0 6px 18px rgba(0,0,0,0.18); }
  .cover-date small { display:block; font-size: 18px; font-weight: 600; margin-top: 4px; opacity: 0.9; }
  .cover-brand { text-align: left; font-size: 14px; line-height: 1.4; opacity: 0.95; }
  .cover-brand strong { display: block; font-size: 17px; font-weight: 900; }
  .cover-newspaper { flex: 1; display: flex; align-items: center; justify-content: center; margin: 40px 0; }
  .cover-newspaper-art { width: 320px; height: 200px; background: #eaf3f4; border-radius: 8px; transform: rotate(-3deg); position: relative; box-shadow: 0 12px 30px rgba(0,0,0,0.2); }
  .cover-newspaper-art::after { content:""; position:absolute; right:-12px; top: 12px; width: 30px; height: calc(100% - 24px); background: #cce4e6; border-radius: 8px; transform: rotate(2deg); z-index:-1; }
  .cover-newspaper-art div { padding: 22px; }
  .cover-newspaper-art div b { display:block; height: 8px; background: #5a8c96; margin: 6px 0; border-radius:2px; }
  .cover-newspaper-art div b.short { width: 50%; }
  .cover-newspaper-art div b.title { height: 14px; background: #2d6470; }
  .cover-title { font-size: 120px; font-weight: 900; line-height: 1; letter-spacing: -2px; text-align: center; margin: -20px 0 8px 0; color: #fff; text-shadow: 0 6px 20px rgba(0,0,0,0.15); }
  .cover-subtitle { font-size: 18px; text-align: center; color: #fff; opacity: 0.95; max-width: 640px; margin: 0 auto 24px; font-weight: 500; }
  .cover-tabs { display: flex; justify-content: center; gap: 8px; padding: 14px 0; background: rgba(255,255,255,0.18); border-radius: 8px; margin: 12px 0; flex-wrap: wrap; }
  .cover-tabs span { padding: 8px 14px; background: rgba(255,255,255,0.22); color: #fff; font-size: 13px; font-weight: 700; border-radius: 6px; }
  .cover-motto { background: rgba(255,255,255,0.92); color: #1a6e7a; padding: 18px 28px; text-align: center; font-size: 20px; font-weight: 800; border-radius: 10px; margin-top: auto; box-shadow: 0 8px 24px rgba(0,0,0,0.1); }
  .cover-pattern { position: absolute; bottom: 0; left: 0; right: 0; height: 60px; background-image: repeating-linear-gradient(45deg, transparent, transparent 8px, rgba(255,255,255,0.06) 8px, rgba(255,255,255,0.06) 16px); }

  /* TOC */
  .toc-page { padding: 40px 50px; page-break-after: always; background: #f0f7f8; min-height: calc(100vh - 0px); }
  .toc-page h2 { font-size: 36px; color: #1a6e7a; border-bottom: 4px solid #1a6e7a; padding-bottom: 10px; margin-bottom: 30px; }
  .toc-row { display: flex; align-items: center; gap: 12px; font-size: 17px; padding: 14px 0; border-bottom: 1px dashed #b6cdd0; }
  .toc-num { width: 36px; height: 36px; background: #1a6e7a; color: #fff; border-radius: 50%; display: inline-flex; align-items: center; justify-content: center; font-weight: 900; flex-shrink: 0; }
  .toc-title { font-weight: 700; color: #0e3b4a; flex: 1; }
  .toc-type { font-size: 13px; color: #5a8c96; background: #d6e9eb; padding: 4px 10px; border-radius: 12px; }
  .toc-dots { flex: 1; border-bottom: 2px dotted #b6cdd0; height: 1px; }

  /* Editor letter */
  .editor-letter { padding: 40px 60px; background: #cce4e6; color: #0e3b4a; text-align: center; font-size: 22px; font-weight: 700; line-height: 1.8; page-break-after: always; min-height: 280px; display:flex; align-items:center; justify-content:center; }

  /* Section page */
  .shorfah-section { padding: 0 0 60px; page-break-after: always; min-height: calc(100vh - 40px); position: relative; }
  .nav-strip { display: flex; justify-content: flex-end; gap: 30px; padding: 18px 50px; background: #f0f7f8; border-bottom: 1px solid #d6e9eb; font-size: 14px; }
  .nav-tab { color: #8aa9b0; font-weight: 700; }
  .nav-tab.active { color: #0e3b4a; border-bottom: 3px solid #1a6e7a; padding-bottom: 4px; }
  .section-hero { display: flex; align-items: center; gap: 30px; padding: 40px 50px 30px; }
  .section-icon { width: 180px; height: 180px; flex-shrink: 0; }
  .section-icon svg { width: 100%; height: 100%; }
  .section-headings { flex: 1; }
  .section-title { font-size: 64px; color: #1a6e7a; font-weight: 900; margin: 0 0 8px 0; line-height: 1; }
  .section-desc { color: #5a8c96; font-size: 17px; margin: 0; font-weight: 500; }
  .section-content { padding: 0 50px; font-size: 16px; }
  .section-content h3 { color: #1a6e7a; font-size: 22px; margin-top: 24px; font-weight: 900; border-right: 5px solid #3ec0d0; padding-right: 14px; }
  .section-content ul { padding-right: 24px; list-style: none; }
  .section-content li { margin: 10px 0; padding: 10px 18px; background: #f0f7f8; border-radius: 8px; border-right: 4px solid #3ec0d0; }
  .section-content strong { color: #1a6e7a; }

  /* Themes */
  .theme-dark { background: #0e3b4a; color: #cce4e6; }
  .theme-dark .section-title, .theme-dark .section-content h3, .theme-dark .section-content strong { color: #fff; }
  .theme-dark .section-desc { color: #a0d8de; }
  .theme-dark .section-content li { background: #1a4f60; color: #e2f4f6; border-right-color: #3ec0d0; }
  .theme-dark .nav-strip { background: #0a2c38; color: #5a8c96; border-bottom-color: #1a4f60; }
  .theme-dark .nav-tab.active { color: #fff; border-bottom-color: #3ec0d0; }

  .theme-teal { background: #1a6e7a; color: #fff; }
  .theme-teal .section-title, .theme-teal .section-content h3, .theme-teal .section-content strong { color: #fff; }
  .theme-teal .section-desc { color: #cce4e6; }
  .theme-teal .section-content li { background: rgba(255,255,255,0.12); color: #fff; }
  .theme-teal .nav-strip { background: #155a64; color: #80b0b8; border-bottom-color: #1a6e7a; }
  .theme-teal .nav-tab.active { color: #fff; border-bottom-color: #fff; }

  .theme-navy { background: #0e3b4a; color: #cce4e6; }
  .theme-navy .section-title, .theme-navy .section-content h3, .theme-navy .section-content strong { color: #fff; }
  .theme-navy .section-desc { color: #a0d8de; }
  .theme-navy .section-content li { background: #1a4f60; color: #e2f4f6; }
  .theme-navy .nav-strip { background: #0a2c38; color: #5a8c96; border-bottom-color: #1a4f60; }
  .theme-navy .nav-tab.active { color: #fff; border-bottom-color: #3ec0d0; }

  /* Closing page */
  .closing-page { padding: 80px 60px; background: linear-gradient(180deg, #1a6e7a 0%, #0e3b4a 100%); color: #fff; text-align: center; min-height: 100vh; display:flex; flex-direction:column; align-items:center; justify-content:center; }
  .closing-page h3 { font-size: 64px; font-weight: 900; margin: 0; }
  .closing-page p { font-size: 18px; opacity: 0.95; margin: 16px 0; }
  .closing-stamp { display: inline-block; padding: 16px 32px; border: 3px dashed rgba(255,255,255,0.5); border-radius: 12px; margin-top: 30px; font-weight: 800; font-size: 16px; }

  @media print { .no-print { display: none; } body { -webkit-print-color-adjust: exact; print-color-adjust: exact; } }
</style>
</head>
<body>
  <!-- Cover -->
  <div class="cover">
    <div class="cover-top">
      <div class="cover-date">${arabicMonth}<small>${issue.year}</small></div>
      <div class="cover-brand"><strong>الهيئة العامة للمنافسة</strong>General Authority for Competition</div>
    </div>
    <div class="cover-newspaper">
      <div class="cover-newspaper-art"><div><b class="title"></b><b></b><b class="short"></b><b></b><b class="short"></b></div></div>
    </div>
    <div class="cover-title">شُرفة</div>
    <div class="cover-subtitle">${issue.subtitleAr || "نشرة داخلية شهرية تصدر من الإدارة التنفيذية للتواصل المؤسسي"}</div>
    <div class="cover-tabs">${allLabels.map((l) => `<span>${l}</span>`).join("")}</div>
    <div class="cover-motto">${issue.editorLetter || "بجهودكم تتعزز بيئة المنافسة... وبعملكم يترسخ مبدأ العدالة."}</div>
    <div class="cover-pattern"></div>
  </div>

  <!-- TOC -->
  <div class="toc-page">
    <h2>المحتويات</h2>
    ${tocHtml || '<div style="color:#999">لا توجد أقسام معتمدة في هذا العدد</div>'}
    <div style="margin-top:40px;font-size:13px;color:#5a8c96;text-align:left;">العدد ${issue.issueNo} — ${arabicMonth} ${issue.year}</div>
  </div>

  ${issue.editorLetter ? `<div class="editor-letter">${issue.editorLetter}</div>` : ""}
  ${sectionsHtml}
  <div class="closing-page">
    <h3>شُرفة</h3>
    <p>العدد ${issue.issueNo} — ${arabicMonth} ${issue.year}</p>
    <p>صدر بتاريخ ${issue.publishedAt ? new Date(issue.publishedAt).toLocaleDateString("ar-SA") : "—"}</p>
    <div class="closing-stamp">معتمد ومنشور رسمياً من الإدارة التنفيذية للتواصل المؤسسي — هيئة المنافسة GAC</div>
  </div>
  <script class="no-print">if (location.search.includes("autoprint")) setTimeout(()=>window.print(), 600);</script>
</body>
</html>`;

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
