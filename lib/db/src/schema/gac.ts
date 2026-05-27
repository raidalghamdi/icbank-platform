/**
 * GAC Content Tables — مكتبة الهيئة + خلاصة التواصل الاجتماعي
 *
 * Sources:
 *  - gac_publications: PDFs مستخرجة من gacbep.gac.gov.sa عبر Wayback Machine
 *    (الموقع الأصلي محجوب جغرافياً خارج السعودية).
 *  - gac_social_posts: منشورات LinkedIn (cron كل ساعة) + Twitter يستخدم
 *    publish.twitter.com embed مباشرة بدون DB.
 *  - gac_news_items: أخبار/قرارات يتم استخراجها يدوياً عند الحاجة.
 */

import { pgTable, serial, text, integer, timestamp, boolean, jsonb } from "drizzle-orm/pg-core";
import { createInsertSchema } from "drizzle-zod";

// ─── المكتبة: الأدلة والإصدارات الرسمية ────────────────────────────────
export const gacPublicationsTable = pgTable("gac_publications", {
  id: serial("id").primaryKey(),
  // العنوان بالعربي (إلزامي) والإنجليزي (اختياري)
  titleAr: text("title_ar").notNull(),
  titleEn: text("title_en"),
  // التصنيف: guidelines | regulations | statistics | research | brand | policy
  category: text("category").notNull(),
  // اللغة الأساسية للوثيقة
  language: text("language").notNull().default("ar"), // ar | en | both
  // وصف موجز
  descriptionAr: text("description_ar"),
  descriptionEn: text("description_en"),
  // الإصدار / النسخة (e.g. "v5", "2024", "Q2-2024")
  version: text("version"),
  // تاريخ النشر الأصلي (إن كان معروفاً)
  publishedAt: timestamp("published_at", { withTimezone: true }),
  // الرابط الأصلي على gac.gov.sa (للمرجع — حتى لو لم يعمل من خارج السعودية)
  originalUrl: text("original_url"),
  // الملف بعد رفعه إلى Supabase Storage
  fileUrl: text("file_url").notNull(),
  // الحجم بالبايت + عدد الصفحات (helpful for UI badges)
  fileSizeBytes: integer("file_size_bytes"),
  pageCount: integer("page_count"),
  // كلمات مفتاحية للبحث
  tags: jsonb("tags").$type<string[]>().notNull().default([]),
  // مصدر التحميل: gacbep | acnbe | unescwa | direct | manual
  sourceDomain: text("source_domain").notNull().default("gacbep"),
  // حالة: published | draft | archived
  status: text("status").notNull().default("published"),
  // ترتيب العرض (أقل = أعلى)
  displayOrder: integer("display_order").notNull().default(100),
  createdAt: timestamp("created_at", { withTimezone: true }).notNull().defaultNow(),
  updatedAt: timestamp("updated_at", { withTimezone: true }).notNull().defaultNow(),
});

export const insertGacPublicationSchema = createInsertSchema(gacPublicationsTable).omit({
  id: true,
  createdAt: true,
  updatedAt: true,
});

export type GacPublication = typeof gacPublicationsTable.$inferSelect;
export type InsertGacPublication = typeof gacPublicationsTable.$inferInsert;

// ─── منشورات التواصل الاجتماعي (LinkedIn + Twitter/X) ──────────────────
export const gacSocialPostsTable = pgTable("gac_social_posts", {
  id: serial("id").primaryKey(),
  // المنصة: linkedin | twitter | instagram | youtube
  platform: text("platform").notNull(),
  // معرّف المنشور على المنصة الأصلية (لمنع التكرار)
  externalId: text("external_id").notNull(),
  // محتوى المنشور (نص فقط، بدون HTML)
  contentAr: text("content_ar"),
  contentEn: text("content_en"),
  // الرابط الأصلي للمنشور
  postUrl: text("post_url").notNull(),
  // الصورة المرفقة (إن وجدت)
  mediaUrl: text("media_url"),
  // نوع الوسائط: image | video | none
  mediaType: text("media_type").default("none"),
  // تاريخ النشر الأصلي
  postedAt: timestamp("posted_at", { withTimezone: true }),
  // إحصائيات (إعجابات/تعليقات/مشاركات) — اختياري
  metrics: jsonb("metrics").$type<{ likes?: number; comments?: number; shares?: number }>(),
  // الحساب الذي نشر: SaudiGAC | Saudigac_en | gac-linkedin
  account: text("account").notNull(),
  fetchedAt: timestamp("fetched_at", { withTimezone: true }).notNull().defaultNow(),
});

export const insertGacSocialPostSchema = createInsertSchema(gacSocialPostsTable).omit({
  id: true,
  fetchedAt: true,
});

export type GacSocialPost = typeof gacSocialPostsTable.$inferSelect;
export type InsertGacSocialPost = typeof gacSocialPostsTable.$inferInsert;

// ─── أخبار / قرارات الهيئة (manual seed أو scrape مستقبلي) ──────────────
export const gacNewsItemsTable = pgTable("gac_news_items", {
  id: serial("id").primaryKey(),
  // نوع البند: news | decision | event | press-release
  kind: text("kind").notNull().default("news"),
  titleAr: text("title_ar").notNull(),
  titleEn: text("title_en"),
  bodyAr: text("body_ar"),
  bodyEn: text("body_en"),
  // الفئة: merger-approval | merger-conditional | merger-block | enforcement | awareness
  category: text("category"),
  // الرابط الأصلي (gac.gov.sa أو SPA/news)
  sourceUrl: text("source_url"),
  // الصورة المرفقة
  imageUrl: text("image_url"),
  publishedAt: timestamp("published_at", { withTimezone: true }),
  // معرف خارجي إن أمكن (e.g. decision number)
  externalRef: text("external_ref"),
  tags: jsonb("tags").$type<string[]>().notNull().default([]),
  createdAt: timestamp("created_at", { withTimezone: true }).notNull().defaultNow(),
});

export const insertGacNewsItemSchema = createInsertSchema(gacNewsItemsTable).omit({
  id: true,
  createdAt: true,
});

export type GacNewsItem = typeof gacNewsItemsTable.$inferSelect;
export type InsertGacNewsItem = typeof gacNewsItemsTable.$inferInsert;
