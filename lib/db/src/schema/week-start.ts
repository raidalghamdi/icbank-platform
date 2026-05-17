import { pgTable, serial, text, real, boolean, timestamp, jsonb } from "drizzle-orm/pg-core";
import { createInsertSchema } from "drizzle-zod";
import { z } from "zod/v4";

export const archiveEntriesTable = pgTable("archive_entries", {
  id: serial("id").primaryKey(),
  title: text("title").notNull(),
  bodyText: text("body_text").notNull(),
  date: timestamp("date", { withTimezone: true }),
  occasion: text("occasion"),
  tone: text("tone"),
  sourceFile: text("source_file"),
  embedding: jsonb("embedding").$type<number[]>(),
  createdAt: timestamp("created_at", { withTimezone: true }).notNull().defaultNow(),
});

export const styleProfileTable = pgTable("style_profile", {
  id: serial("id").primaryKey(),
  toneSummary: text("tone_summary"),
  avgParagraphLength: real("avg_paragraph_length"),
  openerPatterns: jsonb("opener_patterns").$type<string[]>(),
  closerPatterns: jsonb("closer_patterns").$type<string[]>(),
  recurringKeywords: jsonb("recurring_keywords").$type<string[]>(),
  quoteUsage: text("quote_usage"),
  updatedAt: timestamp("updated_at", { withTimezone: true }).notNull().defaultNow(),
});

export const generatedOutputsTable = pgTable("generated_outputs", {
  id: serial("id").primaryKey(),
  topic: text("topic").notNull(),
  modelName: text("model_name").notNull(),
  outputText: text("output_text").notNull().default(""),
  archiveRefs: jsonb("archive_refs").$type<number[]>().default([]),
  selected: boolean("selected").notNull().default(false),
  createdAt: timestamp("created_at", { withTimezone: true }).notNull().defaultNow(),
});

export const insertArchiveEntrySchema = createInsertSchema(archiveEntriesTable).omit({ id: true, createdAt: true });
export const insertGeneratedOutputSchema = createInsertSchema(generatedOutputsTable).omit({ id: true, createdAt: true });

export type ArchiveEntry = typeof archiveEntriesTable.$inferSelect;
export type StyleProfile = typeof styleProfileTable.$inferSelect;
export type GeneratedOutput = typeof generatedOutputsTable.$inferSelect;
