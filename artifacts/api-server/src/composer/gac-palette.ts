/**
 * GAC Brand Palette
 * المصدر: GAC-Brand-Manual.pdf — ص 11 (Primary)، ص 12 (Secondary)، ص 13 (Neutral)، ص 15 (Tints)
 *
 * ملاحظات الدليل:
 * - الألوان الأساسية تُستخدم للعناصر الرسمية (Stationery، الشعار)
 * - الألوان الثانوية تُستخدم كخلفيات وعناصر تصميم
 * - يُسمح بـ tints حتى 20% فقط، والدرجات القياسية: 20%, 50%, 80%, 100%
 * - يُحظر استخدام أي لون خارج هذه اللوحة كخلفية للشعار
 */

export const GAC = {
  primary: {
    blue:     "#0069A7", // Pantone 307 C — اللون المؤسسي الأبرز
    darkBlue: "#00567D", // Pantone 308 C
    green:    "#61A60E", // Pantone 369 C
    yellow:   "#F5CE3E", // Pantone 129 C
    coolGray: "#76777A", // Pantone Cool Gray 9C
  },
  secondary: {
    navy:       "#194F90", // Pantone 7686 C
    cyan:       "#46BCCD", // Pantone 319 C
    green:      "#009845", // Pantone 347 C
    orange:     "#D79A2B", // Pantone 7563 C
    lightGreen: "#9DC41A", // Pantone 375 C
  },
  neutral: {
    lightGray: "#DCDDDE",
    midGray:   "#A7A9AC",
    black:     "#231F20",
    white:     "#FFFFFF",
  },
} as const;

/** كل ألوان GAC في مصفوفة واحدة للفحص (whitelist) */
export const GAC_ALL_COLORS: readonly string[] = [
  ...Object.values(GAC.primary),
  ...Object.values(GAC.secondary),
  ...Object.values(GAC.neutral),
];

/** تحويل HEX إلى rgba مع شفافية */
export function withAlpha(hex: string, alpha: number): string {
  const clean = hex.replace("#", "");
  const n = parseInt(clean, 16);
  const r = (n >> 16) & 255;
  const g = (n >> 8) & 255;
  const b = n & 255;
  return `rgba(${r}, ${g}, ${b}, ${alpha})`;
}

/** درجات tint قياسية حسب الدليل (ص 15) */
export type TintLevel = 20 | 50 | 80 | 100;
export function tint(hex: string, percent: TintLevel): string {
  return withAlpha(hex, percent / 100);
}

/** قياس luminance لاختيار نسخة الشعار المناسبة */
export function isColorDark(color: string): boolean {
  // يقبل #RGB أو #RRGGBB أو rgba(...)
  let r = 0, g = 0, b = 0;
  const rgbaMatch = color.match(/rgba?\((\d+),\s*(\d+),\s*(\d+)/);
  if (rgbaMatch) {
    r = parseInt(rgbaMatch[1]);
    g = parseInt(rgbaMatch[2]);
    b = parseInt(rgbaMatch[3]);
  } else if (color.startsWith("#")) {
    const clean = color.replace("#", "");
    const hex = clean.length === 3
      ? clean.split("").map(c => c + c).join("")
      : clean;
    const n = parseInt(hex, 16);
    r = (n >> 16) & 255;
    g = (n >> 8) & 255;
    b = n & 255;
  }
  // relative luminance (sRGB approximation)
  const luminance = (0.299 * r + 0.587 * g + 0.114 * b) / 255;
  return luminance < 0.5;
}

/**
 * استبدال الشرطات حسب قواعد typesetting (ص 20):
 * - " - " (هايفن بمسافات)  →  " – " (en dash)
 * - "--"                  →  "—" (em dash)
 */
export function normalizeDashes(text: string): string {
  if (!text) return text;
  return text
    .replace(/(\s)-(\s)/g, "$1\u2013$2") // en dash بين الكلمات
    .replace(/--/g, "\u2014");           // em dash للفواصل الطويلة
}

/** كشف لغة النص (عربي vs لاتيني) */
export function isArabicText(text: string): boolean {
  return /[\u0600-\u06FF]/.test(text);
}
