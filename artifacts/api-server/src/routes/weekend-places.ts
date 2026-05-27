import { Router, type Request, type Response } from "express";
import { db } from "@workspace/db";
import {
  weekendPlacesTable,
  insertWeekendPlaceSchema,
  weekendDraftsTable,
} from "@workspace/db";
import { eq, asc, desc } from "drizzle-orm";
import { requireAdmin } from "../middleware/auth";
import { ObjectStorageService } from "../lib/objectStorage";

const router = Router();
const objectStorage = new ObjectStorageService();

// ─── Public (auth-gated) endpoint: weekend page data ─────────────────────────
// Returns places (curated by admin) merged with the latest published AI draft
// (deals, podcasts, aiTools, matches, movies, summary). Riyadh-focused.
router.get("/wk2-data", async (_req: Request, res: Response) => {
  const placesRows = await db
    .select()
    .from(weekendPlacesTable)
    .where(eq(weekendPlacesTable.isActive, true))
    .orderBy(asc(weekendPlacesTable.sortOrder), asc(weekendPlacesTable.createdAt));

  const formatted = placesRows.map((p) => ({
    title: p.name,
    body: p.description,
    maps_query: p.mapsQuery || p.name,
    imageUrl: p.imageUrl || null,
    city: p.city,
    id: p.id,
  }));

  // Latest published AI draft (if any) supplies deals/podcasts/aiTools/etc.
  const [latestDraft] = await db
    .select()
    .from(weekendDraftsTable)
    .where(eq(weekendDraftsTable.status, "published"))
    .orderBy(desc(weekendDraftsTable.publishedAt))
    .limit(1);

  const c = (latestDraft?.content || {}) as Record<string, any>;

  res.json({
    places: formatted.length
      ? formatted
      : Array.isArray(c.places)
        ? c.places
        : [],
    deals: Array.isArray(c.deals) ? c.deals : [],
    podcasts: Array.isArray(c.podcasts) ? c.podcasts : [],
    aiTools: Array.isArray(c.aiTools) ? c.aiTools : [],
    matches: Array.isArray(c.matches) ? c.matches : [],
    movies: Array.isArray(c.movies) ? c.movies : [],
    summary: c.summary || null,
    publishedAt: latestDraft?.publishedAt ?? null,
    weekendDate: latestDraft?.weekendDate ?? null,
    city: "الرياض",
  });
});

// ─── Admin-only routes ────────────────────────────────────────────────────────
router.use("/weekend-places", requireAdmin);

// List all places (including inactive)
router.get("/weekend-places", async (_req: Request, res: Response) => {
  const places = await db
    .select()
    .from(weekendPlacesTable)
    .orderBy(asc(weekendPlacesTable.sortOrder), asc(weekendPlacesTable.createdAt));
  res.json(places);
});

// Get upload URL for place image
router.post(
  "/weekend-places/upload-url",
  async (req: Request, res: Response) => {
    const { fileName, contentType } = req.body as {
      fileName?: string;
      contentType?: string;
    };
    if (!fileName) {
      res.status(400).json({ error: "fileName مطلوب" });
      return;
    }
    const result = await objectStorage.getWeekendPlacesUploadURL({
      fileName,
      contentType,
    });
    res.json(result);
  }
);

// Create a new place
router.post("/weekend-places", async (req: Request, res: Response) => {
  const body = insertWeekendPlaceSchema.parse(req.body);
  const [row] = await db.insert(weekendPlacesTable).values(body).returning();
  res.status(201).json(row);
});

// Update a place
router.patch("/weekend-places/:id", async (req: Request, res: Response) => {
  const id = Number(req.params.id);
  const [existing] = await db
    .select()
    .from(weekendPlacesTable)
    .where(eq(weekendPlacesTable.id, id));
  if (!existing) {
    res.status(404).json({ error: "المكان غير موجود" });
    return;
  }
  const body = insertWeekendPlaceSchema.partial().parse(req.body);
  const [row] = await db
    .update(weekendPlacesTable)
    .set(body)
    .where(eq(weekendPlacesTable.id, id))
    .returning();
  res.json(row);
});

// Delete a place
router.delete("/weekend-places/:id", async (req: Request, res: Response) => {
  const id = Number(req.params.id);
  const [row] = await db
    .select()
    .from(weekendPlacesTable)
    .where(eq(weekendPlacesTable.id, id));
  if (!row) {
    res.status(404).json({ error: "المكان غير موجود" });
    return;
  }
  if (row.imageUrl?.startsWith("/objects/weekend/")) {
    await objectStorage.deleteWeekendPlaceObject(row.imageUrl).catch(() => {});
  }
  await db.delete(weekendPlacesTable).where(eq(weekendPlacesTable.id, id));
  res.json({ ok: true });
});

export default router;
