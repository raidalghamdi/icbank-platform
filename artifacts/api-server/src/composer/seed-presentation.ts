/**
 * Presentation slide templates (Internal communications).
 *
 * Adapted from the design-studio reference (qlb-l-lnt-ldkhly-lnskh-1.0).
 * Canvas: 1920×1440 (4:3, 10×7.5in PPT slide).
 *
 * Two layouts:
 *   1) "شريحة عرض — فقرات" (paragraphs)
 *   2) "شريحة عرض — أيقونات 4" (2×2 icon grid)
 */
import type { TextSlot, LogoSlot, TemplateExtras, BackgroundPanelConfig } from "@workspace/db";

export type SeedPresentationTemplate = {
  templateNameAr: string;
  category: string;
  canvasWidth: number;
  canvasHeight: number;
  backgroundPanelConfig: BackgroundPanelConfig;
  textSlots: TextSlot[];
  logoSlots: LogoSlot[];
  thumbnailUrl: string | null;
  promptHint: string;
  extras: TemplateExtras;
};

const PRES_W = 1920;
const PRES_H = 1440;

const SHARED_GRADIENT_HEADER = {
  heightPct: 16,
  colorStart: "#004C50",
  colorEnd: "#1B5728",
  direction: "horizontal" as const,
};

const SHARED_DEPARTMENT_BADGE = {
  x: 6.5,
  y: 5.5,
  width: 22,
  height: 5.5,
  bgColor: "#1C5831",
  textColor: "#FFFFFF",
  fontSize: 30,
  borderRadius: 0,
  textAlign: "center" as const,
};

const SHARED_LOGO_SLOTS: LogoSlot[] = [
  {
    key: "logo_main",
    x: 96,
    y: 3,
    maxWidth: 420,
    maxHeight: 160,
    align: "right",
    tintColor: "#FFFFFF",
  },
];

const SHARED_IMAGE_PLACEHOLDER = {
  x: 0,
  y: 16,
  width: 39,
  height: 84,
  label: "مكان الصور",
  bgColor: "#FFFFFF",
  labelColor: "#0E5F8B",
  labelFontSize: 44,
  borderRadius: 0,
};

const SHARED_VERTICAL_SEPARATOR = {
  x: 39.8,
  y: 17,
  width: 0.2,
  height: 80,
  color: "#B5B5B5",
};

const PANEL_GREEN = "#A6C700";
const PANEL_OPACITY = 0.1;

/* ───── Paragraphs layout ───── */
const SEED_PRESENTATION_PARAGRAPHS: SeedPresentationTemplate = {
  templateNameAr: "شريحة عرض — فقرات",
  category: "عرض تقديمي داخلي",
  canvasWidth: PRES_W,
  canvasHeight: PRES_H,
  backgroundPanelConfig: {
    x: 0, y: 0, width: 0, height: 0, color: "#FFFFFF", opacity: 0, borderRadius: 0,
  },
  textSlots: [
    {
      key: "title",
      label_ar: "العنوان الرئيسي",
      role: "title",
      x: 42, y: 20, width: 55, height: 8,
      default_font_size: 60,
      minFontSize: 24, maxFontSize: 96,
      color: "#0E5F8B",
      fontWeight: 800,
      alignment: "right",
      max_words: 12,
      lineHeight: 1.2,
    },
    {
      key: "body",
      label_ar: "النص التفصيلي",
      role: "body",
      x: 43, y: 36, width: 52, height: 50,
      default_font_size: 30,
      minFontSize: 16, maxFontSize: 48,
      color: "#333333",
      fontWeight: 400,
      alignment: "right",
      max_words: 200,
      lineHeight: 1.7,
    },
  ],
  logoSlots: SHARED_LOGO_SLOTS,
  thumbnailUrl: null,
  promptHint: "شريحة عرض داخلي — صورة على اليسار + عنوان وفقرة في العمود الأيمن.",
  extras: {
    layoutKind: "presentation-paragraphs",
    gradientHeader: SHARED_GRADIENT_HEADER,
    departmentBadge: SHARED_DEPARTMENT_BADGE,
    imagePlaceholder: SHARED_IMAGE_PLACEHOLDER,
    verticalSeparator: SHARED_VERTICAL_SEPARATOR,
    contentPanel: {
      x: 42, y: 30, width: 53, height: 60,
      color: PANEL_GREEN,
      opacity: PANEL_OPACITY,
      borderRadius: 4,
    },
    subHeading: {
      x: 43, y: 30, width: 53, height: 5,
      color: "#A6C700",
      fontSize: 32,
      fontWeight: 700,
      textAlign: "right",
      text: "",
    },
  },
};

/* ───── 2×2 Icons layout ───── */
const SEED_PRESENTATION_ICONS_2X2: SeedPresentationTemplate = {
  templateNameAr: "شريحة عرض — أيقونات 4",
  category: "عرض تقديمي داخلي",
  canvasWidth: PRES_W,
  canvasHeight: PRES_H,
  backgroundPanelConfig: {
    x: 0, y: 0, width: 0, height: 0, color: "#FFFFFF", opacity: 0, borderRadius: 0,
  },
  textSlots: [
    {
      key: "title",
      label_ar: "العنوان الرئيسي",
      role: "title",
      x: 42, y: 19, width: 55, height: 8,
      default_font_size: 56,
      minFontSize: 24, maxFontSize: 88,
      color: "#0E5F8B",
      fontWeight: 800,
      alignment: "right",
      max_words: 10,
      lineHeight: 1.2,
    },
    {
      key: "body",
      label_ar: "النص التفصيلي",
      role: "body",
      x: 42, y: 28, width: 55, height: 4,
      default_font_size: 24,
      minFontSize: 14, maxFontSize: 40,
      color: "#555555",
      fontWeight: 400,
      alignment: "right",
      max_words: 30,
      lineHeight: 1.4,
    },
  ],
  logoSlots: SHARED_LOGO_SLOTS,
  thumbnailUrl: null,
  promptHint: "شريحة عرض داخلي بشبكة 4 أيقونات (2×2) في العمود الأيمن مع صورة على اليسار.",
  extras: {
    layoutKind: "presentation-icons-2x2",
    gradientHeader: SHARED_GRADIENT_HEADER,
    departmentBadge: SHARED_DEPARTMENT_BADGE,
    imagePlaceholder: SHARED_IMAGE_PLACEHOLDER,
    verticalSeparator: SHARED_VERTICAL_SEPARATOR,
    contentPanel: {
      x: 42, y: 34, width: 53, height: 56,
      color: PANEL_GREEN,
      opacity: PANEL_OPACITY,
      borderRadius: 4,
    },
    iconSlots: [
      { x: 56, y: 41, size: 110, lucideName: "laptop", color: "#0E5F8B", strokeWidth: 1.6,
        titleText: "النقطة الأولى", titleColor: "#0E5F8B", titleFontSize: 26,
        bodyText: "وصف موجز للنقطة الأولى.", bodyColor: "#333333", bodyFontSize: 18,
        textWidth: 20, textAlign: "center" },
      { x: 79, y: 41, size: 110, lucideName: "alert-triangle", color: "#0E5F8B", strokeWidth: 1.6,
        titleText: "النقطة الثانية", titleColor: "#0E5F8B", titleFontSize: 26,
        bodyText: "وصف موجز للنقطة الثانية.", bodyColor: "#333333", bodyFontSize: 18,
        textWidth: 20, textAlign: "center" },
      { x: 56, y: 66, size: 110, lucideName: "check-square", color: "#0E5F8B", strokeWidth: 1.6,
        titleText: "النقطة الثالثة", titleColor: "#0E5F8B", titleFontSize: 26,
        bodyText: "وصف موجز للنقطة الثالثة.", bodyColor: "#333333", bodyFontSize: 18,
        textWidth: 20, textAlign: "center" },
      { x: 79, y: 66, size: 110, lucideName: "refresh-cw", color: "#0E5F8B", strokeWidth: 1.6,
        titleText: "النقطة الرابعة", titleColor: "#0E5F8B", titleFontSize: 26,
        bodyText: "وصف موجز للنقطة الرابعة.", bodyColor: "#333333", bodyFontSize: 18,
        textWidth: 20, textAlign: "center" },
    ],
  },
};

export const SEED_PRESENTATION_TEMPLATES: SeedPresentationTemplate[] = [
  SEED_PRESENTATION_PARAGRAPHS,
  SEED_PRESENTATION_ICONS_2X2,
];
