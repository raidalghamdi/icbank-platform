/**
 * Media Monitoring + Prompt Frameworks routes
 *
 *   GET    /api/media-reports                  → list reports (filter: audience, type)
 *   GET    /api/media-reports/:id              → get single report
 *   POST   /api/media-reports/generate         → AI-generate from social/news feed
 *   POST   /api/media-reports                  → save manual report
 *   DELETE /api/media-reports/:id              → admin: delete
 *
 *   GET    /api/prompts                        → list frameworks (filter: category, kind)
 *   GET    /api/prompts/:id                    → get single
 *   POST   /api/prompts                        → create
 *   PUT    /api/prompts/:id                    → update
 *   DELETE /api/prompts/:id                    → admin: delete
 *   POST   /api/prompts/:id/run                → execute prompt with variables
 */
import { Router, type Request, type Response } from "express";
import { db } from "@workspace/db";
import {
  mediaReportsTable,
  promptFrameworksTable,
  gacSocialPostsTable,
  gacNewsItemsTable,
} from "@workspace/db";
import { and, desc, eq, gte, lte, sql } from "drizzle-orm";
import { z } from "zod";
import { requireAdmin } from "../middleware/auth";
import { geminiText } from "../lib/aiProviders";

const router = Router();

// ─── Helper: format posts as readable text for AI ────────────────────────
function formatPostsForAI(posts: any[], news: any[]): string {
  let out = "";
  if (posts.length > 0) {
    out += "=== منشورات لينكدإن ===\n\n";
    posts.forEach((p, i) => {
      const date = p.postedAt ? new Date(p.postedAt).toLocaleDateString("ar-SA") : "";
      out += `[${i + 1}] ${date}\n${p.contentAr || p.contentEn || ""}\nرابط: ${p.postUrl}\n\n`;
    });
  }
  if (news.length > 0) {
    out += "=== الأخبار والقرارات ===\n\n";
    news.forEach((n, i) => {
      const date = n.publishedAt ? new Date(n.publishedAt).toLocaleDateString("ar-SA") : "";
      out += `[${i + 1}] ${date} | ${n.titleAr}\n${n.bodyAr || ""}\nرابط: ${n.sourceUrl}\n\n`;
    });
  }
  return out || "(لا توجد بيانات في النطاق المحدد)";
}

// Audience-specific prompt templates
function getAudiencePrompt(audience: string): string {
  switch (audience) {
    case "executive":
      return `أنت محلل إعلامي تنفيذي. ولّد ملخصاً تنفيذياً موجزاً للقيادة العليا بصيغة Markdown عربية:

## الملخص التنفيذي
3 نقاط رئيسية فقط (سطر واحد لكل نقطة)

## أبرز رسالة
رسالة مؤسسية واحدة بارزة في الفترة

## نبرة الفترة
سطر واحد يصف النبرة العامة

## التوصية التنفيذية
توصية واحدة قابلة للتنفيذ

استخدم لغة موجزة احترافية. لا تكرر النصوص الخام.`;

    case "analyst":
      return `أنت محلل إعلامي خبير. ولّد تقريراً تفصيلياً شاملاً بصيغة Markdown عربية:

## 1. ملخص الفترة
فقرة (4-6 أسطر) تلخّص النشاط

## 2. تحليل كل منشور
لكل منشور: التاريخ، الموضوع، نبرة الصوت (تنظيمية/ترويجية/توعوية/اجتماعية/دينية)، الهدف المحتمل، التأثير المتوقع

## 3. الموضوعات السائدة
قائمة بأبرز 5-7 موضوعات مع نسبة التكرار

## 4. تحليل النبرة الإجمالي
توزيع نسبي بين الأنواع المختلفة + ملاحظات نوعية

## 5. الفجوات والفرص
- ما المحاور الناقصة؟
- ما الفرص للمحتوى المستقبلي؟

## 6. التوصيات
5 توصيات عملية مرتبة بالأولوية

استخدم لغة دقيقة محايدة. أرفق اقتباسات وأرقاماً حيثما أمكن.`;

    case "manager":
    default:
      return `أنت محلل إعلامي محترف. ولّد تقرير رصد متوازناً للإدارة الوسطى بصيغة Markdown عربية:

## ملخص الفترة
فقرة قصيرة (3 أسطر)

## أبرز المنشورات والأخبار
جدول بأهم 5-7 عناصر: التاريخ | الموضوع | النبرة | الرابط

## تحليل النبرة
- التوزيع بين الأنواع (تنظيمي/ترويجي/توعوي/اجتماعي/...)
- ملاحظات على الاتجاه العام

## الموضوعات الرئيسية
قائمة بـ 3-5 موضوعات مع شرح موجز

## توصيات
3-4 توصيات عملية

استخدم لغة احترافية واضحة. أرفق روابط المنشورات.`;
  }
}

// ─── PUBLIC: GET reports list ───────────────────────────────────────────
router.get("/media-reports", async (req: Request, res: Response) => {
  const audience = typeof req.query["audience"] === "string" ? req.query["audience"].trim() : "";
  const reportType = typeof req.query["type"] === "string" ? req.query["type"].trim() : "";
  const limit = Math.min(Number(req.query["limit"]) || 50, 200);

  const conds = [];
  if (audience) conds.push(eq(mediaReportsTable.audience, audience));
  if (reportType) conds.push(eq(mediaReportsTable.reportType, reportType));
  conds.push(eq(mediaReportsTable.status, "published"));

  const rows = await db
    .select({
      id: mediaReportsTable.id,
      title: mediaReportsTable.title,
      reportType: mediaReportsTable.reportType,
      audience: mediaReportsTable.audience,
      dateFrom: mediaReportsTable.dateFrom,
      dateTo: mediaReportsTable.dateTo,
      sources: mediaReportsTable.sources,
      executiveSummary: mediaReportsTable.executiveSummary,
      overallTone: mediaReportsTable.overallTone,
      stats: mediaReportsTable.stats,
      generatedByName: mediaReportsTable.generatedByName,
      createdAt: mediaReportsTable.createdAt,
    })
    .from(mediaReportsTable)
    .where(and(...conds))
    .orderBy(desc(mediaReportsTable.createdAt))
    .limit(limit);

  res.json({ ok: true, count: rows.length, items: rows });
});

// ─── PUBLIC: GET single report ──────────────────────────────────────────
router.get("/media-reports/:id", async (req: Request, res: Response) => {
  const id = Number(req.params["id"]);
  if (!Number.isFinite(id)) {
    res.status(400).json({ error: "Invalid id" });
    return;
  }
  const [row] = await db
    .select()
    .from(mediaReportsTable)
    .where(eq(mediaReportsTable.id, id))
    .limit(1);
  if (!row) {
    res.status(404).json({ error: "Not found" });
    return;
  }
  res.json({ ok: true, item: row });
});

// ─── GENERATE: AI-create new media report ──────────────────────────────
const GenerateSchema = z.object({
  audience: z.enum(["executive", "manager", "analyst"]).default("manager"),
  reportType: z.enum(["weekly", "monthly", "custom", "adhoc"]).default("weekly"),
  dateFrom: z.string().optional(),
  dateTo: z.string().optional(),
  sources: z.array(z.enum(["linkedin", "twitter", "news"])).default(["linkedin", "news"]),
  customTitle: z.string().optional(),
});

router.post("/media-reports/generate", async (req: Request, res: Response) => {
  try {
    const parsed = GenerateSchema.parse(req.body);
    const now = new Date();
    const defaultFrom =
      parsed.reportType === "monthly"
        ? new Date(now.getTime() - 30 * 24 * 60 * 60 * 1000)
        : new Date(now.getTime() - 7 * 24 * 60 * 60 * 1000);

    const dateFrom = parsed.dateFrom ? new Date(parsed.dateFrom) : defaultFrom;
    const dateTo = parsed.dateTo ? new Date(parsed.dateTo) : now;

    // Fetch data from sources
    const posts: any[] = [];
    const news: any[] = [];

    if (parsed.sources.includes("linkedin") || parsed.sources.includes("twitter")) {
      const platforms = parsed.sources.filter((s) => s === "linkedin" || s === "twitter");
      const rows = await db
        .select()
        .from(gacSocialPostsTable)
        .where(
          and(
            gte(gacSocialPostsTable.postedAt, dateFrom),
            lte(gacSocialPostsTable.postedAt, dateTo),
          ),
        )
        .orderBy(desc(gacSocialPostsTable.postedAt));
      posts.push(...rows.filter((r) => platforms.includes(r.platform as any)));
    }

    if (parsed.sources.includes("news")) {
      const rows = await db
        .select()
        .from(gacNewsItemsTable)
        .where(
          and(
            gte(gacNewsItemsTable.publishedAt, dateFrom),
            lte(gacNewsItemsTable.publishedAt, dateTo),
          ),
        )
        .orderBy(desc(gacNewsItemsTable.publishedAt));
      news.push(...rows);
    }

    const totalItems = posts.length + news.length;

    // Build prompt
    const systemPrompt = getAudiencePrompt(parsed.audience);
    const dataText = formatPostsForAI(posts, news);
    const fromStr = dateFrom.toISOString().slice(0, 10);
    const toStr = dateTo.toISOString().slice(0, 10);

    let contentMd = "";
    let executiveSummary = "";
    let overallTone = "";

    if (totalItems === 0) {
      contentMd = `## تقرير الرصد — ${fromStr} إلى ${toStr}\n\n**لا توجد بيانات في النطاق الزمني المحدد.**\n\nالمصادر المفحوصة: ${parsed.sources.join("، ")}\n\nاقتراح: قم بتشغيل مغذي البيانات أو وسّع النطاق الزمني.`;
      executiveSummary = "لا توجد بيانات في الفترة المحددة.";
      overallTone = "غير متوفر";
    } else {
      const fullPrompt = `${systemPrompt}\n\n=== البيانات للفترة ${fromStr} → ${toStr} ===\n\n${dataText}`;
      contentMd = await geminiText(fullPrompt, { maxTokens: 4096 });

      // Generate exec summary separately (short version)
      const summaryPrompt = `لخّص هذه البيانات في 2-3 أسطر فقط بالعربية (ملخص تنفيذي قصير):\n\n${dataText.slice(0, 3000)}`;
      executiveSummary = await geminiText(summaryPrompt, { maxTokens: 300 });

      // Determine overall tone
      const tonePrompt = `حدد نبرة الصوت الإجمالية لهذه المنشورات في كلمتين عربيتين فقط (مثال: "مؤسسية دافئة" أو "تنظيمية رسمية"):\n\n${dataText.slice(0, 2000)}`;
      overallTone = (await geminiText(tonePrompt, { maxTokens: 50 })).trim().split("\n")[0] || "";
    }

    const user = (req as any).user;
    const title =
      parsed.customTitle ||
      `تقرير ${parsed.reportType === "weekly" ? "أسبوعي" : parsed.reportType === "monthly" ? "شهري" : "مخصص"} — ${fromStr} إلى ${toStr}`;

    const [saved] = await db
      .insert(mediaReportsTable)
      .values({
        title,
        reportType: parsed.reportType,
        audience: parsed.audience,
        dateFrom,
        dateTo,
        sources: parsed.sources,
        executiveSummary,
        contentMd,
        overallTone,
        stats: {
          totalPosts: totalItems,
          linkedinCount: posts.filter((p) => p.platform === "linkedin").length,
          newsCount: news.length,
        },
        sourceItems: [
          ...posts.map((p) => ({ type: "social", id: p.id, url: p.postUrl, date: p.postedAt })),
          ...news.map((n) => ({ type: "news", id: n.id, url: n.sourceUrl, date: n.publishedAt })),
        ],
        generatedByUserId: user?.id ?? null,
        generatedByName: user?.name ?? user?.email ?? "system",
        aiModel: "gemini-2.5-flash",
        status: "published",
      })
      .returning();

    res.json({ ok: true, item: saved });
  } catch (err) {
    console.error("[media-reports/generate]", err);
    res.status(500).json({
      error: "Generation failed",
      details: err instanceof Error ? err.message : String(err),
    });
  }
});

// ─── ADMIN: delete report ──────────────────────────────────────────────
router.delete("/media-reports/:id", requireAdmin, async (req: Request, res: Response) => {
  const id = Number(req.params["id"]);
  if (!Number.isFinite(id)) {
    res.status(400).json({ error: "Invalid id" });
    return;
  }
  await db.delete(mediaReportsTable).where(eq(mediaReportsTable.id, id));
  res.json({ ok: true });
});

// ═══════════════════════════════════════════════════════════════════════
// PROMPT FRAMEWORKS
// ═══════════════════════════════════════════════════════════════════════

// GET list
router.get("/prompts", async (req: Request, res: Response) => {
  const category = typeof req.query["category"] === "string" ? req.query["category"].trim() : "";
  const kind = typeof req.query["kind"] === "string" ? req.query["kind"].trim() : "";

  const conds = [eq(promptFrameworksTable.status, "active")];
  if (category) conds.push(eq(promptFrameworksTable.category, category));
  if (kind) conds.push(eq(promptFrameworksTable.kind, kind));

  const rows = await db
    .select()
    .from(promptFrameworksTable)
    .where(and(...conds))
    .orderBy(desc(promptFrameworksTable.isApproved), desc(promptFrameworksTable.usageCount));

  res.json({ ok: true, count: rows.length, items: rows });
});

// GET single
router.get("/prompts/:id", async (req: Request, res: Response) => {
  const id = Number(req.params["id"]);
  if (!Number.isFinite(id)) {
    res.status(400).json({ error: "Invalid id" });
    return;
  }
  const [row] = await db
    .select()
    .from(promptFrameworksTable)
    .where(eq(promptFrameworksTable.id, id))
    .limit(1);
  if (!row) {
    res.status(404).json({ error: "Not found" });
    return;
  }
  res.json({ ok: true, item: row });
});

// CREATE
const PromptCreateSchema = z.object({
  nameAr: z.string().min(1),
  nameEn: z.string().optional(),
  descriptionAr: z.string().optional(),
  category: z.string().default("content-creation"),
  kind: z.enum(["framework", "template"]).default("framework"),
  promptText: z.string().min(10),
  variables: z
    .array(
      z.object({
        key: z.string(),
        label: z.string(),
        type: z.string().optional(),
        required: z.boolean().optional(),
      }),
    )
    .default([]),
  exampleInput: z.string().optional(),
  exampleOutput: z.string().optional(),
  tags: z.array(z.string()).default([]),
  recommendedModel: z.string().optional(),
  isApproved: z.boolean().optional(),
});

router.post("/prompts", async (req: Request, res: Response) => {
  try {
    const parsed = PromptCreateSchema.parse(req.body);
    const user = (req as any).user;
    const [created] = await db
      .insert(promptFrameworksTable)
      .values({
        ...parsed,
        createdByUserId: user?.id ?? null,
        createdByName: user?.name ?? user?.email ?? "system",
      })
      .returning();
    res.json({ ok: true, item: created });
  } catch (err) {
    res.status(400).json({
      error: "Invalid input",
      details: err instanceof Error ? err.message : String(err),
    });
  }
});

// UPDATE
router.put("/prompts/:id", async (req: Request, res: Response) => {
  const id = Number(req.params["id"]);
  if (!Number.isFinite(id)) {
    res.status(400).json({ error: "Invalid id" });
    return;
  }
  try {
    const parsed = PromptCreateSchema.partial().parse(req.body);
    const [updated] = await db
      .update(promptFrameworksTable)
      .set({ ...parsed, updatedAt: new Date() })
      .where(eq(promptFrameworksTable.id, id))
      .returning();
    res.json({ ok: true, item: updated });
  } catch (err) {
    res.status(400).json({
      error: "Invalid input",
      details: err instanceof Error ? err.message : String(err),
    });
  }
});

// DELETE
router.delete("/prompts/:id", requireAdmin, async (req: Request, res: Response) => {
  const id = Number(req.params["id"]);
  if (!Number.isFinite(id)) {
    res.status(400).json({ error: "Invalid id" });
    return;
  }
  await db.delete(promptFrameworksTable).where(eq(promptFrameworksTable.id, id));
  res.json({ ok: true });
});

// RUN: execute prompt with provided variables
router.post("/prompts/:id/run", async (req: Request, res: Response) => {
  const id = Number(req.params["id"]);
  if (!Number.isFinite(id)) {
    res.status(400).json({ error: "Invalid id" });
    return;
  }
  try {
    const [pf] = await db
      .select()
      .from(promptFrameworksTable)
      .where(eq(promptFrameworksTable.id, id))
      .limit(1);
    if (!pf) {
      res.status(404).json({ error: "Not found" });
      return;
    }
    const values = (req.body?.variables ?? {}) as Record<string, string>;

    // Substitute {{key}} with values
    let prompt = pf.promptText;
    const vars = (pf.variables ?? []) as Array<{ key: string; label: string; required?: boolean }>;
    for (const v of vars) {
      const val = values[v.key];
      if (v.required && !val) {
        res.status(400).json({ error: `Missing required variable: ${v.key}` });
        return;
      }
      prompt = prompt.replace(new RegExp(`{{\\s*${v.key}\\s*}}`, "g"), val || "");
    }

    const output = await geminiText(prompt, { maxTokens: 4096 });

    // Increment usage
    await db
      .update(promptFrameworksTable)
      .set({ usageCount: sql`${promptFrameworksTable.usageCount} + 1` })
      .where(eq(promptFrameworksTable.id, id));

    res.json({ ok: true, output, promptSent: prompt.slice(0, 200) + "..." });
  } catch (err) {
    res.status(500).json({
      error: "Execution failed",
      details: err instanceof Error ? err.message : String(err),
    });
  }
});

// Review Round 2: quick smart-assistant endpoint — direct prompt without saving
router.post("/ai/quick", async (req: Request, res: Response) => {
  try {
    const { tool, input, tone, count } = (req.body ?? {}) as { tool?: string; input?: string; tone?: string; count?: number };
    if (!tool || !input) {
      res.status(400).json({ error: "tool and input are required" });
      return;
    }
    let prompt = "";
    switch (tool) {
      case "generate":
        prompt = `أنت محرر محتوى محترف في هيئة حكومية. اكتب محتوى عربي واضح ومتماسك عن الموضوع التالي${tone?` بنبرة ${tone}`:""}:\n\n${input}`;
        break;
      case "tone":
        prompt = `أعد صياغة النص التالي بنبرة ${tone||"رسمية"}، مع الحفاظ على المعنى:\n\n${input}`;
        break;
      case "rephrase":
        prompt = `حسّن صياغة هذه الفقرة لتكون أكثر وضوحاً واحترافية:\n\n${input}`;
        break;
      case "rewrite":
        prompt = `أعد كتابة النص التالي بأسلوب مختلف مع الحفاظ على الرسالة الأساسية:\n\n${input}`;
        break;
      case "headlines":
        prompt = `اقترح ${count||8} عناوين إعلامية جذّابة ومولحة لمحتوى عن:\n\n${input}\n\nرتّبها في قائمة مرقّمة.`;
        break;
      case "summary":
        prompt = `لخّص النص التالي في 3–5 نقاط رئيسية:\n\n${input}`;
        break;
      case "messages":
        prompt = `حسّن رسالة التواصل التالية لتكون أكثر احترافية ووضوحاً${tone?` وبنبرة ${tone}`:""}:\n\n${input}`;
        break;
      default:
        res.status(400).json({ error: "Unknown tool: " + tool });
        return;
    }
    const output = await geminiText(prompt, { maxTokens: 2048 });
    res.json({ ok: true, output, tool });
  } catch (err) {
    res.status(500).json({ error: "Failed", details: err instanceof Error ? err.message : String(err) });
  }
});

export default router;
