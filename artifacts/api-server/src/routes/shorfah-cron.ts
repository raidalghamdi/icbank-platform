/**
 * Shorfah cron routes — registered BEFORE requireAuth so they can be called
 * by Railway/external cron schedulers without a JWT token.
 * Protected by x-cron-secret header instead.
 */
import { Router, type Request, type Response } from "express";
import { db } from "@workspace/db";
import {
  shorfahIssuesTable,
  shorfahSectionsTable,
  shorfahAssignmentsTable,
  usersTable,
} from "@workspace/db";
import { eq, inArray } from "drizzle-orm";
import { sendNotification, buildOverdueEmailHtml } from "../lib/notify";

const router = Router();

const CRON_SECRET = process.env.API_KEY || "9AyAoIvL1gtuf7m_9v7LkTfNzx0CLsFPGmhqP3nt0BI";

router.post("/cron/shorfah/check-overdue", async (req: Request, res: Response) => {
  const secret = req.headers["x-cron-secret"] || req.headers["x-api-key"];
  if (secret !== CRON_SECRET) {
    return res.status(401).json({ error: "Unauthorized" });
  }

  try {
    const now = new Date();
    // Find sections past deadline with non-final workflow statuses
    const overdueSections = await db
      .select()
      .from(shorfahSectionsTable)
      .where(
        inArray(shorfahSectionsTable.workflowStatus, ["pending_contribution", "submitted"])
      );
    // Filter in JS for past deadline (avoids complex drizzle null checks)
    const now_ts = now.getTime();
    const filteredOverdue = overdueSections.filter(s => 
      s.slaDeadline !== null && s.slaDeadline !== undefined && new Date(s.slaDeadline).getTime() < now_ts
    );

    let notified = 0;
    for (const section of filteredOverdue) {
      const [issue] = await db.select().from(shorfahIssuesTable).where(eq(shorfahIssuesTable.id, section.issueId)).limit(1);
      
      const assignments = await db.select().from(shorfahAssignmentsTable)
        .where(eq(shorfahAssignmentsTable.sectionId, section.id));

      const daysOverdue = section.slaDeadline
        ? Math.floor((now.getTime() - section.slaDeadline.getTime()) / 86400000)
        : 0;

      for (const assignment of assignments) {
        const [user] = await db.select({ id: usersTable.id, email: usersTable.email, name: usersTable.name })
          .from(usersTable)
          .where(eq(usersTable.id, assignment.userId))
          .limit(1);

        await sendNotification({
          userId: assignment.userId,
          issueId: section.issueId,
          sectionId: section.id,
          channel: user?.email ? "both" : "in_app",
          type: "reminder_overdue",
          title: `قسم "${section.titleAr}" متأخر عن الموعد بـ ${daysOverdue} يوم`,
          body: `يُرجى تسليم المحتوى الخاص بك في أقرب وقت ممكن.`,
          url: `/#/shorfah/${section.issueId}`,
          recipientEmail: user?.email ?? null,
          assignmentId: assignment.id,
          reminderType: "overdue",
          emailHtml: buildOverdueEmailHtml({
            recipientName: user?.name ?? "المساهم",
            sectionTitle: section.titleAr,
            issueTitleAr: issue?.titleAr || "شرفة",
            daysOverdue,
            url: `https://icbank-platform-internal-comms.vercel.app/#/shorfah/${section.issueId}`,
          }),
        });
        notified++;
      }
    }

    res.json({ ok: true, overdueSections: filteredOverdue.length, notified });
  } catch (err) {
    console.error("[cron/shorfah/check-overdue]", err);
    res.status(500).json({ error: "Internal error", details: err instanceof Error ? err.message : "unknown" });
  }
});

export default router;
