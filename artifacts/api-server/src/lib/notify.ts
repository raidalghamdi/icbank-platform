/**
 * Unified notification helper for Shorfah.
 * Tasks 5 + 7: Inserts shorfah_notifications row, optionally sends email via Resend.
 */
import { db } from "@workspace/db";
import { shorfahNotificationsTable, shorfahRemindersTable } from "@workspace/db";

const RESEND_API_KEY = process.env.RESEND_API_KEY;
const RESEND_FROM = process.env.RESEND_FROM || "شرفة <noreply@internal.sa>";

interface SendNotificationOpts {
  userId: number;
  issueId?: number | null;
  sectionId?: number | null;
  channel: "in_app" | "email" | "both";
  type: string;
  title: string;
  body?: string | null;
  url?: string | null;
  emailHtml?: string | null;
  recipientEmail?: string | null;
  // For reminder log
  assignmentId?: number | null;
  reminderType?: string;
}

export async function sendNotification(opts: SendNotificationOpts): Promise<void> {
  // Always insert in-app notification
  await db.insert(shorfahNotificationsTable).values({
    userId: opts.userId,
    issueId: opts.issueId ?? null,
    sectionId: opts.sectionId ?? null,
    type: opts.type,
    title: opts.title,
    body: opts.body ?? null,
    url: opts.url ?? null,
    isRead: false,
  });

  // Log to shorfah_reminders if it's a reminder-type notification
  if (opts.reminderType && opts.sectionId) {
    await db.insert(shorfahRemindersTable).values({
      sectionId: opts.sectionId,
      assignmentId: opts.assignmentId ?? null,
      recipientUserId: opts.userId,
      channel: opts.channel,
      reminderType: opts.reminderType,
      status: "sent",
      message: opts.body ?? opts.title,
    });
  }

  // Email
  if ((opts.channel === "email" || opts.channel === "both") && opts.recipientEmail) {
    if (!RESEND_API_KEY) {
      // Fallback: console log the email content
      console.log("[notify] RESEND_API_KEY not set — email would have been sent:");
      console.log(`  To: ${opts.recipientEmail}`);
      console.log(`  Subject: ${opts.title}`);
      console.log(`  Body: ${opts.body ?? ""}`);
      return;
    }

    try {
      const { Resend } = await import("resend");
      const resend = new Resend(RESEND_API_KEY);
      const html = opts.emailHtml || buildDefaultEmailHtml(opts.title, opts.body ?? "", opts.url ?? "");
      await resend.emails.send({
        from: RESEND_FROM,
        to: opts.recipientEmail,
        subject: opts.title,
        html,
      });
    } catch (err) {
      console.error("[notify] Resend error:", err);
      // Do NOT throw — graceful fallback, in-app notification already created
    }
  }
}

/** Build a simple branded Arabic HTML email */
export function buildDefaultEmailHtml(title: string, body: string, url: string): string {
  const teal = "#1a6e7a";
  const navy = "#0e3b4a";
  return `<!DOCTYPE html>
<html dir="rtl" lang="ar">
<head>
<meta charset="UTF-8"/>
<link href="https://fonts.googleapis.com/css2?family=Tajawal:wght@400;700;900&display=swap" rel="stylesheet"/>
<style>
body{margin:0;padding:0;background:#f0f7f8;font-family:'Tajawal',system-ui,sans-serif;direction:rtl;}
.wrap{max-width:560px;margin:40px auto;background:#fff;border-radius:12px;overflow:hidden;box-shadow:0 2px 12px rgba(0,0,0,.08);}
.header{background:linear-gradient(135deg,${teal} 0%,#2a8d9b 60%,#3ec0d0 100%);color:#fff;padding:32px 36px;text-align:center;}
.header h1{margin:0;font-size:28px;font-weight:900;letter-spacing:-0.5px;}
.header p{margin:8px 0 0;opacity:.9;font-size:14px;}
.body{padding:28px 36px;}
.body h2{color:${navy};font-size:20px;font-weight:900;margin:0 0 12px;}
.body p{color:#374151;font-size:15px;line-height:1.75;margin:0 0 16px;}
.btn{display:inline-block;background:${teal};color:#fff;text-decoration:none;padding:12px 28px;border-radius:8px;font-weight:700;font-size:15px;margin-top:8px;}
.footer{background:#f9fafb;padding:16px 36px;text-align:center;font-size:12px;color:#9ca3af;border-top:1px solid #e5e7eb;}
</style>
</head>
<body>
<div class="wrap">
  <div class="header">
    <h1>شُرفة</h1>
    <p>المجلة الداخلية الشهرية — الهيئة العامة للمنافسة</p>
  </div>
  <div class="body">
    <h2>${title}</h2>
    <p>${body.replace(/\n/g, "<br/>")}</p>
    ${url ? `<a href="${url}" class="btn">اضغط هنا للاطلاع</a>` : ""}
  </div>
  <div class="footer">الهيئة العامة للمنافسة — شرفة الداخلية</div>
</div>
</body>
</html>`;
}

/** Email template for initial contribution request */
export function buildInitialEmailHtml(opts: {
  recipientName: string;
  sectionTitle: string;
  issueTitleAr: string;
  deadline: string;
  url: string;
}): string {
  return buildDefaultEmailHtml(
    `مطلوب مساهمتك في شُرفة`,
    `عزيزي ${opts.recipientName}،\n\nتمت دعوتك للمساهمة في قسم "${opts.sectionTitle}" من عدد "${opts.issueTitleAr}".\n\nآخر موعد للتسليم: ${opts.deadline}.\n\nيُرجى تسجيل الدخول وإضافة المحتوى المطلوب في أقرب وقت.`,
    opts.url,
  );
}

/** Email template for overdue reminder */
export function buildOverdueEmailHtml(opts: {
  recipientName: string;
  sectionTitle: string;
  issueTitleAr: string;
  daysOverdue: number;
  url: string;
}): string {
  return buildDefaultEmailHtml(
    `تذكير: قسم "${opts.sectionTitle}" متأخر`,
    `عزيزي ${opts.recipientName}،\n\nقسم "${opts.sectionTitle}" في عدد "${opts.issueTitleAr}" تأخر بمقدار ${opts.daysOverdue} يوم عن الموعد المحدد.\n\nيُرجى تسليم المحتوى في أقرب وقت ممكن.`,
    opts.url,
  );
}

/** Email template for publish announcement */
export function buildPublishEmailHtml(opts: {
  issueTitleAr: string;
  month: string;
  year: number;
  issueNo: number;
  url: string;
  pdfUrl: string;
}): string {
  return buildDefaultEmailHtml(
    `عدد جديد من شُرفة — ${opts.month} ${opts.year}`,
    `يسعدنا الإعلان عن صدور العدد ${opts.issueNo} من مجلة شُرفة الداخلية "${opts.issueTitleAr}".\n\nتفضل بقراءة العدد الجديد من خلال الرابط أدناه.`,
    opts.url,
  );
}
