import {
  pgTable,
  serial,
  text,
  integer,
  boolean,
  timestamp,
  jsonb,
} from "drizzle-orm/pg-core";
import { createInsertSchema } from "drizzle-zod";

// ─── Background panel config shape ───────────────────────────────
export type BackgroundPanelConfig = {
  x: number;
  y: number;
  width: number;
  height: number;
  color: string;
  opacity: number;
};

// ─── Text slot shape ──────────────────────────────────────────────
export type TextSlot = {
  key: string;
  label_ar: string;
  x: number;
  y: number;
  width: number;
  height: number;
  default_font_size: number;
  max_words: number;
  alignment: "right" | "center" | "left";
  color: string;
};

// ─── Logo slot shape ──────────────────────────────────────────────
export type LogoSlot = {
  key: string;
  x: number;
  y: number;
  width: number;
  height: number;
};

// ─── Tables ───────────────────────────────────────────────────────

export const designTemplatesTable = pgTable("design_templates", {
  id: serial("id").primaryKey(),
  templateNameAr: text("template_name_ar").notNull(),
  category: text("category").notNull(),
  canvasWidth: integer("canvas_width").notNull().default(1920),
  canvasHeight: integer("canvas_height").notNull().default(1080),
  backgroundPanelConfig: jsonb("background_panel_config").$type<BackgroundPanelConfig>(),
  textSlots: jsonb("text_slots").$type<TextSlot[]>().notNull().default([]),
  logoSlots: jsonb("logo_slots").$type<LogoSlot[]>().notNull().default([]),
  thumbnailUrl: text("thumbnail_url"),
  createdAt: timestamp("created_at", { withTimezone: true }).notNull().defaultNow(),
});

export const brandLogosTable = pgTable("brand_logos", {
  id: serial("id").primaryKey(),
  logoName: text("logo_name").notNull(),
  fileUrl: text("file_url").notNull(),
  transparent: boolean("transparent").notNull().default(false),
  defaultWidth: integer("default_width"),
  uploadedAt: timestamp("uploaded_at", { withTimezone: true }).notNull().defaultNow(),
});

export const brandFontsTable = pgTable("brand_fonts", {
  id: serial("id").primaryKey(),
  fontName: text("font_name").notNull(),
  fontFileUrl: text("font_file_url").notNull(),
  isDefault: boolean("is_default").notNull().default(false),
  uploadedAt: timestamp("uploaded_at", { withTimezone: true }).notNull().defaultNow(),
});

export const generatedDesignsTable = pgTable("generated_designs", {
  id: serial("id").primaryKey(),
  templateId: integer("template_id").references(() => designTemplatesTable.id, { onDelete: "set null" }),
  titleText: text("title_text"),
  bodyText: text("body_text"),
  backgroundImageUrl: text("background_image_url"),
  selectedLogos: jsonb("selected_logos").$type<number[]>().notNull().default([]),
  finalImageUrl: text("final_image_url"),
  createdBy: integer("created_by"),
  createdAt: timestamp("created_at", { withTimezone: true }).notNull().defaultNow(),
});

// ─── Insert schemas ───────────────────────────────────────────────
export const insertDesignTemplateSchema = createInsertSchema(designTemplatesTable).omit({ id: true, createdAt: true });
export const insertBrandLogoSchema = createInsertSchema(brandLogosTable).omit({ id: true, uploadedAt: true });
export const insertBrandFontSchema = createInsertSchema(brandFontsTable).omit({ id: true, uploadedAt: true });
export const insertGeneratedDesignSchema = createInsertSchema(generatedDesignsTable).omit({ id: true, createdAt: true });

// ─── Types ────────────────────────────────────────────────────────
export type DesignTemplate = typeof designTemplatesTable.$inferSelect;
export type BrandLogo = typeof brandLogosTable.$inferSelect;
export type BrandFont = typeof brandFontsTable.$inferSelect;
export type GeneratedDesign = typeof generatedDesignsTable.$inferSelect;
