import { Router, type Request, type Response } from "express";
import { db } from "@workspace/db";
import { weekendDraftsTable } from "@workspace/db";
import { eq, desc, and } from "drizzle-orm";
import { requireAdmin } from "../middleware/auth";
import { geminiJSON } from "../lib/aiProviders";

const router = Router();

// ─── Helper: next Thursday in YYYY-MM-DD ────────────────────────────────────
function nextThursday(fromDate: Date = new Date()): string {
  const d = new Date(fromDate);
  const day = d.getDay(); // 0=Sun, 4=Thu
  const diff = (4 - day + 7) % 7 || 7;
  d.setDate(d.getDate() + diff);
  return d.toISOString().slice(0, 10);
}

// ─── Public (auth-gated): latest PUBLISHED weekend draft ────────────────────
// Returns the most recently published draft for the requested (or upcoming) weekend.
// Falls back to {} if nothing approved yet — the frontend then uses its local seed.
router.get(
  "/weekend/published",
  async (req: Request, res: Response) => {
    const targetDate = (req.query.date as string) || nextThursday();
    const [row] = await db
      .select()
      .from(weekendDraftsTable)
      .where(
        and(
          eq(weekendDraftsTable.status, "published"),
          eq(weekendDraftsTable.weekendDate, targetDate),
        ),
      )
      .orderBy(desc(weekendDraftsTable.publishedAt))
      .limit(1);

    if (!row) {
      // fall back to *any* most recent published draft for the city
      const [latest] = await db
        .select()
        .from(weekendDraftsTable)
        .where(eq(weekendDraftsTable.status, "published"))
        .orderBy(desc(weekendDraftsTable.publishedAt))
        .limit(1);
      res.json({ draft: latest ?? null });
      return;
    }
    res.json({ draft: row });
  },
);

// ─── Admin-only routes ──────────────────────────────────────────────────────
router.use("/weekend/drafts", requireAdmin);
router.use("/weekend/generate", requireAdmin);

// List all drafts, filterable by status
router.get(
  "/weekend/drafts",
  async (req: Request, res: Response) => {
    const status = req.query.status as string | undefined;
    const rows = status
      ? await db
          .select()
          .from(weekendDraftsTable)
          .where(eq(weekendDraftsTable.status, status))
          .orderBy(desc(weekendDraftsTable.createdAt))
      : await db
          .select()
          .from(weekendDraftsTable)
          .orderBy(desc(weekendDraftsTable.createdAt));
    res.json({ drafts: rows });
  },
);

// Get one draft by id
router.get(
  "/weekend/drafts/:id",
  async (req: Request, res: Response) => {
    const id = Number(req.params.id);
    const [row] = await db
      .select()
      .from(weekendDraftsTable)
      .where(eq(weekendDraftsTable.id, id));
    if (!row) {
      res.status(404).json({ error: "المسودة غير موجودة" });
      return;
    }
    res.json({ draft: row });
  },
);

// Generate a new weekend draft using Gemini (Riyadh only)
router.post(
  "/weekend/generate",
  async (req: Request, res: Response) => {
    const weekendDate = (req.body?.weekend_date as string) || nextThursday();
    const userId = (req as any).user?.id ?? null;

    const prompt = `أنت محرر محتوى ترفيهي للموظفين الحكوميين في المملكة العربية السعودية. أنشئ محتوى نهاية أسبوع جاهز للنشر يخص مدينة الرياض ليوم الخميس ${weekendDate} والوينكد بأكمله.

المطلوب: JSON object يحوي الحقول التالية بالعربية الفصحى ولهجة ودودة احترافية مناسبة لجمهور حكومي:

{
  "summary": "فقرة قصيرة (٣ أسطر) ترحيبية تلخص أبرز خيارات الوينكد",
  "places": [
    {"title":"اسم المكان","body":"وصف ٢ سطر يبرز ما يميزه للعائلة","maps_query":"بحث خرائط جوجل بالإنجليزية"}
    // ٤ أماكن في الرياض (متاحف، حدائق، أسواق، فعاليات موسم الرياض)
  ],
  "deals": [
    {"title":"فئة العروض (مثل عروض المطاعم)","items":[
      {"place":"اسم العلامة التجارية","discount":"النسبة أو الميزة","detail":"شرح بسيط","emoji":"🍔"}
    ]}
    // ٣ فئات، كل فئة ٣ عروض من علامات معروفة بالسعودية
  ],
  "podcasts": [
    {"title":"اسم البودكاست","field":"المجال","episode":"اسم الحلقة","body":"وصف","channel":"اسم القناة","tagline":"شعار قصير"}
    // ٣ بودكاستات عربية معروفة
  ],
  "aiTools": [
    {"title":"اسم الأداة","tagline":"شعار قصير","uses":["استخدام ١","استخدام ٢","استخدام ٣"],"emoji":"🤖"}
    // ٣ أدوات ذكاء اصطناعي مفيدة
  ],
  "matches": [
    {"title":"اسم البطولة","teams":"الفريق الأول × الفريق الثاني","time":"وقت المباراة بتوقيت الرياض","channel":"القناة الناقلة"}
    // ٣ مباريات بارزة في الخميس والجمعة والسبت
  ],
  "movies": [
    {"title":"اسم الفيلم","genre":"النوع","cinema":"اسم السينما (Muvi أو VOX)","rating":"التصنيف العمري","body":"وصف ١ سطر للعائلة"}
    // ٣ أفلام عائلية معاصرة
  ]
}

⚠️ تنبيهات حرجة:
- المدينة: الرياض فقط
- جميع الأماكن والعروض حقيقية ومتاحة فعلياً في الرياض
- اذكر علامات تجارية معروفة (Shake Shack, Starbucks, H&M, VOX, Muvi, Fitness Time...)
- يجب أن يكون JSON صالحاً 100% بدون أي نص قبله أو بعده
`;

    let content: any;
    try {
      content = await geminiJSON(prompt, { maxTokens: 4096 });
    } catch (err: any) {
      res.status(502).json({
        error: "فشل التوليد من Gemini",
        detail: String(err?.message || err),
      });
      return;
    }

    const [row] = await db
      .insert(weekendDraftsTable)
      .values({
        weekendDate,
        city: "الرياض",
        status: "pending_review",
        modelName: "gemini-2.5-flash",
        content,
        generatedBy: userId,
      })
      .returning();

    res.status(201).json({ draft: row });
  },
);

// Approve a draft
router.post(
  "/weekend/drafts/:id/approve",
  async (req: Request, res: Response) => {
    const id = Number(req.params.id);
    const userId = (req as any).user?.id ?? null;
    const [existing] = await db
      .select()
      .from(weekendDraftsTable)
      .where(eq(weekendDraftsTable.id, id));
    if (!existing) {
      res.status(404).json({ error: "المسودة غير موجودة" });
      return;
    }
    if (existing.status !== "pending_review") {
      res
        .status(400)
        .json({ error: `لا يمكن اعتماد مسودة بحالة ${existing.status}` });
      return;
    }
    const [row] = await db
      .update(weekendDraftsTable)
      .set({
        status: "approved",
        approvedBy: userId,
        approvedAt: new Date(),
      })
      .where(eq(weekendDraftsTable.id, id))
      .returning();
    res.json({ draft: row });
  },
);

// Publish a draft (sets status=published + publishedAt)
router.post(
  "/weekend/drafts/:id/publish",
  async (req: Request, res: Response) => {
    const id = Number(req.params.id);
    const [existing] = await db
      .select()
      .from(weekendDraftsTable)
      .where(eq(weekendDraftsTable.id, id));
    if (!existing) {
      res.status(404).json({ error: "المسودة غير موجودة" });
      return;
    }
    if (
      existing.status !== "approved" &&
      existing.status !== "pending_review"
    ) {
      res
        .status(400)
        .json({ error: `لا يمكن نشر مسودة بحالة ${existing.status}` });
      return;
    }
    const userId = (req as any).user?.id ?? null;
    const [row] = await db
      .update(weekendDraftsTable)
      .set({
        status: "published",
        approvedBy: existing.approvedBy ?? userId,
        approvedAt: existing.approvedAt ?? new Date(),
        publishedAt: new Date(),
      })
      .where(eq(weekendDraftsTable.id, id))
      .returning();
    res.json({ draft: row });
  },
);

// Reject a draft
router.post(
  "/weekend/drafts/:id/reject",
  async (req: Request, res: Response) => {
    const id = Number(req.params.id);
    const reason = (req.body?.reason as string) || "بدون سبب محدد";
    const [row] = await db
      .update(weekendDraftsTable)
      .set({ status: "rejected", rejectedReason: reason })
      .where(eq(weekendDraftsTable.id, id))
      .returning();
    if (!row) {
      res.status(404).json({ error: "المسودة غير موجودة" });
      return;
    }
    res.json({ draft: row });
  },
);

// Edit a draft's content (admin manual edit before approval)
router.patch(
  "/weekend/drafts/:id",
  async (req: Request, res: Response) => {
    const id = Number(req.params.id);
    const content = req.body?.content;
    if (!content || typeof content !== "object") {
      res.status(400).json({ error: "content مطلوب" });
      return;
    }
    const [row] = await db
      .update(weekendDraftsTable)
      .set({ content })
      .where(eq(weekendDraftsTable.id, id))
      .returning();
    if (!row) {
      res.status(404).json({ error: "المسودة غير موجودة" });
      return;
    }
    res.json({ draft: row });
  },
);

// ═══ إرسال تقرير نهاية الأسبوع عبر قنوات متعددة (ملاحظة 7+8) ═══
// POST /weekend/send  { channels: [{type, to, kind?}], provider: 'unifonic'|'twilio'|'whatsapp-business', period }
// Currently logs the dispatch intent. Wire to actual providers (SMTP/SMS/WhatsApp) when API keys are provisioned.
router.post("/weekend/send", async (req: Request, res: Response) => {
  const { channels = [], provider = 'unifonic', period = 'weekend' } = req.body || {};
  if (!Array.isArray(channels) || channels.length === 0) {
    res.status(400).json({ error: "لا توجد قنوات محددة" });
    return;
  }
  const results: any[] = [];
  for (const c of channels) {
    const ch = String(c?.type || '').toLowerCase();
    const to = String(c?.to || '').trim();
    if (!to) { results.push({ type: ch, ok: false, error: 'فارغ' }); continue; }
    try {
      switch (ch) {
        case 'email':
          // TODO: integrate SMTP / SendGrid. For now mark as queued.
          results.push({ type: 'email', to, kind: c?.kind || 'work', ok: true, status: 'queued', provider: 'smtp' });
          break;
        case 'sms':
          // TODO: integrate Unifonic / Twilio
          results.push({ type: 'sms', to, ok: true, status: 'queued', provider });
          break;
        case 'whatsapp':
          // TODO: integrate WhatsApp Business API / Twilio
          results.push({ type: 'whatsapp', to, ok: true, status: 'queued', provider });
          break;
        default:
          results.push({ type: ch, ok: false, error: 'نوع قناة غير مدعوم' });
      }
    } catch (e: any) {
      results.push({ type: ch, to, ok: false, error: e?.message || String(e) });
    }
  }
  const successCount = results.filter(r => r.ok).length;
  res.json({ ok: successCount > 0, period, provider, channels: channels.length, dispatched: successCount, results });
});

// Delete a draft
router.delete(
  "/weekend/drafts/:id",
  async (req: Request, res: Response) => {
    const id = Number(req.params.id);
    const result = await db
      .delete(weekendDraftsTable)
      .where(eq(weekendDraftsTable.id, id))
      .returning();
    if (result.length === 0) {
      res.status(404).json({ error: "المسودة غير موجودة" });
      return;
    }
    res.json({ ok: true });
  },
);

export default router;
