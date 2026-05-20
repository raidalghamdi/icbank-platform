import { Router, type Request, type Response } from "express";
import Anthropic from "@anthropic-ai/sdk";
import { db } from "@workspace/db";
import {
  internationalDaysTable,
  dayYearlyThemesTable,
  dayActivationsTable,
  intlDaySourcesTable,
  intlSearchHistoryTable,
} from "@workspace/db";
import { eq, desc, ilike, and, or } from "drizzle-orm";

const router: Router = Router();

const anthropic = new Anthropic({
  baseURL: process.env.AI_INTEGRATIONS_ANTHROPIC_BASE_URL,
  apiKey: process.env.AI_INTEGRATIONS_ANTHROPIC_API_KEY ?? "dummy",
});

// ─── Rate limiting ───────────────────────────────────────────────
const rateLimitMap = new Map<string, { count: number; reset: number }>();

function checkRateLimit(ip: string): boolean {
  const now = Date.now();
  const entry = rateLimitMap.get(ip);
  if (!entry || now > entry.reset) {
    rateLimitMap.set(ip, { count: 1, reset: now + 3_600_000 });
    return true;
  }
  if (entry.count >= 10) return false;
  entry.count++;
  return true;
}

function getRemainingSearches(ip: string): number {
  const entry = rateLimitMap.get(ip);
  if (!entry || Date.now() > entry.reset) return 10;
  return Math.max(0, 10 - entry.count);
}

// ─── Prompt builder ──────────────────────────────────────────────
function buildPrompt(dayName: string, year: number): string {
  return `ابحث عن "${dayName}" واستخرج بدقة المعلومات التالية، وأرجع النتيجة كـ JSON منظم فقط بدون أي نص إضافي خارج JSON:

{
  "day_name_ar": "اسم اليوم بالعربية",
  "day_name_en": "Day name in English",
  "annual_date": "التاريخ السنوي مثل: 21 مارس",
  "official_organizer": "الجهة الراعية الرسمية دولياً مثل: منظمة العمل الدولية",
  "official_organizer_source": "رابط الصفحة الرسمية أو null",
  "history_summary": "ملخص تاريخي مختصر في 3 أسطر عن نشأة اليوم",
  "history_source": "رابط المصدر أو null",
  "current_theme_ar": "شعار/ثيم عام ${year} بالعربية",
  "current_theme_en": "Theme of ${year} in English",
  "theme_source_url": "رابط المصدر الرسمي للثيم أو null",
  "activations": [
    {
      "entity_name": "اسم الجهة السعودية (وزارة أو هيئة أو شركة)",
      "entity_type": "حكومي أو خاص",
      "activation_type": "حملة أو فعالية أو منشور أو إنفوجرافيك",
      "platform": "اسم المنصة مثل: تويتر أو لينكدإن أو إنستغرام أو موقع رسمي أو يوتيوب",
      "description": "وصف موجز للتفعيل ومحتواه",
      "source_url": "رابط مباشر للمحتوى أو null",
      "year": ${year - 1}
    }
  ],
  "design_samples": [
    {
      "entity_name": "اسم الجهة التي نشرت التصميم",
      "entity_type": "حكومي أو خاص أو دولي",
      "platform": "اسم المنصة مثل: موقع رسمي أو تويتر أو إنستغرام أو لينكدإن أو فيسبوك",
      "description": "وصف التصميم أو الحملة البصرية ومضمونها",
      "page_url": "رابط المنشور أو الصفحة التي تحتوي التصميم أو null",
      "image_url": "رابط مباشر للصورة أو البوستر (ينتهي بـ .jpg أو .png أو .webp أو .gif) إن وجد أو null",
      "country": "البلد",
      "year": ${year - 1}
    }
  ],
  "suggestions": [
    "فكرة تفعيل مقترحة قابلة للتطبيق في بيئة عمل حكومية سعودية"
  ],
  "sources": [
    {"url": "رابط", "title": "عنوان المصدر", "publisher": "الناشر"}
  ]
}

تعليمات مهمة:
1. التفعيلات للجهات السعودية فقط (وزارات، هيئات، شركات كبرى) — لا تضمّن جهات من دول أخرى في حقل activations.
2. اجمع تفعيلات من الأعوام ${year - 2} و${year - 1} و${year} فقط.
3. أنواع التفعيل المطلوبة حصراً: حملة (توعوية أو إعلانية)، فعالية (مؤتمر أو ورشة أو احتفالية)، منشور (محتوى سوشيال ميديا)، إنفوجرافيك (مادة بصرية توضيحية).
4. قدّم 8 إلى 15 تفعيلاً من جهات سعودية متنوعة موزعة على الأنواع الأربعة.
5. كل حقل source_url يجب أن يكون رابطاً حقيقياً أو null — لا تخترع روابط.
6. design_samples: 3-5 أمثلة بصرية من أي جهة موثقة بروابط.
7. اذكر 5 أفكار تفعيل مقترحة على الأقل.
8. أرجع JSON صالحاً فقط.`;
}

// ─── Perplexity search ───────────────────────────────────────────
interface SearchResult {
  day_name_ar?: string;
  day_name_en?: string;
  annual_date?: string;
  official_organizer?: string;
  official_organizer_source?: string | null;
  history_summary?: string;
  history_source?: string | null;
  current_theme_ar?: string;
  current_theme_en?: string;
  theme_source_url?: string | null;
  activations?: Activation[];
  design_samples?: DesignSample[];
  suggestions?: string[];
  sources?: Source[];
}

interface Activation {
  entity_name?: string;
  entity_type?: string;
  activation_type?: string;
  platform?: string;
  description?: string;
  source_url?: string | null;
  country?: string;
  year?: number;
}

interface DesignSample {
  entity_name?: string;
  entity_type?: string;
  platform?: string;
  description?: string;
  page_url?: string | null;
  image_url?: string | null;
  country?: string;
  year?: number;
}

interface Source {
  url?: string;
  title?: string;
  publisher?: string;
}

async function searchWithPerplexity(dayName: string, year: number): Promise<SearchResult> {
  const apiKey = process.env.PERPLEXITY_API_KEY;
  if (!apiKey) throw new Error("PERPLEXITY_API_KEY not set");

  const response = await fetch("https://api.perplexity.ai/chat/completions", {
    method: "POST",
    headers: {
      Authorization: `Bearer ${apiKey}`,
      "Content-Type": "application/json",
    },
    body: JSON.stringify({
      model: "sonar-pro",
      messages: [
        {
          role: "system",
          content: "أنت محلل بيانات متخصص. أرجع دائماً JSON منظم صالح فقط بدون أي نص إضافي أو markdown.",
        },
        { role: "user", content: buildPrompt(dayName, year) },
      ],
      max_tokens: 4000,
    }),
    signal: AbortSignal.timeout(40_000),
  });

  if (!response.ok) {
    const err = await response.text();
    throw new Error(`Perplexity error ${response.status}: ${err.slice(0, 200)}`);
  }

  const data = (await response.json()) as {
    choices: { message: { content: string } }[];
  };
  const text = data.choices[0]?.message?.content ?? "{}";

  // Extract JSON from possible markdown fences
  const clean = text.replace(/^```(?:json)?\n?/m, "").replace(/\n?```$/m, "").trim();
  const jsonMatch = clean.match(/\{[\s\S]*\}/);
  return JSON.parse(jsonMatch ? jsonMatch[0] : clean) as SearchResult;
}

// ─── Anthropic web search (supplementary) ────────────────────────
async function searchWithAnthropic(dayName: string, year: number): Promise<SearchResult> {
  const prompt = buildPrompt(dayName, year) +
    "\n\nاستخدم أداة البحث للحصول على معلومات حديثة ودقيقة.";

  const controller = new AbortController();
  const timer = setTimeout(() => controller.abort(), 120_000);

  const response = await anthropic.messages.create(
    {
      model: "claude-sonnet-4-5",
      max_tokens: 8096,
      tools: [{ type: "web_search_20250305" as const, name: "web_search" }],
      messages: [{ role: "user", content: prompt }],
    },
    { signal: controller.signal }
  );
  clearTimeout(timer);

  // Extract text from response blocks
  let text = "";
  for (const block of response.content) {
    if (block.type === "text") {
      text += block.text;
    }
  }

  const clean = text.replace(/^```(?:json)?\n?/m, "").replace(/\n?```$/m, "").trim();
  const jsonMatch = clean.match(/\{[\s\S]*/);
  if (!jsonMatch) return {};

  // Attempt parse; if truncated JSON, repair it by closing open structures
  let jsonStr = jsonMatch[0];
  try {
    return JSON.parse(jsonStr) as SearchResult;
  } catch {
    // Try to salvage truncated JSON: strip to last complete top-level field
    jsonStr = repairTruncatedJson(jsonStr);
    try {
      return JSON.parse(jsonStr) as SearchResult;
    } catch {
      return {};
    }
  }
}

// ─── Repair truncated JSON ────────────────────────────────────────
function repairTruncatedJson(raw: string): string {
  // Walk back from end to find last complete key-value at depth 1
  // Strategy: find the last top-level comma at depth 1 and close from there
  let depth = 0;
  let lastSafeCommaPos = -1;
  let inString = false;
  let escape = false;

  for (let i = 0; i < raw.length; i++) {
    const ch = raw[i];
    if (escape) { escape = false; continue; }
    if (ch === "\\") { escape = true; continue; }
    if (ch === '"') { inString = !inString; continue; }
    if (inString) continue;
    if (ch === "{" || ch === "[") depth++;
    else if (ch === "}" || ch === "]") depth--;
    else if (ch === "," && depth === 1) lastSafeCommaPos = i;
  }

  // Trim to last safe comma and close the object
  const trimmed = lastSafeCommaPos > 0
    ? raw.slice(0, lastSafeCommaPos)
    : raw.replace(/,\s*$/, "");

  return trimmed + "}";
}

// ─── Merge results ────────────────────────────────────────────────
const GULF_COUNTRIES = ["الإمارات", "الكويت", "البحرين", "قطر", "عُمان", "عمان", "الأردن"];
const SAUDI_KEYWORDS = ["السعودية", "سعودية", "الرياض", "جدة", "الدمام"];

function regionOrder(country: string = ""): number {
  if (SAUDI_KEYWORDS.some((k) => country.includes(k))) return 0;
  if (GULF_COUNTRIES.some((k) => country.includes(k))) return 1;
  if (["مصر", "الإمارات", "العراق", "المغرب", "تونس", "ليبيا", "لبنان", "سوريا"].some((k) => country.includes(k))) return 2;
  return 3;
}

function mergeResults(primary: SearchResult, secondary: SearchResult): SearchResult {
  const merged: SearchResult = { ...primary };

  // Fill missing fields from secondary
  if (!merged.day_name_en && secondary.day_name_en) merged.day_name_en = secondary.day_name_en;
  if (!merged.annual_date && secondary.annual_date) merged.annual_date = secondary.annual_date;
  if (!merged.official_organizer && secondary.official_organizer) merged.official_organizer = secondary.official_organizer;
  if (!merged.history_summary && secondary.history_summary) merged.history_summary = secondary.history_summary;
  if (!merged.current_theme_ar && secondary.current_theme_ar) merged.current_theme_ar = secondary.current_theme_ar;
  if (!merged.theme_source_url && secondary.theme_source_url) merged.theme_source_url = secondary.theme_source_url;

  // Merge activations
  const primaryActs = primary.activations ?? [];
  const secondaryActs = secondary.activations ?? [];
  const seen = new Set(primaryActs.map((a) => `${a.entity_name}|${a.year}`));
  const extraActs = secondaryActs.filter((a) => !seen.has(`${a.entity_name}|${a.year}`));
  merged.activations = [...primaryActs, ...extraActs].sort(
    (a, b) => regionOrder(a.country) - regionOrder(b.country)
  );

  // Merge design_samples
  const primaryDesigns = primary.design_samples ?? [];
  const secondaryDesigns = secondary.design_samples ?? [];
  const seenDesigns = new Set(primaryDesigns.map((d) => `${d.entity_name}|${d.year}`));
  const extraDesigns = secondaryDesigns.filter((d) => !seenDesigns.has(`${d.entity_name}|${d.year}`));
  merged.design_samples = [...primaryDesigns, ...extraDesigns].sort(
    (a, b) => regionOrder(a.country) - regionOrder(b.country)
  );

  // Merge suggestions
  const allSuggestions = [...(primary.suggestions ?? []), ...(secondary.suggestions ?? [])];
  merged.suggestions = [...new Set(allSuggestions)].slice(0, 8);

  // Merge sources
  const allSources = [...(primary.sources ?? []), ...(secondary.sources ?? [])];
  const seenUrls = new Set<string>();
  merged.sources = allSources.filter((s) => {
    if (!s.url || seenUrls.has(s.url)) return false;
    seenUrls.add(s.url);
    return true;
  });

  return merged;
}

// ─── POST /api/intl-days/search ──────────────────────────────────
router.post("/intl-days/search", async (req: Request, res: Response) => {
  const { query, category, force_refresh } = req.body as {
    query?: string;
    category?: string;
    force_refresh?: boolean;
  };

  if (!query?.trim()) {
    res.status(400).json({ error: "query مطلوب" });
    return;
  }

  const ip = (req.headers["x-forwarded-for"] as string)?.split(",")[0]?.trim() ?? req.socket.remoteAddress ?? "unknown";
  if (!checkRateLimit(ip)) {
    res.status(429).json({ error: "تجاوزت حد 10 عمليات بحث في الساعة. حاول لاحقاً." });
    return;
  }

  const currentYear = new Date().getFullYear();
  const sevenDaysAgo = new Date(Date.now() - 7 * 24 * 3600 * 1000);

  // Check cache
  if (!force_refresh) {
    const [existing] = await db
      .select()
      .from(internationalDaysTable)
      .where(ilike(internationalDaysTable.dayNameAr, `%${query.trim()}%`))
      .limit(1);

    if (existing && existing.lastSearchedAt && existing.lastSearchedAt > sevenDaysAgo) {
      const themes = await db
        .select()
        .from(dayYearlyThemesTable)
        .where(eq(dayYearlyThemesTable.dayId, existing.id))
        .orderBy(desc(dayYearlyThemesTable.year));
      const activations = await db
        .select()
        .from(dayActivationsTable)
        .where(eq(dayActivationsTable.dayId, existing.id))
        .orderBy(desc(dayActivationsTable.year));
      const sources = await db
        .select()
        .from(intlDaySourcesTable)
        .where(and(eq(intlDaySourcesTable.relatedTable, "international_days"), eq(intlDaySourcesTable.relatedId, existing.id)));

      // Log search
      await db.insert(intlSearchHistoryTable).values({ query: query.trim(), dayId: existing.id, ipAddress: ip });

      res.json({
        cached: true,
        remaining_searches: getRemainingSearches(ip),
        day: existing,
        themes,
        activations,
        sources,
      });
      return;
    }
  }

  // Log search (no dayId yet)
  await db.insert(intlSearchHistoryTable).values({ query: query.trim(), ipAddress: ip });

  // SSE
  res.setHeader("Content-Type", "text/event-stream");
  res.setHeader("Cache-Control", "no-cache");
  res.setHeader("Connection", "keep-alive");
  res.setHeader("X-Accel-Buffering", "no");

  const flushRes = () => {
    if (typeof (res as unknown as { flush?: () => void }).flush === "function") {
      (res as unknown as { flush: () => void }).flush();
    }
  };

  // Flush headers immediately so proxy sees activity
  flushRes();

  const send = (event: string, data: unknown) => {
    res.write(`event: ${event}\ndata: ${JSON.stringify(data)}\n\n`);
    flushRes();
  };

  // Keepalive comment every 5s so the proxy doesn't drop the connection
  const keepalive = setInterval(() => {
    try {
      res.write(": keepalive\n\n");
      flushRes();
    } catch { /* closed */ }
  }, 5000);

  const cleanup = () => clearInterval(keepalive);

  try {
    const hasPerplexity = !!process.env.PERPLEXITY_API_KEY;
    let primaryResult: SearchResult = {};
    let secondaryResult: SearchResult = {};

    if (hasPerplexity) {
      // ── Fast path: Perplexity first (~40s), then Anthropic supplement in parallel
      send("status", { message: "جاري البحث السريع…", step: 1 });

      try {
        send("status", { message: "يبحث في قواعد المعرفة…", step: 2 });
        primaryResult = await searchWithPerplexity(query.trim(), currentYear);
        send("status", { message: "اكتمل البحث ✓ جاري تنظيم النتائج…", step: 3 });
      } catch (e) {
        req.log.warn({ err: e }, "Perplexity failed, falling back to Anthropic");
        send("status", { message: "جاري البحث عبر الذكاء الاصطناعي…", step: 2 });
        try {
          primaryResult = await searchWithAnthropic(query.trim(), currentYear);
          send("status", { message: "اكتمل البحث ✓ جاري تنظيم النتائج…", step: 3 });
        } catch (e2) {
          req.log.warn({ err: e2 }, "Anthropic also failed");
          send("error", { message: "تعذّر البحث. حاول مرة أخرى بعد لحظة." });
          cleanup();
          res.end();
          return;
        }
      }
    } else {
      // ── Anthropic-only fallback
      send("status", { message: "جاري البحث عبر Anthropic Claude…", step: 1 });
      try {
        send("status", { message: "الذكاء الاصطناعي يبحث في الإنترنت…", step: 2 });
        primaryResult = await searchWithAnthropic(query.trim(), currentYear);
        send("status", { message: "اكتمل البحث ✓ جاري تنظيم النتائج…", step: 3 });
      } catch (e) {
        req.log.warn({ err: e }, "Anthropic failed");
        send("error", { message: "تعذّر البحث. حاول مرة أخرى بعد لحظة." });
        cleanup();
        res.end();
        return;
      }
    }

    if (!primaryResult.day_name_ar) {
      send("error", { message: "لم تُعثر على نتائج لهذا اليوم. جرب صياغة مختلفة." });
      cleanup();
      res.end();
      return;
    }

    const merged = mergeResults(primaryResult, secondaryResult);

    send("result", {
      remaining_searches: getRemainingSearches(ip),
      cached: false,
      category: category ?? null,
      data: merged,
    });
  } finally {
    cleanup();
  }

  res.end();
});

// ─── POST /api/intl-days/save ────────────────────────────────────
router.post("/intl-days/save", async (req: Request, res: Response) => {
  const { data, category } = req.body as {
    data?: SearchResult & { current_year?: number };
    category?: string;
  };

  if (!data?.day_name_ar) {
    res.status(400).json({ error: "data.day_name_ar مطلوب" });
    return;
  }

  const currentYear = data.current_year ?? new Date().getFullYear();

  // Upsert day
  const [existing] = await db
    .select({ id: internationalDaysTable.id })
    .from(internationalDaysTable)
    .where(ilike(internationalDaysTable.dayNameAr, `%${data.day_name_ar}%`))
    .limit(1);

  let dayId: number;

  if (existing) {
    await db
      .update(internationalDaysTable)
      .set({
        dayNameEn: data.day_name_en,
        annualDate: data.annual_date,
        category: category ?? undefined,
        officialOrganizer: data.official_organizer,
        officialOrganizerSource: data.official_organizer_source ?? undefined,
        historySummary: data.history_summary,
        historySource: data.history_source ?? undefined,
        suggestions: data.suggestions,
        lastSearchedAt: new Date(),
      })
      .where(eq(internationalDaysTable.id, existing.id));
    dayId = existing.id;
  } else {
    const [inserted] = await db
      .insert(internationalDaysTable)
      .values({
        dayNameAr: data.day_name_ar,
        dayNameEn: data.day_name_en,
        annualDate: data.annual_date,
        category: category ?? undefined,
        officialOrganizer: data.official_organizer,
        officialOrganizerSource: data.official_organizer_source ?? undefined,
        historySummary: data.history_summary,
        historySource: data.history_source ?? undefined,
        suggestions: data.suggestions,
        lastSearchedAt: new Date(),
      })
      .returning({ id: internationalDaysTable.id });
    dayId = inserted.id;
  }

  // Upsert yearly theme
  if (data.current_theme_ar || data.current_theme_en) {
    const [themeExists] = await db
      .select({ id: dayYearlyThemesTable.id })
      .from(dayYearlyThemesTable)
      .where(and(eq(dayYearlyThemesTable.dayId, dayId), eq(dayYearlyThemesTable.year, currentYear)))
      .limit(1);

    if (themeExists) {
      await db
        .update(dayYearlyThemesTable)
        .set({ themeAr: data.current_theme_ar, themeEn: data.current_theme_en, themeSourceUrl: data.theme_source_url ?? undefined })
        .where(eq(dayYearlyThemesTable.id, themeExists.id));
    } else {
      await db.insert(dayYearlyThemesTable).values({
        dayId,
        year: currentYear,
        themeAr: data.current_theme_ar,
        themeEn: data.current_theme_en,
        themeSourceUrl: data.theme_source_url ?? undefined,
      });
    }
  }

  // Save activations (skip duplicates by entity + year)
  if (data.activations?.length) {
    for (const act of data.activations) {
      const [actExists] = await db
        .select({ id: dayActivationsTable.id })
        .from(dayActivationsTable)
        .where(
          and(
            eq(dayActivationsTable.dayId, dayId),
            eq(dayActivationsTable.entityName, act.entity_name ?? ""),
            eq(dayActivationsTable.year, act.year ?? currentYear)
          )
        )
        .limit(1);

      if (!actExists) {
        await db.insert(dayActivationsTable).values({
          dayId,
          year: act.year ?? currentYear,
          entityName: act.entity_name,
          entityType: act.entity_type,
          activationType: act.activation_type,
          platform: act.platform,
          description: act.description,
          sourceUrl: act.source_url ?? undefined,
          country: act.country,
          verified: !!act.source_url,
        });
      }
    }
  }

  // Save design_samples as activations (activation_type = "تصميم بصري")
  if (data.design_samples?.length) {
    for (const ds of data.design_samples) {
      if (!ds.entity_name) continue;
      const [dsExists] = await db
        .select({ id: dayActivationsTable.id })
        .from(dayActivationsTable)
        .where(
          and(
            eq(dayActivationsTable.dayId, dayId),
            eq(dayActivationsTable.entityName, ds.entity_name),
            eq(dayActivationsTable.activationType, "تصميم بصري"),
            eq(dayActivationsTable.year, ds.year ?? currentYear)
          )
        )
        .limit(1);
      if (!dsExists) {
        await db.insert(dayActivationsTable).values({
          dayId,
          year: ds.year ?? currentYear,
          entityName: ds.entity_name,
          entityType: ds.entity_type,
          activationType: "تصميم بصري",
          description: [ds.platform ? `[${ds.platform}]` : "", ds.description ?? ""].filter(Boolean).join(" "),
          sourceUrl: ds.page_url ?? ds.image_url ?? undefined,
          country: ds.country,
          verified: !!(ds.page_url || ds.image_url),
        });
      }
    }
  }

  // Save sources
  if (data.sources?.length) {
    for (const src of data.sources) {
      if (!src.url) continue;
      await db.insert(intlDaySourcesTable).values({
        relatedTable: "international_days",
        relatedId: dayId,
        sourceUrl: src.url,
        sourceTitle: src.title,
        sourcePublisher: src.publisher,
      });
    }
  }

  const [saved] = await db.select().from(internationalDaysTable).where(eq(internationalDaysTable.id, dayId));
  res.json({ ok: true, id: dayId, day: saved });
});

// ─── GET /api/intl-days/archive ──────────────────────────────────
router.get("/intl-days/archive", async (req: Request, res: Response) => {
  const { q, category, year } = req.query as { q?: string; category?: string; year?: string };

  const conditions = [];
  if (q) conditions.push(or(ilike(internationalDaysTable.dayNameAr, `%${q}%`), ilike(internationalDaysTable.dayNameEn ?? "", `%${q}%`)));
  if (category) conditions.push(eq(internationalDaysTable.category, category));

  const days = await db
    .select()
    .from(internationalDaysTable)
    .where(conditions.length ? and(...conditions) : undefined)
    .orderBy(desc(internationalDaysTable.updatedAt));

  const result = await Promise.all(
    days.map(async (day) => {
      const themesQ = db
        .select()
        .from(dayYearlyThemesTable)
        .where(
          year
            ? and(eq(dayYearlyThemesTable.dayId, day.id), eq(dayYearlyThemesTable.year, parseInt(year)))
            : eq(dayYearlyThemesTable.dayId, day.id)
        )
        .orderBy(desc(dayYearlyThemesTable.year))
        .limit(3);
      const actCountQ = db
        .select()
        .from(dayActivationsTable)
        .where(eq(dayActivationsTable.dayId, day.id));

      const [themes, activations] = await Promise.all([themesQ, actCountQ]);
      return { ...day, themes, activation_count: activations.length };
    })
  );

  res.json({ count: result.length, days: result });
});

// ─── GET /api/intl-days/:id ──────────────────────────────────────
router.get("/intl-days/:id", async (req: Request, res: Response) => {
  const id = parseInt(String(req.params.id), 10);
  if (isNaN(id)) { res.status(400).json({ error: "id غير صالح" }); return; }

  const [day] = await db.select().from(internationalDaysTable).where(eq(internationalDaysTable.id, id));
  if (!day) { res.status(404).json({ error: "غير موجود" }); return; }

  const [themes, activations, sources] = await Promise.all([
    db.select().from(dayYearlyThemesTable).where(eq(dayYearlyThemesTable.dayId, id)).orderBy(desc(dayYearlyThemesTable.year)),
    db.select().from(dayActivationsTable).where(eq(dayActivationsTable.dayId, id)).orderBy(desc(dayActivationsTable.year)),
    db.select().from(intlDaySourcesTable).where(and(eq(intlDaySourcesTable.relatedTable, "international_days"), eq(intlDaySourcesTable.relatedId, id))),
  ]);

  res.json({ day, themes, activations, sources });
});

// ─── DELETE /api/intl-days/:id ───────────────────────────────────
router.delete("/intl-days/:id", async (req: Request, res: Response) => {
  const id = parseInt(String(req.params.id), 10);
  if (isNaN(id)) { res.status(400).json({ error: "id غير صالح" }); return; }
  await db.delete(internationalDaysTable).where(eq(internationalDaysTable.id, id));
  res.json({ ok: true });
});

// ─── GET /api/intl-days/export/:id ──────────────────────────────
router.get("/intl-days/export/:id", async (req: Request, res: Response) => {
  const id = parseInt(String(req.params.id), 10);
  if (isNaN(id)) { res.status(400).json({ error: "id غير صالح" }); return; }

  const [day] = await db.select().from(internationalDaysTable).where(eq(internationalDaysTable.id, id));
  if (!day) { res.status(404).json({ error: "غير موجود" }); return; }

  const [themes, activations, sources] = await Promise.all([
    db.select().from(dayYearlyThemesTable).where(eq(dayYearlyThemesTable.dayId, id)).orderBy(desc(dayYearlyThemesTable.year)),
    db.select().from(dayActivationsTable).where(eq(dayActivationsTable.dayId, id)).orderBy(desc(dayActivationsTable.year)),
    db.select().from(intlDaySourcesTable).where(and(eq(intlDaySourcesTable.relatedTable, "international_days"), eq(intlDaySourcesTable.relatedId, id))),
  ]);

  const latestTheme = themes[0];
  const currentYear = new Date().getFullYear();

  const activationsHtml = activations
    .map(
      (a, i) => `
      <tr>
        <td>${i + 1}</td>
        <td>${a.entityName ?? "—"}</td>
        <td>${a.entityType ?? "—"}</td>
        <td>${a.activationType ?? "—"}</td>
        <td>${a.description ?? "—"}</td>
        <td>${a.country ?? "—"}</td>
        <td>${a.year ?? "—"}</td>
        <td>${a.sourceUrl ? `<a href="${a.sourceUrl}">رابط</a>` : a.verified ? "موثق" : "⚠️ غير موثق"}</td>
      </tr>`
    )
    .join("");

  const suggestionsHtml = (day.suggestions as string[] | null ?? [])
    .map((s, i) => `<li>${i + 1}. ${s}</li>`)
    .join("");

  const sourcesHtml = sources
    .map((s, i) => `<li>${i + 1}. <a href="${s.sourceUrl}">${s.sourceTitle ?? s.sourceUrl}</a> — ${s.sourcePublisher ?? ""}</li>`)
    .join("");

  const html = `<!DOCTYPE html>
<html dir="rtl" lang="ar">
<head>
<meta charset="UTF-8">
<style>
  body { font-family: 'Arial', sans-serif; direction: rtl; margin: 40px; color: #1a1a2e; }
  h1 { color: #1e40af; border-bottom: 2px solid #1e40af; padding-bottom: 8px; }
  h2 { color: #1e40af; margin-top: 28px; font-size: 16px; border-right: 4px solid #1e40af; padding-right: 10px; }
  .meta { background: #f0f4ff; padding: 12px 16px; border-radius: 8px; margin: 16px 0; font-size: 14px; }
  .meta span { margin-left: 24px; }
  table { width: 100%; border-collapse: collapse; margin-top: 12px; font-size: 13px; }
  th { background: #1e40af; color: white; padding: 8px 10px; text-align: right; }
  td { padding: 7px 10px; border-bottom: 1px solid #e5e7eb; }
  tr:nth-child(even) td { background: #f8fafc; }
  ul { padding-right: 20px; line-height: 2; }
  .theme-box { background: #ecfdf5; border: 1px solid #6ee7b7; padding: 14px; border-radius: 8px; margin: 12px 0; }
  .footer { margin-top: 40px; font-size: 11px; color: #9ca3af; text-align: center; border-top: 1px solid #e5e7eb; padding-top: 12px; }
</style>
</head>
<body>
<h1>${day.dayNameAr} ${day.dayNameEn ? `— ${day.dayNameEn}` : ""}</h1>
<div class="meta">
  <span>📅 التاريخ السنوي: <strong>${day.annualDate ?? "غير محدد"}</strong></span>
  <span>🏛 الجهة الراعية: <strong>${day.officialOrganizer ?? "—"}</strong></span>
  <span>🏷 الفئة: <strong>${day.category ?? "—"}</strong></span>
</div>

<h2>الملخص التاريخي</h2>
<p>${day.historySummary ?? "لا يوجد ملخص."}</p>
${day.historySource ? `<p style="font-size:12px;color:#6b7280">المصدر: <a href="${day.historySource}">${day.historySource}</a></p>` : ""}

<h2>شعار ${currentYear}</h2>
<div class="theme-box">
  <p><strong>عربي:</strong> ${latestTheme?.themeAr ?? "⚠️ غير موثق"}</p>
  <p><strong>English:</strong> ${latestTheme?.themeEn ?? "N/A"}</p>
  ${latestTheme?.themeSourceUrl ? `<p style="font-size:12px"><a href="${latestTheme.themeSourceUrl}">🔗 المصدر الرسمي</a></p>` : ""}
</div>

<h2>تفعيلات سابقة (${activations.length})</h2>
${activations.length ? `<table>
  <tr><th>#</th><th>الجهة</th><th>النوع</th><th>التفعيل</th><th>الوصف</th><th>البلد</th><th>السنة</th><th>المصدر</th></tr>
  ${activationsHtml}
</table>` : "<p>لا توجد تفعيلات مسجلة.</p>"}

<h2>أفكار مقترحة للتفعيل</h2>
<ul>${suggestionsHtml || "<li>لا توجد اقتراحات.</li>"}</ul>

<h2>المصادر (${sources.length})</h2>
<ul>${sourcesHtml || "<li>لا توجد مصادر مسجلة.</li>"}</ul>

<div class="footer">تم التصدير من بنك التواصل الداخلي · ${new Date().toLocaleDateString("ar-SA", { year: "numeric", month: "long", day: "numeric" })}</div>
</body></html>`;

  res.setHeader("Content-Type", "application/vnd.ms-word; charset=utf-8");
  res.setHeader("Content-Disposition", `attachment; filename="${encodeURIComponent(day.dayNameAr)}.doc"`);
  res.send(html);
});

export default router;
