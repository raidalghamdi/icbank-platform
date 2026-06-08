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

export const insertPromptFrameworkSchema = createInsertSchema(promptFrameworksTable).omit({
  id: true,
  createdAt: true,
  updatedAt: true,
  usageCount: true,
});

export type PromptFramework = typeof promptFrameworksTable.$inferSelect;
export type InsertPromptFramework = typeof promptFrameworksTable.$inferInsert;
