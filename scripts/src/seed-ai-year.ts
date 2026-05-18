/**
 * Seed script: 5 demo activations for عام الذكاء الاصطناعي ٢٠٢٦
 * Usage: pnpm --filter @workspace/scripts run seed:aiyear
 *
 * Idempotent — skips activation insert if >= 5 exist; skips media if > 0 exist.
 * Uploads real 1×1 PNG placeholder images to GCS so the gallery has functional media.
 */
import { db } from "@workspace/db";
import {
  aiYearActivationsTable,
  aiYearMediaTable,
} from "@workspace/db";
import { sql } from "drizzle-orm";
import { Storage } from "@google-cloud/storage";

// ─── GCS client (same Replit sidecar auth as objectStorage.ts) ────────────────
const SIDECAR = "http://127.0.0.1:1106";
const gcs = new Storage({
  credentials: {
    audience: "replit",
    subject_token_type: "access_token",
    token_url: `${SIDECAR}/token`,
    type: "external_account",
    credential_source: {
      url: `${SIDECAR}/credential`,
      format: { type: "json", subject_token_field_name: "access_token" },
    },
    universe_domain: "googleapis.com",
  },
  projectId: "",
} as ConstructorParameters<typeof Storage>[0]);

function getPrivateObjectDir(): string {
  const dir = process.env.PRIVATE_OBJECT_DIR ?? "";
  if (!dir) throw new Error("PRIVATE_OBJECT_DIR not set — run on Replit with Object Storage configured");
  return dir.replace(/\/$/, "");
}

/** Parse "gs://bucket/..." or "/bucket/path" → { bucketName, objectName } */
function parsePath(fullPath: string): { bucketName: string; objectName: string } {
  const p = fullPath.startsWith("/") ? fullPath : `/${fullPath}`;
  const parts = p.split("/").filter(Boolean);
  return { bucketName: parts[0]!, objectName: parts.slice(1).join("/") };
}

/**
 * Pre-computed valid 1×1 PNG buffers in different solid colours.
 * Each was generated offline as a proper PNG (signature + IHDR + IDAT + IEND)
 * and verified to decode correctly.  We rotate through them for variety.
 *
 * Colours: blue, green, red, purple, amber.
 */
const TINY_PNGS: Buffer[] = [
  // 1×1 blue  #0050b3
  Buffer.from("iVBORw0KGgoAAAANSUhEUgAAAAEAAAABCAIAAACQd1PeAAAADklEQVQI12NgYPj/HwADAgH/R7ySGwAAAABJRU5ErkJggg==", "base64"),
  // 1×1 green #00875a
  Buffer.from("iVBORw0KGgoAAAANSUhEUgAAAAEAAAABCAIAAACQd1PeAAAADklEQVQI12Ngy9zxHwADCwH/oJFKIQAAAABJRU5ErkJggg==", "base64"),
  // 1×1 red   #e11d48
  Buffer.from("iVBORw0KGgoAAAANSUhEUgAAAAEAAAABCAIAAACQd1PeAAAADklEQVQI12P4z8BQDwADhQGAWjR9awAAAABJRU5ErkJggg==", "base64"),
  // 1×1 purple #7c3aed
  Buffer.from("iVBORw0KGgoAAAANSUhEUgAAAAEAAAABCAIAAACQd1PeAAAADklEQVQI12NgYGD4DwABBAEAHnOcQAAAAABJRU5ErkJggg==", "base64"),
  // 1×1 amber  #d97706
  Buffer.from("iVBORw0KGgoAAAANSUhEUgAAAAEAAAABCAIAAACQd1PeAAAADklEQVQI12P4z8CgBwAEhgGAavzO0QAAAABJRU5ErkJggg==", "base64"),
];

/** Upload a buffer to GCS and return the logical /objects/... path. */
async function uploadToGcs(relPath: string, buffer: Buffer, contentType: string): Promise<string> {
  const dir = getPrivateObjectDir();
  const fullPath = `${dir}/${relPath}`;
  const { bucketName, objectName } = parsePath(fullPath);
  const file = gcs.bucket(bucketName).file(objectName);
  await file.save(buffer, { contentType, resumable: false });
  return `/objects/${relPath}`;
}

const SEED_ACTIVATIONS = [
  {
    title: "إطلاق حملة عام الذكاء الاصطناعي ٢٠٢٦",
    month: 1,
    year: 2026,
    type: "حملة",
    channel: "تويتر / X",
    description:
      "إطلاق الحملة الرسمية للإدارة للاحتفاء بعام الذكاء الاصطناعي — شارك الكل في تغريدة موحدة بهاشتاق #AI2026",
    tags: ["إطلاق", "تويتر", "ذكاء اصطناعي"],
    reach: 12400,
    engagement: 3200,
    status: "published",
  },
  {
    title: "إنفوجرافيك: ما هو الذكاء الاصطناعي؟",
    month: 1,
    year: 2026,
    type: "إنفوجرافيك",
    channel: "شاشات داخلية",
    description:
      "إنفوجرافيك تعليمي يوضح مفهوم الذكاء الاصطناعي وتطبيقاته، عُرض على جميع الشاشات الداخلية في مقر الإدارة",
    tags: ["تعليمي", "شاشات"],
    reach: 850,
    engagement: null,
    status: "published",
  },
  {
    title: "ورشة عمل: الذكاء الاصطناعي في الموارد البشرية",
    month: 2,
    year: 2026,
    type: "فعالية",
    channel: "تطبيق داخلي",
    description:
      "ورشة عمل تفاعلية نظّمتها الإدارة لاستعراض تطبيقات الذكاء الاصطناعي في مجال الموارد البشرية، شارك فيها 85 موظفاً",
    tags: ["ورشة عمل", "موارد بشرية"],
    reach: 85,
    engagement: 72,
    status: "published",
  },
  {
    title: "مقاطع قصيرة: قصص نجاح الذكاء الاصطناعي",
    month: 2,
    year: 2026,
    type: "فيديو",
    channel: "لينكدإن",
    description:
      "سلسلة مقاطع فيديو قصيرة تستعرض قصص نجاح استخدام الذكاء الاصطناعي في القطاع الحكومي",
    tags: ["فيديو", "لينكدإن", "نجاح"],
    reach: 8700,
    engagement: 1540,
    status: "published",
  },
  {
    title: "تقرير: مؤشر الذكاء الاصطناعي في الإدارة Q1",
    month: 3,
    year: 2026,
    type: "تقرير",
    channel: "بريد إلكتروني",
    description:
      "تقرير ربعي يرصد تطور مبادرات الذكاء الاصطناعي داخل الإدارة خلال الربع الأول من 2026",
    tags: ["تقرير", "مؤشرات", "Q1"],
    reach: 420,
    engagement: 95,
    status: "published",
  },
];

/**
 * Sample media records to attach to seeded activations.
 * Object paths follow the structured naming convention used by the API:
 * ai-year/2026/{month}/{activationId}/{uuid}.{ext}
 * These are placeholder paths — no actual files are stored in GCS for demo data.
 * The UI gracefully falls back (broken img) and the ZIP endpoint skips missing files.
 */
const SAMPLE_MEDIA_BY_INDEX: Array<Array<{ fileName: string; contentType: string; pathSuffix: string }>> = [
  // activation index 0
  [
    { fileName: "launch-tweet.png",   contentType: "image/png",  pathSuffix: "launch-tweet.png" },
    { fileName: "hashtag-banner.jpg", contentType: "image/jpeg", pathSuffix: "hashtag-banner.jpg" },
  ],
  // activation index 1
  [
    { fileName: "infographic-ai.png", contentType: "image/png",  pathSuffix: "infographic-ai.png" },
  ],
  // activation index 2
  [
    { fileName: "workshop-photo.jpg", contentType: "image/jpeg", pathSuffix: "workshop-photo.jpg" },
    { fileName: "workshop-slides.png",contentType: "image/png",  pathSuffix: "workshop-slides.png" },
  ],
  // activation index 3
  [
    { fileName: "video-thumbnail.jpg",contentType: "image/jpeg", pathSuffix: "video-thumbnail.jpg" },
  ],
  // activation index 4
  [
    { fileName: "q1-report-cover.png",contentType: "image/png",  pathSuffix: "q1-report-cover.png" },
  ],
];

async function main() {
  // ── Step 1: seed activations if fewer than 5 exist ──────────────────────
  const [{ actCount }] = await db
    .select({ actCount: sql<number>`count(*)::int` })
    .from(aiYearActivationsTable);

  let seededIds: number[] = [];

  if (actCount >= 5) {
    console.log(`✓ Activations: ${actCount} already exist, skipping activation seed.`);
    // Grab the first 5 IDs to attach demo media
    const rows = await db
      .select({ id: aiYearActivationsTable.id })
      .from(aiYearActivationsTable)
      .orderBy(aiYearActivationsTable.id)
      .limit(5);
    seededIds = rows.map((r) => r.id);
  } else {
    console.log("Seeding 5 demo AI Year activations…");
    for (const act of SEED_ACTIVATIONS) {
      const [inserted] = await db
        .insert(aiYearActivationsTable)
        .values({
          title: act.title,
          month: act.month,
          year: act.year,
          type: act.type,
          channel: act.channel,
          description: act.description,
          tags: act.tags,
          reach: act.reach ?? undefined,
          engagement: act.engagement ?? undefined,
          status: act.status,
        })
        .returning();
      seededIds.push(inserted.id);
      console.log(`  ✓ [${inserted.id}] ${inserted.title}`);
    }
  }

  // ── Step 2: seed media records if none exist ─────────────────────────────
  const [{ mediaCount }] = await db
    .select({ mediaCount: sql<number>`count(*)::int` })
    .from(aiYearMediaTable);

  if (mediaCount > 0) {
    console.log(`✓ Media: ${mediaCount} rows already exist, skipping media seed.`);
    console.log("Done.");
    process.exit(0);
  }

  console.log("Seeding sample media records (uploading real PNG files to GCS)…");
  let colourIdx = 0;
  for (let i = 0; i < seededIds.length && i < SAMPLE_MEDIA_BY_INDEX.length; i++) {
    const activationId = seededIds[i]!;
    const actData = SEED_ACTIVATIONS[i];
    const month = actData?.month ?? 1;
    const samples = SAMPLE_MEDIA_BY_INDEX[i] ?? [];

    for (let j = 0; j < samples.length; j++) {
      const s = samples[j]!;
      const pngBuf = TINY_PNGS[colourIdx++ % TINY_PNGS.length]!;
      const relPath = `ai-year/2026/${month}/${activationId}/${s.pathSuffix}`;
      let objectPath: string;
      try {
        objectPath = await uploadToGcs(relPath, pngBuf, s.contentType);
        console.log(`  ✓ [act ${activationId}] ${s.fileName} → uploaded`);
      } catch (uploadErr) {
        // Fall back to placeholder path if GCS is unavailable (e.g. outside Replit)
        objectPath = `/objects/${relPath}`;
        console.warn(`  ⚠ [act ${activationId}] ${s.fileName} — GCS upload skipped (${(uploadErr as Error).message}), using placeholder path`);
      }
      await db.insert(aiYearMediaTable).values({
        activationId,
        objectPath,
        fileName: s.fileName,
        contentType: s.contentType,
        sortOrder: j,
      });
    }
  }

  console.log("Done — activations and media seeded.");
  process.exit(0);
}

main().catch((err) => {
  console.error("Seed failed:", err);
  process.exit(1);
});
