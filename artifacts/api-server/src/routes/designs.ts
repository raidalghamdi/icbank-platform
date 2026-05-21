import { Router, type Request, type Response } from "express";
import { db } from "@workspace/db";
import {
  brandLogosTable,
  brandFontsTable,
  insertBrandLogoSchema,
  insertBrandFontSchema,
} from "@workspace/db";
import { eq, desc } from "drizzle-orm";
import { requireAdmin } from "../middleware/auth";
import { ObjectStorageService } from "../lib/objectStorage";

const router = Router();
const objectStorage = new ObjectStorageService();

// All /designs/* routes require admin role
router.use(requireAdmin);

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
