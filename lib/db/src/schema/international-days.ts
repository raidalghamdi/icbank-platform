import {
  pgTable, serial, text, integer, boolean, timestamp, jsonb,
} from "drizzle-orm/pg-core";
import { createInsertSchema } from "drizzle-zod";
import { z } from "zod/v4";

export const internationalDaysTable = pgTable("international_days", {
  id: serial("id").primaryKey(),
  dayNameAr: text("day_name_ar").notNull(),
  dayNameEn: text("day_name_en"),
  annualDate: text("annual_date"),
  category: text("category"),
  officialOrganizer: text("official_organizer"),
  officialOrganizerSource: text("official_organizer_source"),
  historySummary: text("history_summary"),
  historySource: text("history_source"),
  suggestions: jsonb("suggestions").$type<string[]>(),
  lastSearchedAt: timestamp("last_searched_at", { withTimezone: true }),
  createdAt: timestamp("created_at", { withTimezone: true }).notNull().defaultNow(),
  updatedAt: timestamp("updated_at", { withTimezone: true }).notNull().defaultNow().$onUpdate(() => new Date()),
});

export const dayYearlyThemesTable = pgTable("day_yearly_themes", {
  id: serial("id").primaryKey(),
  dayId: integer("day_id").notNull().references(() => internationalDaysTable.id, { onDelete: "cascade" }),
  year: integer("year").notNull(),
  themeAr: text("theme_ar"),
  themeEn: text("theme_en"),
  themeSourceUrl: text("theme_source_url"),
});

export const dayActivationsTable = pgTable("day_activations", {
  id: serial("id").primaryKey(),
  dayId: integer("day_id").notNull().references(() => internationalDaysTable.id, { onDelete: "cascade" }),
  year: integer("year"),
  entityName: text("entity_name"),
  entityType: text("entity_type"),
  activationType: text("activation_type"),
  description: text("description"),
  mediaUrl: text("media_url"),
  sourceUrl: text("source_url"),
  country: text("country"),
  verified: boolean("verified").notNull().default(false),
});

export const intlDaySourcesTable = pgTable("intl_day_sources", {
  id: serial("id").primaryKey(),
  relatedTable: text("related_table").notNull(),
  relatedId: integer("related_id").notNull(),
  sourceUrl: text("source_url"),
  sourceTitle: text("source_title"),
  sourcePublisher: text("source_publisher"),
  accessedAt: timestamp("accessed_at", { withTimezone: true }).notNull().defaultNow(),
});

export const intlSearchHistoryTable = pgTable("intl_search_history", {
  id: serial("id").primaryKey(),
  query: text("query").notNull(),
  dayId: integer("day_id"),
  ipAddress: text("ip_address"),
  searchedAt: timestamp("searched_at", { withTimezone: true }).notNull().defaultNow(),
});

export const insertInternationalDaySchema = createInsertSchema(internationalDaysTable).omit({ id: true, createdAt: true, updatedAt: true });
export const insertDayThemeSchema = createInsertSchema(dayYearlyThemesTable).omit({ id: true });
export const insertDayActivationSchema = createInsertSchema(dayActivationsTable).omit({ id: true });

export type InternationalDay = typeof internationalDaysTable.$inferSelect;
export type DayYearlyTheme = typeof dayYearlyThemesTable.$inferSelect;
export type DayActivation = typeof dayActivationsTable.$inferSelect;
export type IntlDaySource = typeof intlDaySourcesTable.$inferSelect;
