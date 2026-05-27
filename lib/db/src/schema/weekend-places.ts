import {
  pgTable,
  serial,
  text,
  boolean,
  integer,
  jsonb,
  timestamp,
} from "drizzle-orm/pg-core";
import { createInsertSchema } from "drizzle-zod";

export const weekendPlacesTable = pgTable("weekend_places", {
  id: serial("id").primaryKey(),
  name: text("name").notNull(),
  description: text("description").notNull(),
  imageUrl: text("image_url"),
  city: text("city").notNull().default("الرياض"),
  mapsQuery: text("maps_query"),
  isActive: boolean("is_active").notNull().default(true),
  sortOrder: integer("sort_order").notNull().default(0),
  createdAt: timestamp("created_at", { withTimezone: true }).notNull().defaultNow(),
});

export const insertWeekendPlaceSchema = createInsertSchema(
  weekendPlacesTable
).omit({ id: true, createdAt: true });

export type WeekendPlace = typeof weekendPlacesTable.$inferSelect;

// ─── Weekend AI-generated drafts (Riyadh-only) ──────────────────────────────
// Status workflow: pending_review → approved → published  (or rejected)
export const weekendDraftsTable = pgTable("weekend_drafts", {
  id: serial("id").primaryKey(),
  weekendDate: text("weekend_date").notNull(), // ISO date string of Thursday
  city: text("city").notNull().default("الرياض"),
  status: text("status").notNull().default("pending_review"),
  modelName: text("model_name").notNull().default("gemini-2.0-flash-exp"),
  // Sections: places[], deals[], podcasts[], aiTools[], matches[], movies[]
  content: jsonb("content").$type<Record<string, any>>().notNull(),
  generatedBy: integer("generated_by"),
  approvedBy: integer("approved_by"),
  rejectedReason: text("rejected_reason"),
  createdAt: timestamp("created_at", { withTimezone: true }).notNull().defaultNow(),
  approvedAt: timestamp("approved_at", { withTimezone: true }),
  publishedAt: timestamp("published_at", { withTimezone: true }),
});

export const insertWeekendDraftSchema = createInsertSchema(
  weekendDraftsTable
).omit({ id: true, createdAt: true, approvedAt: true, publishedAt: true });

export type WeekendDraft = typeof weekendDraftsTable.$inferSelect;
