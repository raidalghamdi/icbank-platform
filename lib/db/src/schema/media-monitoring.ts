/**
 * Media Monitoring + Prompt Frameworks
 *
 * Tables:
 *  - media_reports: تقارير الرصد الإعلامي المؤرشفة (نصوص مولّدة بـ AI + ملخصات + نبرة)
 *  - prompt_frameworks: مكتبة Prompts المعتمدة لتوحيد المخرجات
 */

import { pgTable, serial, text, integer, timestamp, jsonb, boolean } from "drizzle-orm/pg-core";
import { createInsertSchema } from "drizzle-zod";

// ─── تقارير الرصد الإعلامي ────────────────────────────────────────────
export const mediaReportsTable = pgTable("media_reports", {
  id: serial("id").primaryKey(),
  // عنوان التقرير
  title: text("title").notNull(),
  // نوع التقرير: weekly | monthly | custom | adhoc
  reportType: text("report_type").notNull().default("weekly"),
  // قالب الجمهور: executive | manager | analyst | full
  audience: text("audience").notNull().default("manager"),
  // النطاق الزمني
  dateFrom: timestamp("date_from", { withTimezone: true }).notNull(),
  dateTo: timestamp("date_to", { withTimezone: true }).notNull(),
  // المصادر المشمولة: linkedin, twitter, news, etc.
  sources: jsonb("sources").$type<string[]>().notNull().default([]),
  // الملخص التنفيذي المولّد من AI (Markdown)
  executiveSummary: text("executive_summary"),
  // المحتوى الكامل بصيغة Markdown
  contentMd: text("content_md").notNull(),
  // إحصائيات: عدد المنشورات، التوزيع النبري، إلخ
  stats: jsonb("stats").$type<{
    totalPosts?: number;
    linkedinCount?: number;
    newsCount?: number;
    toneDistribution?: Record<string, number>;
    topThemes?: string[];
  }>(),
  // تحليل النبرة الإجمالي للفترة
  overallTone: text("overall_tone"),
  // البيانات الخام المستخدمة (snapshot for audit)
  sourceItems: jsonb("source_items").$type<unknown[]>().notNull().default([]),
  // المستخدم الذي ولّد التقرير
  generatedByUserId: integer("generated_by_user_id"),
  generatedByName: text("generated_by_name"),
  // الموديل المستخدم
  aiModel: text("ai_model").default("gemini-2.5-flash"),
  // الحالة: draft | published | archived
  status: text("status").notNull().default("published"),
  // تاريخ الإنشاء والتحديث
  createdAt: timestamp("created_at", { withTimezone: true }).notNull().defaultNow(),
  updatedAt: timestamp("updated_at", { withTimezone: true }).notNull().defaultNow(),
});

export const insertMediaReportSchema = createInsertSchema(mediaReportsTable).omit({
  id: true,
  createdAt: true,
  updatedAt: true,
});

export type MediaReport = typeof mediaReportsTable.$inferSelect;
export type InsertMediaReport = typeof mediaReportsTable.$inferInsert;

// ─── مكتبة Prompt Frameworks ───────────────────────────────────────────
export const promptFrameworksTable = pgTable("prompt_frameworks", {
  id: serial("id").primaryKey(),
  // اسم القالب (عربي)
  nameAr: text("name_ar").notNull(),
  nameEn: text("name_en"),
  // الوصف
  descriptionAr: text("description_ar"),
  // الفئة: media-report | content-creation | analysis | summarization | rewriting | insights
  category: text("category").notNull().default("content-creation"),
  // النوع: framework (هيكلي) | template (جاهز للنسخ)
  kind: text("kind").notNull().default("framework"),
  // نص الـ Prompt (مع متغيرات {{var}})
  promptText: text("prompt_text").notNull(),
  // المتغيرات الديناميكية
  variables: jsonb("variables").$type<Array<{ key: string; label: string; type?: string; required?: boolean }>>().notNull().default([]),
  // مثال استخدام
  exampleInput: text("example_input"),
  exampleOutput: text("example_output"),
  // العلامات / الكلمات المفتاحية
  tags: jsonb("tags").$type<string[]>().notNull().default([]),
  // الموديل الموصى به
  recommendedModel: text("recommended_model").default("gemini-2.5-flash"),
  // معتمد رسمياً؟
  isApproved: boolean("is_approved").notNull().default(false),
  // عدد مرات الاستخدام
  usageCount: integer("usage_count").notNull().default(0),
  // المستخدم الذي أنشأ
  createdByUserId: integer("created_by_user_id"),
  createdByName: text("created_by_name"),
  // الحالة
  status: text("status").notNull().default("active"),
  createdAt: timestamp("created_at", { withTimezone: true }).notNull().defaultNow(),
  updatedAt: timestamp("updated_at", { withTimezone: true }).notNull().defaultNow(),
});

// ─── التقارير النهائية المحفوظة (immutable) ────────────────────────────
// تطابق القالب الرسمي للهيئة العامة للمنافسة (8 أقسام)
// لا يمكن حذفها أو تعديلها بعد الإصدار (status='final')
export const finalMediaReportsTable = pgTable("final_media_reports", {
  id: serial("id").primaryKey(),
  // رقم التقرير الرسمي مثل GAC-MEDIA-21/2026
  reportNumber: text("report_number").notNull().unique(),
  // عنوان التقرير
  title: text("title").notNull(),
  // نوع: weekly | monthly | custom
  reportType: text("report_type").notNull().default("weekly"),
  // النطاق الزمني
  periodLabel: text("period_label").notNull(), // "مايو 2026" / "12 - 26 مايو 2026"
  dateFrom: timestamp("date_from", { withTimezone: true }).notNull(),
  dateTo: timestamp("date_to", { withTimezone: true }).notNull(),
  // البيانات الأساسية للغلاف
  preparedBy: text("prepared_by").default("الإدارة التنفيذية للتواصل المؤسسي"),
  beneficiary: text("beneficiary").default("الإدارة التنفيذية"),
  referenceNumber: text("reference_number"),
  classification: text("classification").default("سري — للاستخدام الداخلي"),
  issueDate: timestamp("issue_date", { withTimezone: true }).notNull().defaultNow(),
  // المؤشرات الرئيسية
  kpis: jsonb("kpis").$type<{
    totalNews?: number;
    positivePercent?: number;
    mediaOutlets?: number;
    keyTopics?: number;
    reach?: string; // "7.2 م"
    alertsCount?: number;
  }>().notNull().default({}),
  // 1. الملخص التنفيذي
  executiveSummary: text("executive_summary"),
  // 2. أبرز الأخبار
  topNews: jsonb("top_news").$type<Array<{
    date: string; tone: string; headline: string;
    details: string[]; source: string;
  }>>().notNull().default([]),
  // 3. الجدول الزمني التفصيلي
  timeline: jsonb("timeline").$type<Array<{
    date: string; event: string; outlet: string; tone: string; count: number;
  }>>().notNull().default([]),
  // 4. تحليل الحضور الرقمي
  digitalPresence: jsonb("digital_presence").$type<{
    platforms: Array<{ name: string; mentions: number; reposts: number; engagement: number; reach: string }>;
    hashtags: Array<{ tag: string; uses: number; trend: string }>;
  }>().notNull().default({ platforms: [], hashtags: [] }),
  // 5. تحليل التوجه الإعلامي
  editorialTone: jsonb("editorial_tone").$type<{
    distribution: Array<{ tone: string; percent: number; count: number }>;
    classification: Array<{ topic: string; percent: number; count: number }>;
    sources: Array<{ source: string; percent: number; count: number }>;
  }>().notNull().default({ distribution: [], classification: [], sources: [] }),
  // 6. تحليل عميق ومؤشرات قطاعية
  deepAnalysis: jsonb("deep_analysis").$type<{
    keywords: Array<{ keyword: string; frequency: number; context: string }>;
    quote: { text: string; source: string; date: string } | null;
    strengths: string[];
    weaknesses: string[];
  }>().notNull().default({ keywords: [], quote: null, strengths: [], weaknesses: [] }),
  // 7. مقارنة إقليمية
  regionalComparison: jsonb("regional_comparison").$type<Array<{
    authority: string; country: string; mentions: number; tone: string; highlights: string;
  }>>().notNull().default([]),
  // 8. التوصيات وخطة العمل + التنبيهات والمواقف المقترحة
  recommendations: jsonb("recommendations").$type<Array<{
    title: string; description: string; priority: string;
    responsible: string; kpi: string; deadline: string; dependencies: string;
  }>>().notNull().default([]),
  alerts: jsonb("alerts").$type<Array<{
    alert: string; suggestedPosition: string;
  }>>().notNull().default([]),
  // ملحق الاقتباسات
  quotesAppendix: jsonb("quotes_appendix").$type<Array<{
    quote: string; source: string; date: string; topic: string;
  }>>().notNull().default([]),
  // المنهجية والمصادر
  methodology: text("methodology"),
  sources: jsonb("sources").$type<Array<{ name: string; url: string; description?: string }>>().notNull().default([]),
  // البيانات الخام كـ snapshot
  sourceItems: jsonb("source_items").$type<unknown[]>().notNull().default([]),
  // ميتاداتا التوليد
  generatedByUserId: integer("generated_by_user_id"),
  generatedByName: text("generated_by_name"),
  aiModel: text("ai_model").default("gemini-2.5-flash"),
  // الحالة دائماً 'final' (no DELETE/UPDATE)
  status: text("status").notNull().default("final"),
  // وقت القفل + بصمة sha256 للنزاهة
  lockedAt: timestamp("locked_at", { withTimezone: true }).notNull().defaultNow(),
  contentSha256: text("content_sha256").notNull(),
  // المرفقات
  pdfStorageKey: text("pdf_storage_key"),
  // عدد مرات الاطلاع
  viewCount: integer("view_count").notNull().default(0),
  createdAt: timestamp("created_at", { withTimezone: true }).notNull().defaultNow(),
});

export const insertFinalMediaReportSchema = createInsertSchema(finalMediaReportsTable).omit({
  id: true,
  createdAt: true,
  lockedAt: true,
  viewCount: true,
});

export type FinalMediaReport = typeof finalMediaReportsTable.$inferSelect;
export type InsertFinalMediaReport = typeof finalMediaReportsTable.$inferInsert;

// ─── سجل استعلامات معالج الأسئلة + البحث ───────────────────────────────
export const reportsQaQueriesTable = pgTable("reports_qa_queries", {
  id: serial("id").primaryKey(),
  userId: integer("user_id"),
  userName: text("user_name"),
  // نوع: wizard | search-full | search-info
  queryType: text("query_type").notNull(),
  // مدخلات معالج الأسئلة (للنوع wizard)
  wizardAnswers: jsonb("wizard_answers").$type<{
    period?: string;
    audience?: string;
    sources?: string[];
    focusTopics?: string;
    language?: string;
    recipients?: string;
    mode?: string; // generate | search
  }>(),
  // نص البحث (للأنواع search-*)
  searchQuery: text("search_query"),
  // معرف التقرير المرتبط (إن وُجد)
  finalReportId: integer("final_report_id"),
  // النتيجة المختصرة
  resultSummary: text("result_summary"),
  // ميتاداتا
  metadata: jsonb("metadata").$type<Record<string, unknown>>(),
  createdAt: timestamp("created_at", { withTimezone: true }).notNull().defaultNow(),
});

export const insertReportsQaQuerySchema = createInsertSchema(reportsQaQueriesTable).omit({
  id: true,
  createdAt: true,
});

export type ReportsQaQuery = typeof reportsQaQueriesTable.$inferSelect;
export type InsertReportsQaQuery = typeof reportsQaQueriesTable.$inferInsert;

export const insertPromptFrameworkSchema = createInsertSchema(promptFrameworksTable).omit({
  id: true,
  createdAt: true,
  updatedAt: true,
  usageCount: true,
});

export type PromptFramework = typeof promptFrameworksTable.$inferSelect;
export type InsertPromptFramework = typeof promptFrameworksTable.$inferInsert;
