import { Router, type Request, type Response } from "express";
import { timingSafeEqual } from "crypto";
import { basename } from "path";
import { db } from "@workspace/db";
import {
  aiYearActivationsTable,
  aiYearMediaTable,
  aiYearMetricsTable,
} from "@workspace/db";
import { eq, desc, ilike, and, or } from "drizzle-orm";
import { ObjectStorageService, ObjectNotFoundError } from "../lib/objectStorage";
import {
  Document, Paragraph, Table, TableRow, TableCell, TextRun,
  HeadingLevel, AlignmentType, WidthType, Packer,
} from "docx";
import { createRequire } from "module";
const _require = createRequire(import.meta.url);
// archiver is CJS-only; use createRequire so esbuild can bundle it in ESM mode
const archiver = _require("archiver") as typeof import("archiver");

const router: Router = Router();
const objectStorage = new ObjectStorageService();

/**
 * Validates that a media objectPath was produced by getAiYearUploadURL().
 * Accepted form: /objects/ai-year/2026/{month}/{activationId}/{filename}
 *   - month: 1-12
 *   - activationId: digits
 *   - filename: word chars, dots, hyphens only (no quotes/brackets/slashes)
 */
const SAFE_OBJECT_PATH = /^\/objects\/ai-year\/2026\/(1[0-2]|[1-9])\/\d+\/[\w.\-]+$/;
function validateMediaPaths(
  media: { objectPath: string }[],
  res: Response
): boolean {
  for (const m of media) {
    if (!SAFE_OBJECT_PATH.test(m.objectPath)) {
      res.status(400).json({
        error: `objectPath غير صالح: ${m.objectPath}`,
      });
      return false;
    }
  }
  return true;
}

// ─── GET /api/ai-year/activations ────────────────────────────────
router.get("/ai-year/activations", async (req: Request, res: Response) => {
  const { month, type, channel, q } = req.query as {
    month?: string;
    type?: string;
    channel?: string;
    q?: string;
  };

  const conditions = [];
  if (month) conditions.push(eq(aiYearActivationsTable.month, parseInt(month)));
  if (type) conditions.push(eq(aiYearActivationsTable.type, type));
  if (channel) conditions.push(eq(aiYearActivationsTable.channel, channel));
  if (q) conditions.push(
    or(
      ilike(aiYearActivationsTable.title, `%${q}%`),
      ilike(aiYearActivationsTable.description ?? "", `%${q}%`)
    )
  );

  const activations = await db
    .select()
    .from(aiYearActivationsTable)
    .where(conditions.length ? and(...conditions) : undefined)
    .orderBy(desc(aiYearActivationsTable.month), desc(aiYearActivationsTable.createdAt));

  const result = await Promise.all(
    activations.map(async (a) => {
      const media = await db
        .select()
        .from(aiYearMediaTable)
        .where(eq(aiYearMediaTable.activationId, a.id))
        .orderBy(aiYearMediaTable.sortOrder);
      const metrics = await db
        .select()
        .from(aiYearMetricsTable)
        .where(eq(aiYearMetricsTable.activationId, a.id));
      return { ...a, media, metrics };
    })
  );

  res.json({ count: result.length, activations: result });
});

// ─── POST /api/ai-year/activations ───────────────────────────────
router.post("/ai-year/activations", async (req: Request, res: Response) => {
  const { activation, media = [], metrics = [] } = req.body as {
    activation: {
      title: string;
      month: number;
      year?: number;
      type: string;
      channel: string;
      description?: string;
      tags?: string[];
      status?: string;
      reach?: number;
      engagement?: number;
      notes?: string;
    };
    media?: { objectPath: string; fileName?: string; contentType?: string; sortOrder?: number }[];
    metrics?: { metricKey: string; metricValue?: string }[];
  };

  if (!activation?.title || !activation?.month || !activation?.type || !activation?.channel) {
    res.status(400).json({ error: "الحقول المطلوبة: title, month, type, channel" });
    return;
  }

  // Pre-validate media paths before any DB write to avoid partial inserts
  if (media.length && !validateMediaPaths(media, res)) return;

  await db.transaction(async (tx) => {
    const [inserted] = await tx
      .insert(aiYearActivationsTable)
      .values({
        title: activation.title,
        month: activation.month,
        year: activation.year ?? 2026,
        type: activation.type,
        channel: activation.channel,
        description: activation.description,
        tags: activation.tags ?? [],
        status: activation.status ?? "published",
        reach: activation.reach,
        engagement: activation.engagement,
        notes: activation.notes,
      })
      .returning();

    if (media.length) {
      await tx.insert(aiYearMediaTable).values(
        media.map((m, i) => ({
          activationId: inserted.id,
          objectPath: m.objectPath,
          fileName: m.fileName,
          contentType: m.contentType,
          sortOrder: m.sortOrder ?? i,
        }))
      );
    }

    if (metrics.length) {
      await tx.insert(aiYearMetricsTable).values(
        metrics.map((m) => ({
          activationId: inserted.id,
          metricKey: m.metricKey,
          metricValue: m.metricValue,
        }))
      );
    }

    res.json({ ok: true, id: inserted.id, activation: inserted });
  });
});

// ─── GET /api/ai-year/activations/:id ────────────────────────────
router.get("/ai-year/activations/:id", async (req: Request, res: Response) => {
  const id = parseInt(String(req.params.id), 10);
  if (isNaN(id)) { res.status(400).json({ error: "id غير صالح" }); return; }

  const [activation] = await db
    .select()
    .from(aiYearActivationsTable)
    .where(eq(aiYearActivationsTable.id, id));
  if (!activation) { res.status(404).json({ error: "التفعيل غير موجود" }); return; }

  const media = await db
    .select()
    .from(aiYearMediaTable)
    .where(eq(aiYearMediaTable.activationId, id))
    .orderBy(aiYearMediaTable.sortOrder);

  res.json({ ...activation, media });
});

// ─── PUT /api/ai-year/activations/:id ────────────────────────────
router.put("/ai-year/activations/:id", async (req: Request, res: Response) => {
  const id = parseInt(String(req.params.id), 10);
  if (isNaN(id)) { res.status(400).json({ error: "id غير صالح" }); return; }

  const { activation, media, metrics } = req.body as {
    activation?: Partial<{
      title: string;
      month: number;
      type: string;
      channel: string;
      description: string;
      tags: string[];
      status: string;
      reach: number;
      engagement: number;
      notes: string;
    }>;
    media?: { objectPath: string; fileName?: string; contentType?: string; sortOrder?: number }[];
    metrics?: { metricKey: string; metricValue?: string }[];
  };

  if (activation && Object.keys(activation).length) {
    await db.update(aiYearActivationsTable).set(activation).where(eq(aiYearActivationsTable.id, id));
  }

  if (media !== undefined) {
    if (media.length && !validateMediaPaths(media, res)) return;
    await db.delete(aiYearMediaTable).where(eq(aiYearMediaTable.activationId, id));
    if (media.length) {
      await db.insert(aiYearMediaTable).values(
        media.map((m, i) => ({
          activationId: id,
          objectPath: m.objectPath,
          fileName: m.fileName,
          contentType: m.contentType,
          sortOrder: m.sortOrder ?? i,
        }))
      );
    }
  }

  if (metrics !== undefined) {
    await db.delete(aiYearMetricsTable).where(eq(aiYearMetricsTable.activationId, id));
    if (metrics.length) {
      await db.insert(aiYearMetricsTable).values(
        metrics.map((m) => ({
          activationId: id,
          metricKey: m.metricKey,
          metricValue: m.metricValue,
        }))
      );
    }
  }

  const [updated] = await db.select().from(aiYearActivationsTable).where(eq(aiYearActivationsTable.id, id));
  res.json({ ok: true, activation: updated });
});

// ─── DELETE /api/ai-year/activations/:id ─────────────────────────
// Admin-only: requires X-Admin-Key header matching ADMIN_KEY env var.
router.delete("/ai-year/activations/:id", async (req: Request, res: Response) => {
  const adminKey = process.env.ADMIN_KEY;
  if (!adminKey) {
    res.status(503).json({ error: "ADMIN_KEY env var not configured — set it in Replit Secrets" });
    return;
  }
  const provided = String(req.headers["x-admin-key"] ?? "");
  // Use timing-safe comparison to prevent timing-based key extraction attacks
  const keysMatch =
    provided.length === adminKey.length &&
    timingSafeEqual(Buffer.from(provided), Buffer.from(adminKey));
  if (!keysMatch) {
    res.status(403).json({ error: "ممنوع: مفتاح المدير غير صحيح" });
    return;
  }

  const id = parseInt(String(req.params.id), 10);
  if (isNaN(id)) { res.status(400).json({ error: "id غير صالح" }); return; }

  await db.delete(aiYearMediaTable).where(eq(aiYearMediaTable.activationId, id));
  await db.delete(aiYearMetricsTable).where(eq(aiYearMetricsTable.activationId, id));
  await db.delete(aiYearActivationsTable).where(eq(aiYearActivationsTable.id, id));
  res.json({ ok: true });
});

// ─── GET /api/ai-year/stats ───────────────────────────────────────
router.get("/ai-year/stats", async (req: Request, res: Response) => {
  const activations = await db.select().from(aiYearActivationsTable);
  const media = await db.select().from(aiYearMediaTable);

  const totalActivations = activations.length;
  const totalMedia = media.length;

  const channels = new Set(activations.map((a) => a.channel));
  const totalChannels = channels.size;

  const lastUpdated = activations.length
    ? activations.reduce((max, a) => a.updatedAt > max ? a.updatedAt : max, activations[0].updatedAt)
    : null;

  const byMonth: Record<number, number> = {};
  for (let m = 1; m <= 12; m++) byMonth[m] = 0;
  activations.forEach((a) => { byMonth[a.month] = (byMonth[a.month] ?? 0) + 1; });

  const byType: Record<string, number> = {};
  activations.forEach((a) => { byType[a.type] = (byType[a.type] ?? 0) + 1; });

  const byChannel: Record<string, number> = {};
  activations.forEach((a) => { byChannel[a.channel] = (byChannel[a.channel] ?? 0) + 1; });

  res.json({
    totalActivations,
    totalMedia,
    totalChannels,
    lastUpdated,
    byMonth,
    byType,
    byChannel,
  });
});

// ─── POST /api/ai-year/upload-url ────────────────────────────────
// Generates a GCS presigned PUT URL for a media file.
// Requires activationId and month to build structured storage path:
//   {PRIVATE_OBJECT_DIR}/ai-year/2026/{month}/{activationId}/{uuid}.{ext}
router.post("/ai-year/upload-url", async (req: Request, res: Response) => {
  const { name, contentType, activationId, month } = req.body as {
    name?: string;
    contentType?: string;
    activationId?: number;
    month?: number;
  };

  const ALLOWED_MIME_TYPES = new Set([
    "image/jpeg", "image/png", "image/webp", "image/gif", "image/svg+xml",
    "video/mp4", "video/webm",
  ]);
  const MAX_FILE_SIZE_BYTES = 50 * 1024 * 1024; // 50 MB

  if (!activationId || !month || !name) {
    res.status(400).json({ error: "activationId, month, name are required" });
    return;
  }

  if (contentType && !ALLOWED_MIME_TYPES.has(contentType)) {
    res.status(400).json({ error: `نوع الملف غير مسموح: ${contentType}` });
    return;
  }

  const fileSize = Number((req.body as { fileSize?: number }).fileSize);
  if (fileSize && (isNaN(fileSize) || fileSize > MAX_FILE_SIZE_BYTES)) {
    res.status(400).json({ error: "حجم الملف يتجاوز الحد المسموح (50 ميغابايت)" });
    return;
  }

  const monthNum = Number(month);
  if (!Number.isInteger(monthNum) || monthNum < 1 || monthNum > 12) {
    res.status(400).json({ error: "month يجب أن يكون رقماً بين 1 و12" });
    return;
  }

  const activationIdNum = Number(activationId);
  if (!Number.isInteger(activationIdNum) || activationIdNum <= 0) {
    res.status(400).json({ error: "activationId غير صالح" });
    return;
  }

  const [existing] = await db
    .select({ id: aiYearActivationsTable.id })
    .from(aiYearActivationsTable)
    .where(eq(aiYearActivationsTable.id, activationIdNum));
  if (!existing) {
    res.status(404).json({ error: "التفعيل المرتبط غير موجود" });
    return;
  }

  try {
    const { uploadURL, objectPath } = await objectStorage.getAiYearUploadURL({
      month: monthNum,
      activationId: activationIdNum,
      fileName: name,
      contentType,
    });
    res.json({ uploadURL, objectPath, name, contentType });
  } catch (err) {
    req.log.error({ err }, "Failed to generate upload URL");
    res.status(500).json({ error: "فشل توليد رابط الرفع" });
  }
});

// ─── GET /api/ai-year/activations/:id/zip ────────────────────────
// Download all media for an activation as a ZIP archive.
router.get("/ai-year/activations/:id/zip", async (req: Request, res: Response) => {
  const id = parseInt(String(req.params.id), 10);
  if (isNaN(id)) { res.status(400).json({ error: "id غير صالح" }); return; }

  const [activation] = await db
    .select()
    .from(aiYearActivationsTable)
    .where(eq(aiYearActivationsTable.id, id));
  if (!activation) { res.status(404).json({ error: "التفعيل غير موجود" }); return; }

  const mediaRows = await db
    .select()
    .from(aiYearMediaTable)
    .where(eq(aiYearMediaTable.activationId, id))
    .orderBy(aiYearMediaTable.sortOrder);

  if (!mediaRows.length) {
    res.status(404).json({ error: "لا توجد صور لهذا التفعيل" });
    return;
  }

  // Content-Disposition: use RFC 5987 encoding for non-ASCII titles
  const asciiName = `activation-${id}.zip`;
  const utf8Name = encodeURIComponent(`activation-${id}-${activation.title}.zip`);
  res.setHeader("Content-Type", "application/zip");
  res.setHeader(
    "Content-Disposition",
    `attachment; filename="${asciiName}"; filename*=UTF-8''${utf8Name}`
  );

  const archive = archiver("zip", { zlib: { level: 6 } });
  archive.pipe(res);

  let hasError = false;
  archive.on("error", (err) => {
    req.log.error({ err }, "Archive error during ZIP generation");
    hasError = true;
    if (!res.headersSent) res.status(500).json({ error: "فشل توليد ملف ZIP" });
  });

  for (const media of mediaRows) {
    try {
      const file = await objectStorage.getObjectEntityFile(media.objectPath);
      const [metadata] = await file.getMetadata();
      const ext = (media.fileName?.split(".").pop()) ||
        (String(metadata.contentType ?? "").split("/")[1]) || "bin";
      const rawName = media.fileName || `file-${media.id}.${ext}`;
      // Sanitize entry name: take only the basename and strip unsafe path chars
      const entryName = basename(rawName).replace(/[^\w.\-]/g, "_") || `file-${media.id}.bin`;
      // Stream directly from GCS — archiver.file() needs a local FS path,
      // so we use append() with the GCS read stream instead.
      archive.append(file.createReadStream(), { name: entryName });
    } catch (err) {
      if (err instanceof ObjectNotFoundError) {
        req.log.info({ mediaId: media.id }, "Skipping missing media in zip");
      } else {
        // Non-recoverable storage error: log and abort the archive
        req.log.error({ err, mediaId: media.id }, "Storage error during zip; aborting archive");
        archive.abort();
        if (!res.headersSent) res.status(500).json({ error: "خطأ في قراءة الملفات من التخزين" });
        return;
      }
    }
  }

  if (!hasError) await archive.finalize();
});

// ─── POST /api/ai-year/report ─────────────────────────────────────
router.post("/ai-year/report", async (req: Request, res: Response) => {
  const activations = await db
    .select()
    .from(aiYearActivationsTable)
    .orderBy(aiYearActivationsTable.month, desc(aiYearActivationsTable.createdAt));

  const media = await db.select().from(aiYearMediaTable);

  const totalActivations = activations.length;
  const totalMedia = media.length;
  const channels = new Set(activations.map((a) => a.channel));
  const byType: Record<string, number> = {};
  activations.forEach((a) => { byType[a.type] = (byType[a.type] ?? 0) + 1; });

  const top3 = activations
    .filter((a) => a.reach != null)
    .sort((a, b) => (b.reach ?? 0) - (a.reach ?? 0))
    .slice(0, 3);

  const MONTHS_AR = ["يناير","فبراير","مارس","أبريل","مايو","يونيو","يوليو","أغسطس","سبتمبر","أكتوبر","نوفمبر","ديسمبر"];

  const tableRows = [
    new TableRow({
      children: [
        new TableCell({ children: [new Paragraph({ children: [new TextRun({ text: "#", bold: true })] })] }),
        new TableCell({ children: [new Paragraph({ children: [new TextRun({ text: "العنوان", bold: true })] })] }),
        new TableCell({ children: [new Paragraph({ children: [new TextRun({ text: "الشهر", bold: true })] })] }),
        new TableCell({ children: [new Paragraph({ children: [new TextRun({ text: "النوع", bold: true })] })] }),
        new TableCell({ children: [new Paragraph({ children: [new TextRun({ text: "القناة", bold: true })] })] }),
        new TableCell({ children: [new Paragraph({ children: [new TextRun({ text: "الوصول", bold: true })] })] }),
      ],
    }),
    ...activations.map((a, i) =>
      new TableRow({
        children: [
          new TableCell({ children: [new Paragraph(String(i + 1))] }),
          new TableCell({ children: [new Paragraph(a.title)] }),
          new TableCell({ children: [new Paragraph(MONTHS_AR[a.month - 1] ?? String(a.month))] }),
          new TableCell({ children: [new Paragraph(a.type)] }),
          new TableCell({ children: [new Paragraph(a.channel)] }),
          new TableCell({ children: [new Paragraph(a.reach != null ? String(a.reach) : "—")] }),
        ],
      })
    ),
  ];

  const doc = new Document({
    sections: [
      {
        children: [
          new Paragraph({
            text: "تقرير عام الذكاء الاصطناعي ٢٠٢٦",
            heading: HeadingLevel.HEADING_1,
            alignment: AlignmentType.RIGHT,
          }),
          new Paragraph({
            children: [
              new TextRun({
                text: `تاريخ الإصدار: ${new Date().toLocaleDateString("ar-SA", { year: "numeric", month: "long", day: "numeric" })}`,
                italics: true,
              }),
            ],
            alignment: AlignmentType.RIGHT,
          }),
          new Paragraph(""),
          new Paragraph({
            text: "مقدمة",
            heading: HeadingLevel.HEADING_2,
            alignment: AlignmentType.RIGHT,
          }),
          new Paragraph({
            children: [
              new TextRun(
                "يوثّق هذا التقرير جميع تفعيلات وأنشطة إدارة التواصل الداخلي خلال عام الذكاء الاصطناعي ٢٠٢٦، " +
                "ويستعرض الإحصائيات والتوزيعات الشهرية والتحليلات الكاملة."
              ),
            ],
            alignment: AlignmentType.RIGHT,
          }),
          new Paragraph(""),
          new Paragraph({
            text: "إحصائيات العام",
            heading: HeadingLevel.HEADING_2,
            alignment: AlignmentType.RIGHT,
          }),
          new Paragraph({ children: [new TextRun({ text: `إجمالي التفعيلات: ${totalActivations}`, bold: true })], alignment: AlignmentType.RIGHT }),
          new Paragraph({ children: [new TextRun({ text: `إجمالي الصور/الوسائط: ${totalMedia}`, bold: true })], alignment: AlignmentType.RIGHT }),
          new Paragraph({ children: [new TextRun({ text: `عدد القنوات المستخدمة: ${channels.size}`, bold: true })], alignment: AlignmentType.RIGHT }),
          new Paragraph({ children: [new TextRun({ text: `توزيع الأنواع: ${Object.entries(byType).map(([k, v]) => `${k} (${v})`).join(" — ")}`, bold: true })], alignment: AlignmentType.RIGHT }),
          new Paragraph(""),
          ...(top3.length
            ? [
                new Paragraph({
                  text: "أبرز ٣ تفعيلات (بحسب الوصول)",
                  heading: HeadingLevel.HEADING_2,
                  alignment: AlignmentType.RIGHT,
                }),
                ...top3.map((a, i) =>
                  new Paragraph({
                    children: [
                      new TextRun({ text: `${i + 1}. ${a.title} — `, bold: true }),
                      new TextRun(`${MONTHS_AR[a.month - 1]} · ${a.channel} · وصول: ${a.reach ?? "—"}`),
                    ],
                    alignment: AlignmentType.RIGHT,
                  })
                ),
                new Paragraph(""),
              ]
            : []),
          new Paragraph({
            text: "جدول التفعيلات الكامل",
            heading: HeadingLevel.HEADING_2,
            alignment: AlignmentType.RIGHT,
          }),
          new Table({
            width: { size: 100, type: WidthType.PERCENTAGE },
            rows: tableRows,
          }),
        ],
      },
    ],
  });

  const buffer = await Packer.toBuffer(doc);

  res.setHeader("Content-Type", "application/vnd.openxmlformats-officedocument.wordprocessingml.document");
  res.setHeader("Content-Disposition", "attachment; filename=\"AI-Year-2026-Report.docx\"");
  res.send(buffer);
});

export default router;
