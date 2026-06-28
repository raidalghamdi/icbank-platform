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
  const prompt = `
أنت مصمم بصري ذكي للهيئة العامة للمنافسة (GAC). ستحلل بيانات الفعالية (قد تكون نصاً خاماً غير منظم) وتستخرج منها:
1. عنوان رئيسي قصير (2-4 كلمات)
2. عنوان فرعي (سطر واحد يصف الفعالية)
3. اسم الإدارة (إن لم يُذكر اتركه فارغاً)
4. هاشتاج (إن لم يوجد ابتكر واحداً مناسباً)
5. ثلاث إحصائيات (الأهم) — كل واحدة: أيقونة + قيمة + وصف قصير
6. أيقونة رئيسية (للتخطيطات الأخرى) + 3 أيقونات داعمة
7. نظام ألوان من: teal | blue | green | cyan | navy (افتراضي teal للتصميم الرسمي)

البيانات (قد تكون خاماً):
"""
${raw_data || `العنوان: ${headline}\nالوصف: ${subtitle || "-"}\nالإدارة: ${department || "-"}\nالتاريخ: ${date || "-"}\nالوقت: ${time || "-"}\nالمكان: ${location || "-"}\nالنوع: ${event_type || "-"}`}
"""

${department ? `ملاحظة: اسم الإدارة المؤكد هو "${department}" — استخدمه كما هو.` : ""}
${hashtag ? `ملاحظة: الهاشتاج المؤكد هو "${hashtag}".` : ""}

الأيقونات المتاحة (اختر فقط من هذه القائمة):
${iconListForAI()}

أعد JSON بهذا الشكل بالضبط (3 تنويعات — أولها stats-hero مطابق للهوية الرسمية):
{
  "extracted": {
    "headline": "<عنوان قصير 2-4 كلمات>",
    "subtitle": "<سطر فرعي>",
    "department": "<اسم الإدارة أو سلسلة فارغة>",
    "hashtag": "<#هاشتاج>",
    "stats": [
      { "icon": "<اسم أيقونة>", "value": "<رقم/قيمة مثل 135+ أو 20>", "label": "<وصف قصير سطرين كحد أقصى>" },
      { "icon": "...", "value": "...", "label": "..." },
      { "icon": "...", "value": "...", "label": "..." }
    ]
  },
  "variants": [
    {
      "layout": "stats-hero",
      "color_scheme": "teal",
      "main_icon": "<أيقونة من القائمة>",
      "supporting_icons": ["<اسم>","<اسم>","<اسم>"],
      "rationale": "<سطر يشرح لماذا هذا الاختيار>"
    },
    {
      "layout": "hero",
      "color_scheme": "blue",
      "main_icon": "...",
      "supporting_icons": ["...","...","..."],
      "rationale": "..."
    },
    {
      "layout": "split",
      "color_scheme": "green",
      "main_icon": "...",
      "supporting_icons": ["...","...","..."],
      "rationale": "..."
    }
  ]
}

قواعد صارمة:
- TODO: استخرج كل إحصائية من البيانات (الأرقام، النسب، الأعداد) واختر أيقونة مناسبة لكل واحدة.
- استخدم فقط أسماء أيقونات من القائمة (مثل "users", "building", "brain", "graduation-cap"...).
- color_scheme فقط من: teal, blue, green, cyan, navy.
- layout فقط من: stats-hero, hero, grid, split.
- العنوان الرئيسي 2-4 كلمات بحد أقصى.
- إن لم توجد إحصائيات في البيانات، اقترح إحصائيات منطقية حسب نوع الفعالية.
- لا تضف أي شرح خارج JSON.
`.trim();

  try {
    const parsed: any = await aiJSONWithFallback(prompt, { maxTokens: 3000 });

    if (!parsed?.variants || !Array.isArray(parsed.variants)) {
      throw new Error("AI لم يُرجع تنويعات صالحة");
    }

    const validIconNames = new Set(ICON_LIBRARY.map((i) => i.name));
    const validLayouts: LayoutType[] = ["stats-hero", "hero", "grid", "split"];
    const validColors: ColorScheme[] = ["teal", "blue", "green", "cyan", "navy"];

    const logoUrl = "/brand-assets/logos/gac-white.png";
    const extracted = parsed.extracted || {};

    // تطبيع الإحصائيات
    const rawStats: any[] = Array.isArray(extracted.stats) ? extracted.stats : [];
    const stats: IconEventStat[] = rawStats.slice(0, 3).map((s: any) => ({
      icon: validIconNames.has(s.icon) ? s.icon : "sparkles",
      value: String(s.value || "—"),
      label: String(s.label || ""),
    }));

    // استخدام القيم المقدّمة من المستخدم إن وُجدت، وإلا المستخرجة من AI
    const finalHeadline = headline?.trim() || extracted.headline || "عنوان الفعالية";
    const finalSubtitle = subtitle?.trim() || extracted.subtitle || "";
    const finalDepartment = department?.trim() || extracted.department || "";
    const finalHashtag = hashtag?.trim() || extracted.hashtag || "";

    const variants = parsed.variants.slice(0, 3).map((v: any, idx: number) => {
      const layout: LayoutType = validLayouts.includes(v.layout) ? v.layout : validLayouts[idx % 4];
      const color_scheme: ColorScheme = validColors.includes(v.color_scheme)
        ? v.color_scheme
        : (["teal", "blue", "green"] as ColorScheme[])[idx % 3];
      const main_icon = validIconNames.has(v.main_icon) ? v.main_icon : "sparkles";
      const supporting = (Array.isArray(v.supporting_icons) ? v.supporting_icons : [])
        .filter((s: string) => validIconNames.has(s))
        .slice(0, 3);

      const input: IconEventInput = {
        headline: finalHeadline,
        subtitle: finalSubtitle,
        department: finalDepartment || undefined,
        hashtag: finalHashtag || undefined,
        date,
        time,
        location,
        main_icon,
        supporting_icons: supporting,
        stats: stats.length > 0 ? stats : undefined,
        color_scheme,
        layout,
        size: size!,
        logo_url: logoUrl,
      };

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
