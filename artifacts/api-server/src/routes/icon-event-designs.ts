/**
 * Icon Event Designs API
 * توليد تصاميم الفعاليات بالأيقونات تلقائياً باستخدام AI
 *
 * Endpoints:
 *   POST /api/designs/icon-event/generate  → يولّد 3 تنويعات (AI يقترح + composer يرسم)
 *   POST /api/designs/icon-event/render    → يحول HTML إلى PNG ويحفظه
 *   GET  /api/designs/icon-event/icons     → قائمة الأيقونات المتاحة
 */

import { Router, type Request, type Response } from "express";
import { requireAuth } from "../middleware/auth";
import { aiJSONWithFallback } from "../lib/aiProviders";
import { ICON_LIBRARY, iconListForAI } from "../composer/icon-library";
import {
  renderIconEventDesign,
  type IconEventInput,
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

// ─── POST generate: AI يولّد 3 تنويعات ───────────────────────────────────────
router.post("/designs/icon-event/generate", async (req: Request, res: Response) => {
  const {
    headline,
    subtitle,
    date,
    time,
    location,
    event_type,
    size,
  } = req.body as {
    headline?: string;
    subtitle?: string;
    date?: string;
    time?: string;
    location?: string;
    event_type?: "workshop" | "meeting" | "launch" | "social";
    size?: SizePreset;
  };

  if (!headline || headline.trim().length < 3) {
    res.status(400).json({ error: "العنوان مطلوب" });
    return;
  }
  if (!size || !SIZE_MAP[size]) {
    res.status(400).json({ error: "المقاس مطلوب (square | story | landscape)" });
    return;
  }

  // === AI Prompt ===
  const prompt = `
أنت مصمم بصري ذكي. ستحلّل بيانات فعالية وتختار:
1. أيقونة رئيسية مناسبة للفكرة
2. 3 أيقونات داعمة (تاريخ/وقت/مكان أو فكرة مكملة)
3. نظام ألوان من هوية GAC: blue | green | cyan | navy
4. تخطيط لكل تنويعة (hero, grid, split)
5. صياغة عنوان قصير محسّن (5-8 كلمات بحد أقصى)

البيانات:
- العنوان الأصلي: "${headline}"
- الوصف: "${subtitle || "-"}"
- التاريخ: "${date || "-"}"
- الوقت: "${time || "-"}"
- المكان: "${location || "-"}"
- نوع الفعالية: "${event_type || "غير محدد"}"
- المقاس: "${size}"

الأيقونات المتاحة (50 أيقونة Lucide — اختر من هذه القائمة فقط):
${iconListForAI()}

أعد JSON دقيق بهذا الشكل (3 تنويعات مختلفة):
{
  "variants": [
    {
      "layout": "hero",
      "main_icon": "<اسم من القائمة>",
      "supporting_icons": ["<اسم>", "<اسم>", "<اسم>"],
      "headline": "<عنوان محسّن قصير>",
      "subtitle": "<وصف موجز سطر واحد>",
      "color_scheme": "blue",
      "rationale": "<سطر يشرح الاختيار>"
    },
    {
      "layout": "grid",
      "main_icon": "...",
      "supporting_icons": ["...","...","..."],
      "headline": "...",
      "subtitle": "...",
      "color_scheme": "green",
      "rationale": "..."
    },
    {
      "layout": "split",
      "main_icon": "...",
      "supporting_icons": ["...","...","..."],
      "headline": "...",
      "subtitle": "...",
      "color_scheme": "cyan",
      "rationale": "..."
    }
  ]
}

قواعد صارمة:
- استخدم فقط أسماء أيقونات من القائمة أعلاه (مثل "rocket", "users", "calendar"...).
- color_scheme فقط من: blue, green, cyan, navy.
- layout فقط من: hero, grid, split (تنويعة لكل تخطيط).
- العنوان لا يتجاوز 8 كلمات.
- الوصف سطر واحد فقط.
- لا تضف أي شرح خارج JSON.
`.trim();

  try {
    const parsed: any = await aiJSONWithFallback(prompt, { maxTokens: 2000 });

    if (!parsed?.variants || !Array.isArray(parsed.variants) || parsed.variants.length === 0) {
      throw new Error("AI لم يُرجع تنويعات صالحة");
    }

    // التحقق وتطبيع الأيقونات
    const validIconNames = new Set(ICON_LIBRARY.map((i) => i.name));
    const validLayouts: LayoutType[] = ["hero", "grid", "split"];
    const validColors: ColorScheme[] = ["blue", "green", "cyan", "navy"];

    const logoUrl = "/brand-assets/logos/gac-white.png";

    const variants = parsed.variants.slice(0, 3).map((v: any, idx: number) => {
      const layout: LayoutType = validLayouts.includes(v.layout) ? v.layout : validLayouts[idx % 3];
      const color_scheme: ColorScheme = validColors.includes(v.color_scheme)
        ? v.color_scheme
        : (["blue", "green", "cyan"] as ColorScheme[])[idx % 3];
      const main_icon = validIconNames.has(v.main_icon) ? v.main_icon : "sparkles";
      const supporting = (Array.isArray(v.supporting_icons) ? v.supporting_icons : [])
        .filter((s: string) => validIconNames.has(s))
        .slice(0, 3);

      const input: IconEventInput = {
        headline: v.headline?.trim() || headline,
        subtitle: v.subtitle?.trim() || subtitle,
        date,
        time,
        location,
        main_icon,
        supporting_icons: supporting,
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
        rationale: v.rationale || "",
        html,
        input,
      };
    });

    res.json({ ok: true, variants, count: variants.length });
  } catch (e: any) {
    req.log?.error({ err: e?.message }, "icon-event generate failed");

    // Fallback: توليد 3 تنويعات افتراضية محلياً بدون AI
    const fallbackIcon =
      event_type === "workshop" ? "graduation-cap" :
      event_type === "meeting" ? "users" :
      event_type === "launch" ? "rocket" :
      event_type === "social" ? "party-popper" : "sparkles";

    const fallbackVariants = (["hero", "grid", "split"] as LayoutType[]).map((layout, idx) => {
      const color: ColorScheme = (["blue", "green", "cyan"] as ColorScheme[])[idx];
      const input: IconEventInput = {
        headline,
        subtitle,
        date,
        time,
        location,
        main_icon: fallbackIcon,
        supporting_icons: ["calendar", "clock", "map-pin"],
        color_scheme: color,
        layout,
        size: size!,
        logo_url: "/brand-assets/logos/gac-white.png",
      };
      return {
        id: `variant-${idx + 1}`,
        layout,
        main_icon: fallbackIcon,
        supporting_icons: ["calendar", "clock", "map-pin"],
        color_scheme: color,
        headline,
        subtitle: subtitle || "",
        rationale: "تنويعة افتراضية (تعذّر الاتصال بـ AI)",
        html: renderIconEventDesign(input),
        input,
      };
    });

    res.json({
      ok: true,
      variants: fallbackVariants,
      count: 3,
      warning: "تم استخدام التنويعات الافتراضية لتعذّر الاتصال بـ AI",
    });
  }
});

// ─── POST render: HTML → PNG ────────────────────────────────────────────────
router.post("/designs/icon-event/render", async (req: Request, res: Response) => {
  const { html, size } = req.body as { html?: string; size?: SizePreset };

  if (!html || !size || !SIZE_MAP[size]) {
    res.status(400).json({ error: "html و size مطلوبان" });
    return;
  }

  const { width, height } = SIZE_MAP[size];

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
      defaultViewport: { width, height, deviceScaleFactor: 2 },
      executablePath,
      headless: true,
    });

    const page = await browser.newPage();
    await page.setViewport({ width, height, deviceScaleFactor: 2 });
    await page.setContent(html, { waitUntil: "networkidle0", timeout: 30000 });

    // إعطاء وقت كافٍ للخطوط
    await new Promise((r) => setTimeout(r, 800));

    const posterHandle = await page.$(".poster");
    const screenshot = posterHandle
      ? await posterHandle.screenshot({ type: "png", omitBackground: false })
      : await page.screenshot({ type: "png", fullPage: false, clip: { x: 0, y: 0, width, height } });

    await browser.close();

    const buffer = Buffer.from(screenshot);

    // حفظ في object storage
    const url = await objectStorage.saveComposedDesign(buffer);

    res.json({ ok: true, url, size, width, height });
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
