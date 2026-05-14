import { Router, type IRouter, type Request, type Response } from "express";
import { db, dailyReportsTable } from "@workspace/db";
import { desc, eq } from "drizzle-orm";
import { z } from "zod";

const router: IRouter = Router();

function requireApiKey(req: Request, res: Response, next: () => void) {
  const key = req.headers["x-api-key"];
  if (!key || key !== process.env.REPORT_API_KEY) {
    res.status(401).json({ error: "Unauthorized" });
    return;
  }
  next();
}

const DailyReportInputSchema = z.object({
  reportDate: z.string().regex(/^\d{4}-\d{2}-\d{2}$/),
  reportData: z.record(z.string(), z.unknown()),
});

router.post("/daily-report", requireApiKey, async (req: Request, res: Response) => {
  const parsed = DailyReportInputSchema.safeParse(req.body);
  if (!parsed.success) {
    res.status(400).json({ error: "Invalid payload", details: parsed.error.issues });
    return;
  }

  const { reportDate, reportData } = parsed.data;

  const existing = await db
    .select()
    .from(dailyReportsTable)
    .where(eq(dailyReportsTable.reportDate, reportDate))
    .limit(1);

  let report;
  if (existing.length > 0) {
    const updated = await db
      .update(dailyReportsTable)
      .set({ reportData })
      .where(eq(dailyReportsTable.reportDate, reportDate))
      .returning();
    report = updated[0];
  } else {
    const inserted = await db
      .insert(dailyReportsTable)
      .values({ reportDate, reportData })
      .returning();
    report = inserted[0];
  }

  res.status(201).json(report);
});

router.get("/daily-report/latest", async (_req: Request, res: Response) => {
  const rows = await db
    .select()
    .from(dailyReportsTable)
    .orderBy(desc(dailyReportsTable.reportDate))
    .limit(1);

  if (!rows.length) {
    res.status(404).json({ error: "No report found" });
    return;
  }

  res.json(rows[0]);
});

export default router;
