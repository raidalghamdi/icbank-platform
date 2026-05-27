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
import { z } from "zod";

// ─── Background panel config shape ───────────────────────────────
export type BackgroundPanelConfig = {
  x: number;
  y: number;
  width: number;
  height: number;
  color: string;
  opacity: number;
  borderRadius?: number;
};

// ─── Text slot shape ──────────────────────────────────────────────
export type TextSlot = {
  key: string;
  label_ar: string;
  role?: "title" | "body";
  x: number;
  y: number;
  width: number;
  height: number;
  default_font_size: number;
  max_words: number;
  alignment: "right" | "center" | "left";
  color: string;
  // — Optional advanced fields from design-studio composer —
  minFontSize?: number;
  maxFontSize?: number;
  fontWeight?: string | number;
  lineHeight?: number;
};

// ─── Logo slot shape ──────────────────────────────────────────────
// `width`/`height` are kept for backwards compatibility with the legacy
// `seed-test` template. Advanced templates (presentation/v2) use
// `maxWidth`/`maxHeight`/`align`/`tintColor`.
export type LogoSlot = {
  key: string;
  x: number;
  y: number;
  width?: number;
  height?: number;
  maxWidth?: number;
  maxHeight?: number;
  align?: "left" | "center" | "right";
  // When set, opaque pixels are re-tinted to this color (e.g. "#FFFFFF" for a
  // white logo over a dark header).
  tintColor?: string;
};

/* ============================================================
 *  Extras — قوالب العروض التقديمية الداخلية
 *  Header gradient + Image placeholder + Icon grid + Department badge
 * ============================================================ */

export type GradientHeader = {
  heightPct: number;
  colorStart: string;
  colorEnd: string;
  direction?: "horizontal" | "vertical" | "diagonal";
};

export type DepartmentBadge = {
  x: number;
  y: number;
  width: number;
  height: number;
  bgColor: string;
  textColor: string;
  fontSize: number;
  borderRadius?: number;
  textAlign?: "right" | "center" | "left";
};

export type ImagePlaceholder = {
  x: number;
  y: number;
  width: number;
  height: number;
  label?: string;
  bgColor?: string;
  labelColor?: string;
  labelFontSize?: number;
  borderRadius?: number;
};

export type IconSlot = {
  x: number;
  y: number;
  size: number;
  lucideName: string;
  color?: string;
  strokeWidth?: number;
  titleText?: string;
  titleColor?: string;
  titleFontSize?: number;
  bodyText?: string;
  bodyColor?: string;
  bodyFontSize?: number;
  textWidth?: number;
  textAlign?: "right" | "center" | "left";
};

export type VerticalSeparator = {
  x: number;
  y: number;
  width: number;
  height: number;
  color?: string;
};

export type ContentPanel = {
  x: number;
  y: number;
  width: number;
  height: number;
  color: string;
  opacity?: number;
  borderRadius?: number;
};

export type SubHeading = {
  x: number;
  y: number;
  width: number;
  height: number;
  color?: string;
  fontSize?: number;
  fontWeight?: string | number;
  textAlign?: "right" | "center" | "left";
  text?: string;
};

export type TemplateExtras = {
  layoutKind: "social" | "presentation-paragraphs" | "presentation-icons-2x2";
  gradientHeader?: GradientHeader;
  departmentBadge?: DepartmentBadge;
  imagePlaceholder?: ImagePlaceholder;
  verticalSeparator?: VerticalSeparator;
  contentPanel?: ContentPanel;
  iconSlots?: IconSlot[];
  subHeading?: SubHeading;
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
  // Optional prompt hint that gets appended to the AI background prompt
  promptHint: text("prompt_hint"),
  // Extended configuration for presentation / v2 social templates
  extras: jsonb("extras").$type<TemplateExtras>(),
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
  department: text("department"),
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

/* ============================================================
 *  الإدارات التنفيذية الافتراضية (للقائمة المنسدلة)
 * ============================================================ */
export const EXECUTIVE_DEPARTMENTS = [
  "المحافظ",
  "نائب المحافظ",
  "الشؤون القانونية",
  "مكافحة الممارسات الاحتكارية",
  "الشؤون الاقتصادية والدراسات",
  "الشؤون الإدارية والمالية",
  "الاتصال المؤسسي",
  "التحول الرقمي",
] as const;
export type ExecutiveDepartment = (typeof EXECUTIVE_DEPARTMENTS)[number];
