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

const N8NReportSchema = z.object({
  report_date: z.string().regex(/^\d{4}-\d{2}-\d{2}$/).optional(),
  reportDate: z.string().regex(/^\d{4}-\d{2}-\d{2}$/).optional(),
  title: z.string().optional(),
  subtitle: z.string().optional(),
  summary: z.string().optional(),
  risk_level: z.string().optional(),
  kpis: z.record(z.string(), z.unknown()).optional(),
  breakdowns: z.record(z.string(), z.unknown()).optional(),
  overdue_projects: z.array(z.record(z.string(), z.unknown())).optional(),
  target_initiatives: z.array(z.record(z.string(), z.unknown())).optional(),
  keyObservations: z.array(z.string()).optional(),
  recommendations: z.array(z.string()).optional(),
}).passthrough();

function normalizeN8NPayload(body: Record<string, unknown>): { reportDate: string; reportData: Record<string, unknown> } {
  const date = (body.report_date || body.reportDate) as string;

  const reportData: Record<string, unknown> = { ...body };

  if (body.overdue_projects) {
    reportData.overdueProjects = body.overdue_projects;
  }
  if (body.target_initiatives) {
    reportData.initiatives = body.target_initiatives;
  }
  if (body.kpis && typeof body.kpis === "object") {
    reportData.kpis = body.kpis;
  }
  if (body.breakdowns && typeof body.breakdowns === "object") {
    reportData.breakdowns = body.breakdowns;
  }

  reportData._receivedAt = new Date().toISOString();
  reportData._source = "n8n";

  delete reportData.report_date;
  delete reportData.reportDate;

  return { reportDate: date, reportData };
}

async function upsertReport(reportDate: string, reportData: Record<string, unknown>, res: Response) {
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
}

router.post("/daily-report", requireApiKey, async (req: Request, res: Response) => {
  const parsed = DailyReportInputSchema.safeParse(req.body);
  if (!parsed.success) {
    res.status(400).json({ error: "Invalid payload", details: parsed.error.issues });
    return;
  }
  await upsertReport(parsed.data.reportDate, parsed.data.reportData, res);
});

router.post("/report", requireApiKey, async (req: Request, res: Response) => {
  const parsed = N8NReportSchema.safeParse(req.body);
  if (!parsed.success) {
    res.status(400).json({ error: "Invalid payload", details: parsed.error.issues });
    return;
  }

  const date = (parsed.data.report_date || parsed.data.reportDate) as string | undefined;
  if (!date) {
    res.status(400).json({ error: "report_date is required (YYYY-MM-DD)" });
    return;
  }

  const { reportDate, reportData } = normalizeN8NPayload(parsed.data as Record<string, unknown>);
  await upsertReport(reportDate, reportData, res);
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

router.get("/report/latest", async (_req: Request, res: Response) => {
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
