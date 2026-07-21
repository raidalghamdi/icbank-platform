/**
 * Icon Event Designs API
 * توليد تصاميم الفعاليات بالأيقونات تلقائياً باستخدام AI
 *
 * Endpoints:
 *   POST /api/designs/icon-event/generate  → AI يحلل البيانات الخام ويولّد 3 تنويعات
 *   POST /api/designs/icon-event/render    → HTML → PNG عالي الجودة (3x HD)
 *   GET  /api/designs/icon-event/icons     → قائمة الأيقونات المتاحة
 */

import { Router, type Request, type Response } from "express";
import { requireAuth } from "../middleware/auth";
import { aiJSONWithFallback } from "../lib/aiProviders";
import { ICON_LIBRARY, iconListForAI } from "../composer/icon-library";
import {
  renderIconEventDesign,
  type IconEventInput,
  type IconEventStat,
  type LayoutType,
  type SizePreset,
  type ColorScheme,
  SIZE_MAP,
} from "../composer/icon-event-composer";
import { ObjectStorageService } from "../lib/objectStorage";

const router = Router();
const objectStorage = new ObjectStorageService();

router.use(requireAuth);

// ─── GET icons catalog ──────────────────────────────────────────────────────
router.get("/designs/icon-event/icons", async (_req: Request, res: Response) => {
  res.json({
    icons: ICON_LIBRARY.map((i) => ({
      name: i.name,
      label_ar: i.label_ar,
      category: i.category,
      keywords: i.keywords,
    })),
    count: ICON_LIBRARY.length,
  });
});

// ─── POST generate ──────────────────────────────────────────────────────────
router.post("/designs/icon-event/generate", async (req: Request, res: Response) => {
  const {
    raw_data,          // البيانات الخام (نص حر يكتبه المستخدم)
    headline,          // عنوان (اختياري — يستخدمه AI كمرجع)
    subtitle,
    department,        // اسم الإدارة (اختياري)
    hashtag,
    date,
    time,
    location,
    event_type,
    size,
  } = req.body as {
    raw_data?: string;
    headline?: string;
    subtitle?: string;
    department?: string;
    hashtag?: string;
    date?: string;
    time?: string;
    location?: string;
    event_type?: "workshop" | "meeting" | "launch" | "social";
    size?: SizePreset;
  };

  // يحتاج إما raw_data أو headline على الأقل
  if ((!raw_data || raw_data.trim().length < 5) && (!headline || headline.trim().length < 3)) {
    res.status(400).json({ error: "يجب إدخال بيانات خام أو عنوان للفعالية" });
    return;
  }
  if (!size || !SIZE_MAP[size]) {
    res.status(400).json({ error: "المقاس مطلوب (square | story | landscape)" });
    return;
  }

  // === AI Prompt — يفهم البيانات الخام ويستخرج كل شيء ===
  // القواعد الأساسية: احترام مدخلات المستخدم، عدم اختراع بيانات، الهوية الرسمية GAC (teal فقط)
  const prompt = `
أنت مصمم بصري ذكي للهيئة العامة للمنافسة (GAC). ستحلل بيانات الفعالية (قد تكون نصاً خاماً غير منظم) وتستخرج منها معلومات دقيقة **دون اختراع** أي شيء غير موجود في المدخلات.

مهامك:
1. عنوان رئيسي قصير (2-6 كلمات) — من البيانات فقط
2. عنوان فرعي (سطر واحد للتصاميم المكتظة، أو سطرين للتصاميم البسيطة)
3. اسم الإدارة (إن لم يُذكر → اتركه فارغاً، لا تخترع)
4. هاشتاج (إن لم يُذكر → اتركه فارغاً، لا تخترع)
5. إحصائيات (0-3 حسب ما هو موجود فعلياً في البيانات، لا تخترع أرقاماً)
6. أيقونة رئيسية + أيقونات داعمة (دلالية، تعكس المعنى)
7. اختيار ذكي للـ layouts الثلاثة (لا نمط ثابت)

البيانات (قد تكون خاماً):
"""
${raw_data || `العنوان: ${headline}\nالوصف: ${subtitle || "-"}\nالإدارة: ${department || "-"}\nالتاريخ: ${date || "-"}\nالوقت: ${time || "-"}\nالمكان: ${location || "-"}\nالنوع: ${event_type || "-"}`}
"""

${department ? `ملاحظة: اسم الإدارة المؤكد هو "${department}" — استخدمه كما هو (بدون اختصار في هذا الحقل).` : "⚠️ لم يُذكر اسم إدارة — اتركه سلسلة فارغة \"\" (لا تخترع)."}
${hashtag ? `ملاحظة: الهاشتاج المؤكد هو "${hashtag}" — استخدمه كما هو.` : "⚠️ لم يُذكر هاشتاج — اتركه سلسلة فارغة \"\" (لا تخترع هاشتاقاً)."}

الأيقونات المتاحة (اختر فقط من هذه القائمة):
${iconListForAI()}

═══════════════════════════════════════
قواعد اختيار الإحصائيات (حرجة):
═══════════════════════════════════════
- **لا تخترع أرقاماً أبداً**. إذا لم توجد أرقام صريحة في البيانات → اترك stats مصفوفة فارغة [].
- إذا وُجد رقم واحد فقط في البيانات → أنتج إحصائية واحدة فقط.
- إذا وُجد رقمان → أنتج اثنين. ثلاثة → ثلاثة. الحد الأقصى 3.
- استخرج الأرقام حرفياً كما وردت (مثال: "135+" يبقى "135+"، "20" يبقى "20").
- التسمية (label) يجب أن تعكس السياق الفعلي من البيانات، لا اختراعاً.

═══════════════════════════════════════
قواعد اختيار الأيقونات (دلالية):
═══════════════════════════════════════
- users → للأشخاص/المشاركين/الموظفين
- building → للإدارات/الجهات/المكاتب
- calendar → للجلسات/التواريخ/المواعيد
- monitor / presentation → للعروض/الشاشات
- target → للأهداف/المحاور
- trending-up → للنمو/النسب المتصاعدة
- lightbulb → للأفكار/الابتكار
- graduation-cap → للتدريب/التعليم
- rocket → للإطلاقات/المبادرات الجديدة
- award → للإنجازات/التكريم
- **لا تختر أيقونة "جميلة" بدون معنى** — الأيقونة يجب أن تعبر عن معنى الرقم.

═══════════════════════════════════════
قواعد اختيار الـ layouts (حسب المحتوى):
═══════════════════════════════════════
حلل نوع المحتوى ثم اختر ثلاث تنويعات مختلفة **مناسبة** له:

• إذا كانت أرقام موجودة (إعلان ورشة/تقرير) → الأولى "stats-hero" (إجباري)
• إذا لا أرقام (تهنئة/خبر عاجل/إعلان بسيط) → الأولى "hero" (بدون إحصائيات)
• إذا أرقام فقط بدون سياق قوي → "grid" يبرز الأرقام
• التنويعات الثلاث يجب أن تكون **متنوعة** (ليس كلها stats-hero)

أمثلة إرشادية:
- "ورشة استراتيجية — 20 إدارة، 135 موظف" → [stats-hero, split, grid]
- "مبروك للفريق على الإنجاز" → [hero, split, hero بأسلوب مختلف]
- "إطلاق مبادرة الابتكار الجديدة" → [hero, split, grid]
- "20 إدارة، 135 موظف، 10 جلسات" (أرقام فقط) → [grid, stats-hero, split]
- "خبر عاجل: تعليق العمل غداً" → [hero, hero, split]

═══════════════════════════════════════
قواعد الهوية الرسمية (ثابتة):
═══════════════════════════════════════
- **color_scheme دائماً "teal"** لجميع التنويعات الثلاث (الهوية الرسمية للهيئة).
- لا تستخدم blue/green/cyan/navy مطلقاً.
- المحتوى رسمي دائماً — لا سخرية، لا نبرة غير مؤسسية، لا محتوى قد يُسيء.

═══════════════════════════════════════
صيغة الاستجابة:
═══════════════════════════════════════
أعد JSON فقط (بدون أي شرح خارجه):
{
  "extracted": {
    "headline": "<عنوان 2-6 كلمات من البيانات>",
    "subtitle": "<النص الفرعي كاملاً — لا تختصر، لا تحذف، انسخ الجمل الأصلية كما هي مع الحفاظ على المعنى الكامل>",
    "department": "<اسم الإدارة الكامل أو \"\">",
    "hashtag": "<#الهاشتاج أو \"\">",
    "contact_email": "<البريد الإلكتروني حرفياً إن وُجد أو \"\">",
    "contact_phone": "<رقم الهاتف حرفياً إن وُجد أو \"\">",
    "stats": [
      { "icon": "<أيقونة دلالية>", "value": "<الرقم كما ورد>", "label": "<وصف من السياق>" }
    ]
  },
  "variants": [
    {
      "layout": "<اختيار ذكي حسب المحتوى>",
      "color_scheme": "teal",
      "main_icon": "<أيقونة دلالية>",
      "supporting_icons": ["<دلالية>","<دلالية>","<دلالية>"],
      "rationale": "<اشرح بوضوح: لماذا اخترت هذا الـ layout بالذات لهذا المحتوى — مثال: 'اخترت stats-hero لأنك ذكرت 3 أرقام محددة (20 إدارة، 135 موظف، 10 جلسات) وهي جوهر الرسالة'>"
    },
    { "layout": "...", "color_scheme": "teal", "main_icon": "...", "supporting_icons": ["...","...","..."], "rationale": "..." },
    { "layout": "...", "color_scheme": "teal", "main_icon": "...", "supporting_icons": ["...","...","..."], "rationale": "..." }
  ]
}

قواعد صارمة أخيرة:
- استخدم فقط أسماء أيقونات من القائمة المعطاة.
- color_scheme = "teal" لكل تنويعة (لا استثناء).
- layout من: stats-hero | hero | grid | split.
- **لا تخترع** أرقاماً أو هاشتاقاً أو اسم إدارة إذا لم يكن في المدخلات.
- stats مصفوفة فارغة [] إذا لا أرقام في البيانات.
- rationale يشرح **لماذا** بالتحديد لهذا المحتوى (ليس عبارة عامة).
- لا تضف أي شرح خارج JSON.

═══════════════════════════════════════
قواعد المحافظة على النص الأصلي (حرجة جداً):
═══════════════════════════════════════
- **الـ subtitle**: انسخ النص الفرعي من المُدخلات **كاملاً** — لا تختصر، لا تحذف جملاً، لا تعيد صياغة. المستخدم كتبه بالضبط كما يريده أن يظهر.
  - إذا كان النص طويلاً (أكثر من 400 حرف): احتفظ بكل المعلومات المهمة (أسماء الجهات، الأسباب، التوجيهات) ولكن يمكنك تكثيف الحشو اللغوي فقط.
  - **لا تحذف أبداً**: البريد الإلكتروني، رقم الهاتف، الرابط، اسم إدارة، اسم شخص، تاريخ، أو أي معلومة تواصل.
- **contact_email**: إذا ظهر بريد إلكتروني في المُدخلات (مثل staffrelations@gac.gov.sa) → استخرجه حرفياً في حقل contact_email **واحذفه من الـ subtitle** (سيُعرض كعنصر ميتا منفصل مع أيقونة).
- **contact_phone**: إذا ظهر رقم هاتف → استخرجه حرفياً في contact_phone واحذفه من الـ subtitle.
- **الأرقام والنسب المئوية**: تُحفظ حرفياً في stats أو subtitle — لا تُقرَّب، لا تُحوَّل.
`.trim();

  try {
    const parsed: any = await aiJSONWithFallback(prompt, { maxTokens: 3000 });

    if (!parsed?.variants || !Array.isArray(parsed.variants)) {
      throw new Error("AI لم يُرجع تنويعات صالحة");
    }

    const validIconNames = new Set(ICON_LIBRARY.map((i) => i.name));
    const validLayouts: LayoutType[] = ["stats-hero", "hero", "grid", "split", "typography"];

    const logoUrl = "/brand-assets/logos/gac-white.png";
    const extracted = parsed.extracted || {};

    // ═══ كشف وجود أرقام في المدخلات (للتحقق من عدم اختراع AI للإحصائيات) ═══
    const inputText = [raw_data, headline, subtitle].filter(Boolean).join(" ");
    const hasNumbersInInput = /\d/.test(inputText);

    // ═══ تطبيع الإحصائيات: 0 إلى 3 حسب الموجود فعلياً (لا حشو) ═══
    const rawStats: any[] = Array.isArray(extracted.stats) ? extracted.stats : [];
    const cleanStats: IconEventStat[] = rawStats
      .filter((s: any) => s && s.value && String(s.value).trim() && String(s.value).trim() !== "—")
      .slice(0, 3)
      .map((s: any) => ({
        icon: validIconNames.has(s.icon) ? s.icon : "sparkles",
        value: String(s.value).trim(),
        label: String(s.label || "").trim(),
      }));

    // إذا لا أرقام في المدخلات → إجبار stats على الفراغ (منع اختراع AI)
    const stats: IconEventStat[] = hasNumbersInInput ? cleanStats : [];

    // ═══ الحقول: تفضيل مدخلات المستخدم، ثم AI، الإدارة والهاشتاق لا يُخترعان ═══
    const finalHeadline = headline?.trim() || extracted.headline || "عنوان الفعالية";

    // ═══ استخراج حاسم للبريد/الهاتف من النص الخام (مستقل عن AI) ═══
    const rawFull = String(raw_data || "").trim();
    const emailRegex = /[a-zA-Z0-9._+-]+@[a-zA-Z0-9-]+(?:\.[a-zA-Z0-9-]+)+/;
    const phoneRegex = /(?:\+?\d[\d\s\-]{7,}\d)|(?:\b0\d{8,}\b)/;
    const emailMatch = rawFull.match(emailRegex);
    const phoneMatch = rawFull.match(phoneRegex);
    const finalContactEmail = emailMatch ? emailMatch[0] : (extracted.contact_email || "").trim();
    const finalContactPhone = phoneMatch ? phoneMatch[0] : (extracted.contact_phone || "").trim();

    // ═══ الـ subtitle: للحفاظ على النص الأصلي — استخدم راو_داتا إذا توفر ═══
    let finalSubtitle = subtitle?.trim() || "";
    if (!finalSubtitle && rawFull) {
      // Use raw text, remove headline line + any contact email/phone (they're shown as meta)
      let cleanedRaw = rawFull;
      if (finalHeadline && cleanedRaw.startsWith(finalHeadline)) {
        cleanedRaw = cleanedRaw.slice(finalHeadline.length).trim();
      } else {
        // Try removing first line if it matches or resembles headline
        const lines = cleanedRaw.split(/\n/).map(l => l.trim()).filter(Boolean);
        if (lines.length > 1 && (lines[0] === finalHeadline || lines[0].includes(finalHeadline.slice(0, 15)))) {
          cleanedRaw = lines.slice(1).join("\n").trim();
        }
      }
      // Remove trailing/inline email + phone (they render as meta chips)
      if (finalContactEmail) cleanedRaw = cleanedRaw.replace(finalContactEmail, "").trim();
      if (finalContactPhone) cleanedRaw = cleanedRaw.replace(finalContactPhone, "").trim();
      // Collapse multiple newlines/spaces, drop dangling colons/labels like "...:"
      cleanedRaw = cleanedRaw
        .replace(/[\u064B-\u0652]/g, "") // leave diacritics as-is; noop safe
        .replace(/[\r\n]+/g, " ")
        .replace(/\s{2,}/g, " ")
        .replace(/[:\uFF1A]\s*$/g, "")
        .trim();
      finalSubtitle = cleanedRaw;
    }
    if (!finalSubtitle) finalSubtitle = (extracted.subtitle || "").trim();

    // الإدارة والهاشتاق: فقط من مدخل المستخدم — لا نأخذ من AI لتجنب الاختراع
    const finalDepartment = department?.trim() || "";
    const finalHashtag = hashtag?.trim() || "";

    // ═══ تطبيع layouts: منع التكرار الكامل وضمان التنوع ═══
    const requestedLayouts: LayoutType[] = parsed.variants
      .slice(0, 3)
      .map((v: any) => (validLayouts.includes(v.layout) ? v.layout : "hero"));

    // إذا لا أرقام → استبدل أي stats-hero بـ hero (منع إحصائيات فارغة)
    const adjustedLayouts: LayoutType[] = requestedLayouts.map((l) =>
      !hasNumbersInInput && l === "stats-hero" ? "hero" : l
    );

    // ضمان التنوع: إذا كلها متطابقة، ابدل الثانية والثالثة
    if (adjustedLayouts[0] === adjustedLayouts[1] && adjustedLayouts[1] === adjustedLayouts[2]) {
      const alternatives: LayoutType[] = hasNumbersInInput
        ? ["stats-hero", "split", "typography"]
        : ["hero", "split", "typography"];
      adjustedLayouts[0] = alternatives[0];
      adjustedLayouts[1] = alternatives[1];
      adjustedLayouts[2] = alternatives[2];
    }

    // ═══ ضمان وجود تصميم typography واحد على الأقل (نص فقط، بدون أيقونة رئيسية) ═══
    if (!adjustedLayouts.includes("typography")) {
      // استبدل الثالث (الأقل أولوية) بـ typography
      adjustedLayouts[2] = "typography";
    }

    const variants = parsed.variants.slice(0, 3).map((v: any, idx: number) => {
      const layout: LayoutType = adjustedLayouts[idx] || "hero";
      // ═══ الهوية الرسمية: teal دائماً لكل التنويعات (لا استثناء) ═══
      const color_scheme: ColorScheme = "teal";
      const main_icon = validIconNames.has(v.main_icon) ? v.main_icon : "sparkles";
      const supporting = (Array.isArray(v.supporting_icons) ? v.supporting_icons : [])
        .filter((s: string) => validIconNames.has(s))
        .slice(0, 3);

      // ═══ الإحصائيات فقط للـ layouts التي تعرضها (stats-hero, grid) ═══
      const layoutUsesStats = layout === "stats-hero" || layout === "grid";
      const finalStats = layoutUsesStats && stats.length > 0 ? stats : undefined;

      const input: IconEventInput = {
        headline: finalHeadline,
        subtitle: finalSubtitle,
        department: finalDepartment || undefined,
        hashtag: finalHashtag || undefined,
        contact_email: finalContactEmail || undefined,
        contact_phone: finalContactPhone || undefined,
        date,
        time,
        location,
        main_icon,
        supporting_icons: supporting,
        stats: finalStats,
        color_scheme,
        layout,
        size: size!,
        logo_url: logoUrl,
      } as any;

      const html = renderIconEventDesign(input);

      return {
        id: `variant-${idx + 1}`,
        layout,
        main_icon,
        supporting_icons: supporting,
        color_scheme,
        headline: input.headline,
        subtitle: input.subtitle,
        department: input.department,
        hashtag: input.hashtag,
        stats: input.stats,
        rationale: v.rationale || "",
        html,
        input,
      };
    });

    res.json({ ok: true, variants, count: variants.length, extracted });
  } catch (e: any) {
    req.log?.error({ err: e?.message }, "icon-event generate failed");

    // Fallback: توليد 3 تنويعات افتراضية محلياً بدون AI
    const fallbackIcon =
      event_type === "workshop" ? "graduation-cap" :
      event_type === "meeting" ? "users" :
      event_type === "launch" ? "rocket" :
      event_type === "social" ? "party-popper" : "sparkles";

    const fallbackHeadline = headline || (raw_data ? raw_data.split("\n")[0].slice(0, 60) : "فعالية");
    const fallbackStats: IconEventStat[] = [
      { icon: "users", value: "—", label: "مشاركة" },
      { icon: "building", value: "—", label: "إدارة" },
      { icon: "calendar", value: "—", label: "فعالية" },
    ];

    const layouts: LayoutType[] = ["stats-hero", "hero", "split"];
    const colors: ColorScheme[] = ["teal", "blue", "green"];

    const fallbackVariants = layouts.map((layout, idx) => {
      const input: IconEventInput = {
        headline: fallbackHeadline,
        subtitle,
        department,
        hashtag,
        date,
        time,
        location,
        main_icon: fallbackIcon,
        supporting_icons: ["calendar", "clock", "map-pin"],
        stats: fallbackStats,
        color_scheme: colors[idx],
        layout,
        size: size!,
        logo_url: "/brand-assets/logos/gac-white.png",
      };
      return {
        id: `variant-${idx + 1}`,
        layout,
        main_icon: fallbackIcon,
        supporting_icons: ["calendar", "clock", "map-pin"],
        color_scheme: colors[idx],
        headline: fallbackHeadline,
        subtitle: subtitle || "",
        department,
        hashtag,
        stats: fallbackStats,
        rationale: "تنويعة افتراضية (تعذّر الاتصال بـ AI)",
        html: renderIconEventDesign(input),
        input,
      };
    });

    res.json({
      ok: true,
      variants: fallbackVariants,
      count: 3,
      warning: "تم استخدام التنويعات الافتراضية لتعذّر الاتصال بـ AI — يمكنك إعادة المحاولة",
    });
  }
});

// ─── POST render: HTML → PNG (HD 3x) ────────────────────────────────────────
router.post("/designs/icon-event/render", async (req: Request, res: Response) => {
  const { html, size, quality } = req.body as { html?: string; size?: SizePreset; quality?: "hd" | "ultra" };

  if (!html || !size || !SIZE_MAP[size]) {
    res.status(400).json({ error: "html و size مطلوبان" });
    return;
  }

  const { width, height } = SIZE_MAP[size];
  // HD = 3x device scale factor (افتراضي)، ultra = 4x
  const deviceScaleFactor = quality === "ultra" ? 4 : 3;

  try {
    const puppeteer = await import("puppeteer-core");
    const fs = await import("node:fs");
    const systemChromium = process.env.PUPPETEER_EXECUTABLE_PATH;
    let executablePath: string;
    let extraArgs: string[] = [];

    if (systemChromium && fs.existsSync(systemChromium)) {
      executablePath = systemChromium;
    } else {
      const chromium = await import("@sparticuz/chromium-min");
      const CHROMIUM_URL =
        process.env.CHROMIUM_URL ||
        "https://github.com/Sparticuz/chromium/releases/download/v131.0.1/chromium-v131.0.1-pack.tar";
      executablePath = await chromium.default.executablePath(CHROMIUM_URL);
      extraArgs = chromium.default.args;
    }

    const browser = await puppeteer.default.launch({
      args: [...extraArgs, "--no-sandbox", "--disable-setuid-sandbox", "--disable-dev-shm-usage"],
      defaultViewport: { width, height, deviceScaleFactor },
      executablePath,
      headless: true,
    });

    const page = await browser.newPage();
    await page.setViewport({ width, height, deviceScaleFactor });
    await page.setContent(html, { waitUntil: "networkidle0", timeout: 30000 });

    // إعطاء وقت كافٍ للخطوط والصور (الشعار)
    await new Promise((r) => setTimeout(r, 1500));

    // محاولة انتظار جاهزية الصور (الشعار)
    try {
      await page.evaluate(async () => {
        const imgs = Array.from(document.images);
        await Promise.all(
          imgs.map((img) => {
            if (img.complete) return Promise.resolve();
            return new Promise((resolve) => {
              img.addEventListener("load", () => resolve(null));
              img.addEventListener("error", () => resolve(null));
            });
          })
        );
      });
    } catch {}

    const posterHandle = await page.$(".poster");
    const screenshot = posterHandle
      ? await posterHandle.screenshot({
          type: "png",
          omitBackground: false,
          captureBeyondViewport: true,
        })
      : await page.screenshot({
          type: "png",
          fullPage: false,
          clip: { x: 0, y: 0, width, height },
          captureBeyondViewport: true,
        });

    await browser.close();

    const buffer = Buffer.from(screenshot);

    // وضع التنزيل المباشر: يرجع PNG في الاستجابة مع Content-Disposition
    // هذا يتجنب مشاكل CORS/blob/cache على iOS Safari
    const wantsDownload =
      req.query.download === "1" ||
      req.query.download === "true" ||
      (req.body && (req.body as any).download === true);

    if (wantsDownload) {
      const filename = `gac-design-${size}-${Date.now()}.png`;
      res.setHeader("Content-Type", "image/png");
      res.setHeader("Content-Disposition", `attachment; filename="${filename}"`);
      res.setHeader("Content-Length", buffer.length.toString());
      res.setHeader("Cache-Control", "no-store");
      res.setHeader("X-Render-Width", String(width * deviceScaleFactor));
      res.setHeader("X-Render-Height", String(height * deviceScaleFactor));
      res.status(200).end(buffer);
      return;
    }

    const url = await objectStorage.saveComposedDesign(buffer);

    res.json({
      ok: true,
      url,
      size,
      width: width * deviceScaleFactor,
      height: height * deviceScaleFactor,
      quality: quality === "ultra" ? "ultra (4x)" : "hd (3x)",
    });
  } catch (e: any) {
    req.log?.error({ err: e?.message, stack: e?.stack }, "icon-event render failed");
    res.status(500).json({
      error: "فشل تحويل التصميم إلى صورة",
      detail: e?.message,
      hint: "تأكد من توفر Chromium في الخادم",
    });
  }
});

export default router;
