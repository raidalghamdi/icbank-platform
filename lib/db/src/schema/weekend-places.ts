import {
  pgTable,
  serial,
  text,
  boolean,
  integer,
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
