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

export type LayoutType = "stats-hero" | "hero" | "grid" | "split" | "typography";
export type SizePreset = "square" | "story" | "landscape";
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
};

// Helper: department tag (rectangular, sharp corners, white text, size-aware)
function renderDeptTag(department: string | undefined, colors: any, size: SizePreset, opts?: { top?: number; left?: number; zIndex?: number }): string {
  if (!department) return "";
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
<div class="poster stats-hero-layout" style="width:${width}px;height:${height}px;position:relative;overflow:hidden;font-family:'Cairo','Tajawal',sans-serif;direction:rtl;color:${WHITE};background-image:url('${BG_STATS_HERO_DATA_URI}');background-size:cover;background-position:center;">

  <!-- (1) Official GAC Logo — exact, transparent, no rect (top right) -->
  <div style="position:absolute;top:${T.logoTop}px;right:${T.logoRight}px;width:${T.logoWidth}px;height:auto;line-height:0;background:transparent;">
    <img src="${logoSrc}" style="width:100%;height:auto;display:block;background:transparent;" crossorigin="anonymous" alt="GAC" />
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
  const logoSrc = input.logo_url && input.logo_url.startsWith("http") ? input.logo_url : GAC_LOGO_WHITE_DATA_URI;

  // Size-aware sizing
  const mainIconSize = isStory ? 180 : isSquare ? 140 : 140;
  const iconTopPct = isStory ? "12%" : isSquare ? "10%" : "12%";
  const textTopPct = isStory ? "32%" : isSquare ? "32%" : "38%";
  const subtitleMaxWidth = isStory ? 1000 : isSquare ? 1000 : 1600;
  const heroSubtitleSize = isSquare ? 34 : T.subtitleSize;

  const paragraphs = splitIntoParagraphs(input.subtitle || "");
  const email = (input as any).contact_email;
  const phone = (input as any).contact_phone;

  const dateTimeLocationChips = [
    input.date && renderMetaChip("calendar", input.date, colors, T.metaFont),
    input.time && renderMetaChip("clock", input.time, colors, T.metaFont),
    input.location && renderMetaChip("map-pin", input.location, colors, T.metaFont),
  ].filter(Boolean).join("");

  const contactChips = [
    email && renderContactChip("mail", email, colors, T.metaFont, true),
    phone && renderContactChip("phone", phone, colors, T.metaFont, false),
  ].filter(Boolean).join("");

  return `
<div class="poster hero-layout" style="width:${width}px;height:${height}px;position:relative;overflow:hidden;font-family:'Cairo','Tajawal',sans-serif;direction:rtl;color:#fff;background-image:url('${BG_STATS_HERO_DATA_URI}');background-size:cover;background-position:center;">
  ${renderDeptTag(input.department, colors, input.size)}
  <img src="${logoSrc}" style="position:absolute;top:${T.margin}px;right:${T.margin}px;height:${T.logoHeight}px;z-index:5;" crossorigin="anonymous" alt="GAC" />

  <!-- Main icon (compact) -->
  <div style="position:absolute;top:${iconTopPct};left:50%;transform:translateX(-50%);width:${mainIconSize + 50}px;height:${mainIconSize + 50}px;background:rgba(255,255,255,0.12);border:3px solid rgba(255,255,255,0.25);border-radius:50%;display:flex;align-items:center;justify-content:center;backdrop-filter:blur(10px);">
    <div style="color:#fff;">${renderIcon(input.main_icon, mainIconSize, "#fff")}</div>
  </div>

  <!-- Text block: title + paragraph-split subtitle -->
  <div style="position:absolute;top:${textTopPct};left:0;right:0;text-align:center;padding:0 ${T.margin + 40}px;">
    <h1 style="font-size:${T.titleSize}px;font-weight:900;margin:0 0 ${T.paragraphGap + 12}px;line-height:1.2;letter-spacing:-1px;">${input.headline}</h1>
    ${paragraphs.length > 0 ? `<div style="max-width:${subtitleMaxWidth}px;margin:0 auto;display:flex;flex-direction:column;gap:${T.paragraphGap - 4}px;">
      ${paragraphs.map(p => `<p style="font-size:${heroSubtitleSize}px;margin:0;opacity:0.95;font-weight:500;line-height:${T.lineHeight - 0.1};">${p}</p>`).join("")}
    </div>` : ""}
  </div>

  <!-- Meta chips (bottom) -->
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

  const paragraphs = splitIntoParagraphs(input.subtitle || "");
  const email = (input as any).contact_email;
  const phone = (input as any).contact_phone;

  const metaChips = [
    input.date && renderMetaChip("calendar", input.date, colors, T.metaFont),
    input.time && renderMetaChip("clock", input.time, colors, T.metaFont),
    input.location && renderMetaChip("map-pin", input.location, colors, T.metaFont),
    email && renderContactChip("mail", email, colors, T.metaFont, true),
    phone && renderContactChip("phone", phone, colors, T.metaFont, false),
  ].filter(Boolean).join("");

  return `
<div class="poster grid-layout" style="width:${width}px;height:${height}px;position:relative;overflow:hidden;font-family:'Cairo','Tajawal',sans-serif;direction:rtl;color:#fff;background-image:url('${BG_STATS_HERO_DATA_URI}');background-size:cover;background-position:center;">
  ${renderDeptTag(input.department, colors, input.size)}
  <img src="${logoSrc}" style="position:absolute;top:${T.margin}px;right:${T.margin}px;height:${T.logoHeight}px;z-index:5;" crossorigin="anonymous" alt="GAC" />

  <div style="position:absolute;top:${isStory ? "200px" : "170px"};right:${T.margin}px;left:${T.margin}px;color:#fff;text-align:center;">
    <h1 style="font-size:${titleSize}px;font-weight:900;margin:0 0 ${T.paragraphGap}px;line-height:1.15;letter-spacing:-1px;">${input.headline}</h1>
    ${paragraphs.length > 0 ? `<div style="max-width:${isStory ? 950 : 1500}px;margin:0 auto;display:flex;flex-direction:column;gap:${T.paragraphGap - 4}px;">
      ${paragraphs.map(p => `<p style="font-size:${T.subtitleSize}px;margin:0;opacity:0.95;font-weight:500;color:#fff;line-height:${T.lineHeight};">${p}</p>`).join("")}
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

  const paragraphs = splitIntoParagraphs(input.subtitle || "");
  const email = (input as any).contact_email;
  const phone = (input as any).contact_phone;

  const dateTimeLocationChips = [
    input.date && renderMetaChip("calendar", input.date, colors, T.metaFont),
    input.time && renderMetaChip("clock", input.time, colors, T.metaFont),
    input.location && renderMetaChip("map-pin", input.location, colors, T.metaFont),
  ].filter(Boolean).join("");

  const contactChips = [
    email && renderContactChip("mail", email, colors, T.metaFont, true),
    phone && renderContactChip("phone", phone, colors, T.metaFont, false),
  ].filter(Boolean).join("");

  const supportingRow = (input.supporting_icons || []).slice(0, 3);

  if (splitVertical) {
    // Landscape / Square — horizontal split with generous margins
    const headerReserve = T.margin + T.deptPaddingV * 2 + T.deptFont + 60;
    return `
<div class="poster split-layout" style="width:${width}px;height:${height}px;position:relative;overflow:hidden;font-family:'Cairo','Tajawal',sans-serif;direction:rtl;color:#fff;background-image:url('${BG_STATS_HERO_DATA_URI}');background-size:cover;background-position:center;display:flex;">
  ${renderDeptTag(input.department, colors, input.size)}
  <img src="${logoSrc}" style="position:absolute;top:${T.margin}px;right:${T.margin}px;height:${T.logoHeight}px;z-index:10;" crossorigin="anonymous" alt="GAC" />

  <!-- Left: icon (40%) -->
  <div style="width:40%;height:100%;position:relative;display:flex;align-items:center;justify-content:center;padding-top:${headerReserve}px;">
    <div style="position:relative;width:${mainIconSize + 80}px;height:${mainIconSize + 80}px;background:rgba(255,255,255,0.12);border:4px solid rgba(255,255,255,0.28);border-radius:50%;display:flex;align-items:center;justify-content:center;">
      <div style="color:${colors.accent};">${renderIcon(input.main_icon, mainIconSize - 20, colors.accent)}</div>
    </div>
  </div>

  <!-- Right: text (60%) -->
  <div style="width:60%;height:100%;padding:${headerReserve + 40}px ${T.margin + 20}px ${T.margin + 40}px ${T.margin + 20}px;display:flex;flex-direction:column;justify-content:${isSquare ? "flex-start" : "center"};">
    <div style="width:96px;height:8px;background:${colors.accent};margin-bottom:${T.paragraphGap + 4}px;"></div>
    <h1 style="font-size:${titleSize}px;font-weight:900;color:#fff;margin:0 0 ${T.paragraphGap + 12}px;line-height:1.2;letter-spacing:-1px;">${input.headline}</h1>
    ${paragraphs.length > 0 ? `<div style="display:flex;flex-direction:column;gap:${T.paragraphGap - 4}px;margin-bottom:${T.paragraphGap + 12}px;">
      ${paragraphs.map(p => `<p style="font-size:${splitSubtitleSize}px;color:#fff;margin:0;line-height:${T.lineHeight - 0.1};font-weight:500;opacity:0.95;">${p}</p>`).join("")}
    </div>` : ""}
    ${(dateTimeLocationChips || contactChips) ? `<div style="display:flex;gap:14px;flex-wrap:wrap;">${dateTimeLocationChips}${contactChips}</div>` : ""}
  </div>

  <div style="position:absolute;bottom:0;left:0;right:0;height:12px;background:linear-gradient(90deg,${colors.accent} 0%,${colors.secondary} 50%,${colors.primary} 100%);"></div>
</div>`.trim();
  } else {
    // Story (vertical) — icon top, text bottom
    return `
<div class="poster split-layout" style="width:${width}px;height:${height}px;position:relative;overflow:hidden;font-family:'Cairo','Tajawal',sans-serif;direction:rtl;color:#fff;background-image:url('${BG_STATS_HERO_DATA_URI}');background-size:cover;background-position:center;">
  ${renderDeptTag(input.department, colors, input.size)}
  <img src="${logoSrc}" style="position:absolute;top:${T.margin}px;right:${T.margin}px;height:${T.logoHeight}px;z-index:10;" crossorigin="anonymous" alt="GAC" />

  <div style="position:absolute;top:12%;left:0;right:0;height:26%;display:flex;align-items:center;justify-content:center;">
    <div style="width:${mainIconSize + 100}px;height:${mainIconSize + 100}px;background:rgba(255,255,255,0.12);border:4px solid rgba(255,255,255,0.28);border-radius:50%;display:flex;align-items:center;justify-content:center;">
      <div style="color:${colors.accent};">${renderIcon(input.main_icon, mainIconSize, colors.accent)}</div>
    </div>
  </div>

  <div style="position:absolute;top:42%;left:0;right:0;bottom:${T.margin + 30}px;padding:0 ${T.margin}px;display:flex;flex-direction:column;justify-content:flex-start;">
    <div style="width:96px;height:8px;background:${colors.accent};margin:0 auto ${T.paragraphGap}px;"></div>
    <h1 style="font-size:${titleSize}px;font-weight:900;color:#fff;margin:0 0 ${T.paragraphGap + 8}px;line-height:1.15;text-align:center;">${input.headline}</h1>
    ${paragraphs.length > 0 ? `<div style="max-width:1050px;margin:0 auto ${T.paragraphGap + 12}px;display:flex;flex-direction:column;gap:${T.paragraphGap}px;text-align:center;">
      ${paragraphs.map(p => `<p style="font-size:${T.subtitleSize}px;color:#fff;margin:0;line-height:${T.lineHeight};font-weight:500;opacity:0.95;">${p}</p>`).join("")}
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

  const paragraphs = splitIntoParagraphs(input.subtitle || "");
  const email = (input as any).contact_email;
  const phone = (input as any).contact_phone;

  const dateTimeLocationChips = [
    input.date && renderMetaChip("calendar", input.date, colors, T.metaFont),
    input.time && renderMetaChip("clock", input.time, colors, T.metaFont),
    input.location && renderMetaChip("map-pin", input.location, colors, T.metaFont),
  ].filter(Boolean).join("");

  const contactChips = [
    email && renderContactChip("mail", email, colors, T.metaFont, true),
    phone && renderContactChip("phone", phone, colors, T.metaFont, false),
  ].filter(Boolean).join("");

  return `
<div class="poster typography-layout" style="width:${width}px;height:${height}px;position:relative;overflow:hidden;font-family:'Cairo','Tajawal',sans-serif;direction:rtl;color:#fff;background-image:url('${BG_STATS_HERO_DATA_URI}');background-size:cover;background-position:center;">
  ${renderDeptTag(input.department, colors, input.size)}
  <img src="${logoSrc}" style="position:absolute;top:${T.margin}px;right:${T.margin}px;height:${T.logoHeight}px;z-index:10;" crossorigin="anonymous" alt="GAC" />

  <!-- Centered content, text only — no accent bar, sits below header safe zone -->
  <div style="position:absolute;top:${isSquare ? "52%" : "50%"};left:50%;transform:translate(-50%,-50%);width:100%;padding:0 ${contentPadding}px;text-align:center;">
    <h1 style="font-size:${isSquare ? 84 : titleSize}px;font-weight:900;margin:0 0 ${T.paragraphGap + 16}px;line-height:1.15;letter-spacing:-2px;color:#fff;">${input.headline}</h1>
    ${paragraphs.length > 0 ? `<div style="max-width:${contentMaxWidth}px;margin:0 auto ${T.paragraphGap + 20}px;display:flex;flex-direction:column;gap:${T.paragraphGap}px;">
      ${paragraphs.map(p => `<p style="font-size:${subtitleSize}px;margin:0;line-height:${T.lineHeight};font-weight:500;color:#fff;opacity:0.95;">${p}</p>`).join("")}
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
