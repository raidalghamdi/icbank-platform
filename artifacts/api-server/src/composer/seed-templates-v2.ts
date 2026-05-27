/**
 * قوالب GAC v2 — مطابقة 100% لدليل العلامة التجارية للهيئة العامة للمنافسة
 *
 * المرجع: GAC-Brand-Manual.pdf
 *  - ص 11-15: الألوان الرسمية (Primary/Secondary/Neutral)
 *  - ص 16-20: الخطوط Frutiger LT Arabic (Bold 65 / Roman 55 / Light 45)
 *  - ص 99: 7.22 Facebook cover (1200×675) — Bold/Light 48pt
 *  - ص 100: 7.23 Social Media post (1080×1080) — Bold/Light 60pt
 *  - ص 101: 7.24 Twitter header (1500×500) — Bold/Light 48pt
 *  - ص 103: 7.26 Twitter image post (1024×512) — Bold/Light 48pt
 *  - ص 22-23: Clear Space (½ logoWidth) + Minimum Size (30mm = 113px)
 *
 * أبعاد ومواضع الشعار + أحجام الخطوط حرفياً من الدليل.
 */

import type { TextSlot, LogoSlot, BackgroundPanelConfig } from "@workspace/db";
import { GAC } from "./gac-palette";

// Local seed type (mirrors icbank's insertDesignTemplateSchema shape; matches
// the pattern used by seed-presentation.ts). `extras` is intentionally omitted
// — v2 social templates don't use department badges / image placeholders.
export type SeedTemplateV2 = {
  templateNameAr: string;
  category: string;
  canvasWidth: number;
  canvasHeight: number;
  backgroundPanelConfig: BackgroundPanelConfig;
  textSlots: TextSlot[];
  logoSlots: LogoSlot[];
  thumbnailUrl: string | null;
  promptHint: string;
};
type InsertDesignTemplate = SeedTemplateV2;

// ⚠️ ألوان GAC صلبة 100% — لا شفافية
const C_BLUE = GAC.primary.blue;        // #0069A7
const C_DARK_BLUE = GAC.primary.darkBlue; // #00567D
const C_GREEN = GAC.primary.green;      // #61A60E
const C_NAVY = GAC.secondary.navy;      // #194F90
const C_LIGHT_GRN = GAC.secondary.lightGreen; // #9DC41A

// ──────────────────────────────────────────────────────────
// 1) SQUARE POST (1080×1080) — مرجع 7.23 ص 100
// ──────────────────────────────────────────────────────────
const SQUARE_TEMPLATES: InsertDesignTemplate[] = [
  {
    templateNameAr: "منشور مربع — عرض / خصم",
    category: "Square — عرض / خصم",
    canvasWidth: 1080,
    canvasHeight: 1080,
    promptHint:
      "تصميم تسويقي احترافي بألوان متناغمة مع باليت الهيئة (أزرق/أخضر/رمادي)، اترك مساحة في الأعلى متجانسة لوضع النص الرئيسي، تجنّب أي شعارات أو نصوص في الصورة.",
    backgroundPanelConfig: {
      x: 0, y: 0, width: 100, height: 50,
      color: C_BLUE, opacity: 1.0, borderRadius: 0,
    },
    textSlots: [
      {
        key: "title", label_ar: "العنوان الرئيسي", role: "title",
        x: 8, y: 10, width: 84, height: 18,
        default_font_size: 80, minFontSize: 40, maxFontSize: 100,
        max_words: 8, alignment: "right",
        color: GAC.neutral.white, fontWeight: 700, lineHeight: 1.15,
      },
      {
        key: "body", label_ar: "العنوان الفرعي", role: "body",
        x: 8, y: 30, width: 84, height: 14,
        default_font_size: 80, minFontSize: 30, maxFontSize: 90,
        max_words: 12, alignment: "right",
        color: GAC.neutral.white, fontWeight: 300, lineHeight: 1.25,
      },
    ],
    logoSlots: [
      // الشعار 21% من W = 226px، أسفل-يمين (clear space 30%X يمين، 28%X سفل)
      { key: "logo_main", x: 96, y: 78, maxWidth: 226, maxHeight: 95, align: "right" },
    ],
    thumbnailUrl: null,
  },
  {
    templateNameAr: "منشور مربع — إعلان ورشة",
    category: "Square — ورشة",
    canvasWidth: 1080,
    canvasHeight: 1080,
    promptHint:
      "صورة بيئة عمل أو تدريب احترافية بأسلوب modern minimal، اترك النصف العلوي هادئاً ومتجانساً، إضاءة طبيعية محايدة، بدون نصوص أو شعارات.",
    backgroundPanelConfig: {
      x: 0, y: 0, width: 100, height: 50,
      color: C_GREEN, opacity: 1.0, borderRadius: 0,
    },
    textSlots: [
      {
        key: "title", label_ar: "العنوان الرئيسي", role: "title",
        x: 8, y: 10, width: 84, height: 18,
        default_font_size: 80, minFontSize: 40, maxFontSize: 100,
        max_words: 8, alignment: "right",
        color: GAC.neutral.white, fontWeight: 700, lineHeight: 1.15,
      },
      {
        key: "body", label_ar: "العنوان الفرعي", role: "body",
        x: 8, y: 30, width: 84, height: 14,
        default_font_size: 80, minFontSize: 30, maxFontSize: 90,
        max_words: 12, alignment: "right",
        color: GAC.neutral.white, fontWeight: 300, lineHeight: 1.25,
      },
    ],
    logoSlots: [
      { key: "logo_main", x: 96, y: 78, maxWidth: 226, maxHeight: 95, align: "right" },
    ],
    thumbnailUrl: null,
  },
  {
    templateNameAr: "منشور مربع — تهنئة",
    category: "Square — تهنئة",
    canvasWidth: 1080,
    canvasHeight: 1080,
    promptHint:
      "خلفية احتفالية راقية متجانسة بألوان طبيعية دافئة هادئة، فراغ علوي بسيط لكتابة عبارة التهنئة، بدون وجوه أو شعارات أو نصوص.",
    backgroundPanelConfig: {
      x: 0, y: 0, width: 100, height: 50,
      color: C_LIGHT_GRN, opacity: 1.0, borderRadius: 0,
    },
    textSlots: [
      {
        key: "title", label_ar: "العنوان الرئيسي", role: "title",
        x: 8, y: 10, width: 84, height: 18,
        default_font_size: 80, minFontSize: 40, maxFontSize: 100,
        max_words: 8, alignment: "center",
        color: GAC.neutral.black, fontWeight: 700, lineHeight: 1.15,
      },
      {
        key: "body", label_ar: "العنوان الفرعي", role: "body",
        x: 8, y: 30, width: 84, height: 14,
        default_font_size: 80, minFontSize: 30, maxFontSize: 90,
        max_words: 12, alignment: "center",
        color: GAC.neutral.black, fontWeight: 300, lineHeight: 1.25,
      },
    ],
    logoSlots: [
      { key: "logo_main", x: 96, y: 78, maxWidth: 226, maxHeight: 95, align: "right" },
    ],
    thumbnailUrl: null,
  },
];

// ──────────────────────────────────────────────────────────
// 2) FACEBOOK COVER (1200×675) — مرجع 7.22 ص 99
// ──────────────────────────────────────────────────────────
const FB_COVER_TEMPLATES: InsertDesignTemplate[] = [
  {
    templateNameAr: "غلاف فيسبوك — رسمي",
    category: "FB Cover",
    canvasWidth: 1200,
    canvasHeight: 675,
    promptHint:
      "صورة احترافية مؤسسية بألوان متناغمة مع باليت الهيئة، اترك النصف الأيمن هادئاً ومتجانساً، تجنّب أي نصوص أو شعارات في الصورة.",
    backgroundPanelConfig: {
      x: 50, y: 0, width: 50, height: 100,
      color: C_BLUE, opacity: 1.0, borderRadius: 0,
    },
    textSlots: [
      {
        key: "title", label_ar: "العنوان", role: "title",
        x: 52, y: 22, width: 44, height: 16,
        default_font_size: 64, minFontSize: 32, maxFontSize: 80,
        max_words: 6, alignment: "right",
        color: GAC.neutral.white, fontWeight: 700, lineHeight: 1.15,
      },
      {
        key: "body", label_ar: "العنوان الفرعي", role: "body",
        x: 52, y: 40, width: 44, height: 14,
        default_font_size: 64, minFontSize: 28, maxFontSize: 72,
        max_words: 10, alignment: "right",
        color: GAC.neutral.white, fontWeight: 300, lineHeight: 1.25,
      },
    ],
    logoSlots: [
      // الشعار 37% من W = 444px، يمين-وسط (دليل ص 99)
      { key: "logo_main", x: 96, y: 68, maxWidth: 444, maxHeight: 180, align: "right" },
    ],
    thumbnailUrl: null,
  },
];

// ──────────────────────────────────────────────────────────
// 3) TWITTER HEADER (1500×500) — مرجع 7.24 ص 101
// ──────────────────────────────────────────────────────────
const TWITTER_HEADER_TEMPLATES: InsertDesignTemplate[] = [
  {
    templateNameAr: "غلاف تويتر — رسمي",
    category: "Twitter Header",
    canvasWidth: 1500,
    canvasHeight: 500,
    promptHint:
      "صورة بانورامية احترافية مع تركيبة بصرية متوازنة، اترك الجوانب هادئة بصرياً، تجنّب أي نصوص أو شعارات في الصورة.",
    backgroundPanelConfig: {
      x: 50, y: 0, width: 50, height: 100,
      color: C_DARK_BLUE, opacity: 1.0, borderRadius: 0,
    },
    textSlots: [
      {
        key: "title", label_ar: "العنوان", role: "title",
        x: 52, y: 28, width: 44, height: 22,
        default_font_size: 64, minFontSize: 32, maxFontSize: 80,
        max_words: 5, alignment: "right",
        color: GAC.neutral.white, fontWeight: 700, lineHeight: 1.15,
      },
      {
        key: "body", label_ar: "العنوان الفرعي", role: "body",
        x: 52, y: 54, width: 44, height: 18,
        default_font_size: 64, minFontSize: 28, maxFontSize: 72,
        max_words: 8, alignment: "right",
        color: GAC.neutral.white, fontWeight: 300, lineHeight: 1.25,
      },
    ],
    logoSlots: [
      // الشعار 19%W من 1500 = 285px، يسار-وسط (دليل ص 101)
      { key: "logo_main", x: 4, y: 38, maxWidth: 285, maxHeight: 120, align: "left" },
    ],
    thumbnailUrl: null,
  },
];

// ──────────────────────────────────────────────────────────
// 4) TWITTER IMAGE POST (1024×512) — مرجع 7.26 ص 103
// ──────────────────────────────────────────────────────────
const TWITTER_IMAGE_TEMPLATES: InsertDesignTemplate[] = [
  {
    templateNameAr: "صورة تويتر — رسمية",
    category: "Twitter Image",
    canvasWidth: 1024,
    canvasHeight: 512,
    promptHint:
      "صورة احترافية بنسبة 2:1 بألوان متجانسة مع باليت الهيئة، اترك الجانب الأيسر متجانساً لوضع النص، تجنّب أي نصوص أو شعارات في الصورة.",
    backgroundPanelConfig: {
      x: 0, y: 0, width: 100, height: 50,
      color: C_NAVY, opacity: 1.0, borderRadius: 0,
    },
    textSlots: [
      {
        key: "title", label_ar: "العنوان", role: "title",
        x: 5, y: 12, width: 90, height: 16,
        default_font_size: 64, minFontSize: 30, maxFontSize: 80,
        max_words: 7, alignment: "right",
        color: GAC.neutral.white, fontWeight: 700, lineHeight: 1.15,
      },
      {
        key: "body", label_ar: "العنوان الفرعي", role: "body",
        x: 5, y: 30, width: 90, height: 14,
        default_font_size: 64, minFontSize: 26, maxFontSize: 72,
        max_words: 10, alignment: "right",
        color: GAC.neutral.white, fontWeight: 300, lineHeight: 1.25,
      },
    ],
    logoSlots: [
      // 21%W من 1024 = 215px، أسفل-يمين (دليل ص 103)
      { key: "logo_main", x: 96, y: 82, maxWidth: 215, maxHeight: 90, align: "right" },
    ],
    thumbnailUrl: null,
  },
];

// ──────────────────────────────────────────────────────────
// التصدير
// ──────────────────────────────────────────────────────────
export const SEED_TEMPLATES_V2: InsertDesignTemplate[] = [
  ...SQUARE_TEMPLATES,         // 3 قوالب
  ...FB_COVER_TEMPLATES,       // 1 قالب
  ...TWITTER_HEADER_TEMPLATES, // 1 قالب
  ...TWITTER_IMAGE_TEMPLATES,  // 1 قالب
];

// إجمالي: 6 قوالب — جميعها مطابقة 100% لأبعاد دليل GAC
