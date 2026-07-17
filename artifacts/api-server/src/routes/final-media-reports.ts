/**
 * Final Media Reports (immutable, official-template aligned)
 *
 *   GET    /api/final-media-reports                  → list (public read)
 *   GET    /api/final-media-reports/:id              → single (public read)
 *   POST   /api/final-media-reports/generate         → AI-generate 8-section structured report
 *   POST   /api/final-media-reports                  → admin: lock + save final
 *   POST   /api/final-media-reports/:id/export-pdf   → render PDF (puppeteer)
 *   POST   /api/final-media-reports/:id/send-email   → email to recipients (resend)
 *   POST   /api/final-media-reports/:id/exec-summary → regenerate executive summary
 *   POST   /api/final-media-reports/search           → search archive (full report OR specific info via Gemini)
 *   POST   /api/qa-queries                           → log wizard answers (audit trail)
 *
 *   NOTE: NO PUT, NO DELETE — final reports are immutable.
 */
import { Router, type Request, type Response } from "express";
import { db } from "@workspace/db";
import {
  finalMediaReportsTable,
  reportsQaQueriesTable,
  gacSocialPostsTable,
  gacNewsItemsTable,
} from "@workspace/db";
import { and, desc, eq, gte, lte, sql, ilike, or } from "drizzle-orm";
import { z } from "zod";
import * as crypto from "crypto";
import { requireAdmin } from "../middleware/auth";
import { geminiText, geminiJSON, aiJSONWithFallback } from "../lib/aiProviders";
import { buildFinalReportHtml } from "./final-media-reports-html";

const router = Router();

// ─── Helper: produce sequential report number GAC-MEDIA-{n}/{year} ────────
async function nextReportNumber(): Promise<string> {
  const year = new Date().getFullYear();
  const rows = await db
    .select({ rn: finalMediaReportsTable.reportNumber })
    .from(finalMediaReportsTable)
    .where(ilike(finalMediaReportsTable.reportNumber, `GAC-MEDIA-%/${year}`));
  let maxN = 0;
  for (const r of rows) {
    const m = /GAC-MEDIA-(\d+)\//.exec(r.rn);
    if (m) maxN = Math.max(maxN, parseInt(m[1], 10));
  }
  return `GAC-MEDIA-${maxN + 1}/${year}`;
}

// ─── Helper: format raw items as bilingual context for AI ─────────────────
function formatFeedForAI(posts: any[], news: any[]): string {
  let out = "";
  if (news.length > 0) {
    out += "=== الأخبار والقرارات ===\n\n";
    news.forEach((n, i) => {
      const date = n.publishedAt ? new Date(n.publishedAt).toISOString().slice(0, 10) : "";
      out += `[N${i + 1}] ${date} | ${n.titleAr || ""}\n${(n.bodyAr || "").slice(0, 600)}\nالمصدر: ${n.sourceName || ""} | ${n.sourceUrl || ""}\n\n`;
    });
  }
  if (posts.length > 0) {
    out += "=== منشورات لينكدإن ===\n\n";
    posts.forEach((p, i) => {
      const date = p.postedAt ? new Date(p.postedAt).toISOString().slice(0, 10) : "";
      out += `[L${i + 1}] ${date}\n${(p.contentAr || p.contentEn || "").slice(0, 500)}\nرابط: ${p.postUrl || ""}\n\n`;
    });
  }
  return out || "(لا توجد بيانات في النطاق المحدد)";
}

// ─── Helper: build canonical 8-section AI prompt ──────────────────────────
function buildEightSectionPrompt(opts: {
  periodLabel: string;
  audience: string;
  focusTopics?: string;
  feed: string;
}): string {
  return `أنت محلل إعلامي تنفيذي خبير في الإدارة التنفيذية للتواصل المؤسسي بالهيئة العامة للمنافسة (GAC) — المملكة العربية السعودية.
مهمتك: توليد تقرير رصد إعلامي رسمي يطابق القالب التالي بدقة (8 أقسام مرقمة).

الفترة: ${opts.periodLabel}
الجمهور المستهدف: ${opts.audience}
${opts.focusTopics ? `موضوعات التركيز: ${opts.focusTopics}` : ""}

البيانات المرصودة:
${opts.feed}

أنتج **كائن JSON واحد فقط** (بدون نص خارج JSON، بدون \`\`\`) بهذا الشكل بالضبط:

{
  "executiveSummary": "فقرة 3-5 أسطر تلخّص الفترة بنبرة احترافية رسمية، تذكر أبرز الإنجازات والقرارات والمؤشرات",
  "kpis": {
    "totalNews": <رقم إجمالي الأخبار المرصودة>,
    "positivePercent": <نسبة التغطية الإيجابية كرقم 0-100>,
    "mediaOutlets": <عدد الوسائل الإعلامية المتميزة>,
    "keyTopics": <عدد الموضوعات الرئيسية>,
    "reach": "تقدير الوصول الجماهيري مثل '7.2 م'",
    "alertsCount": <عدد التنبيهات التي تستوجب متابعة>
  },
  "topNews": [
    {
      "date": "YYYY-MM-DD",
      "tone": "إيجابي|محايد|سلبي",
      "headline": "عنوان الخبر",
      "details": ["تفصيل 1", "تفصيل 2", "تفصيل 3"],
      "source": "اسم المصدر"
    }
    // 5-8 أخبار بارزة
  ],
  "timeline": [
    {
      "date": "YYYY-MM-DD",
      "event": "ملخص الحدث",
      "outlet": "الوسيلة",
      "tone": "إيجابي|محايد|سلبي",
      "count": <عدد الإشارات>
    }
    // كل التواريخ الرئيسية بالترتيب الزمني
  ],
  "digitalPresence": {
    "platforms": [
      { "name": "إكس", "mentions": <رقم>, "reposts": <رقم>, "engagement": <رقم>, "reach": "تقديري مثل 1.2 م" },
      { "name": "لينكدإن", "mentions": <رقم>, "reposts": <رقم>, "engagement": <رقم>, "reach": "تقديري" },
      { "name": "تليجرام", "mentions": <رقم>, "reposts": <رقم>, "engagement": <رقم>, "reach": "تقديري" },
      { "name": "يوتيوب", "mentions": <رقم>, "reposts": <رقم>, "engagement": <رقم>, "reach": "تقديري" }
    ],
    "hashtags": [
      { "tag": "#الهيئة_العامة_للمنافسة", "uses": <رقم>, "trend": "صاعد|ثابت|نازل" }
      // 4-7 وسوم
    ]
  },
  "editorialTone": {
    "distribution": [
      { "tone": "إيجابي", "percent": <0-100>, "count": <رقم> },
      { "tone": "محايد", "percent": <0-100>, "count": <رقم> },
      { "tone": "سلبي", "percent": <0-100>, "count": <رقم> }
    ],
    "classification": [
      { "topic": "قرارات تركز", "percent": <0-100>, "count": <رقم> },
      { "topic": "ملفات تحقيق", "percent": <0-100>, "count": <رقم> },
      { "topic": "تنظيم سوق", "percent": <0-100>, "count": <رقم> }
      // أهم 4-6 تصنيفات موضوعية
    ],
    "sources": [
      { "source": "صحف يومية", "percent": <0-100>, "count": <رقم> },
      { "source": "مواقع اقتصادية", "percent": <0-100>, "count": <رقم> },
      { "source": "تواصل اجتماعي", "percent": <0-100>, "count": <رقم> },
      { "source": "إعلام دولي", "percent": <0-100>, "count": <رقم> }
    ]
  },
  "deepAnalysis": {
    "keywords": [
      { "keyword": "المنافسة", "frequency": <رقم>, "context": "السياق الذي وردت فيه" }
      // 6-10 كلمات مفتاحية
    ],
    "quote": {
      "text": "اقتباس بارز من المسؤولين",
      "source": "مصدر الاقتباس",
      "date": "YYYY-MM-DD"
    },
    "strengths": ["نقطة قوة 1", "نقطة قوة 2", "نقطة قوة 3"],
    "weaknesses": ["نقطة ضعف أو مخاطرة 1", "نقطة ضعف 2"]
  },
  "regionalComparison": [
    {
      "authority": "هيئة المنافسة الإماراتية",
      "country": "الإمارات",
      "mentions": <رقم>,
      "tone": "إيجابي|محايد|سلبي",
      "highlights": "أبرز ما تم تداوله"
    }
    // 3-5 هيئات إقليمية مماثلة
  ],
  "recommendations": [
    {
      "title": "عنوان التوصية",
      "description": "وصف موجز",
      "priority": "عالية|متوسطة|منخفضة",
      "responsible": "الجهة المسؤولة",
      "kpi": "مؤشر القياس",
      "deadline": "نص الموعد مثل خلال أسبوعين",
      "dependencies": "التبعيات"
    }
    // 4-6 توصيات عملية
  ],
  "alerts": [
    {
      "alert": "وصف التنبيه أو الموقف الذي يستوجب المتابعة",
      "suggestedPosition": "الموقف المقترح للهيئة"
    }
    // 2-4 تنبيهات
  ],
  "quotesAppendix": [
    {
      "quote": "نص الاقتباس",
      "source": "اسم المصدر مع التاريخ",
      "date": "YYYY-MM-DD",
      "topic": "الموضوع"
    }
    // 4-8 اقتباسات بارزة
  ],
  "methodology": "فقرة قصيرة تصف منهجية الرصد المعتمدة",
  "sources": [
    { "name": "اسم المصدر", "url": "https://...", "description": "وصف موجز" }
    // كل المصادر الرئيسية المعتمدة
  ]
}

⚠️ القواعد الصارمة:
- أنتج JSON صالح فقط — بدون أي نص خارجه
- اعتمد فقط على البيانات المرصودة المرفقة — لا تختلق أرقاماً أو أخباراً
- إذا لم تتوفر بيانات لقسم معين، أنتج مصفوفة فارغة [] أو null حسب السياق
- استخدم اللغة العربية الفصحى الرسمية في كل النصوص`;
}

// ─── Helper: parse AI response robustly ───────────────────────────────────
function parseAIJson(raw: string): any {
  let txt = (raw || "").trim();
  // Strip code fences
  txt = txt.replace(/^```(?:json)?\s*/i, "").replace(/```\s*$/, "");
  // Find first { and last }
  const first = txt.indexOf("{");
  const last = txt.lastIndexOf("}");
  if (first !== -1 && last !== -1) txt = txt.slice(first, last + 1);
  try {
    return JSON.parse(txt);
  } catch (err) {
    console.error("[final-media-reports] JSON parse failed:", err);
    throw new Error("AI response was not valid JSON");
  }
}

// ─── Helper: integrity hash ───────────────────────────────────────────────
function computeSha256(obj: unknown): string {
  return crypto.createHash("sha256").update(JSON.stringify(obj)).digest("hex");
}

// ─── Helper: arabic month name ────────────────────────────────────────────
const ARABIC_MONTHS = [
  "يناير", "فبراير", "مارس", "أبريل", "مايو", "يونيو",
  "يوليو", "أغسطس", "سبتمبر", "أكتوبر", "نوفمبر", "ديسمبر",
];
function periodLabelFor(from: Date, to: Date, type: string): string {
  const m = ARABIC_MONTHS[from.getMonth()];
  const y = from.getFullYear();
  if (type === "monthly") return `${m} ${y}`;
  if (type === "weekly") return `${from.getDate()} - ${to.getDate()} ${m} ${y}`;
  return `${from.getDate()}/${from.getMonth() + 1} - ${to.getDate()}/${to.getMonth() + 1} ${y}`;
}

// ─── GET /api/final-media-reports — list (public read) ────────────────────
router.get("/final-media-reports", async (req: Request, res: Response) => {
  try {
    const { type, year } = req.query as { type?: string; year?: string };
    const conds: any[] = [];
    if (type) conds.push(eq(finalMediaReportsTable.reportType, type));
    if (year) {
      const y = parseInt(year, 10);
      if (!isNaN(y)) {
        conds.push(gte(finalMediaReportsTable.dateFrom, new Date(`${y}-01-01`)));
        conds.push(lte(finalMediaReportsTable.dateFrom, new Date(`${y}-12-31`)));
      }
    }
    const rows = await db
      .select({
        id: finalMediaReportsTable.id,
        reportNumber: finalMediaReportsTable.reportNumber,
        title: finalMediaReportsTable.title,
        reportType: finalMediaReportsTable.reportType,
        periodLabel: finalMediaReportsTable.periodLabel,
        dateFrom: finalMediaReportsTable.dateFrom,
        dateTo: finalMediaReportsTable.dateTo,
        kpis: finalMediaReportsTable.kpis,
        executiveSummary: finalMediaReportsTable.executiveSummary,
        status: finalMediaReportsTable.status,
        lockedAt: finalMediaReportsTable.lockedAt,
        issueDate: finalMediaReportsTable.issueDate,
        viewCount: finalMediaReportsTable.viewCount,
        generatedByName: finalMediaReportsTable.generatedByName,
      })
      .from(finalMediaReportsTable)
      .where(conds.length > 0 ? and(...conds) : undefined)
      .orderBy(desc(finalMediaReportsTable.dateFrom))
      .limit(200);
    res.json({ ok: true, count: rows.length, items: rows });
  } catch (err: any) {
    console.error("[final-media-reports] list error:", err);
    res.status(500).json({ ok: false, error: err.message || "Internal error" });
  }
});

// ─── GET /api/final-media-reports/:id ─────────────────────────────────────
router.get("/final-media-reports/:id", async (req: Request, res: Response) => {
  try {
    const id = parseInt(req.params.id, 10);
    if (isNaN(id)) return res.status(400).json({ ok: false, error: "Invalid id" });
    const [row] = await db.select().from(finalMediaReportsTable).where(eq(finalMediaReportsTable.id, id));
    if (!row) return res.status(404).json({ ok: false, error: "Not found" });
    // increment view count (fire-and-forget)
    db.update(finalMediaReportsTable)
      .set({ viewCount: (row.viewCount || 0) + 1 })
      .where(eq(finalMediaReportsTable.id, id))
      .then(() => {})
      .catch(() => {});
    res.json({ ok: true, item: row });
  } catch (err: any) {
    console.error("[final-media-reports] get error:", err);
    res.status(500).json({ ok: false, error: err.message || "Internal error" });
  }
});

// ─── POST /api/final-media-reports/generate ───────────────────────────────
const generateSchema = z.object({
  reportType: z.enum(["weekly", "monthly", "custom"]).default("weekly"),
  audience: z.enum(["executive", "manager", "analyst"]).default("manager"),
  dateFrom: z.string().optional(),
  dateTo: z.string().optional(),
  sources: z.array(z.string()).default(["linkedin", "news"]),
  focusTopics: z.string().optional(),
  language: z.string().optional(),
  title: z.string().optional(),
  autoSave: z.boolean().default(false), // if true → admin-only auto-save as final
});

router.post("/final-media-reports/generate", async (req: Request, res: Response) => {
  try {
    const body = generateSchema.parse(req.body || {});
    const now = new Date();
    let from = body.dateFrom ? new Date(body.dateFrom) : new Date(now);
    let to = body.dateTo ? new Date(body.dateTo) : new Date(now);
    if (!body.dateFrom) {
      if (body.reportType === "weekly") from = new Date(now.getTime() - 7 * 24 * 60 * 60 * 1000);
      else if (body.reportType === "monthly") from = new Date(now.getTime() - 30 * 24 * 60 * 60 * 1000);
    }
    const periodLabel = periodLabelFor(from, to, body.reportType);

    // Pull source data
    const posts = body.sources.includes("linkedin")
      ? await db.select().from(gacSocialPostsTable)
          .where(and(
            eq(gacSocialPostsTable.platform, "linkedin"),
            gte(gacSocialPostsTable.postedAt, from),
            lte(gacSocialPostsTable.postedAt, to),
          ))
          .orderBy(desc(gacSocialPostsTable.postedAt))
          .limit(120)
      : [];
    const news = body.sources.includes("news")
      ? await db.select().from(gacNewsItemsTable)
          .where(and(
            gte(gacNewsItemsTable.publishedAt, from),
            lte(gacNewsItemsTable.publishedAt, to),
          ))
          .orderBy(desc(gacNewsItemsTable.publishedAt))
          .limit(120)
      : [];

    const feed = formatFeedForAI(posts, news);
    const prompt = buildEightSectionPrompt({
      periodLabel,
      audience: body.audience,
      focusTopics: body.focusTopics,
      feed,
    });

    let parsed: any;
    try {
      // Multi-provider fallback: gemini-pro → flash → flash-lite → Perplexity
      parsed = await aiJSONWithFallback<any>(prompt, { maxTokens: 8000 });
    } catch (err) {
      // Last-ditch: raw geminiText + manual parse (in case the unified wrapper itself failed)
      try {
        const raw = await geminiText(prompt, { maxTokens: 8000 });
        parsed = parseAIJson(raw);
      } catch {
        // Surface the friendly Arabic message from aiJSONWithFallback
        throw err;
      }
    }

    // Build the report draft (not yet saved if autoSave=false)
    const title = body.title || `الرصد الإعلامي ${body.reportType === "weekly" ? "الأسبوعي" : body.reportType === "monthly" ? "الشهري" : "المخصص"} — ${periodLabel}`;
    const draft = {
      title,
      reportType: body.reportType,
      audience: body.audience,
      periodLabel,
      dateFrom: from,
      dateTo: to,
      kpis: parsed.kpis || {},
      executiveSummary: parsed.executiveSummary || "",
      topNews: parsed.topNews || [],
      timeline: parsed.timeline || [],
      digitalPresence: parsed.digitalPresence || { platforms: [], hashtags: [] },
      editorialTone: parsed.editorialTone || { distribution: [], classification: [], sources: [] },
      deepAnalysis: parsed.deepAnalysis || { keywords: [], quote: null, strengths: [], weaknesses: [] },
      regionalComparison: parsed.regionalComparison || [],
      recommendations: parsed.recommendations || [],
      alerts: parsed.alerts || [],
      quotesAppendix: parsed.quotesAppendix || [],
      methodology: parsed.methodology || "",
      sources: parsed.sources || [],
      sourceItems: [...posts.slice(0, 50), ...news.slice(0, 50)],
    };

    // If autoSave + admin → persist as final
    let saved: any = null;
    if (body.autoSave && (req as any).user?.role && ["admin", "super_admin"].includes((req as any).user.role)) {
      const reportNumber = await nextReportNumber();
      const contentSha256 = computeSha256({ ...draft, dateFrom: from.toISOString(), dateTo: to.toISOString() });
      const user = (req as any).user || {};
      const [row] = await db.insert(finalMediaReportsTable).values({
        ...draft,
        reportNumber,
        generatedByUserId: user.id ?? null,
        generatedByName: user.name || user.email || "system",
        contentSha256,
      }).returning();
      saved = row;
    }

    res.json({ ok: true, draft, saved });
  } catch (err: any) {
    console.error("[final-media-reports] generate error:", err);
    res.status(500).json({ ok: false, error: err.message || "Internal error" });
  }
});

// ─── POST /api/final-media-reports — admin: lock + save final ─────────────
router.post("/final-media-reports", requireAdmin, async (req: Request, res: Response) => {
  try {
    const data = req.body || {};
    if (!data.title || !data.dateFrom || !data.dateTo) {
      return res.status(400).json({ ok: false, error: "title, dateFrom, dateTo required" });
    }
    const reportNumber = data.reportNumber || (await nextReportNumber());
    const contentSha256 = computeSha256({
      ...data,
      dateFrom: new Date(data.dateFrom).toISOString(),
      dateTo: new Date(data.dateTo).toISOString(),
    });
    const user = (req as any).user || {};
    const [row] = await db.insert(finalMediaReportsTable).values({
      title: data.title,
      reportType: data.reportType || "weekly",
      reportNumber,
      periodLabel: data.periodLabel || "",
      dateFrom: new Date(data.dateFrom),
      dateTo: new Date(data.dateTo),
      kpis: data.kpis || {},
      executiveSummary: data.executiveSummary || "",
      topNews: data.topNews || [],
      timeline: data.timeline || [],
      digitalPresence: data.digitalPresence || { platforms: [], hashtags: [] },
      editorialTone: data.editorialTone || { distribution: [], classification: [], sources: [] },
      deepAnalysis: data.deepAnalysis || { keywords: [], quote: null, strengths: [], weaknesses: [] },
      regionalComparison: data.regionalComparison || [],
      recommendations: data.recommendations || [],
      alerts: data.alerts || [],
      quotesAppendix: data.quotesAppendix || [],
      methodology: data.methodology || "",
      sources: data.sources || [],
      sourceItems: data.sourceItems || [],
      generatedByUserId: user.id ?? null,
      generatedByName: user.name || user.email || "admin",
      contentSha256,
    }).returning();
    res.json({ ok: true, item: row });
  } catch (err: any) {
    console.error("[final-media-reports] save error:", err);
    res.status(500).json({ ok: false, error: err.message || "Internal error" });
  }
});

// ─── POST /api/final-media-reports/:id/export-pdf ─────────────────────────
router.post("/final-media-reports/:id/export-pdf", async (req: Request, res: Response) => {
  try {
    const id = parseInt(req.params.id, 10);
    if (isNaN(id)) return res.status(400).json({ ok: false, error: "Invalid id" });
    const [row] = await db.select().from(finalMediaReportsTable).where(eq(finalMediaReportsTable.id, id));
    if (!row) return res.status(404).json({ ok: false, error: "Not found" });

    const html = buildFinalReportHtml(row);

    // Prefer system chromium (Dockerfile installs /usr/bin/chromium).
    // Fallback to @sparticuz/chromium-min download if not present (local dev).
    const puppeteer = await import("puppeteer-core");
    const systemChromium = process.env.PUPPETEER_EXECUTABLE_PATH;
    const fs = await import("node:fs");
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
    await page.setContent(html, { waitUntil: "networkidle0" });
    const pdfBuffer = await page.pdf({
      format: "A4",
      printBackground: true,
      margin: { top: "0", right: "0", bottom: "0", left: "0" },
    });
    await browser.close();

    const filename = `${row.reportNumber.replace(/[\/\s]/g, "-")}.pdf`;
    res.setHeader("Content-Type", "application/pdf");
    res.setHeader("Content-Disposition", `attachment; filename="${filename}"`);
    res.send(Buffer.from(pdfBuffer));
  } catch (err: any) {
    console.error("[final-media-reports] export-pdf error:", err);
    res.status(500).json({ ok: false, error: err.message || "PDF generation failed" });
  }
});

// ─── POST /api/final-media-reports/:id/send-email ─────────────────────────
const emailSchema = z.object({
  recipients: z.array(z.string().email()).min(1),
  subject: z.string().optional(),
  message: z.string().optional(),
});

router.post("/final-media-reports/:id/send-email", async (req: Request, res: Response) => {
  try {
    const id = parseInt(req.params.id, 10);
    if (isNaN(id)) return res.status(400).json({ ok: false, error: "Invalid id" });
    const body = emailSchema.parse(req.body || {});
    const [row] = await db.select().from(finalMediaReportsTable).where(eq(finalMediaReportsTable.id, id));
    if (!row) return res.status(404).json({ ok: false, error: "Not found" });

    const RESEND_API_KEY = process.env.RESEND_API_KEY;
    const RESEND_FROM = process.env.RESEND_FROM || "الرصد الإعلامي <noreply@internal.sa>";
    const subject = body.subject || `${row.reportNumber} — ${row.title}`;
    const html = buildFinalReportHtml(row);

    if (!RESEND_API_KEY) {
      console.log("[final-media-reports] RESEND_API_KEY not set — would send to:", body.recipients);
      return res.json({ ok: true, sent: false, note: "Email service not configured (RESEND_API_KEY)", recipients: body.recipients });
    }

    const { Resend } = await import("resend");
    const resend = new Resend(RESEND_API_KEY);
    const result = await resend.emails.send({
      from: RESEND_FROM,
      to: body.recipients,
      subject,
      html,
    });

    res.json({ ok: true, sent: true, recipients: body.recipients, result });
  } catch (err: any) {
    console.error("[final-media-reports] send-email error:", err);
    res.status(500).json({ ok: false, error: err.message || "Email send failed" });
  }
});

// ─── POST /api/final-media-reports/:id/exec-summary ───────────────────────
router.post("/final-media-reports/:id/exec-summary", async (req: Request, res: Response) => {
  try {
    const id = parseInt(req.params.id, 10);
    if (isNaN(id)) return res.status(400).json({ ok: false, error: "Invalid id" });
    const [row] = await db.select().from(finalMediaReportsTable).where(eq(finalMediaReportsTable.id, id));
    if (!row) return res.status(404).json({ ok: false, error: "Not found" });

    const prompt = `أنت محلل تنفيذي. ولّد ملخصاً تنفيذياً موجزاً (5-7 أسطر فقط) للقيادة العليا بصيغة Markdown عربية، اعتماداً على التقرير التالي:

العنوان: ${row.title}
الفترة: ${row.periodLabel}
المؤشرات: ${JSON.stringify(row.kpis)}
أبرز الأخبار: ${JSON.stringify((row.topNews || []).slice(0, 5))}
التوصيات: ${JSON.stringify((row.recommendations || []).slice(0, 3))}

أنتج:
## الملخص التنفيذي — ${row.periodLabel}

ثم 3-4 نقاط مرقمة + توصية تنفيذية واحدة في النهاية. لغة موجزة احترافية.`;
    const summary = await geminiText(prompt, { maxTokens: 1000 });
    res.json({ ok: true, summary, reportNumber: row.reportNumber });
  } catch (err: any) {
    console.error("[final-media-reports] exec-summary error:", err);
    res.status(500).json({ ok: false, error: err.message || "Internal error" });
  }
});

// ─── POST /api/final-media-reports/search ─────────────────────────────────
const searchSchema = z.object({
  mode: z.enum(["full", "info"]).default("info"),
  query: z.string().min(2),
  limit: z.number().int().min(1).max(20).default(8),
});

router.post("/final-media-reports/search", async (req: Request, res: Response) => {
  try {
    const body = searchSchema.parse(req.body || {});
    const q = `%${body.query}%`;

    // Find candidate reports by text match in title/summary/json content
    const candidates = await db
      .select()
      .from(finalMediaReportsTable)
      .where(or(
        ilike(finalMediaReportsTable.title, q),
        ilike(finalMediaReportsTable.executiveSummary, q),
        ilike(finalMediaReportsTable.periodLabel, q),
        ilike(finalMediaReportsTable.reportNumber, q),
        sql`${finalMediaReportsTable.topNews}::text ILIKE ${q}`,
        sql`${finalMediaReportsTable.recommendations}::text ILIKE ${q}`,
        sql`${finalMediaReportsTable.deepAnalysis}::text ILIKE ${q}`,
        sql`${finalMediaReportsTable.quotesAppendix}::text ILIKE ${q}`,
      ))
      .orderBy(desc(finalMediaReportsTable.dateFrom))
      .limit(body.limit);

    // Log query
    const user = (req as any).user || {};
    await db.insert(reportsQaQueriesTable).values({
      userId: user.id ?? null,
      userName: user.name || user.email || null,
      queryType: body.mode === "full" ? "search-full" : "search-info",
      searchQuery: body.query,
      resultSummary: `${candidates.length} matches`,
    }).catch(() => {});

    if (body.mode === "full") {
      // Return candidate reports only (full report mode)
      return res.json({ ok: true, mode: "full", count: candidates.length, reports: candidates });
    }

    // info mode → use Gemini to answer the question from candidate reports
    if (candidates.length === 0) {
      return res.json({ ok: true, mode: "info", answer: "لم يتم العثور على تقارير تتعلق بهذا الاستعلام.", sources: [] });
    }

    const context = candidates.slice(0, 5).map((r) => `
=== التقرير ${r.reportNumber} — ${r.periodLabel} ===
الملخص التنفيذي: ${r.executiveSummary || ""}
أبرز الأخبار: ${JSON.stringify(r.topNews).slice(0, 1500)}
التوصيات: ${JSON.stringify(r.recommendations).slice(0, 800)}
التحليل العميق: ${JSON.stringify(r.deepAnalysis).slice(0, 800)}
الاقتباسات: ${JSON.stringify(r.quotesAppendix).slice(0, 800)}
`).join("\n");

    const prompt = `أنت مساعد بحث ذكي في أرشيف تقارير الرصد الإعلامي للهيئة العامة للمنافسة.

السؤال: ${body.query}

السياق من التقارير المحفوظة:
${context}

أجب بدقة بناءً على السياق فقط، باللغة العربية الرسمية. أضف في النهاية قائمة "المصادر:" بأرقام التقارير التي اعتمدت عليها. إذا لم يكن السياق كافياً، صرّح بذلك.`;

    const answer = await geminiText(prompt, { maxTokens: 1500 });
    res.json({
      ok: true,
      mode: "info",
      answer,
      sources: candidates.slice(0, 5).map((r) => ({ id: r.id, reportNumber: r.reportNumber, title: r.title, periodLabel: r.periodLabel })),
    });
  } catch (err: any) {
    console.error("[final-media-reports] search error:", err);
    res.status(500).json({ ok: false, error: err.message || "Search failed" });
  }
});

// ─── POST /api/qa-queries — log wizard inputs (audit) ─────────────────────
router.post("/qa-queries", async (req: Request, res: Response) => {
  try {
    const data = req.body || {};
    const user = (req as any).user || {};
    const [row] = await db.insert(reportsQaQueriesTable).values({
      userId: user.id ?? null,
      userName: user.name || user.email || null,
      queryType: data.queryType || "wizard",
      wizardAnswers: data.wizardAnswers || null,
      searchQuery: data.searchQuery || null,
      finalReportId: data.finalReportId ?? null,
      resultSummary: data.resultSummary || null,
      metadata: data.metadata || null,
    }).returning();
    res.json({ ok: true, item: row });
  } catch (err: any) {
    console.error("[final-media-reports] qa log error:", err);
    res.status(500).json({ ok: false, error: err.message || "Internal error" });
  }
});

// ─── GUARD: explicitly reject DELETE / PUT to enforce immutability ────────
router.delete("/final-media-reports/:id", (_req, res) => {
  res.status(403).json({ ok: false, error: "التقارير النهائية محفوظة بشكل دائم — لا يمكن حذفها." });
});
router.put("/final-media-reports/:id", (_req, res) => {
  res.status(403).json({ ok: false, error: "التقارير النهائية محفوظة بشكل دائم — لا يمكن تعديلها." });
});

export default router;
