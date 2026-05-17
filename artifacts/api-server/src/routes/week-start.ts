import { Router, type Request, type Response } from "express";
import multer from "multer";
import pdfParse from "pdf-parse";
import mammoth from "mammoth";
import Anthropic from "@anthropic-ai/sdk";
import OpenAI from "openai";
import { GoogleGenAI } from "@google/genai";
import { db } from "@workspace/db";
import {
  archiveEntriesTable,
  styleProfileTable,
  generatedOutputsTable,
} from "@workspace/db";
import { eq, desc, isNull } from "drizzle-orm";

const router: Router = Router();

const upload = multer({
  storage: multer.memoryStorage(),
  limits: { fileSize: 25 * 1024 * 1024, files: 100 },
});

const anthropic = new Anthropic({
  baseURL: process.env.AI_INTEGRATIONS_ANTHROPIC_BASE_URL,
  apiKey: process.env.AI_INTEGRATIONS_ANTHROPIC_API_KEY ?? "dummy",
});

const openai = new OpenAI({
  baseURL: process.env.AI_INTEGRATIONS_OPENAI_BASE_URL,
  apiKey: process.env.AI_INTEGRATIONS_OPENAI_API_KEY ?? "dummy",
});

const geminiAI = new GoogleGenAI({
  apiKey: process.env.AI_INTEGRATIONS_GEMINI_API_KEY ?? "dummy",
  httpOptions: {
    apiVersion: "",
    baseUrl: process.env.AI_INTEGRATIONS_GEMINI_BASE_URL,
  },
});

function cosineSimilarity(a: number[], b: number[]): number {
  if (!a || !b || a.length !== b.length) return 0;
  let dot = 0, magA = 0, magB = 0;
  for (let i = 0; i < a.length; i++) {
    dot += a[i] * b[i];
    magA += a[i] * a[i];
    magB += b[i] * b[i];
  }
  const denom = Math.sqrt(magA) * Math.sqrt(magB);
  return denom === 0 ? 0 : dot / denom;
}

async function extractText(
  buffer: Buffer,
  mimetype: string,
  originalname: string
): Promise<string> {
  const ext = originalname.toLowerCase().split(".").pop() ?? "";

  if (mimetype === "application/pdf" || ext === "pdf") {
    const data = await pdfParse(buffer);
    return data.text;
  }

  if (
    mimetype ===
      "application/vnd.openxmlformats-officedocument.wordprocessingml.document" ||
    ext === "docx"
  ) {
    const result = await mammoth.extractRawText({ buffer });
    return result.value;
  }

  if (ext === "txt" || ext === "md") {
    return buffer.toString("utf-8");
  }

  if (mimetype.startsWith("image/") || ["jpg", "jpeg", "png"].includes(ext)) {
    const base64 = buffer.toString("base64");
    const response = await openai.chat.completions.create({
      model: "gpt-4o",
      max_tokens: 4096,
      messages: [
        {
          role: "user",
          content: [
            {
              type: "image_url",
              image_url: { url: `data:${mimetype};base64,${base64}` },
            },
            {
              type: "text",
              text: "استخرج النص الموجود في هذه الصورة كاملاً. أخرج النص فقط دون أي تعليقات.",
            },
          ],
        },
      ],
    });
    return response.choices[0]?.message?.content ?? "";
  }

  return "";
}

async function generateEmbedding(text: string): Promise<number[] | null> {
  try {
    const controller = new AbortController();
    const timeout = setTimeout(() => controller.abort(), 12000);
    const response = await openai.embeddings.create(
      { model: "text-embedding-3-small", input: text.slice(0, 8000) },
      { signal: controller.signal }
    );
    clearTimeout(timeout);
    return response.data[0]?.embedding ?? null;
  } catch {
    return null;
  }
}

async function backfillEmbeddings(): Promise<void> {
  try {
    const entries = await db
      .select({ id: archiveEntriesTable.id, bodyText: archiveEntriesTable.bodyText })
      .from(archiveEntriesTable)
      .where(isNull(archiveEntriesTable.embedding));
    for (const entry of entries) {
      const emb = await generateEmbedding(entry.bodyText);
      if (emb) {
        await db
          .update(archiveEntriesTable)
          .set({ embedding: emb })
          .where(eq(archiveEntriesTable.id, entry.id));
      }
    }
  } catch {
    /* best-effort */
  }
}

async function updateStyleProfile(): Promise<void> {
  const entries = await db.select().from(archiveEntriesTable);
  if (entries.length === 0) return;

  const allText = entries.map((e) => e.bodyText).join("\n\n");
  const paragraphs = allText.split(/\n+/).filter((p) => p.trim().length > 20);
  const avgParaLength =
    paragraphs.reduce((sum, p) => sum + p.split(" ").length, 0) /
    Math.max(paragraphs.length, 1);

  const openers = entries
    .map((e) => e.bodyText.split(/[.!?؟]/)[0]?.trim())
    .filter(Boolean)
    .slice(0, 10) as string[];

  const closers = entries
    .map((e) => {
      const sentences = e.bodyText.split(/[.!?؟]/).filter((s: string) => s.trim());
      return sentences[sentences.length - 1]?.trim() ?? "";
    })
    .filter(Boolean)
    .slice(0, 10) as string[];

  const arabicStopwords = new Set([
    "في", "من", "إلى", "على", "عن", "مع", "هذا", "هذه", "التي", "الذي",
    "وفي", "كما", "قد", "لا", "ما", "إن", "أن", "كان", "أو", "ولا",
    "هو", "هي", "نحن", "أنا", "لم", "وهو", "وهي", "وقد", "ثم", "بين",
  ]);

  const wordFreq: Record<string, number> = {};
  allText.split(/\s+/).forEach((word) => {
    const clean = word.replace(/[^\u0600-\u06FF]/g, "").trim();
    if (clean.length > 3 && !arabicStopwords.has(clean)) {
      wordFreq[clean] = (wordFreq[clean] ?? 0) + 1;
    }
  });
  const topKeywords = Object.entries(wordFreq)
    .sort(([, a], [, b]) => b - a)
    .slice(0, 20)
    .map(([w]) => w);

  const quotePatterns = ["قال", "روي", "قرآن", "آية", "حديث", "تعالى"];
  const quoteCount = quotePatterns.reduce(
    (sum, p) => sum + (allText.match(new RegExp(p, "g"))?.length ?? 0),
    0
  );
  const quoteUsage =
    quoteCount > entries.length * 2
      ? "كثيف"
      : quoteCount > entries.length
        ? "معتدل"
        : "محدود";

  const profileData = {
    toneSummary: "رسمية مبسطة",
    avgParagraphLength: Math.round(avgParaLength),
    openerPatterns: openers,
    closerPatterns: closers,
    recurringKeywords: topKeywords,
    quoteUsage,
    updatedAt: new Date(),
  };

  const existing = await db.select().from(styleProfileTable).limit(1);
  if (existing.length > 0) {
    await db
      .update(styleProfileTable)
      .set(profileData)
      .where(eq(styleProfileTable.id, existing[0].id));
  } else {
    await db.insert(styleProfileTable).values(profileData);
  }
}

// POST /api/week-start/upload
router.post(
  "/week-start/upload",
  (req: Request, res: Response, next) => {
    const handler = upload.array("files", 100);
    handler(req, res, (err) => {
      if (err) {
        res.status(400).json({ error: err.message });
        return;
      }
      next();
    });
  },
  async (req: Request, res: Response) => {
    const files = req.files as Express.Multer.File[] | undefined;
    if (!files || files.length === 0) {
      res.status(400).json({ error: "لم يتم رفع أي ملف" });
      return;
    }

    // Process all files in parallel — extract text and save to DB immediately
    // Embeddings are generated asynchronously in background (non-blocking)
    const results = await Promise.all(
      files.map(async (file) => {
        try {
          const text = await extractText(file.buffer, file.mimetype, file.originalname);
          if (!text.trim()) {
            return { skipped: file.originalname, reason: "لا يوجد نص قابل للاستخراج" };
          }

          const [entry] = await db
            .insert(archiveEntriesTable)
            .values({
              title: file.originalname.replace(/\.[^.]+$/, ""),
              bodyText: text,
              sourceFile: file.originalname,
              embedding: null,
            })
            .returning();

          return { id: entry.id, title: entry.title, wordCount: text.split(/\s+/).length };
        } catch (err) {
          req.log.error({ err, file: file.originalname }, "upload extract failed");
          return { error: `فشل ${file.originalname}: ${(err as Error).message}` };
        }
      })
    );

    // Fire-and-forget: backfill embeddings + style profile after responding
    setImmediate(() => {
      backfillEmbeddings().catch(() => {});
      updateStyleProfile().catch(() => {});
    });

    res.json({ processed: results.filter((r) => r.id).length, results });
  }
);

// GET /api/week-start/archive
router.get("/week-start/archive", async (_req: Request, res: Response) => {
  const entries = await db
    .select({
      id: archiveEntriesTable.id,
      title: archiveEntriesTable.title,
      occasion: archiveEntriesTable.occasion,
      tone: archiveEntriesTable.tone,
      sourceFile: archiveEntriesTable.sourceFile,
      createdAt: archiveEntriesTable.createdAt,
      preview: archiveEntriesTable.bodyText,
    })
    .from(archiveEntriesTable)
    .orderBy(desc(archiveEntriesTable.createdAt))
    .limit(50);

  res.json({
    count: entries.length,
    entries: entries.map((e) => ({
      ...e,
      preview: e.preview?.slice(0, 200),
    })),
  });
});

// GET /api/week-start/style-profile
router.get(
  "/week-start/style-profile",
  async (_req: Request, res: Response) => {
    const [profile] = await db
      .select()
      .from(styleProfileTable)
      .orderBy(desc(styleProfileTable.updatedAt))
      .limit(1);
    res.json(profile ?? null);
  }
);

// POST /api/week-start/generate  (SSE streaming)
router.post("/week-start/generate", async (req: Request, res: Response) => {
  const { topic, occasion, audience, tone, length } = req.body as {
    topic?: string;
    occasion?: string;
    audience?: string;
    tone?: string;
    length?: string;
  };

  if (!topic?.trim()) {
    res.status(400).json({ error: "الموضوع مطلوب" });
    return;
  }

  res.setHeader("Content-Type", "text/event-stream");
  res.setHeader("Cache-Control", "no-cache");
  res.setHeader("Connection", "keep-alive");
  res.setHeader("X-Accel-Buffering", "no");
  res.flushHeaders();

  const sendEvent = (data: object) => {
    if (!res.writableEnded) {
      res.write(`data: ${JSON.stringify(data)}\n\n`);
    }
  };

  try {
    // Find top 5 similar archive entries
    let archiveContext = "";
    const entries = await db.select().from(archiveEntriesTable);

    if (entries.length > 0) {
      const topicEmbedding = await generateEmbedding(topic);
      type WithEmb = (typeof entries)[number] & { embedding: number[] };
      const entriesWithEmb = entries.filter(
        (e): e is WithEmb => Array.isArray(e.embedding) && e.embedding.length > 0
      );
      if (topicEmbedding && entriesWithEmb.length > 0) {
      const scored = entriesWithEmb
        .map((e) => ({
          entry: e,
          score: cosineSimilarity(topicEmbedding, e.embedding),
        }))
        .sort((a, b) => b.score - a.score)
        .slice(0, 5);

      archiveContext = scored
        .map((s, i) => `نموذج ${i + 1}:\n${s.entry.bodyText.slice(0, 600)}`)
        .join("\n\n---\n\n");
      } // end if topicEmbedding
    }

    const [styleProfile] = await db
      .select()
      .from(styleProfileTable)
      .limit(1);

    const styleInfo = styleProfile
      ? `النبرة الغالبة: ${styleProfile.toneSummary}، متوسط طول الفقرة: ${styleProfile.avgParagraphLength} كلمة، الاقتباسات: ${styleProfile.quoteUsage}، كلمات مفتاحية: ${(styleProfile.recurringKeywords ?? []).slice(0, 8).join("، ")}`
      : "اتبع أسلوب التواصل الداخلي المؤسسي الرسمي المبسط";

    const lengthMap: Record<string, string> = {
      short: "قصير (100-150 كلمة)",
      medium: "متوسط (200-250 كلمة)",
      long: "طويل (300-400 كلمة)",
    };
    const lengthStr = lengthMap[length ?? "medium"] ?? "متوسط (200-250 كلمة)";

    const prompt = `أنت كاتب محتوى داخلي لجهة حكومية سعودية.
اكتب رسالة "بداية أسبوع" بالعربية الفصحى المبسطة، ملتزماً بهذا الأسلوب:
${styleInfo}

${archiveContext ? `مستفيداً من النماذج السابقة:\n${archiveContext}\n\n` : ""}موضوع هذا الأسبوع: ${topic}
${occasion ? `المناسبة: ${occasion}` : ""}
${audience ? `الجمهور: ${audience}` : ""}
النبرة: ${tone ?? "ودية"}
الطول: ${lengthStr}

أخرج النص جاهزاً للنشر دون مقدمات أو شروحات.`;

    const models = ["claude", "openai", "gemini"] as const;
    const savedIds: Record<string, number> = {};

    for (const model of models) {
      const [out] = await db
        .insert(generatedOutputsTable)
        .values({ topic, modelName: model, outputText: "", archiveRefs: [], selected: false })
        .returning();
      savedIds[model] = out.id;
    }

    sendEvent({ type: "start", models, outputIds: savedIds });

    await Promise.all([
      // Claude
      (async () => {
        try {
          const stream = anthropic.messages.stream({
            model: "claude-sonnet-4-6",
            max_tokens: 8192,
            messages: [{ role: "user", content: prompt }],
          });
          let full = "";
          for await (const event of stream) {
            if (
              event.type === "content_block_delta" &&
              event.delta.type === "text_delta"
            ) {
              full += event.delta.text;
              sendEvent({ model: "claude", chunk: event.delta.text });
            }
          }
          await db
            .update(generatedOutputsTable)
            .set({ outputText: full })
            .where(eq(generatedOutputsTable.id, savedIds.claude));
          sendEvent({
            model: "claude",
            done: true,
            wordCount: full.split(/\s+/).filter(Boolean).length,
            outputId: savedIds.claude,
          });
        } catch (err) {
          sendEvent({ model: "claude", error: (err as Error).message });
        }
      })(),

      // OpenAI
      (async () => {
        try {
          const stream = await openai.chat.completions.create({
            model: "gpt-4o",
            messages: [{ role: "user", content: prompt }],
            stream: true,
            max_tokens: 8192,
          });
          let full = "";
          for await (const chunk of stream) {
            const text = chunk.choices[0]?.delta?.content ?? "";
            if (text) {
              full += text;
              sendEvent({ model: "openai", chunk: text });
            }
          }
          await db
            .update(generatedOutputsTable)
            .set({ outputText: full })
            .where(eq(generatedOutputsTable.id, savedIds.openai));
          sendEvent({
            model: "openai",
            done: true,
            wordCount: full.split(/\s+/).filter(Boolean).length,
            outputId: savedIds.openai,
          });
        } catch (err) {
          sendEvent({ model: "openai", error: (err as Error).message });
        }
      })(),

      // Gemini
      (async () => {
        try {
          const response = geminiAI.models.generateContentStream({
            model: "gemini-2.5-pro",
            contents: [{ role: "user", parts: [{ text: prompt }] }],
          });
          let full = "";
          for await (const chunk of await response) {
            const text =
              (chunk as { text?: string }).text ??
              (chunk as { candidates?: Array<{ content?: { parts?: Array<{ text?: string }> } }> })
                .candidates?.[0]?.content?.parts?.[0]?.text ??
              "";
            if (text) {
              full += text;
              sendEvent({ model: "gemini", chunk: text });
            }
          }
          await db
            .update(generatedOutputsTable)
            .set({ outputText: full })
            .where(eq(generatedOutputsTable.id, savedIds.gemini));
          sendEvent({
            model: "gemini",
            done: true,
            wordCount: full.split(/\s+/).filter(Boolean).length,
            outputId: savedIds.gemini,
          });
        } catch (err) {
          sendEvent({ model: "gemini", error: (err as Error).message });
        }
      })(),
    ]);

    sendEvent({ allDone: true });
    res.end();
  } catch (err) {
    sendEvent({ error: (err as Error).message, allDone: true });
    res.end();
  }
});

// POST /api/week-start/approve
router.post("/week-start/approve", async (req: Request, res: Response) => {
  const { id } = req.body as { id?: number };
  if (!id) {
    res.status(400).json({ error: "id مطلوب" });
    return;
  }
  const [updated] = await db
    .update(generatedOutputsTable)
    .set({ selected: true })
    .where(eq(generatedOutputsTable.id, id))
    .returning();

  if (updated) {
    const modelLabel =
      updated.modelName === "claude"
        ? "Claude Sonnet"
        : updated.modelName === "openai"
          ? "GPT-4o"
          : "Gemini 2.5 Pro";
    setImmediate(async () => {
      try {
        const [entry] = await db
          .insert(archiveEntriesTable)
          .values({
            title: updated.topic.slice(0, 80),
            bodyText: updated.outputText,
            sourceFile: `معتمد · ${modelLabel}`,
          })
          .returning();
        if (entry) {
          const emb = await generateEmbedding(updated.outputText.slice(0, 800));
          if (emb) {
            await db
              .update(archiveEntriesTable)
              .set({ embedding: emb })
              .where(eq(archiveEntriesTable.id, entry.id));
          }
        }
      } catch (err) {
        req.log?.error({ err }, "archive approved output failed");
      }
    });
  }

  res.json(updated ?? null);
});

// DELETE /api/week-start/archive/:id
router.delete("/week-start/archive/:id", async (req: Request, res: Response) => {
  const id = parseInt(String(req.params.id), 10);
  if (isNaN(id)) {
    res.status(400).json({ error: "id غير صالح" });
    return;
  }
  await db.delete(archiveEntriesTable).where(eq(archiveEntriesTable.id, id));
  res.json({ ok: true });
});

// GET /api/week-start/outputs
router.get("/week-start/outputs", async (_req: Request, res: Response) => {
  const outputs = await db
    .select()
    .from(generatedOutputsTable)
    .orderBy(desc(generatedOutputsTable.createdAt))
    .limit(30);
  res.json(outputs);
});

export default router;
