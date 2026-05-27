import { Router, type Request, type Response } from "express";
import { db } from "@workspace/db";
import {
  brandLogosTable,
  brandFontsTable,
  designTemplatesTable,
  insertBrandLogoSchema,
  insertBrandFontSchema,
  insertDesignTemplateSchema,
} from "@workspace/db";
import { eq, desc } from "drizzle-orm";
import { requireAdmin } from "../middleware/auth";
import { ObjectStorageService } from "../lib/objectStorage";

const router = Router();
const objectStorage = new ObjectStorageService();

// All /designs/* routes require admin role
router.use(requireAdmin);

// ─── Templates CRUD ───────────────────────────────────────────────────────────
router.get("/designs/templates", async (_req: Request, res: Response) => {
  const templates = await db.select().from(designTemplatesTable).orderBy(desc(designTemplatesTable.createdAt));
  res.json(templates);
});

router.get("/designs/templates/:id", async (req: Request, res: Response) => {
  const id = Number(req.params.id);
  const [row] = await db.select().from(designTemplatesTable).where(eq(designTemplatesTable.id, id));
  if (!row) { res.status(404).json({ error: "القالب غير موجود" }); return; }
  res.json(row);
});

router.post("/designs/templates", async (req: Request, res: Response) => {
  const body = insertDesignTemplateSchema.parse(req.body);
  const [row] = await db.insert(designTemplatesTable).values(body).returning();
  res.status(201).json(row);
});

// ─── Seed one test template ────────────────────────────────────────────────────
router.post("/designs/templates/seed-test", async (_req: Request, res: Response) => {
  const existing = await db.select().from(designTemplatesTable).limit(1);
  if (existing.length > 0) {
    res.json({ ok: true, skipped: true, template: existing[0] }); return;
  }
  const [row] = await db.insert(designTemplatesTable).values({
    templateNameAr: "قالب تجريبي — إعلان عام",
    category: "general",
    canvasWidth: 1920,
    canvasHeight: 1080,
    backgroundPanelConfig: {
      x: 0, y: 700, width: 1920, height: 380, color: "#1a3a6b", opacity: 0.88,
    },
    textSlots: [
      {
        key: "title", label_ar: "العنوان الرئيسي",
        x: 80, y: 740, width: 1760, height: 120,
        default_font_size: 72, max_words: 10, alignment: "right", color: "#ffffff",
      },
      {
        key: "body", label_ar: "النص التفصيلي",
        x: 80, y: 880, width: 1760, height: 160,
        default_font_size: 40, max_words: 30, alignment: "right", color: "#d0dcff",
      },
    ],
    logoSlots: [
      { key: "logo_main", x: 80, y: 30, width: 210, height: 130 },
    ],
  }).returning();
  res.status(201).json({ ok: true, skipped: false, template: row });
});

// ─── Logo Upload URL ──────────────────────────────────────────────────────────
router.post("/designs/logos/upload-url", async (req: Request, res: Response) => {
  const { fileName, contentType } = req.body as { fileName?: string; contentType?: string };
  if (!fileName) { res.status(400).json({ error: "fileName مطلوب" }); return; }
  const result = await objectStorage.getDesignsUploadURL({ folder: "logos", fileName, contentType });
  res.json(result);
});

// ─── Logos CRUD ───────────────────────────────────────────────────────────────
router.get("/designs/logos", async (_req: Request, res: Response) => {
  const logos = await db.select().from(brandLogosTable).orderBy(desc(brandLogosTable.uploadedAt));
  res.json(logos);
});

router.post("/designs/logos", async (req: Request, res: Response) => {
  const body = insertBrandLogoSchema.parse(req.body);
  const [row] = await db.insert(brandLogosTable).values(body).returning();
  res.status(201).json(row);
});

router.delete("/designs/logos/:id", async (req: Request, res: Response) => {
  const id = Number(req.params.id);
  const [row] = await db.select().from(brandLogosTable).where(eq(brandLogosTable.id, id));
  if (!row) { res.status(404).json({ error: "الشعار غير موجود" }); return; }
  if (row.fileUrl?.startsWith("/objects/designs/")) {
    await objectStorage.deleteDesignObject(row.fileUrl).catch(() => {});
  }
  await db.delete(brandLogosTable).where(eq(brandLogosTable.id, id));
  res.json({ ok: true });
});

// ─── Font Upload URL ──────────────────────────────────────────────────────────
router.post("/designs/fonts/upload-url", async (req: Request, res: Response) => {
  const { fileName, contentType } = req.body as { fileName?: string; contentType?: string };
  if (!fileName) { res.status(400).json({ error: "fileName مطلوب" }); return; }
  const result = await objectStorage.getDesignsUploadURL({ folder: "fonts", fileName, contentType });
  res.json(result);
});

// ─── Fonts CRUD ───────────────────────────────────────────────────────────────
router.get("/designs/fonts", async (_req: Request, res: Response) => {
  const fonts = await db.select().from(brandFontsTable).orderBy(desc(brandFontsTable.uploadedAt));
  res.json(fonts);
});

router.post("/designs/fonts", async (req: Request, res: Response) => {
  const body = insertBrandFontSchema.parse(req.body);
  const [row] = await db.insert(brandFontsTable).values(body).returning();
  res.status(201).json(row);
});

router.patch("/designs/fonts/:id/default", async (req: Request, res: Response) => {
  const id = Number(req.params.id);
  await db.update(brandFontsTable).set({ isDefault: false });
  const [row] = await db
    .update(brandFontsTable)
    .set({ isDefault: true })
    .where(eq(brandFontsTable.id, id))
    .returning();
  if (!row) { res.status(404).json({ error: "الخط غير موجود" }); return; }
  res.json(row);
});

router.delete("/designs/fonts/:id", async (req: Request, res: Response) => {
  const id = Number(req.params.id);
  const [row] = await db.select().from(brandFontsTable).where(eq(brandFontsTable.id, id));
  if (!row) { res.status(404).json({ error: "الخط غير موجود" }); return; }
  if (row.fontFileUrl?.startsWith("/objects/designs/")) {
    await objectStorage.deleteDesignObject(row.fontFileUrl).catch(() => {});
  }
  await db.delete(brandFontsTable).where(eq(brandFontsTable.id, id));
  res.json({ ok: true });
});

// ─── AI Background Generation ────────────────────────────────────────────────
//
// TRIAL MODE — Nano Banana only (gemini-2.5-flash-image × 4)
// ─────────────────────────────────────────────────────────────
// After the free trial, re-enable OpenAI (gpt-image-1) by setting:
//   const USE_OPENAI = true;
// and wiring up OPENAI_API_KEY below the Google key check.
// ─────────────────────────────────────────────────────────────
const USE_OPENAI = false; // ← flip to true after trial to re-enable gpt-image-1

router.post("/designs/generate-backgrounds", async (req: Request, res: Response) => {
  void USE_OPENAI; // referenced above — will be used when re-enabled

  const { prompt, templateId } = req.body as { prompt?: string; templateId?: number };
  if (!prompt?.trim()) { res.status(400).json({ error: "prompt مطلوب" }); return; }

  const googleKey =
    process.env["GEMINI_API_KEY"] ??
    process.env["GOOGLE_AI_API_KEY"] ??
    process.env["AI_INTEGRATIONS_GEMINI_API_KEY"];
  if (!googleKey) { res.status(503).json({ error: "GEMINI_API_KEY غير مضبوط على الخادم" }); return; }

  // Build a template-aware spatial hint so the model leaves room for the text panel
  let templateHint = "";
  if (templateId) {
    const [tpl] = await db
      .select()
      .from(designTemplatesTable)
      .where(eq(designTemplatesTable.id, templateId));
    if (tpl?.backgroundPanelConfig) {
      const bp = tpl.backgroundPanelConfig as { x: number; y: number; width: number; height: number };
      const H = tpl.canvasHeight || 1080;
      if (bp.y > H * 0.55) {
        templateHint =
          "Leave the bottom third of the image visually calm, low-contrast, and uncluttered — a semi-transparent text-overlay panel will cover that region.";
      } else if (bp.y < H * 0.3 && bp.height < H * 0.4) {
        templateHint =
          "Leave the top third of the image visually calm and uncluttered — a semi-transparent text-overlay panel will cover that region.";
      } else {
        templateHint =
          "Leave a calm, low-contrast visual zone in the lower portion for a text overlay panel.";
      }
    }
  }

  const fullPrompt = [
    prompt.trim(),
    templateHint,
    "Professional high-quality photo, 16:9 widescreen aspect ratio, no text or watermarks.",
  ]
    .filter(Boolean)
    .join(" ");

  // TESTING MODE: 1 image to save quota. Change GEN_COUNT back to 4 for production.
  const GEN_COUNT = 1; // ← change to 4 when done testing
  req.log.info({ fullPrompt, GEN_COUNT }, "Calling Nano Banana (gemini-2.5-flash-image)");

  const GEMINI_URL = `https://generativelanguage.googleapis.com/v1beta/models/gemini-2.5-flash-image:generateContent?key=${googleKey}`;

  type GeminiPart =
    | { text: string }
    | { inlineData: { mimeType: string; data: string } };

  const generateOne = async (seed: number): Promise<{ url: string; source: string }> => {
    const variation = seed === 0 ? "" : ` (style variation ${seed + 1})`;
    const r = await fetch(GEMINI_URL, {
      method: "POST",
      headers: { "Content-Type": "application/json" },
      body: JSON.stringify({
        contents: [{ parts: [{ text: fullPrompt + variation }] }],
        generationConfig: { responseModalities: ["IMAGE", "TEXT"] },
      }),
      signal: AbortSignal.timeout(120_000),
    });

    if (!r.ok) {
      const errText = await r.text();
      // Detect quota / rate-limit errors and surface a user-friendly Arabic message
      if (r.status === 429 || errText.includes("quota") || errText.includes("RESOURCE_EXHAUSTED")) {
        throw new Error("QUOTA_EXCEEDED");
      }
      throw new Error(`Gemini API ${r.status}: ${errText.slice(0, 200)}`);
    }

    const data = (await r.json()) as {
      candidates?: Array<{ content: { parts: GeminiPart[] } }>;
    };

    const parts = data.candidates?.[0]?.content?.parts ?? [];
    const imgPart = parts.find(
      (p): p is { inlineData: { mimeType: string; data: string } } => "inlineData" in p
    );

    if (!imgPart) throw new Error(`No image in Gemini response (seed ${seed})`);

    // Save the raw bytes exactly as Gemini returns them — no resize/crop
    const buf = Buffer.from(imgPart.inlineData.data, "base64");
    const objectPath = await objectStorage.saveGeneratedBackground(buf, imgPart.inlineData.mimeType || "image/png");
    return { url: objectPath, source: "gemini" };
  };

  // Run GEN_COUNT in parallel; return whatever succeeds (Promise.allSettled)
  const results = await Promise.allSettled(Array.from({ length: GEN_COUNT }, (_, i) => generateOne(i)));

  const saved = results
    .filter((r): r is PromiseFulfilledResult<{ url: string; source: string }> => r.status === "fulfilled")
    .map((r) => r.value);

  if (saved.length === 0) {
    const firstErr = (results[0] as PromiseRejectedResult).reason?.message ?? "unknown";
    req.log.error({ firstErr }, "All Gemini image generations failed");

    // Friendly Arabic message for quota / rate-limit errors
    if (firstErr === "QUOTA_EXCEEDED") {
      res.status(429).json({
        error: "تم تجاوز حد التوليد المؤقت، انتظر دقيقة وحاول مجدداً.",
        code: "QUOTA_EXCEEDED",
      });
      return;
    }

    res.status(502).json({ error: "فشل التوليد من Nano Banana", detail: firstErr });
    return;
  }

  req.log.info({ count: saved.length }, "Backgrounds saved to storage");
  res.json({ images: saved });
});

export default router;
