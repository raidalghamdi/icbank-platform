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

export default router;
