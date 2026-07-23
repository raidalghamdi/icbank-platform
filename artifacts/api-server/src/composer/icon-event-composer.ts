/**
 * Icon Event Composer
 * توليد HTML/CSS لتصاميم الفعاليات باستخدام أيقونات فقط (بدون صور)
 *
 * المعمارية:
 * - 4 تخطيطات: stats-hero (مطابق للمرجع الرسمي) / hero / grid / split
 * - 3 مقاسات: Square (1080×1080) / Story (1080×1920) / Landscape (1920×1080)
 * - متوافق مع هوية GAC بالكامل (الألوان من palette، الشعار من brand-assets)
 */

import { GAC } from "./gac-palette";
import { renderIcon } from "./icon-library";
import { BG_STATS_HERO_DATA_URI, GAC_LOGO_WHITE_DATA_URI } from "./assets-v5";
import { FRUTIGER_FONT_CSS } from "./frutiger-fonts";
import { GAC_LOGO_WHITE_SVG } from "./gac-logo-svg";

/** Render GAC logo as inline SVG (vector, crisp at any size). */
function renderLogoSvg(styleAttr: string): string {
  // أضف attributes for sizing (aspect ratio preserved)
  const styled = GAC_LOGO_WHITE_SVG.replace(
    /<svg([^>]*)>/,
    `<svg$1 style="${styleAttr}" preserveAspectRatio="xMidYMid meet">`
  );
  return styled;
}

/** If external URL provided, use <img>; otherwise inline SVG. Returns "" when hidden. */
function renderLogo(logoUrl: string | undefined, positionStyle: string, heightPx: number, size?: SizePreset): string {
  // للمقاسات المُصَغَّرة — لا شعار أبدًا
  if (size && (size === "web-small" || size === "web-mini")) return "";
  if (logoUrl && logoUrl.startsWith("http")) {
    return `<img src="${logoUrl}" style="${positionStyle};height:${heightPx}px;" crossorigin="anonymous" alt="GAC" />`;
  }
  // aspect ≈ 7338/2279 = 3.22
  const widthPx = Math.round(heightPx * 3.22);
  return renderLogoSvg(`${positionStyle};height:${heightPx}px;width:${widthPx}px;`);
}

export type LayoutType = "stats-hero" | "hero" | "grid" | "split" | "typography";
export type SizePreset =
  | "square" | "story" | "landscape"          // legacy presets
  | "uhd-4k"           // 3840×2160  — 4K UHD
  | "desktop-hd"       // 1440×864   — Desktop HD
  | "web-standard"     // 1067×712   — Web / Email
  | "web-small"        // 799×479    — Small (حَذف الشعار/الإدارة)
  | "web-mini"         // 639×479    — Mini (حَذف الشعار/الإدارة)
  | "story-hd";        // 2823×5016 — Story HD
export type ColorScheme = "teal" | "blue" | "green" | "cyan" | "navy";

export interface IconEventStat {
  /** الأيقونة (من المكتبة) */
  icon: string;
  /** القيمة (رقم/نص قصير مثل "135+" أو "20") */
  value: string;
  /** الوصف (سطرين كحد أقصى) */
  label: string;
}

export interface IconEventInput {
  /** عنوان الفعالية الرئيسي */
  headline: string;
  /** عنوان فرعي/سياق */
  subtitle?: string;
  /** اسم الإدارة (يظهر في شارة أعلى اليمين، اختياري) */
  department?: string;
  /** هاشتاج (يظهر أسفل اليسار) */
  hashtag?: string;
  /** البريد الإلكتروني للتواصل (يظهر كعنصر ميتا بارز) */
  contact_email?: string;
  /** رقم الهاتف للتواصل (يظهر كعنصر ميتا بارز) */
  contact_phone?: string;
  /** التاريخ */
  date?: string;
  /** الوقت */
  time?: string;
  /** المكان */
  location?: string;
  /** الأيقونة الرئيسية (للتخطيطات القديمة) */
  main_icon: string;
  /** أيقونات داعمة */
  supporting_icons?: string[];
  /** الإحصائيات (3 إحصائيات لـ stats-hero) */
  stats?: IconEventStat[];
  /** نظام الألوان */
  color_scheme: ColorScheme;
  /** التخطيط */
  layout: LayoutType;
  /** المقاس */
  size: SizePreset;
  /** شعار GAC URL */
  logo_url?: string;
}

// Canvas dimensions — slightly expanded for more horizontal breathing room while keeping exact aspect ratios
const SIZE_MAP: Record<SizePreset, { width: number; height: number; aspectLabel: string }> = {
  square: { width: 1200, height: 1200, aspectLabel: "1:1" },
  story: { width: 1200, height: 2133, aspectLabel: "9:16" },
  landscape: { width: 2000, height: 1125, aspectLabel: "16:9" },
  "uhd-4k":       { width: 3840, height: 2160, aspectLabel: "16:9 UHD" },
  "desktop-hd":   { width: 1440, height: 864,  aspectLabel: "5:3" },
  "web-standard": { width: 1067, height: 712,  aspectLabel: "3:2" },
  "web-small":    { width: 799,  height: 479,  aspectLabel: "5:3" },
  "web-mini":     { width: 639,  height: 479,  aspectLabel: "4:3" },
  "story-hd":     { width: 2823, height: 5016, aspectLabel: "9:16 HD" },
};

// Per-size design tokens — each preset gets its own visual tuning
const SIZE_TOKENS: Record<SizePreset, {
  margin: number;
  deptFont: number;
  deptPaddingV: number;
  deptPaddingH: number;
  logoHeight: number;
  titleSize: number;
  subtitleSize: number;
  metaFont: number;
  paragraphGap: number;
  lineHeight: number;
}> = {
  landscape: { margin: 72, deptFont: 24, deptPaddingV: 16, deptPaddingH: 42, logoHeight: 78, titleSize: 74, subtitleSize: 42, metaFont: 30, paragraphGap: 28, lineHeight: 1.75 },
  square:    { margin: 64, deptFont: 26, deptPaddingV: 18, deptPaddingH: 44, logoHeight: 78, titleSize: 78, subtitleSize: 44, metaFont: 30, paragraphGap: 26, lineHeight: 1.75 },
  story:     { margin: 60, deptFont: 28, deptPaddingV: 20, deptPaddingH: 48, logoHeight: 92, titleSize: 96, subtitleSize: 50, metaFont: 34, paragraphGap: 32, lineHeight: 1.8  },
  // مقاسات جديدة رسمية (scaled proportionally from 2000×1125 landscape base)
  "uhd-4k":       { margin: 140, deptFont: 46, deptPaddingV: 30, deptPaddingH: 82, logoHeight: 150, titleSize: 142, subtitleSize: 80, metaFont: 58, paragraphGap: 54, lineHeight: 1.75 },
  "desktop-hd":   { margin: 52,  deptFont: 18, deptPaddingV: 12, deptPaddingH: 30, logoHeight: 56,  titleSize: 54,  subtitleSize: 30, metaFont: 22, paragraphGap: 20, lineHeight: 1.7  },
  "web-standard": { margin: 40,  deptFont: 14, deptPaddingV: 9,  deptPaddingH: 24, logoHeight: 42,  titleSize: 40,  subtitleSize: 22, metaFont: 16, paragraphGap: 14, lineHeight: 1.65 },
  "web-small":    { margin: 28,  deptFont: 11, deptPaddingV: 6,  deptPaddingH: 18, logoHeight: 32,  titleSize: 32,  subtitleSize: 18, metaFont: 13, paragraphGap: 10, lineHeight: 1.6  },
  "web-mini":     { margin: 24,  deptFont: 10, deptPaddingV: 6,  deptPaddingH: 16, logoHeight: 28,  titleSize: 28,  subtitleSize: 16, metaFont: 12, paragraphGap: 9,  lineHeight: 1.6  },
  "story-hd":     { margin: 150, deptFont: 60, deptPaddingV: 40, deptPaddingH: 100,logoHeight: 200, titleSize: 200, subtitleSize: 105,metaFont: 72, paragraphGap: 66, lineHeight: 1.85 },
};

/** المقاسات التي لا تعرض الشعار ولا الإدارة (حسب المتطلب) */
export function isMiniSize(size: SizePreset): boolean {
  return size === "web-small" || size === "web-mini";
}

// Helper: department tag (rectangular, sharp corners, white text, size-aware)
function renderDeptTag(department: string | undefined, colors: any, size: SizePreset, opts?: { top?: number; left?: number; zIndex?: number }): string {
  if (!department) return "";
  // للمقاسات المُصَغَّرة — لا إدارة أبدًا
  if (size === "web-small" || size === "web-mini") return "";
  const T = SIZE_TOKENS[size];
  const top = opts?.top ?? T.margin;
  const left = opts?.left ?? T.margin;
  const z = opts?.zIndex ?? 10;
  return `<div style="position:absolute;top:${top}px;left:${left}px;background:${colors.accent};color:#fff;padding:${T.deptPaddingV}px ${T.deptPaddingH}px;border-radius:0;font-weight:800;font-size:${T.deptFont}px;letter-spacing:0.5px;line-height:1;white-space:nowrap;z-index:${z};box-shadow:0 2px 6px rgba(0,0,0,0.15);">${department}</div>`;
}

// Helper: split long subtitle into paragraph blocks for better hierarchy
function splitIntoParagraphs(text: string): string[] {
  if (!text) return [];
  // Prefer existing newlines
  const byNewline = text.split(/\n{1,}/).map(s => s.trim()).filter(Boolean);
  if (byNewline.length > 1) return byNewline;
  // Otherwise split by sentence endings (Arabic period, Latin period, question, exclamation)
  const sentences = text.split(/(?<=[\.؟!۔])\s+/).map(s => s.trim()).filter(Boolean);
  if (sentences.length <= 1) return [text];
  // Group sentences into 2 balanced blocks maximum for readability
  if (sentences.length <= 2) return sentences;
  const mid = Math.ceil(sentences.length / 2);
  return [sentences.slice(0, mid).join(" "), sentences.slice(mid).join(" ")];
}

// ============================================================================
// Inline contact detection
// ---------------------------------------------------------------------------
// الفكرة: إذا انتهت فقرة ما بإشارة للبريد/الهاتف ("... عبر البريد الإلكتروني") 
// → المستخدم يتوقع أن يرى البريد تحتها مباشرة، لا في أسفل التصميم.
// لذا: ندمج شريحة البريد/الهاتف في موقعها الطبيعي داخل الفقرات.
// ============================================================================

type ParagraphBlock =
  | { type: "text"; content: string }
  | { type: "sub-heading"; content: string }
  | { type: "bullet-list"; items: string[] }
  | { type: "email-chip"; email: string }
  | { type: "phone-chip"; phone: string };

// أنماط الإشارة للبريد (في نهاية أو وسط الفقرة)
const EMAIL_MENTION_RE = /(البريد\s*الإلكتروني|email|e-?mail|إيميل)\s*[:ـ\-：]?\s*$/i;
const PHONE_MENTION_RE = /(الهاتف|رقم\s*التواصل|phone|tel|جوال)\s*[:ـ\-：]?\s*$/i;

// أنماط bullet points المدخلة (Markdown/Unicode)
// * item   OR   - item   OR   • item   OR   ⁃ item   OR   ◦ item
const BULLET_LINE_RE = /^\s*[*\-•⁃◦▪﹅]\s+(.+)$/;

// أسطر تنتهي بـ ؟ تعتبر عنوانين فرعية ("متى قد تحدث المشكلة؟" ...)
// أو أسطر قصيرة (< 45 حرفاً) تنتهي بـ :
const SUBHEAD_QUESTION_RE = /^[^\n]{3,80}؟\s*$/;
const SUBHEAD_COLON_RE = /^[^\n]{3,45}[:：]\s*$/;

function isSubHeading(line: string): boolean {
  const t = line.trim();
  if (!t) return false;
  if (BULLET_LINE_RE.test(t)) return false; // لا تعتبر bullet عنوان
  return SUBHEAD_QUESTION_RE.test(t) || SUBHEAD_COLON_RE.test(t);
}

// يبني تدفق الفقرات مع دعم:
//   • فقرات نصية (text)
//   • عناوين فرعية ("متى قد تحدث؟" أو "كيف تكتشفها؟")
//   • bullet points (* item أو - item)
//   • دمج البريد/الهاتف inline
function buildParagraphFlow(
  subtitle: string,
  email: string | undefined,
  phone: string | undefined,
): { blocks: ParagraphBlock[]; emailUsedInline: boolean; phoneUsedInline: boolean } {
  const blocks: ParagraphBlock[] = [];
  let emailUsedInline = false;
  let phoneUsedInline = false;

  if (!subtitle || !subtitle.trim()) {
    return { blocks, emailUsedInline, phoneUsedInline };
  }

  // فصل على أسطر فردية (ليس فقرات) لأن bullets تُقرأ سطرًا بسطر
  const lines = subtitle.split(/\n/).map(l => l.replace(/\s+$/, ""));
  const nonEmpty = lines.filter(l => l.trim() !== "");

  // إذا لم يوجد أي bullet أو عنوان فرعي → ترجع للمنطق القديم (فقرات حرة)
  const hasBullets = nonEmpty.some(l => BULLET_LINE_RE.test(l));
  const hasSubHead = nonEmpty.some(l => isSubHeading(l));

  if (!hasBullets && !hasSubHead) {
    // المنطق القديم: فقرات مفصولة بأسطر فارغة
    const paragraphs = splitIntoParagraphs(subtitle);
    for (const para of paragraphs) {
      blocks.push({ type: "text", content: para });
      if (email && !emailUsedInline && EMAIL_MENTION_RE.test(para)) {
        blocks.push({ type: "email-chip", email });
        emailUsedInline = true;
      }
      if (phone && !phoneUsedInline && PHONE_MENTION_RE.test(para)) {
        blocks.push({ type: "phone-chip", phone });
        phoneUsedInline = true;
      }
    }
    return { blocks, emailUsedInline, phoneUsedInline };
  }

  // المنطق الجديد: قراءة سطراً بسطر، تجميع bullet lines في bullet-list
  let currentBullets: string[] = [];
  let currentTextBuffer: string[] = [];

  const flushText = () => {
    if (currentTextBuffer.length) {
      const combined = currentTextBuffer.join(" ").trim();
      if (combined) {
        blocks.push({ type: "text", content: combined });
        // فحص إدراج البريد/الهاتف inline
        if (email && !emailUsedInline && EMAIL_MENTION_RE.test(combined)) {
          blocks.push({ type: "email-chip", email });
          emailUsedInline = true;
        }
        if (phone && !phoneUsedInline && PHONE_MENTION_RE.test(combined)) {
          blocks.push({ type: "phone-chip", phone });
          phoneUsedInline = true;
        }
      }
      currentTextBuffer = [];
    }
  };

  const flushBullets = () => {
    if (currentBullets.length) {
      blocks.push({ type: "bullet-list", items: currentBullets });
      // فحص دمج البريد إذا انتهيت آخر bullet بذكر البريد
      const lastBullet = currentBullets[currentBullets.length - 1];
      if (email && !emailUsedInline && EMAIL_MENTION_RE.test(lastBullet)) {
        blocks.push({ type: "email-chip", email });
        emailUsedInline = true;
      }
      if (phone && !phoneUsedInline && PHONE_MENTION_RE.test(lastBullet)) {
        blocks.push({ type: "phone-chip", phone });
        phoneUsedInline = true;
      }
      currentBullets = [];
    }
  };

  for (const rawLine of lines) {
    const line = rawLine.trim();
    if (!line) {
      // سطر فارغ: flush buffers
      flushText();
      flushBullets();
      continue;
    }

    const bulletMatch = line.match(BULLET_LINE_RE);
    if (bulletMatch) {
      // bullet: flush text buffer أولاً، ثم أضف لـ bullets
      flushText();
      currentBullets.push(bulletMatch[1].trim());
      continue;
    }

    if (isSubHeading(line)) {
      // sub-heading: flush everything, add heading, don't buffer
      flushText();
      flushBullets();
      blocks.push({ type: "sub-heading", content: line });
      continue;
    }

    // سطر نص عادي: flush bullets أولاً، ثم أضف لـ text buffer
    flushBullets();
    currentTextBuffer.push(line);
  }

  // التفريغ الأخير
  flushText();
  flushBullets();

  return { blocks, emailUsedInline, phoneUsedInline };
}

// يرسم كتلة فقرات HTML مع دعم email/phone inline chips + sub-heading + bullet-list.
// paragraphStyle: الـ-CSS المطبق على أوسمة <p>.
function renderParagraphFlow(
  blocks: ParagraphBlock[],
  paragraphStyle: string,
  colors: any,
  metaFont: number,
  opts?: { subHeadSize?: number; bulletSize?: number; align?: "center" | "right" },
): string {
  // مقاس الخط من paragraphStyle (الأساس)
  const fsMatch = paragraphStyle.match(/font-size:(\d+)px/);
  const baseSize = fsMatch ? parseInt(fsMatch[1], 10) : 32;
  const subHeadSize = opts?.subHeadSize ?? Math.round(baseSize * 1.15);
  const bulletSize = opts?.bulletSize ?? Math.max(baseSize - 4, 22);
  const align = opts?.align ?? "center";
  const listAlign = align === "center" ? "right" : align; // القوائم دائمًا RTL right-aligned

  // دالة ترسم bullet-list HTML فقط (بدون wrapper container)
  const renderBulletList = (items: string[]): string => {
    const itemsHtml = items.map((it) =>
      `<li style="display:flex;align-items:flex-start;gap:12px;text-align:${listAlign};direction:rtl;">`
      + `<span style="flex-shrink:0;margin-top:${Math.round(bulletSize*0.42)}px;width:9px;height:9px;border-radius:50%;background:${colors.accent};"></span>`
      + `<span style="flex:1;font-size:${bulletSize}px;line-height:1.55;font-weight:500;color:#fff;opacity:0.95;">${it}</span>`
      + `</li>`
    ).join("");
    return `<ul style="list-style:none;padding:0;margin:0;display:flex;flex-direction:column;gap:8px;text-align:${listAlign};">${itemsHtml}</ul>`;
  };

  // دالة ترسم sub-heading HTML فقط (مع إمكانية تصغير الهوامش للدمج مع bullets)
  const renderSubHead = (content: string, tightBottom = false): string =>
    `<h3 style="font-size:${subHeadSize}px;color:${colors.accent};font-weight:800;margin:${tightBottom ? 0 : 10}px 0 ${tightBottom ? 6 : 4}px;line-height:1.25;text-align:${align};">${content}</h3>`;

  // مر على الوحدات: إذا sub-heading يتبعه bullet-list مباشرة → ادمجهما معًا (subhead فوق المجموعة مباشرة بدون فراغ)
  const parts: string[] = [];
  for (let i = 0; i < blocks.length; i++) {
    const b = blocks[i];
    if (b.type === "text") {
      parts.push(`<p style="${paragraphStyle}">${b.content}</p>`);
      continue;
    }
    if (b.type === "sub-heading") {
      const next = blocks[i + 1];
      if (next && next.type === "bullet-list") {
        // دمج: subhead ملتصق بـ bullets مباشرة (أقل margin أسفل)
        parts.push(
          `<div style="display:flex;flex-direction:column;gap:6px;text-align:${listAlign};">`
          + renderSubHead(b.content, true)
          + renderBulletList(next.items)
          + `</div>`
        );
        i++; // تخطّ bullet-list التالية
        continue;
      }
      parts.push(renderSubHead(b.content));
      continue;
    }
    if (b.type === "bullet-list") {
      parts.push(renderBulletList(b.items));
      continue;
    }
    if (b.type === "email-chip") {
      parts.push(`<div style="text-align:center;margin:6px 0;">${renderContactChip("mail", b.email, colors, metaFont, true)}</div>`);
      continue;
    }
    // phone-chip
    parts.push(`<div style="text-align:center;margin:6px 0;">${renderContactChip("phone", b.phone, colors, metaFont, false)}</div>`);
  }
  return parts.join("");
}

// Helper: email/phone meta chip with icon on LEFT (LTR content)
function renderContactChip(icon: string, text: string, colors: any, fontSize: number, isEmail: boolean): string {
  // Email/phone are LTR content — icon goes on the LEFT of the text
  return `<div style="display:inline-flex;align-items:center;gap:12px;background:rgba(255,255,255,0.14);padding:14px 26px;border-radius:50px;border:1.5px solid rgba(255,255,255,0.22);direction:ltr;">
    <span style="color:${colors.accent};display:inline-flex;">${renderIcon(icon, fontSize + 4, colors.accent)}</span>
    <span style="font-size:${fontSize}px;font-weight:700;color:#fff;">${text}</span>
  </div>`;
}

// Helper: regular meta chip (date/time/location) — icon on right in RTL, but flex order handles it
function renderMetaChip(icon: string, text: string, colors: any, fontSize: number): string {
  return `<div style="display:inline-flex;align-items:center;gap:12px;background:rgba(255,255,255,0.14);padding:14px 26px;border-radius:50px;border:1.5px solid rgba(255,255,255,0.22);">
    <span style="color:${colors.accent};display:inline-flex;">${renderIcon(icon, fontSize + 4, colors.accent)}</span>
    <span style="font-size:${fontSize}px;font-weight:700;color:#fff;">${text}</span>
  </div>`;
}

const COLOR_MAP: Record<ColorScheme, { primary: string; secondary: string; accent: string; bgPattern: string }> = {
  teal: {
    // مطابق للمرجع — أزرق-أخضر داكن
    primary: "#0E5862", // Teal مرجعي
    secondary: "#0A4148", // أغمق
    accent: GAC.secondary.lightGreen, // 9DC41A — لون الأرقام والأيقونات
    bgPattern: "#0E5862",
  },
  blue: {
    primary: GAC.primary.blue,
    secondary: GAC.primary.darkBlue,
    accent: GAC.secondary.cyan,
    bgPattern: GAC.primary.blue,
  },
  green: {
    primary: GAC.primary.green,
    secondary: GAC.secondary.green,
    accent: GAC.secondary.lightGreen,
    bgPattern: GAC.primary.green,
  },
  cyan: {
    primary: GAC.secondary.cyan,
    secondary: GAC.primary.blue,
    accent: GAC.primary.darkBlue,
    bgPattern: GAC.secondary.cyan,
  },
  navy: {
    primary: GAC.secondary.navy,
    secondary: GAC.primary.darkBlue,
    accent: GAC.secondary.cyan,
    bgPattern: GAC.secondary.navy,
  },
};

/** خلفية مزخرفة بالأيقونات */
function backgroundPattern(iconName: string, color: string): string {
  const icon = renderIcon(iconName, 60, color);
  if (!icon) return "";
  const encoded = encodeURIComponent(icon).replace(/'/g, "%27");
  return `data:image/svg+xml;utf8,${encoded}`;
}

/** زخرفة هندسية (Diamond/Mesh) — مطابقة للمرجع الرسمي */
function diamondMeshPattern(color: string, opacity: number = 0.08): string {
  // SVG شبكة معينات (rhombus mesh) — تماماً كالمرجع
  const svg = `<svg xmlns="http://www.w3.org/2000/svg" width="600" height="600" viewBox="0 0 600 600">
    <g fill="none" stroke="${color}" stroke-width="2" opacity="${opacity}">
      <path d="M 0 300 L 300 0 L 600 300 L 300 600 Z"/>
      <path d="M -150 300 L 150 0 L 450 300 L 150 600 Z"/>
      <path d="M 150 300 L 450 0 L 750 300 L 450 600 Z"/>
      <path d="M 0 150 L 300 -150 L 600 150 L 300 450 Z"/>
      <path d="M 0 450 L 300 150 L 600 450 L 300 750 Z"/>
    </g>
  </svg>`;
  const encoded = encodeURIComponent(svg).replace(/'/g, "%27");
  return `data:image/svg+xml;utf8,${encoded}`;
}

/** ============ LAYOUT 0: STATS HERO (v5 — مطابق للمواصفات النهائية) ============
 *  - خلفية رسمية من PPTX الأصلي (تيل + زخرفة معينات أصلية)
 *  - اللوقو الرسمي الأبيض الكامل (الهيئة + GAC) بدون مستطيل خلفه
 *  - شارة إدارة أعلى يسار + ثلاث إحصائيات + هاشتاج أسفل يسار
 *  - تطبيق نقاط التحسين الـ 14 بدقة
 */
function statsHeroLayout(input: IconEventInput): string {
  const { width, height } = SIZE_MAP[input.size];
  const isStory = input.size === "story";
  const isSquare = input.size === "square";
  const isLandscape = input.size === "landscape";

  // ─── الألوان النهائية (v5) ───
  // اللون يأتي من الخلفية مباشرة؛ هذه الألوان للنصوص فقط
  const ACCENT = "#9DC41A"; // ليموني — الأرقام/الأيقونات/الفواصل/الهاشتاق/الشارة
  const BADGE_TEXT = "#FFFFFF"; // أبيض لنص بادج الإدارة (على خلفية ليمونية)
  const WHITE = "#FFFFFF";

  // ─── الأحجام النسبية (مرجع landscape 1920x1080 — يُقاس عليه) ───
  // landscape هو المرجع الأساسي من v5؛ نسب البقية مشتقة
  const scale = isLandscape ? 1 : isStory ? 1 : 1; // ثابت — كل مقاس مرئي مستقل

  // landscape (1920×1080) — قيم v5 بالضبط
  const L = {
    logoTop: 58,
    logoRight: 70,
    logoWidth: 266, // 95% من 280 (نقطة 1)
    deptTop: 90, // نقطة 3: +20px
    deptLeft: 80,
    deptPadding: "18px 34px",
    deptFont: 18,
    titleTop: 235,
    titleSize: 87, // نقطة 4: -5%
    titleMaxWidth: 1680, // عرض أقصى للعنوان (يسمح بالالتفاف)
    subtitleTop: 370,
    subtitleSize: 44,
    subtitleMaxWidth: 1640,
    statsTop: 530,
    statsWidth: 1480,
    statPadding: 28,
    iconSize: 104,
    iconMb: 24,
    lineWidth: 200,
    lineHeight: 2.5,
    lineMb: 30,
    valueSize: 146,
    valueMb: 28, // نقطة 9: +10px
    labelSize: 26,
    dividerTop: 8,
    dividerHeight: 380, // نقطة 10: +20px
    dividerOpacity: 0.35, // نقطة 10: +10%
    hashtagBottom: 105, // نقطة 11: +15px
    hashtagLeft: 110, // نقطة 11: +15px
    hashtagSize: 38,
  };

  // story (1080×1920) — نسب رأسية
  const S = {
    logoTop: 70,
    logoRight: 60,
    logoWidth: 280,
    deptTop: 110,
    deptLeft: 60,
    deptPadding: "22px 38px",
    deptFont: 22,
    titleTop: 440,
    titleSize: 88, // خفض من 110 لمنع القص + يلتف على سطرين عند الحاجة
    titleMaxWidth: 960,
    subtitleTop: 660,
    subtitleSize: 38, // خفض من 52
    subtitleMaxWidth: 940,
    statsTop: 980,
    statsWidth: 1000,
    statPadding: 14,
    iconSize: 96,
    iconMb: 22,
    lineWidth: 170,
    lineHeight: 2.5,
    lineMb: 26,
    valueSize: 130,
    valueMb: 26,
    labelSize: 28,
    dividerTop: 8,
    dividerHeight: 400,
    dividerOpacity: 0.35,
    hashtagBottom: 110,
    hashtagLeft: 80,
    hashtagSize: 42,
  };

  // square (1080×1080) — تخطيط مدمج
  const Q = {
    logoTop: 50,
    logoRight: 55,
    logoWidth: 220,
    deptTop: 75,
    deptLeft: 55,
    deptPadding: "14px 26px",
    deptFont: 16,
    titleTop: 200,
    titleSize: 64, // خفض من 78 لمنع القص
    titleMaxWidth: 940,
    subtitleTop: 320,
    subtitleSize: 32, // خفض من 38
    subtitleMaxWidth: 920,
    statsTop: 480,
    statsWidth: 940,
    statPadding: 20,
    iconSize: 88,
    iconMb: 20,
    lineWidth: 150,
    lineHeight: 2,
    lineMb: 24,
    valueSize: 110,
    valueMb: 22,
    labelSize: 22,
    dividerTop: 6,
    dividerHeight: 340,
    dividerOpacity: 0.35,
    hashtagBottom: 70,
    hashtagLeft: 75,
    hashtagSize: 30,
  };

  const T = isLandscape ? L : isStory ? S : Q;

  // ─── الإحصائيات: نضمن 3 إحصائيات ───
  // (نقطة 6) الترتيب من العميل: أول عنصر في المصفوفة = يمين بصرياً في RTL
  const stats: IconEventStat[] = (input.stats && input.stats.length > 0
    ? input.stats
    : [
        { icon: "building", value: "—", label: "إدارة" },
        { icon: "users", value: "—", label: "مشاركة" },
        { icon: "presentation", value: "—", label: "جلسة" },
      ]
  ).slice(0, 3);
  while (stats.length < 3) {
    stats.push({ icon: "sparkles", value: "—", label: "" });
  }

  // ─── HTML — مطابق لـ v5 ───
  // اللوقو يستخدم input.logo_url إن مُرّر، وإلا الـ data URI المضمن الافتراضي
  const logoSrc = input.logo_url && input.logo_url.startsWith("http") ? input.logo_url : GAC_LOGO_WHITE_DATA_URI;

  return `
<div class="poster stats-hero-layout" style="width:${width}px;height:${height}px;position:relative;overflow:hidden;font-family:'Frutiger LT Arabic','Cairo','Tajawal',sans-serif;direction:rtl;color:${WHITE};background-image:url('${BG_STATS_HERO_DATA_URI}');background-size:cover;background-position:center;">

  <!-- (1) Official GAC Logo — exact, transparent, no rect (top right) -->
  <div style="position:absolute;top:${T.logoTop}px;right:${T.logoRight}px;width:${T.logoWidth}px;height:auto;line-height:0;background:transparent;">
    ${renderLogo(input.logo_url, `width:100%;height:auto;display:block;background:transparent`, T.logoHeight, input.size)}
  </div>

  <!-- (3) Department Badge — moved 20px lower (top left) -->
  ${
    input.department
      ? `<div style="position:absolute;top:${T.deptTop}px;left:${T.deptLeft}px;background:${ACCENT};color:#ffffff;padding:${T.deptPadding};border-radius:0;font-weight:800;font-size:${T.deptFont}px;line-height:1.1;white-space:nowrap;letter-spacing:-0.2px;">${input.department}</div>`
      : ""
  }

  <!-- (4) Title — reduced 5%, no shadow, more whitespace, wraps within max-width -->
  <div style="position:absolute;top:${T.titleTop}px;left:50%;transform:translateX(-50%);width:${T.titleMaxWidth}px;color:${WHITE};font-weight:900;font-size:${T.titleSize}px;line-height:1.05;text-align:center;letter-spacing:-0.5px;word-wrap:break-word;overflow-wrap:break-word;">${input.headline}</div>

  <!-- (5) Subtitle — lime, en-dash preserved, wraps within max-width -->
  ${
    input.subtitle
      ? `<div style="position:absolute;top:${T.subtitleTop}px;left:50%;transform:translateX(-50%);width:${T.subtitleMaxWidth}px;color:${ACCENT};font-weight:700;font-size:${T.subtitleSize}px;line-height:1.25;text-align:center;word-wrap:break-word;overflow-wrap:break-word;">${input.subtitle}</div>`
      : ""
  }

  <!-- (5-10) Stats Grid — RTL grid, 3 columns, first array element rightmost -->
  <div style="position:absolute;top:${T.statsTop}px;left:50%;transform:translateX(-50%);display:grid;grid-template-columns:1fr 1fr 1fr;width:${T.statsWidth}px;align-items:start;justify-items:center;">
    ${stats
      .map(
        (s, idx) => `
      <div style="display:flex;flex-direction:column;align-items:center;position:relative;width:100%;padding:0 ${T.statPadding}px;">
        ${
          idx < 2
            ? `<div style="content:'';position:absolute;left:0;top:${T.dividerTop}px;height:${T.dividerHeight}px;width:1.5px;background:rgba(255,255,255,${T.dividerOpacity});"></div>`
            : ""
        }
        <!-- (7) Icon — lime, 104px, presentation aligned -->
        <div style="width:${T.iconSize}px;height:${T.iconSize}px;color:${ACCENT};display:flex;align-items:center;justify-content:center;margin-bottom:${T.iconMb}px;">
          ${renderIcon(s.icon, T.iconSize, ACCENT)}
        </div>
        <!-- Lime horizontal line -->
        <div style="width:${T.lineWidth}px;height:${T.lineHeight}px;background:${ACCENT};margin-bottom:${T.lineMb}px;border-radius:2px;"></div>
        <!-- (8) Value — large bold white -->
        <div style="color:${WHITE};font-weight:900;font-size:${T.valueSize}px;line-height:1;margin-bottom:${T.valueMb}px;direction:ltr;font-variant-numeric:tabular-nums;letter-spacing:-3px;">${s.value}</div>
        <!-- (9) Label — +10px gap to value -->
        ${s.label ? `<div style="color:${WHITE};font-weight:500;font-size:${T.labelSize}px;line-height:1.4;text-align:center;max-width:260px;">${s.label.replace(/\n/g, "<br>")}</div>` : ""}
      </div>
    `
      )
      .join("")}
  </div>

  <!-- (11) Hashtag — 15px up, 15px inward -->
  ${
    input.hashtag
      ? `<div style="position:absolute;bottom:${T.hashtagBottom}px;left:${T.hashtagLeft}px;color:${ACCENT};font-weight:800;font-size:${T.hashtagSize}px;letter-spacing:0;">${input.hashtag.startsWith("#") ? input.hashtag : "#" + input.hashtag}</div>`
      : ""
  }
</div>
  `.trim();
}

/** ============ LAYOUT 1: HERO ============ */
function heroLayout(input: IconEventInput): string {
  const colors = COLOR_MAP[input.color_scheme] || COLOR_MAP.blue;
  const { width, height } = SIZE_MAP[input.size];
  const T = SIZE_TOKENS[input.size];
  const isStory = input.size === "story";
  const isSquare = input.size === "square";
  const isLandscape = input.size === "landscape";
  const logoSrc = input.logo_url && input.logo_url.startsWith("http") ? input.logo_url : GAC_LOGO_WHITE_DATA_URI;

  // Size-aware sizing — يتكيف مع طول المحتوى
  const contentLength = (input.subtitle || "").length;
  const bulletCount = ((input.subtitle || "").match(/^\s*[*\-•]\s+/gm) || []).length;
  const subHeadCount = ((input.subtitle || "").match(/[^\n]{3,80}؟\s*$/gm) || []).length;
  const isDense = contentLength > 350 || bulletCount >= 3;
  const isVeryDense = contentLength > 650 || bulletCount >= 5 || subHeadCount >= 2;

  const mainIconSize = isVeryDense
    ? (isStory ? 100 : isSquare ? 88 : 96)
    : isDense
      ? (isStory ? 120 : isSquare ? 90 : 100)
      : (isStory ? 180 : isSquare ? 140 : 140);
  const iconTopPct = isVeryDense
    ? (isStory ? "8%" : isSquare ? "7%" : "8%")
    : isDense
      ? (isStory ? "9%" : isSquare ? "8%" : "9%")
      : (isStory ? "12%" : isSquare ? "10%" : "12%");
  const textTopPct = isVeryDense
    ? (isStory ? "23%" : isSquare ? "23%" : "25%")
    : isDense
      ? (isStory ? "27%" : isSquare ? "28%" : "32%")
      : (isStory ? "32%" : isSquare ? "32%" : "38%");
  const subtitleMaxWidth = isStory ? 1000 : isSquare ? 1000 : 1600;
  const heroSubtitleSize = isVeryDense
    ? (isSquare ? 26 : isStory ? 28 : 28)
    : isDense
      ? (isSquare ? 28 : isStory ? 30 : 30)
      : (isSquare ? 34 : T.subtitleSize);

  const email = (input as any).contact_email;
  const phone = (input as any).contact_phone;

  // Inline flow: يدمج البريد/الهاتف inline إذا ذُكر في الفقرات
  const flow = buildParagraphFlow(input.subtitle || "", email, phone);
  const paragraphs = flow.blocks.filter(b => b.type === "text");

  const dateTimeLocationChips = [
    input.date && renderMetaChip("calendar", input.date, colors, T.metaFont),
    input.time && renderMetaChip("clock", input.time, colors, T.metaFont),
    input.location && renderMetaChip("map-pin", input.location, colors, T.metaFont),
  ].filter(Boolean).join("");

  // الشرائح السفلية تظهر فقط للبيانات التي لم تُدمج inline
  const contactChips = [
    !flow.emailUsedInline && email && renderContactChip("mail", email, colors, T.metaFont, true),
    !flow.phoneUsedInline && phone && renderContactChip("phone", phone, colors, T.metaFont, false),
  ].filter(Boolean).join("");

  const paragraphStyle = `font-size:${heroSubtitleSize}px;margin:0;opacity:0.95;font-weight:500;line-height:${T.lineHeight - 0.1};text-align:right;`;
  const heroTitleSize = isVeryDense ? Math.round(T.titleSize * 0.75) : T.titleSize;
  const heroTitleGap = isVeryDense ? Math.round((T.paragraphGap + 12) * 0.5) : (T.paragraphGap + 12);

  // ─── تخطيط side-by-side للمحتوى الكثيف: أيقونة كبيرة يسار + نص يمين (RTL right-aligned) ───
  const useSideLayout = isVeryDense && (isLandscape || isSquare);

  if (useSideLayout) {
    // نسبة: 30% أيقونة / 70% نص
    const iconColWidth = Math.round(width * 0.30);
    // حجم الأيقونة — يملأ العمود (مع مساحة داخلية)
    const bigIconBox = Math.min(iconColWidth - 80, isLandscape ? 480 : 380);
    const bigIconSize = bigIconBox - 100;
    // الأيقونة تتوسط أفقياً داخل عمودها التي في اليسار (RTL: right طبقًا للمرآة، لكن نستخدم left في positioning)
    const iconLeft = Math.round((iconColWidth - bigIconBox) / 2);
    // النص: من يمين الشاشة حتى نهاية عمود الأيقونة
    const textColumnRight = T.margin + 20;
    const textColumnLeft = iconColWidth + 20;

    // مساحة أعلى لتفادي الشعار (الشعار: top=T.margin, height=T.logoHeight → ينتهي عند T.margin+T.logoHeight)
    const textTopSafe = T.margin + T.logoHeight + 40;
    return `
<div class="poster hero-layout" style="width:${width}px;height:${height}px;position:relative;overflow:hidden;font-family:'Frutiger LT Arabic','Cairo','Tajawal',sans-serif;direction:rtl;color:#fff;background-image:url('${BG_STATS_HERO_DATA_URI}');background-size:cover;background-position:center;">
  ${renderDeptTag(input.department, colors, input.size)}
  ${renderLogo(input.logo_url, `position:absolute;top:${T.margin}px;right:${T.margin}px;z-index:5`, T.logoHeight, input.size)}

  <!-- Big icon (left side, vertically centered) -->
  <div style="position:absolute;top:50%;left:${iconLeft}px;transform:translateY(-50%);width:${bigIconBox}px;height:${bigIconBox}px;background:rgba(255,255,255,0.10);border:4px solid rgba(255,255,255,0.22);border-radius:50%;display:flex;align-items:center;justify-content:center;backdrop-filter:blur(10px);box-shadow:0 20px 60px rgba(0,0,0,0.2);">
    <div style="color:${colors.accent};">${renderIcon(input.main_icon, bigIconSize, colors.accent)}</div>
  </div>

  <!-- Text column (right side, RTL right-aligned) -->
  <div style="position:absolute;top:${textTopSafe}px;right:${textColumnRight}px;left:${textColumnLeft}px;bottom:${T.margin + 40}px;display:flex;flex-direction:column;justify-content:center;text-align:right;">
    <h1 style="font-size:${heroTitleSize}px;font-weight:900;margin:0 0 ${heroTitleGap}px;line-height:1.2;letter-spacing:-1px;text-align:right;">${input.headline}</h1>
    ${paragraphs.length > 0 ? `<div style="display:flex;flex-direction:column;gap:${isVeryDense ? Math.max(T.paragraphGap - 8, 14) : (T.paragraphGap - 4)}px;text-align:right;">
      ${renderParagraphFlow(flow.blocks, paragraphStyle, colors, T.metaFont, { subHeadSize: isVeryDense ? 36 : undefined, bulletSize: isVeryDense ? 27 : undefined, align: "right" })}
    </div>` : ""}
  </div>

  <div style="position:absolute;bottom:0;left:0;right:0;height:12px;background:linear-gradient(90deg,${colors.accent} 0%,${colors.secondary} 50%,${colors.primary} 100%);"></div>
</div>
    `.trim();
  }

  return `
<div class="poster hero-layout" style="width:${width}px;height:${height}px;position:relative;overflow:hidden;font-family:'Frutiger LT Arabic','Cairo','Tajawal',sans-serif;direction:rtl;color:#fff;background-image:url('${BG_STATS_HERO_DATA_URI}');background-size:cover;background-position:center;">
  ${renderDeptTag(input.department, colors, input.size)}
  ${renderLogo(input.logo_url, `position:absolute;top:${T.margin}px;right:${T.margin}px;z-index:5`, T.logoHeight, input.size)}

  <!-- Main icon (compact) — lime accent داخل دائرة شفافة -->
  <div style="position:absolute;top:${iconTopPct};left:50%;transform:translateX(-50%);width:${mainIconSize + 50}px;height:${mainIconSize + 50}px;background:rgba(255,255,255,0.10);border:4px solid rgba(255,255,255,0.22);border-radius:50%;display:flex;align-items:center;justify-content:center;backdrop-filter:blur(10px);box-shadow:0 20px 60px rgba(0,0,0,0.2);">
    <div style="color:${colors.accent};">${renderIcon(input.main_icon, mainIconSize, colors.accent)}</div>
  </div>

  <!-- Text block: title + paragraph-split subtitle (with inline email/phone if mentioned) -->
  <div style="position:absolute;top:${textTopPct};left:0;right:0;text-align:center;padding:0 ${T.margin + 40}px;">
    <h1 style="font-size:${heroTitleSize}px;font-weight:900;margin:0 0 ${heroTitleGap}px;line-height:1.2;letter-spacing:-1px;">${input.headline}</h1>
    ${paragraphs.length > 0 ? `<div style="max-width:${subtitleMaxWidth}px;margin:0 auto;display:flex;flex-direction:column;gap:${isVeryDense ? Math.max(T.paragraphGap - 8, 14) : (T.paragraphGap - 4)}px;text-align:right;">
      ${renderParagraphFlow(flow.blocks, paragraphStyle.replace('text-align:center;', 'text-align:right;'), colors, T.metaFont, { subHeadSize: isVeryDense ? 34 : undefined, bulletSize: isVeryDense ? 26 : undefined, align: "right" })}
    </div>` : ""}
  </div>

  <!-- Meta chips (bottom) — only shows data NOT already inline -->
  ${(dateTimeLocationChips || contactChips) ? `<div style="position:absolute;bottom:${isStory ? "5%" : isSquare ? "4%" : "6%"};left:0;right:0;display:flex;justify-content:center;gap:20px;flex-wrap:wrap;padding:0 ${T.margin}px;">
    ${dateTimeLocationChips}${contactChips}
  </div>` : ""}

  <div style="position:absolute;bottom:0;left:0;right:0;height:12px;background:linear-gradient(90deg,${colors.accent} 0%,${colors.secondary} 50%,${colors.primary} 100%);"></div>
</div>
  `.trim();
}

/** ============ LAYOUT 2: GRID ============ */
function gridLayout(input: IconEventInput): string {
  const colors = COLOR_MAP[input.color_scheme] || COLOR_MAP.blue;
  const { width, height } = SIZE_MAP[input.size];
  const T = SIZE_TOKENS[input.size];
  const isStory = input.size === "story";
  const isLandscape = input.size === "landscape";
  const logoSrc = input.logo_url && input.logo_url.startsWith("http") ? input.logo_url : GAC_LOGO_WHITE_DATA_URI;

  const supporting = (input.supporting_icons || []).slice(0, 3);
  const gridIcons = [input.main_icon, ...supporting].slice(0, 4);
  while (gridIcons.length < 4) gridIcons.push("sparkles");

  const titleSize = isStory ? 84 : isLandscape ? 68 : 68;
  const iconBoxSize = isStory ? 220 : isLandscape ? 200 : 220;
  const iconSize = iconBoxSize * 0.55;

  const email = (input as any).contact_email;
  const phone = (input as any).contact_phone;
  const flow = buildParagraphFlow(input.subtitle || "", email, phone);
  const paragraphs = flow.blocks.filter(b => b.type === "text");

  const metaChips = [
    input.date && renderMetaChip("calendar", input.date, colors, T.metaFont),
    input.time && renderMetaChip("clock", input.time, colors, T.metaFont),
    input.location && renderMetaChip("map-pin", input.location, colors, T.metaFont),
    !flow.emailUsedInline && email && renderContactChip("mail", email, colors, T.metaFont, true),
    !flow.phoneUsedInline && phone && renderContactChip("phone", phone, colors, T.metaFont, false),
  ].filter(Boolean).join("");

  const gridParagraphStyle = `font-size:${T.subtitleSize}px;margin:0;opacity:0.95;font-weight:500;color:#fff;line-height:${T.lineHeight};`;

  return `
<div class="poster grid-layout" style="width:${width}px;height:${height}px;position:relative;overflow:hidden;font-family:'Frutiger LT Arabic','Cairo','Tajawal',sans-serif;direction:rtl;color:#fff;background-image:url('${BG_STATS_HERO_DATA_URI}');background-size:cover;background-position:center;">
  ${renderDeptTag(input.department, colors, input.size)}
  ${renderLogo(input.logo_url, `position:absolute;top:${T.margin}px;right:${T.margin}px;z-index:5`, T.logoHeight, input.size)}

  <div style="position:absolute;top:${isStory ? "200px" : "170px"};right:${T.margin}px;left:${T.margin}px;color:#fff;text-align:center;">
    <h1 style="font-size:${titleSize}px;font-weight:900;margin:0 0 ${T.paragraphGap}px;line-height:1.15;letter-spacing:-1px;">${input.headline}</h1>
    ${paragraphs.length > 0 ? `<div style="max-width:${isStory ? 950 : 1500}px;margin:0 auto;display:flex;flex-direction:column;gap:${T.paragraphGap - 4}px;">
      ${renderParagraphFlow(flow.blocks, gridParagraphStyle, colors, T.metaFont)}
    </div>` : ""}
  </div>

  <div style="position:absolute;top:${isStory ? "56%" : isLandscape ? "52%" : "50%"};left:50%;transform:translateX(-50%);display:grid;grid-template-columns:repeat(2,${iconBoxSize}px);gap:${isStory ? 40 : 32}px;">
    ${gridIcons
      .map(
        (icn) => `
      <div style="width:${iconBoxSize}px;height:${iconBoxSize}px;background:rgba(255,255,255,0.12);border:2px solid rgba(255,255,255,0.25);border-radius:28px;display:flex;align-items:center;justify-content:center;backdrop-filter:blur(10px);">
        <div style="color:${colors.accent};">${renderIcon(icn, iconSize, colors.accent)}</div>
      </div>`
      )
      .join("")}
  </div>

  ${metaChips ? `<div style="position:absolute;bottom:${isStory ? "90px" : "70px"};left:${T.margin}px;right:${T.margin}px;display:flex;justify-content:center;gap:16px;flex-wrap:wrap;">${metaChips}</div>` : ""}

  <div style="position:absolute;bottom:0;left:0;right:0;height:12px;background:linear-gradient(90deg,${colors.accent} 0%,${colors.secondary} 50%,${colors.primary} 100%);"></div>
</div>
  `.trim();
}

/** ============ LAYOUT 3: SPLIT ============ */
function splitLayout(input: IconEventInput): string {
  const colors = COLOR_MAP[input.color_scheme] || COLOR_MAP.blue;
  const { width, height } = SIZE_MAP[input.size];
  const T = SIZE_TOKENS[input.size];
  const isStory = input.size === "story";
  const isLandscape = input.size === "landscape";
  const isSquare = input.size === "square";
  const logoSrc = input.logo_url && input.logo_url.startsWith("http") ? input.logo_url : GAC_LOGO_WHITE_DATA_URI;
  const splitVertical = !isStory; // landscape + square use horizontal split

  const titleSize = isStory ? 82 : isLandscape ? 76 : 62;
  const mainIconSize = isStory ? 240 : isLandscape ? 260 : 200;
  // Narrower text column in split — tighten subtitle size to fit
  const splitSubtitleSize = isStory ? T.subtitleSize : isLandscape ? T.subtitleSize : 34;

  const email = (input as any).contact_email;
  const phone = (input as any).contact_phone;
  const flow = buildParagraphFlow(input.subtitle || "", email, phone);
  const paragraphs = flow.blocks.filter(b => b.type === "text");

  const dateTimeLocationChips = [
    input.date && renderMetaChip("calendar", input.date, colors, T.metaFont),
    input.time && renderMetaChip("clock", input.time, colors, T.metaFont),
    input.location && renderMetaChip("map-pin", input.location, colors, T.metaFont),
  ].filter(Boolean).join("");

  const contactChips = [
    !flow.emailUsedInline && email && renderContactChip("mail", email, colors, T.metaFont, true),
    !flow.phoneUsedInline && phone && renderContactChip("phone", phone, colors, T.metaFont, false),
  ].filter(Boolean).join("");

  const splitParagraphStyleH = `font-size:${splitSubtitleSize}px;color:#fff;margin:0;line-height:${T.lineHeight - 0.1};font-weight:500;opacity:0.95;`;
  const splitParagraphStyleV = `font-size:${T.subtitleSize}px;color:#fff;margin:0;line-height:${T.lineHeight};font-weight:500;opacity:0.95;`;

  const supportingRow = (input.supporting_icons || []).slice(0, 3);

  if (splitVertical) {
    // Landscape / Square — horizontal split with generous margins
    const headerReserve = T.margin + T.deptPaddingV * 2 + T.deptFont + 60;
    return `
<div class="poster split-layout" style="width:${width}px;height:${height}px;position:relative;overflow:hidden;font-family:'Frutiger LT Arabic','Cairo','Tajawal',sans-serif;direction:rtl;color:#fff;background-image:url('${BG_STATS_HERO_DATA_URI}');background-size:cover;background-position:center;display:flex;">
  ${renderDeptTag(input.department, colors, input.size)}
  ${renderLogo(input.logo_url, `position:absolute;top:${T.margin}px;right:${T.margin}px;z-index:10`, T.logoHeight, input.size)}

  <!-- Left: icon (40%) -->
  <div style="width:40%;height:100%;position:relative;display:flex;align-items:center;justify-content:center;padding-top:${headerReserve}px;">
    <div style="position:relative;width:${mainIconSize + 80}px;height:${mainIconSize + 80}px;background:rgba(255,255,255,0.12);border:4px solid rgba(255,255,255,0.28);border-radius:50%;display:flex;align-items:center;justify-content:center;">
      <div style="color:${colors.accent};">${renderIcon(input.main_icon, mainIconSize - 20, colors.accent)}</div>
    </div>
  </div>

  <!-- Right: text (60%) -->
  <div style="width:60%;height:100%;padding:${headerReserve + 60}px ${T.margin + 20}px ${T.margin + 40}px ${T.margin + 20}px;display:flex;flex-direction:column;justify-content:${isSquare ? "flex-start" : "center"};text-align:right;">
    <div style="width:96px;height:8px;background:${colors.accent};margin-bottom:${T.paragraphGap + 4}px;"></div>
    <h1 style="font-size:${titleSize}px;font-weight:900;color:#fff;margin:0 0 ${T.paragraphGap + 12}px;line-height:1.25;letter-spacing:-0.5px;text-align:right;">${input.headline}</h1>
    ${paragraphs.length > 0 ? `<div style="display:flex;flex-direction:column;gap:${T.paragraphGap - 4}px;margin-bottom:${T.paragraphGap + 12}px;text-align:right;">
      ${renderParagraphFlow(flow.blocks, splitParagraphStyleH.replace('text-align:center;','text-align:right;'), colors, T.metaFont, { align: "right" })}
    </div>` : ""}
    ${(dateTimeLocationChips || contactChips) ? `<div style="display:flex;gap:14px;flex-wrap:wrap;">${dateTimeLocationChips}${contactChips}</div>` : ""}
  </div>

  <div style="position:absolute;bottom:0;left:0;right:0;height:12px;background:linear-gradient(90deg,${colors.accent} 0%,${colors.secondary} 50%,${colors.primary} 100%);"></div>
</div>`.trim();
  } else {
    // Story (vertical) — icon top, text bottom
    return `
<div class="poster split-layout" style="width:${width}px;height:${height}px;position:relative;overflow:hidden;font-family:'Frutiger LT Arabic','Cairo','Tajawal',sans-serif;direction:rtl;color:#fff;background-image:url('${BG_STATS_HERO_DATA_URI}');background-size:cover;background-position:center;">
  ${renderDeptTag(input.department, colors, input.size)}
  ${renderLogo(input.logo_url, `position:absolute;top:${T.margin}px;right:${T.margin}px;z-index:10`, T.logoHeight, input.size)}

  <div style="position:absolute;top:12%;left:0;right:0;height:26%;display:flex;align-items:center;justify-content:center;">
    <div style="width:${mainIconSize + 100}px;height:${mainIconSize + 100}px;background:rgba(255,255,255,0.12);border:4px solid rgba(255,255,255,0.28);border-radius:50%;display:flex;align-items:center;justify-content:center;">
      <div style="color:${colors.accent};">${renderIcon(input.main_icon, mainIconSize, colors.accent)}</div>
    </div>
  </div>

  <div style="position:absolute;top:42%;left:0;right:0;bottom:${T.margin + 30}px;padding:0 ${T.margin}px;display:flex;flex-direction:column;justify-content:flex-start;">
    <div style="width:96px;height:8px;background:${colors.accent};margin:0 auto ${T.paragraphGap}px;"></div>
    <h1 style="font-size:${titleSize}px;font-weight:900;color:#fff;margin:0 0 ${T.paragraphGap + 8}px;line-height:1.15;text-align:center;">${input.headline}</h1>
    ${paragraphs.length > 0 ? `<div style="max-width:1050px;margin:0 auto ${T.paragraphGap + 12}px;display:flex;flex-direction:column;gap:${T.paragraphGap}px;text-align:center;">
      ${renderParagraphFlow(flow.blocks, splitParagraphStyleV, colors, T.metaFont)}
    </div>` : ""}
    ${(dateTimeLocationChips || contactChips) ? `<div style="display:flex;justify-content:center;gap:14px;flex-wrap:wrap;">${dateTimeLocationChips}${contactChips}</div>` : ""}
  </div>

  <div style="position:absolute;bottom:0;left:0;right:0;height:12px;background:linear-gradient(90deg,${colors.accent} 0%,${colors.secondary} 50%,${colors.primary} 100%);"></div>
</div>`.trim();
  }
}

// ─────────────────────────────────────────────────────────────────────────
// TYPOGRAPHY LAYOUT: تصميم طباعي بدون أيقونات — نص فقط
// ─────────────────────────────────────────────────────────────────────────
function typographyLayout(input: IconEventInput): string {
  const colors = COLOR_MAP[input.color_scheme] || COLOR_MAP.blue;
  const { width, height } = SIZE_MAP[input.size];
  const T = SIZE_TOKENS[input.size];
  const isStory = input.size === "story";
  const isSquare = input.size === "square";
  const logoSrc = input.logo_url && input.logo_url.startsWith("http") ? input.logo_url : GAC_LOGO_WHITE_DATA_URI;

  // Size-aware typography scale
  const titleSize = isStory ? 120 : isSquare ? 104 : 96;
  const subtitleSize = T.subtitleSize + 4;
  const contentMaxWidth = isStory ? 1050 : isSquare ? 1000 : 1600;
  const contentPadding = isStory ? 90 : isSquare ? 100 : 160;

  const email = (input as any).contact_email;
  const phone = (input as any).contact_phone;
  const flow = buildParagraphFlow(input.subtitle || "", email, phone);
  const paragraphs = flow.blocks.filter(b => b.type === "text");

  const dateTimeLocationChips = [
    input.date && renderMetaChip("calendar", input.date, colors, T.metaFont),
    input.time && renderMetaChip("clock", input.time, colors, T.metaFont),
    input.location && renderMetaChip("map-pin", input.location, colors, T.metaFont),
  ].filter(Boolean).join("");

  const contactChips = [
    !flow.emailUsedInline && email && renderContactChip("mail", email, colors, T.metaFont, true),
    !flow.phoneUsedInline && phone && renderContactChip("phone", phone, colors, T.metaFont, false),
  ].filter(Boolean).join("");

  const typoParagraphStyle = `font-size:${subtitleSize}px;margin:0;line-height:${T.lineHeight};font-weight:500;color:#fff;opacity:0.95;`;

  return `
<div class="poster typography-layout" style="width:${width}px;height:${height}px;position:relative;overflow:hidden;font-family:'Frutiger LT Arabic','Cairo','Tajawal',sans-serif;direction:rtl;color:#fff;background-image:url('${BG_STATS_HERO_DATA_URI}');background-size:cover;background-position:center;">
  ${renderDeptTag(input.department, colors, input.size)}
  ${renderLogo(input.logo_url, `position:absolute;top:${T.margin}px;right:${T.margin}px;z-index:10`, T.logoHeight, input.size)}

  <!-- Centered content, text only — no accent bar, sits below header safe zone -->
  <div style="position:absolute;top:${isSquare ? "56%" : "50%"};left:50%;transform:translate(-50%,-50%);width:100%;padding:0 ${contentPadding}px;text-align:center;">
    <h1 style="font-size:${isSquare ? 62 : titleSize}px;font-weight:900;margin:0 0 ${T.paragraphGap + 16}px;line-height:1.2;letter-spacing:-1px;color:#fff;">${input.headline}</h1>
    ${paragraphs.length > 0 ? `<div style="max-width:${contentMaxWidth}px;margin:0 auto ${T.paragraphGap + 20}px;display:flex;flex-direction:column;gap:${T.paragraphGap}px;">
      ${renderParagraphFlow(flow.blocks, typoParagraphStyle, colors, T.metaFont)}
    </div>` : ""}
    ${(dateTimeLocationChips || contactChips) ? `<div style="display:flex;justify-content:center;gap:18px;flex-wrap:wrap;">
      ${dateTimeLocationChips}${contactChips}
    </div>` : ""}
  </div>

  <div style="position:absolute;bottom:0;left:0;right:0;height:14px;background:linear-gradient(90deg,${colors.accent} 0%,${colors.secondary} 50%,${colors.primary} 100%);"></div>
</div>`.trim();
}

/** المُرسل الرئيسي */
export function renderIconEventDesign(input: IconEventInput): string {
  const inner = (() => {
    switch (input.layout) {
      case "stats-hero":
        return statsHeroLayout(input);
      case "hero":
        return heroLayout(input);
      case "grid":
        return gridLayout(input);
      case "split":
        return splitLayout(input);
      case "typography":
        return typographyLayout(input);
    }
  })();

  return `
<!DOCTYPE html>
<html lang="ar" dir="rtl">
<head>
<meta charset="UTF-8" />
<title>${input.headline}</title>
<link rel="preconnect" href="https://fonts.googleapis.com">
<link rel="preconnect" href="https://fonts.gstatic.com" crossorigin>
<link href="https://fonts.googleapis.com/css2?family=Tajawal:wght@400;500;700;900&family=Cairo:wght@400;700;900&display=swap" rel="stylesheet">
<style>
  ${FRUTIGER_FONT_CSS}
  *{box-sizing:border-box;}
  body{margin:0;padding:0;background:#f0f0f0;display:flex;align-items:center;justify-content:center;min-height:100vh;}
  .poster{box-shadow:0 20px 60px rgba(0,0,0,0.2);}
  svg{display:block;}
</style>
</head>
<body>
${inner}
</body>
</html>
  `.trim();
}

export { SIZE_MAP, COLOR_MAP };
