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

export type LayoutType = "stats-hero" | "hero" | "grid" | "split";
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

const SIZE_MAP: Record<SizePreset, { width: number; height: number; aspectLabel: string }> = {
  square: { width: 1080, height: 1080, aspectLabel: "1:1" },
  story: { width: 1080, height: 1920, aspectLabel: "9:16" },
  landscape: { width: 1920, height: 1080, aspectLabel: "16:9" },
};

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
      ? `<div style="position:absolute;top:${T.deptTop}px;left:${T.deptLeft}px;background:${ACCENT};color:${BADGE_TEXT};padding:${T.deptPadding};border-radius:8px;font-weight:800;font-size:${T.deptFont}px;line-height:1.1;white-space:nowrap;box-shadow:0 1px 2px rgba(0,0,0,0.05);">${input.department}</div>`
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
  const isStory = input.size === "story";
  const isSquare = input.size === "square";
  const logoSrc = input.logo_url && input.logo_url.startsWith("http") ? input.logo_url : GAC_LOGO_WHITE_DATA_URI;
  const mainIconSize = isStory ? 280 : isSquare ? 240 : 200;
  const titleSize = isStory ? 88 : isSquare ? 72 : 64;
  const subtitleSize = isStory ? 36 : isSquare ? 32 : 28;
  const metaSize = isStory ? 30 : 26;

  const metaItems = [
    input.date && { icon: "calendar", text: input.date },
    input.time && { icon: "clock", text: input.time },
    input.location && { icon: "map-pin", text: input.location },
  ].filter(Boolean) as { icon: string; text: string }[];

  return `
<div class="poster hero-layout" style="width:${width}px;height:${height}px;position:relative;overflow:hidden;font-family:'Cairo','Tajawal',sans-serif;direction:rtl;color:#fff;background-image:url('${BG_STATS_HERO_DATA_URI}');background-size:cover;background-position:center;">
  ${input.department ? `<div style="position:absolute;top:48px;left:48px;background:${colors.accent};color:${colors.secondary};padding:10px 22px;border-radius:8px;font-weight:800;font-size:20px;">${input.department}</div>` : ""}
  <img src="${logoSrc}" style="position:absolute;top:48px;right:48px;height:${isStory ? 90 : 70}px;z-index:5;" crossorigin="anonymous" alt="GAC" />
  <div style="position:absolute;top:${isStory ? "26%" : "22%"};left:50%;transform:translateX(-50%);width:${mainIconSize + 80}px;height:${mainIconSize + 80}px;background:rgba(255,255,255,0.12);border:3px solid rgba(255,255,255,0.25);border-radius:50%;display:flex;align-items:center;justify-content:center;backdrop-filter:blur(10px);">
    <div style="color:#fff;">${renderIcon(input.main_icon, mainIconSize, "#fff")}</div>
  </div>
  <div style="position:absolute;top:${isStory ? "55%" : "52%"};left:0;right:0;text-align:center;padding:0 80px;">
    <h1 style="font-size:${titleSize}px;font-weight:900;margin:0 0 24px;line-height:1.2;letter-spacing:-1px;">${input.headline}</h1>
    ${input.subtitle ? `<p style="font-size:${subtitleSize}px;margin:0 0 40px;opacity:0.92;font-weight:500;">${input.subtitle}</p>` : ""}
  </div>
  ${
    metaItems.length > 0
      ? `<div style="position:absolute;bottom:${isStory ? "12%" : "10%"};left:0;right:0;display:flex;justify-content:center;gap:48px;flex-wrap:wrap;padding:0 60px;">
      ${metaItems
        .map(
          (m) => `<div style="display:flex;align-items:center;gap:14px;background:rgba(255,255,255,0.15);padding:18px 28px;border-radius:50px;backdrop-filter:blur(10px);">
        <span style="color:${colors.accent};">${renderIcon(m.icon, 32, colors.accent)}</span>
        <span style="font-size:${metaSize}px;font-weight:600;">${m.text}</span>
      </div>`
        )
        .join("")}
    </div>`
      : ""
  }
  <div style="position:absolute;bottom:0;left:0;right:0;height:12px;background:linear-gradient(90deg,${colors.accent} 0%,${colors.secondary} 50%,${colors.primary} 100%);"></div>
</div>
  `.trim();
}

/** ============ LAYOUT 2: GRID ============ */
function gridLayout(input: IconEventInput): string {
  const colors = COLOR_MAP[input.color_scheme] || COLOR_MAP.blue;
  const { width, height } = SIZE_MAP[input.size];
  const isStory = input.size === "story";
  const isLandscape = input.size === "landscape";
  const logoSrc = input.logo_url && input.logo_url.startsWith("http") ? input.logo_url : GAC_LOGO_WHITE_DATA_URI;
  const supporting = (input.supporting_icons || []).slice(0, 3);
  const gridIcons = [input.main_icon, ...supporting].slice(0, 4);
  while (gridIcons.length < 4) gridIcons.push("sparkles");
  const titleSize = isStory ? 80 : isLandscape ? 64 : 64;
  const iconBoxSize = isStory ? 220 : isLandscape ? 200 : 220;
  const iconSize = iconBoxSize * 0.55;

  return `
<div class="poster grid-layout" style="width:${width}px;height:${height}px;position:relative;overflow:hidden;font-family:'Cairo','Tajawal',sans-serif;direction:rtl;color:#fff;background-image:url('${BG_STATS_HERO_DATA_URI}');background-size:cover;background-position:center;">
  ${input.department ? `<div style="position:absolute;top:48px;left:48px;background:${colors.accent};color:${colors.secondary};padding:10px 22px;border-radius:8px;font-weight:800;font-size:20px;z-index:6;">${input.department}</div>` : ""}
  <img src="${logoSrc}" style="position:absolute;top:48px;right:48px;height:${isStory ? 80 : 70}px;z-index:5;" crossorigin="anonymous" alt="GAC" />
  <div style="position:absolute;top:${isStory ? "200px" : "170px"};right:60px;left:60px;color:#fff;text-align:center;">
    <h1 style="font-size:${titleSize}px;font-weight:900;margin:0;line-height:1.15;letter-spacing:-1px;">${input.headline}</h1>
    ${input.subtitle ? `<p style="font-size:${isStory ? 34 : 28}px;margin:18px 0 0;opacity:0.95;font-weight:500;color:${colors.accent};">${input.subtitle}</p>` : ""}
  </div>
  <div style="position:absolute;top:${isStory ? "48%" : isLandscape ? "46%" : "44%"};left:50%;transform:translateX(-50%);display:grid;grid-template-columns:repeat(2,${iconBoxSize}px);gap:${isStory ? 40 : 32}px;">
    ${gridIcons
      .map(
        (icn) => `
      <div style="width:${iconBoxSize}px;height:${iconBoxSize}px;background:rgba(255,255,255,0.12);border:2px solid rgba(255,255,255,0.25);border-radius:28px;display:flex;align-items:center;justify-content:center;backdrop-filter:blur(10px);">
        <div style="color:${colors.accent};">${renderIcon(icn, iconSize, colors.accent)}</div>
      </div>`
      )
      .join("")}
  </div>
  <div style="position:absolute;bottom:${isStory ? "100px" : "70px"};left:60px;right:60px;display:flex;justify-content:center;gap:${isStory ? 32 : 40}px;flex-wrap:wrap;">
    ${[
      input.date && { icon: "calendar", text: input.date },
      input.time && { icon: "clock", text: input.time },
      input.location && { icon: "map-pin", text: input.location },
    ]
      .filter(Boolean)
      .map((m: any) =>
        `<div style="display:flex;align-items:center;gap:12px;color:#fff;font-weight:700;font-size:${isStory ? 30 : 24}px;">
        ${renderIcon(m.icon, isStory ? 36 : 30, colors.accent)}
        <span>${m.text}</span>
      </div>`
      )
      .join("")}
  </div>
  <div style="position:absolute;bottom:0;left:0;right:0;height:12px;background:linear-gradient(90deg,${colors.accent} 0%,${colors.secondary} 50%,${colors.primary} 100%);"></div>
</div>
  `.trim();
}

/** ============ LAYOUT 3: SPLIT ============ */
function splitLayout(input: IconEventInput): string {
  const colors = COLOR_MAP[input.color_scheme] || COLOR_MAP.blue;
  const { width, height } = SIZE_MAP[input.size];
  const isStory = input.size === "story";
  const isLandscape = input.size === "landscape";
  const logoSrc = input.logo_url && input.logo_url.startsWith("http") ? input.logo_url : GAC_LOGO_WHITE_DATA_URI;
  const splitVertical = !isStory;
  const titleSize = isStory ? 78 : isLandscape ? 70 : 60;
  const mainIconSize = isStory ? 240 : isLandscape ? 280 : 240;
  const metaItems = [
    input.date && { icon: "calendar", text: input.date },
    input.time && { icon: "clock", text: input.time },
    input.location && { icon: "map-pin", text: input.location },
  ].filter(Boolean) as { icon: string; text: string }[];
  const supportingRow = (input.supporting_icons || []).slice(0, 3);

  if (splitVertical) {
    // Unified teal background with BG_STATS_HERO pattern (same as stats-hero baseline)
    return `
<div class="poster split-layout" style="width:${width}px;height:${height}px;position:relative;overflow:hidden;font-family:'Cairo','Tajawal',sans-serif;direction:rtl;color:#fff;background-image:url('${BG_STATS_HERO_DATA_URI}');background-size:cover;background-position:center;display:flex;">
  <!-- Fixed identity: department badge top-left (lime bg) + logo top-right -->
  ${input.department ? `<div style="position:absolute;top:48px;left:48px;background:${colors.accent};color:${colors.secondary};padding:10px 22px;border-radius:8px;font-weight:800;font-size:20px;z-index:10;">${input.department}</div>` : ""}
  <img src="${logoSrc}" style="position:absolute;top:48px;right:48px;height:${isLandscape ? 70 : 80}px;z-index:10;" crossorigin="anonymous" alt="GAC" />

  <!-- Left half: main icon in circle -->
  <div style="width:50%;height:100%;position:relative;display:flex;align-items:center;justify-content:center;">
    <div style="position:relative;width:${mainIconSize + 100}px;height:${mainIconSize + 100}px;background:rgba(255,255,255,0.12);border:4px solid rgba(255,255,255,0.28);border-radius:50%;display:flex;align-items:center;justify-content:center;">
      <div style="color:${colors.accent};">${renderIcon(input.main_icon, mainIconSize, colors.accent)}</div>
    </div>
    ${supportingRow.length > 0 ? `<div style="position:absolute;bottom:80px;left:0;right:0;display:flex;justify-content:center;gap:30px;">${supportingRow.map((s) => `<div style="width:80px;height:80px;background:rgba(255,255,255,0.15);border-radius:20px;display:flex;align-items:center;justify-content:center;"><div style="color:${colors.accent};">${renderIcon(s, 48, colors.accent)}</div></div>`).join("")}</div>` : ""}
  </div>

  <!-- Right half: text content -->
  <div style="width:50%;height:100%;padding:180px 80px 80px;display:flex;flex-direction:column;justify-content:center;">
    <div style="width:80px;height:6px;background:${colors.accent};border-radius:3px;margin-bottom:30px;"></div>
    <h1 style="font-size:${titleSize}px;font-weight:900;color:#fff;margin:0 0 24px;line-height:1.15;letter-spacing:-1px;">${input.headline}</h1>
    ${input.subtitle ? `<p style="font-size:${isLandscape ? 30 : 26}px;color:${colors.accent};margin:0 0 50px;line-height:1.5;font-weight:500;">${input.subtitle}</p>` : ""}
    <div style="display:flex;flex-direction:column;gap:20px;">
      ${metaItems.map((m) => `<div style="display:flex;align-items:center;gap:16px;color:#fff;font-weight:700;font-size:${isLandscape ? 26 : 24}px;"><div style="width:50px;height:50px;background:rgba(255,255,255,0.15);border:2px solid rgba(255,255,255,0.25);border-radius:14px;display:flex;align-items:center;justify-content:center;color:${colors.accent};">${renderIcon(m.icon, 28, colors.accent)}</div><span>${m.text}</span></div>`).join("")}
    </div>
  </div>
</div>`.trim();
  } else {
    // Story (vertical) — unified teal bg, icon top area, text bottom area
    return `
<div class="poster split-layout" style="width:${width}px;height:${height}px;position:relative;overflow:hidden;font-family:'Cairo','Tajawal',sans-serif;direction:rtl;color:#fff;background-image:url('${BG_STATS_HERO_DATA_URI}');background-size:cover;background-position:center;">
  ${input.department ? `<div style="position:absolute;top:60px;left:60px;background:${colors.accent};color:${colors.secondary};padding:10px 22px;border-radius:8px;font-weight:800;font-size:22px;z-index:10;">${input.department}</div>` : ""}
  <img src="${logoSrc}" style="position:absolute;top:60px;right:60px;height:80px;z-index:10;" crossorigin="anonymous" alt="GAC" />

  <div style="position:absolute;top:20%;left:0;right:0;height:40%;display:flex;align-items:center;justify-content:center;">
    <div style="width:${mainIconSize + 100}px;height:${mainIconSize + 100}px;background:rgba(255,255,255,0.12);border:4px solid rgba(255,255,255,0.28);border-radius:50%;display:flex;align-items:center;justify-content:center;">
      <div style="color:${colors.accent};">${renderIcon(input.main_icon, mainIconSize, colors.accent)}</div>
    </div>
  </div>

  <div style="position:absolute;bottom:0;top:65%;left:0;right:0;padding:0 60px;display:flex;flex-direction:column;justify-content:center;">
    <div style="width:80px;height:6px;background:${colors.accent};border-radius:3px;margin-bottom:30px;align-self:center;"></div>
    <h1 style="font-size:${titleSize}px;font-weight:900;color:#fff;margin:0 0 20px;line-height:1.15;text-align:center;">${input.headline}</h1>
    ${input.subtitle ? `<p style="font-size:30px;color:${colors.accent};margin:0 0 36px;line-height:1.5;text-align:center;font-weight:500;">${input.subtitle}</p>` : ""}
    <div style="display:flex;justify-content:center;gap:40px;flex-wrap:wrap;">
      ${metaItems.map((m) => `<div style="display:flex;align-items:center;gap:12px;color:#fff;font-weight:700;font-size:26px;">${renderIcon(m.icon, 30, colors.accent)}<span>${m.text}</span></div>`).join("")}
    </div>
  </div>
</div>`.trim();
  }
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
