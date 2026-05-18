import {
  pgTable, serial, text, integer, boolean, timestamp, jsonb,
} from "drizzle-orm/pg-core";
import { createInsertSchema } from "drizzle-zod";
import { z } from "zod/v4";

export const aiYearActivationsTable = pgTable("ai_year_activations", {
  id: serial("id").primaryKey(),
  title: text("title").notNull(),
  month: integer("month").notNull(),
  year: integer("year").notNull().default(2026),
  type: text("type").notNull(),
  channel: text("channel").notNull(),
  description: text("description"),
  tags: jsonb("tags").$type<string[]>().default([]),
  status: text("status").notNull().default("published"),
  reach: integer("reach"),
  engagement: integer("engagement"),
  notes: text("notes"),
  createdAt: timestamp("created_at", { withTimezone: true }).notNull().defaultNow(),
  updatedAt: timestamp("updated_at", { withTimezone: true }).notNull().defaultNow().$onUpdate(() => new Date()),
});

export const aiYearMediaTable = pgTable("ai_year_media", {
  id: serial("id").primaryKey(),
  activationId: integer("activation_id").notNull().references(() => aiYearActivationsTable.id, { onDelete: "cascade" }),
  objectPath: text("object_path").notNull(),
  fileName: text("file_name"),
  contentType: text("content_type"),
  sortOrder: integer("sort_order").notNull().default(0),
  createdAt: timestamp("created_at", { withTimezone: true }).notNull().defaultNow(),
});

export const aiYearMetricsTable = pgTable("ai_year_metrics", {
  id: serial("id").primaryKey(),
  activationId: integer("activation_id").notNull().references(() => aiYearActivationsTable.id, { onDelete: "cascade" }),
  metricKey: text("metric_key").notNull(),
  metricValue: text("metric_value"),
  createdAt: timestamp("created_at", { withTimezone: true }).notNull().defaultNow(),
});

export const insertAiYearActivationSchema = createInsertSchema(aiYearActivationsTable).omit({ id: true, createdAt: true, updatedAt: true });
export const insertAiYearMediaSchema = createInsertSchema(aiYearMediaTable).omit({ id: true, createdAt: true });
export const insertAiYearMetricsSchema = createInsertSchema(aiYearMetricsTable).omit({ id: true, createdAt: true });

export type AiYearActivation = typeof aiYearActivationsTable.$inferSelect;
export type AiYearMedia = typeof aiYearMediaTable.$inferSelect;
export type AiYearMetric = typeof aiYearMetricsTable.$inferSelect;
