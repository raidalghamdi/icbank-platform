import { Router, type Request, type Response } from "express";
import { anthropicAdapter as anthropic } from "../lib/aiProviders";
import { db } from "@workspace/db";
import {
  aiYearActivationsTable,
  internationalDaysTable,
  archiveEntriesTable,
} from "@workspace/db";
import { desc, count } from "drizzle-orm";

const router: Router = Router();

// AI calls are routed through the unified provider (Gemini-backed).

// ─── GET /api/dashboard/summary ─────────────────────────────────────────────
router.get("/dashboard/summary", async (req: Request, res: Response) => {
  try {
    const now = new Date();
    const currentMonth = now.getMonth() + 1;
    const currentYear = now.getFullYear();

    const [aiYearStatsResult, recentActivationsResult, weekStartResult, weekStartTotalRow, intlDaysResult] =
      await Promise.all([
        db.select({ count: count() }).from(aiYearActivationsTable),
        db
          .select({
            id: aiYearActivationsTable.id,
            title: aiYearActivationsTable.title,
            type: aiYearActivationsTable.type,
            status: aiYearActivationsTable.status,
            activationDate: aiYearActivationsTable.activationDate,
            createdAt: aiYearActivationsTable.createdAt,
          })
          .from(aiYearActivationsTable)
          .orderBy(desc(aiYearActivationsTable.createdAt))
          .limit(5),
        db
          .select({
            id: archiveEntriesTable.id,
            title: archiveEntriesTable.title,
            createdAt: archiveEntriesTable.createdAt,
          })
          .from(archiveEntriesTable)
          .orderBy(desc(archiveEntriesTable.createdAt))
          .limit(50),
        db.select({ count: count() }).from(archiveEntriesTable),
        db
          .select({
            id: internationalDaysTable.id,
            dayNameAr: internationalDaysTable.dayNameAr,
            annualDate: internationalDaysTable.annualDate,
            category: internationalDaysTable.category,
          })
          .from(internationalDaysTable)
          .limit(300),
      ]);

    // Arabic month names → month number
    const AR_MONTHS: Record<string, number> = {
      يناير: 1, فبراير: 2, مارس: 3, أبريل: 4, ابريل: 4,
      مايو: 5, يونيو: 6, يوليو: 7, أغسطس: 8, اغسطس: 8,
      سبتمبر: 9, أكتوبر: 10, اكتوبر: 10, نوفمبر: 11, ديسمبر: 12,
    };

    // Parse "DD MonthAr" (e.g. "17 مايو") OR "MM-DD" (e.g. "05-17")
    function parseAnnualDate(raw: string): { mm: number; dd: number } | null {
      const trimmed = raw.trim();
      // Arabic format: "17 مايو"
      const arMatch = trimmed.match(/^(\d{1,2})\s+(\S+)$/);
      if (arMatch) {
        const dd = parseInt(arMatch[1]!);
        const mm = AR_MONTHS[arMatch[2]!.trim()] ?? NaN;
        if (!isNaN(mm) && !isNaN(dd)) return { mm, dd };
      }
      // MM-DD format
      const numMatch = trimmed.match(/^(\d{1,2})-(\d{1,2})$/);
      if (numMatch) {
        const mm = parseInt(numMatch[1]!);
        const dd = parseInt(numMatch[2]!);
        if (!isNaN(mm) && !isNaN(dd)) return { mm, dd };
      }
      return null;
    }

    // Resolve upcoming international days within the next 30 days
    type UpcomingDay = { id: number; name: string; date: string; daysUntil: number; category: string | null };
    const upcomingDays: UpcomingDay[] = [];
    const todayStart = new Date(now);
    todayStart.setHours(0, 0, 0, 0);

    for (const day of intlDaysResult) {
      if (!day.annualDate) continue;
      const parsed = parseAnnualDate(day.annualDate);
      if (!parsed) continue;
      const { mm, dd } = parsed;

      const thisYearDate = new Date(currentYear, mm - 1, dd);
      thisYearDate.setHours(0, 0, 0, 0);
      const target = thisYearDate >= todayStart ? thisYearDate : new Date(currentYear + 1, mm - 1, dd);
      const daysUntil = Math.round((target.getTime() - todayStart.getTime()) / 86_400_000);

      if (daysUntil >= 0 && daysUntil <= 30) {
        upcomingDays.push({ id: day.id, name: day.dayNameAr, date: target.toISOString().split("T")[0], daysUntil, category: day.category });
      }
    }
    upcomingDays.sort((a, b) => a.daysUntil - b.daysUntil);

    // Week-start entries this month
    const wsThisMonth = weekStartResult.filter((e) => {
      if (!e.createdAt) return false;
      const d = new Date(e.createdAt);
      return d.getMonth() + 1 === currentMonth && d.getFullYear() === currentYear;
    });

    res.json({
      kpi: {
        aiYearActivations: Number(aiYearStatsResult[0]?.count ?? 0),
        weekStartThisMonth: wsThisMonth.length,
        weekStartTotal: Number(weekStartTotalRow[0]?.count ?? 0),
        intlDaysUpcomingCount: upcomingDays.length,
      },
      weekStart: {
        thisMonthCount: wsThisMonth.length,
        totalCount: Number(weekStartTotalRow[0]?.count ?? 0),
        lastTitle: weekStartResult[0]?.title ?? null,
      },
      aiYear: {
        totalActivations: Number(aiYearStatsResult[0]?.count ?? 0),
        recentActivations: recentActivationsResult,
      },
      intlDaysUpcoming: upcomingDays.slice(0, 3),
    });
  } catch (err) {
    req.log.error({ err }, "dashboard summary error");
    res.status(500).json({ error: "خطأ في تحميل لوحة القيادة" });
  }
});

// ─── POST /api/dashboard/ai-summary ─────────────────────────────────────────
router.post("/dashboard/ai-summary", async (req: Request, res: Response) => {
  try {
    const [aiStats, recentAct, wsEntries] = await Promise.all([
      db.select({ count: count() }).from(aiYearActivationsTable),
      db
        .select({ title: aiYearActivationsTable.title, type: aiYearActivationsTable.type })
        .from(aiYearActivationsTable)
        .orderBy(desc(aiYearActivationsTable.createdAt))
        .limit(5),
      db
        .select({ title: archiveEntriesTable.title })
        .from(archiveEntriesTable)
        .orderBy(desc(archiveEntriesTable.createdAt))
        .limit(3),
    ]);

    const lines = [
      `إجمالي تفعيلات عام الذكاء الاصطناعي: ${Number(aiStats[0]?.count || 0)}`,
      recentAct.length ? `آخر التفعيلات المضافة: ${recentAct.map((a) => a.title).join("، ")}` : "",
      wsEntries.length ? `آخر رسائل بداية الأسبوع: ${wsEntries.map((e) => e.title).join("، ")}` : "",
    ].filter(Boolean).join("\n");

    const message = await anthropic.messages.create({
      model: "claude-sonnet-4-5",
      max_tokens: 400,
      messages: [
        {
          role: "user",
          content: `أنت مساعد تنفيذي متخصص في التواصل الداخلي المؤسسي. بناءً على البيانات التالية:\n${lines}\n\nاكتب ملخصاً تنفيذياً قصيراً (3-4 نقاط عربية) يلخص نشاط الإدارة في التواصل الداخلي. كل نقطة في سطر منفصل تبدأ بـ •. كن موجزاً ومهنياً.`,
        },
      ],
    });

    const summary = ((message.content[0] as { type: string; text: string }).text ?? "").trim();
    res.json({ summary, generatedAt: new Date().toISOString() });
  } catch (err) {
    req.log.error({ err }, "dashboard ai-summary error");
    res.status(500).json({ error: "فشل توليد الملخص الذكي" });
  }
});

export default router;
