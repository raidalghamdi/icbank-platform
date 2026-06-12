/**
 * قوالب 2026 — هندسة Hybrid (HTML/CSS تتحكم بالبنية والامتثال للهوية،
 * الذكاء الاصطناعي يولّد العناصر المتغيّرة فقط: العنوان والمحتوى).
 *
 * 1) Announcement 16:9  — إعلان مؤسسي رسمي (1920×1080)
 * 2) Workshop      4:5  — إعلان ورشة/فعالية (1080×1350)
 * 3) Social Modern 1:1  — منشور سوشيال حديث (1080×1080)
 *
 * كل قالب: header بـ gradient الهوية، panel نصي بمساحة كافية، logo slot
 * أعلى-يمين بنسبة 12-21% حسب الكانفس (مطابق لـ GAC Brand Manual).
 */

import type { TextSlot, LogoSlot, BackgroundPanelConfig } from "@workspace/db";
import { GAC } from "./gac-palette";

export type SeedTemplate2026 = {
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

const C_BLUE = GAC.primary.blue;
const C_GREEN = GAC.primary.green;
const C_DARK_BLUE = GAC.primary.darkBlue;

// ──────────────────────────────────────────────────────────
// 1) ANNOUNCEMENT 16:9 (1920×1080) — إعلان مؤسسي رسمي
// ──────────────────────────────────────────────────────────
const ANNOUNCEMENT_TEMPLATE: SeedTemplate2026 = {
  templateNameAr: "إعلان مؤسسي 2026 — رسمي",
  category: "Announcement 16:9 — رسمي",
  canvasWidth: 1920,
  canvasHeight: 1080,
  promptHint:
    "تصميم مؤسسي رسمي وراقي، خلفية بألوان GAC المعتمدة (أزرق #0069A7 وأخضر #61A60E)، نصف الكانفس الأيسر مساحة فارغة هادئة لوضع صورة احترافية، ولا أي نصوص أو شعارات على الصورة.",
  backgroundPanelConfig: {
    // panel جانبي يمين 45% بعرض ثابت — gradient أزرق→أخضر داكن
    x: 55, y: 0, width: 45, height: 100,
    color: C_DARK_BLUE, opacity: 1.0, borderRadius: 0,
  },
  textSlots: [
    {
      key: "title", label_ar: "العنوان الرئيسي", role: "title",
      x: 58, y: 26, width: 38, height: 22,
      default_font_size: 80, minFontSize: 40, maxFontSize: 96,
      max_words: 8, alignment: "right",
      color: GAC.neutral.white, fontWeight: 700, lineHeight: 1.18,
    },
    {
      key: "body", label_ar: "النص التفصيلي", role: "body",
      x: 58, y: 50, width: 38, height: 36,
      default_font_size: 32, minFontSize: 22, maxFontSize: 44,
      max_words: 40, alignment: "right",
      color: GAC.neutral.white, fontWeight: 400, lineHeight: 1.45,
    },
  ],
  logoSlots: [
    // الشعار 12% من العرض = 230px أعلى-يمين (طبقة 4)
    { key: "logo_main", x: 96, y: 6, maxWidth: 230, maxHeight: 100, align: "right" },
  ],
  thumbnailUrl: null,
};

// ──────────────────────────────────────────────────────────
// 2) WORKSHOP 4:5 (1080×1350) — إعلان ورشة/فعالية (Instagram-friendly)
// ──────────────────────────────────────────────────────────
const WORKSHOP_TEMPLATE: SeedTemplate2026 = {
  templateNameAr: "إعلان ورشة 2026 — Instagram 4:5",
  category: "Workshop 4:5 — Instagram",
  canvasWidth: 1080,
  canvasHeight: 1350,
  promptHint:
    "صورة بيئة عمل تدريبية احترافية بإضاءة طبيعية ناعمة، تركيبة modern minimal، النصف السفلي يكون متجانساً ليُغطّى ببانل أخضر GAC، بدون أي نصوص أو شعارات.",
  backgroundPanelConfig: {
    // panel سفلي 55% — أخضر GAC
    x: 0, y: 45, width: 100, height: 55,
    color: C_GREEN, opacity: 1.0, borderRadius: 0,
  },
  textSlots: [
    {
      key: "title", label_ar: "اسم الورشة", role: "title",
      x: 8, y: 52, width: 84, height: 18,
      default_font_size: 84, minFontSize: 44, maxFontSize: 100,
      max_words: 7, alignment: "right",
      color: GAC.neutral.white, fontWeight: 700, lineHeight: 1.15,
    },
    {
      key: "body", label_ar: "التفاصيل (التاريخ/المكان/المدرب)", role: "body",
      x: 8, y: 74, width: 84, height: 18,
      default_font_size: 42, minFontSize: 26, maxFontSize: 52,
      max_words: 25, alignment: "right",
      color: GAC.neutral.white, fontWeight: 400, lineHeight: 1.35,
    },
  ],
  logoSlots: [
    // الشعار 18% = 195px أعلى-يمين (مساحة أمان أعلى الصورة)
    { key: "logo_main", x: 96, y: 4, maxWidth: 195, maxHeight: 85, align: "right" },
  ],
  thumbnailUrl: null,
};

// ──────────────────────────────────────────────────────────
// 3) SOCIAL MODERN 1:1 (1080×1080) — منشور سوشيال حديث
// ──────────────────────────────────────────────────────────
const SOCIAL_MODERN_TEMPLATE: SeedTemplate2026 = {
  templateNameAr: "منشور سوشيال 2026 — حديث",
  category: "Social 1:1 — حديث",
  canvasWidth: 1080,
  canvasHeight: 1080,
  promptHint:
    "تصميم بصري عصري بأسلوب editorial، ألوان متناغمة مع باليت GAC، عناصر هندسية بسيطة (دوائر/خطوط) بدون نصوص أو شعارات، النصف العلوي يبقى هادئاً وقابلاً للقراءة.",
  backgroundPanelConfig: {
    // panel علوي 50% — أزرق GAC الرئيسي
    x: 0, y: 0, width: 100, height: 50,
    color: C_BLUE, opacity: 1.0, borderRadius: 0,
  },
  textSlots: [
    {
      key: "title", label_ar: "العنوان الرئيسي", role: "title",
      x: 8, y: 9, width: 84, height: 22,
      default_font_size: 88, minFontSize: 44, maxFontSize: 108,
      max_words: 7, alignment: "right",
      color: GAC.neutral.white, fontWeight: 700, lineHeight: 1.15,
    },
    {
      key: "body", label_ar: "العنوان الفرعي / الوصف", role: "body",
      x: 8, y: 32, width: 84, height: 14,
      default_font_size: 44, minFontSize: 26, maxFontSize: 54,
      max_words: 15, alignment: "right",
      color: GAC.neutral.white, fontWeight: 300, lineHeight: 1.3,
    },
  ],
  logoSlots: [
    // الشعار 21% = 226px أسفل-يمين (matches V2 pattern)
    { key: "logo_main", x: 96, y: 78, maxWidth: 226, maxHeight: 95, align: "right" },
  ],
  thumbnailUrl: null,
};

// ──────────────────────────────────────────────────────────
// التصدير
// ──────────────────────────────────────────────────────────
export const SEED_TEMPLATES_2026: SeedTemplate2026[] = [
  ANNOUNCEMENT_TEMPLATE,
  WORKSHOP_TEMPLATE,
  SOCIAL_MODERN_TEMPLATE,
];
