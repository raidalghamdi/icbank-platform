/**
 * GAC content router — Library, Social Feed, News.
 *
 * Public reads + admin-protected writes.
 *
 *   GET  /api/gac/publications              → list (q, category, language filters)
 *   GET  /api/gac/publications/categories   → category counts
 *   POST /api/gac/publications/reseed       → admin: upload bundled PDFs + insert rows
 *   GET  /api/gac/social-feed               → list latest social posts
 *   GET  /api/gac/news                      → list latest news/decisions
 */
import { Router, type Request, type Response, type NextFunction } from "express";
import { promises as fs } from "node:fs";
import path from "node:path";
import { db } from "@workspace/db";
import {
  gacPublicationsTable,
  gacSocialPostsTable,
  gacNewsItemsTable,
} from "@workspace/db";
import { and, desc, eq, ilike, or, sql } from "drizzle-orm";
import { z } from "zod";
import { requireAdmin } from "../middleware/auth";
import { ObjectStorageService } from "../lib/objectStorage";
import { SEED_PUBLICATIONS } from "../composer/seed-publications";

// API-key guard for the cron-driven social ingest endpoint. Uses
// GAC_INGEST_API_KEY (set in Railway env) — falls back to REPORT_API_KEY so
// a single shared secret can be used across n8n + cron callers.
function requireIngestKey(req: Request, res: Response, next: NextFunction) {
  const provided = req.headers["x-api-key"];
  const expected =
    process.env["GAC_INGEST_API_KEY"] || process.env["REPORT_API_KEY"];
  if (!expected) {
    res.status(503).json({ error: "Ingest endpoint not configured" });
    return;
  }
  if (!provided || provided !== expected) {
    res.status(401).json({ error: "Unauthorized" });
    return;
  }
  next();
}

const SocialPostInputSchema = z.object({
  platform: z.enum(["linkedin", "twitter", "instagram", "youtube"]),
  externalId: z.string().min(1),
  contentAr: z.string().optional(),
  contentEn: z.string().optional(),
  postUrl: z.string().url().optional(),
  mediaUrl: z.string().url().optional(),
  mediaType: z.enum(["image", "video", "none"]).optional(),
  postedAt: z.string().optional(),
  metrics: z.record(z.string(), z.number()).optional(),
  account: z.string().optional(),
});

const SocialIngestPayloadSchema = z.object({
  posts: z.array(SocialPostInputSchema).min(0).max(100),
});

const router = Router();
const objectStorage = new ObjectStorageService();

// Where the bundled PDFs live. In dev: artifacts/api-server/assets/...
// In Docker runtime: /app/assets/... (copied via Dockerfile).
function publicationsDir(): string {
  return path.resolve(process.cwd(), "assets", "gac-publications");
}

// ─── Public: list publications ────────────────────────────────────────────
// Query params:
//   q         — fuzzy match on titleAr / titleEn / tags
//   category  — guidelines | regulations | statistics | research | policy | brand
//   language  — ar | en | bilingual
router.get("/gac/publications", async (req: Request, res: Response) => {
  const q = typeof req.query["q"] === "string" ? req.query["q"].trim() : "";
  const category =
    typeof req.query["category"] === "string" ? req.query["category"].trim() : "";
  const language =
    typeof req.query["language"] === "string" ? req.query["language"].trim() : "";

  const conds = [eq(gacPublicationsTable.status, "published")];
  if (category) {
    conds.push(eq(gacPublicationsTable.category, category));
  }
  if (language) {
    conds.push(eq(gacPublicationsTable.language, language));
  }
  if (q) {
    const pattern = `%${q}%`;
    conds.push(
      or(
        ilike(gacPublicationsTable.titleAr, pattern),
        ilike(gacPublicationsTable.titleEn, pattern),
        ilike(gacPublicationsTable.descriptionAr, pattern),
      )!,
    );
  }

  const rows = await db
    .select()
    .from(gacPublicationsTable)
    .where(and(...conds))
    .orderBy(
      desc(gacPublicationsTable.displayOrder),
      desc(gacPublicationsTable.publishedAt),
    );
  res.json({ ok: true, count: rows.length, items: rows });
});

// ─── Public: category counts (for filter chips) ────────────────────────────
router.get("/gac/publications/categories", async (_req: Request, res: Response) => {
  const rows = await db
    .select({
      category: gacPublicationsTable.category,
      count: sql<number>`count(*)::int`,
    })
    .from(gacPublicationsTable)
    .where(eq(gacPublicationsTable.status, "published"))
    .groupBy(gacPublicationsTable.category);
  res.json({ ok: true, categories: rows });
});

// ─── Admin: reseed publications ────────────────────────────────────────────
// Loads each PDF from disk, uploads to Supabase Storage, inserts row.
// Idempotent on titleAr (skips if a row with the same titleAr exists).
router.post(
  "/gac/publications/reseed",
  requireAdmin,
  async (_req: Request, res: Response) => {
    const inserted: unknown[] = [];
    const skipped: string[] = [];
    const errors: string[] = [];
    const baseDir = publicationsDir();

    for (const pub of SEED_PUBLICATIONS) {
      try {
        // Idempotency on titleAr
        const existing = await db
          .select({ id: gacPublicationsTable.id })
          .from(gacPublicationsTable)
          .where(eq(gacPublicationsTable.titleAr, pub.titleAr))
          .limit(1);
        if (existing.length > 0) {
          skipped.push(`exists: ${pub.titleAr}`);
          continue;
        }

        const filePath = path.join(baseDir, pub.localFile);
        const buffer = await fs.readFile(filePath);
        const fileUrl = await objectStorage.saveGacPublication(buffer, "application/pdf");

        const [row] = await db
          .insert(gacPublicationsTable)
          .values({
            titleAr: pub.titleAr,
            titleEn: pub.titleEn ?? null,
            category: pub.category,
            language: pub.language,
            descriptionAr: pub.descriptionAr ?? null,
            descriptionEn: pub.descriptionEn ?? null,
            version: pub.version ?? null,
            publishedAt: pub.publishedAt ?? null,
            originalUrl: pub.originalUrl ?? null,
            fileUrl,
            fileSizeBytes: buffer.byteLength,
            pageCount: pub.pageCount ?? null,
            tags: pub.tags ?? [],
            sourceDomain: pub.sourceDomain,
            status: "published",
            displayOrder: pub.displayOrder ?? 0,
          })
          .returning();
        inserted.push(row);
      } catch (err) {
        const msg = err instanceof Error ? err.message : String(err);
        errors.push(`${pub.localFile}: ${msg}`);
      }
    }

    res.json({
      ok: errors.length === 0,
      inserted: inserted.length,
      skipped,
      errors,
      items: inserted,
    });
  },
);

// ─── Public: social feed (LinkedIn + cached Twitter) ───────────────────────
// API-key ingest: upsert social posts (called by hourly cron).
// Uses UNIQUE(platform, external_id) for idempotency.
router.post(
  "/gac/social-feed/ingest",
  requireIngestKey,
  async (req: Request, res: Response) => {
    const parsed = SocialIngestPayloadSchema.safeParse(req.body);
    if (!parsed.success) {
      res.status(400).json({ error: "Invalid payload", details: parsed.error.issues });
      return;
    }
    let inserted = 0;
    let updated = 0;
    const errors: string[] = [];
    for (const p of parsed.data.posts) {
      try {
        const existing = await db
          .select({ id: gacSocialPostsTable.id })
          .from(gacSocialPostsTable)
          .where(
            and(
              eq(gacSocialPostsTable.platform, p.platform),
              eq(gacSocialPostsTable.externalId, p.externalId),
            ),
          )
          .limit(1);
        const values = {
          platform: p.platform,
          externalId: p.externalId,
          contentAr: p.contentAr ?? null,
          contentEn: p.contentEn ?? null,
          postUrl: p.postUrl ?? null,
          mediaUrl: p.mediaUrl ?? null,
          mediaType: p.mediaType ?? null,
          postedAt: p.postedAt ? new Date(p.postedAt) : null,
          metrics: p.metrics ?? {},
          account: p.account ?? null,
          fetchedAt: new Date(),
        };
        if (existing.length > 0) {
          await db
            .update(gacSocialPostsTable)
            .set(values)
            .where(eq(gacSocialPostsTable.id, existing[0]!.id));
          updated++;
        } else {
          await db.insert(gacSocialPostsTable).values(values);
          inserted++;
        }
      } catch (err) {
        errors.push(
          `${p.platform}/${p.externalId}: ${err instanceof Error ? err.message : String(err)}`,
        );
      }
    }
    res.json({ ok: errors.length === 0, inserted, updated, errors });
  },
);

router.get("/gac/social-feed", async (req: Request, res: Response) => {
  const platform =
    typeof req.query["platform"] === "string" ? req.query["platform"].trim() : "";
  const limit = Math.min(Number(req.query["limit"]) || 20, 100);

  const conds = [];
  if (platform) conds.push(eq(gacSocialPostsTable.platform, platform));

  const rows = await db
    .select()
    .from(gacSocialPostsTable)
    .where(conds.length ? and(...conds) : undefined)
    .orderBy(desc(gacSocialPostsTable.postedAt))
    .limit(limit);
  res.json({ ok: true, count: rows.length, items: rows });
});

// ─── Public: news / decisions feed ─────────────────────────────────────────
router.get("/gac/news", async (req: Request, res: Response) => {
  const kind = typeof req.query["kind"] === "string" ? req.query["kind"].trim() : "";
  const limit = Math.min(Number(req.query["limit"]) || 20, 100);

  const conds = [];
  if (kind) conds.push(eq(gacNewsItemsTable.kind, kind));

  const rows = await db
    .select()
    .from(gacNewsItemsTable)
    .where(conds.length ? and(...conds) : undefined)
    .orderBy(desc(gacNewsItemsTable.publishedAt))
    .limit(limit);
  res.json({ ok: true, count: rows.length, items: rows });
});

export default router;
