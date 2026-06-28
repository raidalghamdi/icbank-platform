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

/** ============ LAYOUT 0: STATS HERO (المرجعي الرسمي) ============
 *  - شارة إدارة أعلى يمين + شعار GAC أعلى يسار
 *  - عنوان رئيسي ضخم + فرعي بلون مميز
 *  - 3 إحصائيات بأيقونات + خطوط فاصلة
 *  - هاشتاج أسفل يسار + زخرفة معينات في الخلفية
 */
function statsHeroLayout(input: IconEventInput): string {
  const colors = COLOR_MAP[input.color_scheme] || COLOR_MAP.teal;
  const { width, height } = SIZE_MAP[input.size];
  const isStory = input.size === "story";
  const isSquare = input.size === "square";
  const isLandscape = input.size === "landscape";

  // ضبط الأحجام حسب المقاس
  const titleSize = isStory ? 130 : isLandscape ? 140 : 110;
  const subtitleSize = isStory ? 56 : isLandscape ? 56 : 48;
  const statValueSize = isStory ? 96 : isLandscape ? 100 : 90;
  const statLabelSize = isStory ? 30 : isLandscape ? 28 : 28;
  const statIconSize = isStory ? 100 : isLandscape ? 110 : 100;
  const logoHeight = isStory ? 110 : isLandscape ? 95 : 90;
  const deptBadgeFont = isStory ? 26 : isLandscape ? 22 : 22;
  const hashtagSize = isStory ? 38 : isLandscape ? 34 : 32;

  // ضبط هوامش وزخرفة
  const padding = isStory ? 80 : isLandscape ? 70 : 70;
  const statsTopOffset = isStory ? "58%" : isLandscape ? "54%" : "55%";

  // الإحصائيات: نضمن 3 إحصائيات (نملأ بقيم افتراضية إن نقصت)
  const stats: IconEventStat[] = (input.stats && input.stats.length > 0
    ? input.stats
    : [
        { icon: "users", value: "—", label: "مشاركة" },
        { icon: "building", value: "—", label: "إدارة" },
        { icon: "calendar", value: "—", label: "فعالية" },
      ]
  ).slice(0, 3);
  while (stats.length < 3) {
    stats.push({ icon: "sparkles", value: "—", label: "" });
  }

  return `
<div class="poster stats-hero-layout" style="width:${width}px;height:${height}px;background:${colors.primary};position:relative;overflow:hidden;font-family:'Tajawal','Cairo',sans-serif;direction:rtl;color:#fff;">

  <!-- Diamond Mesh Pattern (decorative — bottom left like reference) -->
  <div style="position:absolute;left:-150px;bottom:-100px;width:900px;height:900px;background-image:url('${diamondMeshPattern("#ffffff", 0.08)}');background-size:600px 600px;background-repeat:no-repeat;"></div>

  <!-- Subtle right-side mesh too -->
  <div style="position:absolute;right:-100px;top:30%;width:500px;height:500px;background-image:url('${diamondMeshPattern("#ffffff", 0.04)}');background-size:600px 600px;background-repeat:no-repeat;"></div>

  <!-- Department Badge (top right, only if provided) -->
  ${
    input.department
      ? `<div style="position:absolute;top:${padding}px;right:${padding}px;background:${colors.accent};color:${colors.secondary};padding:${isStory ? "14px 32px" : "10px 24px"};border-radius:8px;font-weight:800;font-size:${deptBadgeFont}px;max-width:50%;line-height:1.4;">${input.department}</div>`
      : ""
  }

  <!-- GAC Logo (top left) -->
  ${
    input.logo_url
      ? `<img src="${input.logo_url}" style="position:absolute;top:${padding}px;left:${padding}px;height:${logoHeight}px;z-index:5;" crossorigin="anonymous" />`
      : ""
  }

  <!-- Main Title Block (center upper) -->
  <div style="position:absolute;top:${isStory ? "22%" : isLandscape ? "20%" : "22%"};left:0;right:0;text-align:center;padding:0 ${padding}px;">
    <h1 style="font-size:${titleSize}px;font-weight:900;margin:0;line-height:1.05;letter-spacing:-2px;color:#fff;">${input.headline}</h1>
    ${
      input.subtitle
        ? `<h2 style="font-size:${subtitleSize}px;font-weight:800;margin:${isStory ? 30 : 20}px 0 0;line-height:1.2;color:${colors.accent};">${input.subtitle}</h2>`
        : ""
    }
  </div>

  <!-- 3 Stats Row (center lower) -->
  <div style="position:absolute;top:${statsTopOffset};left:0;right:0;display:flex;justify-content:center;align-items:flex-start;gap:0;padding:0 ${padding}px;">
    ${stats
      .map(
        (s, idx) => `
      <div style="flex:1;text-align:center;position:relative;padding:0 ${isStory ? 20 : 30}px;${idx < 2 ? `border-left:2px solid rgba(255,255,255,0.15);` : ""}">
        <!-- Icon -->
        <div style="display:flex;justify-content:center;margin-bottom:${isStory ? 20 : 16}px;color:${colors.accent};">
          ${renderIcon(s.icon, statIconSize, colors.accent)}
        </div>
        <!-- Separator line -->
        <div style="width:${isStory ? 180 : 200}px;height:2px;background:${colors.accent};margin:0 auto ${isStory ? 18 : 14}px;"></div>
        <!-- Value -->
        <div style="font-size:${statValueSize}px;font-weight:900;line-height:1;color:#fff;letter-spacing:-2px;">${s.value}</div>
        <!-- Label -->
        ${s.label ? `<div style="font-size:${statLabelSize}px;font-weight:600;margin-top:${isStory ? 14 : 10}px;color:#fff;line-height:1.3;opacity:0.95;">${s.label}</div>` : ""}
      </div>
    `
      )
      .join("")}
  </div>

  <!-- Hashtag (bottom left) -->
  ${
    input.hashtag
      ? `<div style="position:absolute;bottom:${padding}px;left:${padding}px;color:${colors.accent};font-size:${hashtagSize}px;font-weight:800;letter-spacing:0.5px;">${input.hashtag.startsWith("#") ? input.hashtag : "#" + input.hashtag}</div>`
      : ""
  }

  <!-- Optional Date/Time/Location strip (bottom right, only if present) -->
  ${
    input.date || input.time || input.location
      ? `<div style="position:absolute;bottom:${padding}px;right:${padding}px;display:flex;gap:${isStory ? 24 : 20}px;align-items:center;color:#fff;font-size:${isStory ? 24 : 20}px;font-weight:600;opacity:0.9;">
      ${
        input.date
          ? `<div style="display:flex;align-items:center;gap:8px;">${renderIcon("calendar", isStory ? 26 : 22, colors.accent)}<span>${input.date}</span></div>`
          : ""
      }
      ${
        input.time
          ? `<div style="display:flex;align-items:center;gap:8px;">${renderIcon("clock", isStory ? 26 : 22, colors.accent)}<span>${input.time}</span></div>`
          : ""
      }
      ${
        input.location
          ? `<div style="display:flex;align-items:center;gap:8px;">${renderIcon("map-pin", isStory ? 26 : 22, colors.accent)}<span>${input.location}</span></div>`
          : ""
      }
    </div>`
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
<div class="poster hero-layout" style="width:${width}px;height:${height}px;background:linear-gradient(135deg,${colors.primary} 0%,${colors.secondary} 100%);position:relative;overflow:hidden;font-family:'Tajawal','Cairo',sans-serif;direction:rtl;color:#fff;">
  <div style="position:absolute;inset:0;background-image:url('${backgroundPattern(input.main_icon, "rgba(255,255,255,0.06)")}');background-size:160px 160px;opacity:0.4;"></div>
  ${input.department ? `<div style="position:absolute;top:48px;right:48px;background:${colors.accent};color:${colors.secondary};padding:10px 22px;border-radius:8px;font-weight:800;font-size:20px;">${input.department}</div>` : ""}
  ${input.logo_url ? `<img src="${input.logo_url}" style="position:absolute;top:48px;left:48px;height:${isStory ? 90 : 70}px;z-index:5;" crossorigin="anonymous" />` : ""}
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
  const supporting = (input.supporting_icons || []).slice(0, 3);
  const gridIcons = [input.main_icon, ...supporting].slice(0, 4);
  while (gridIcons.length < 4) gridIcons.push("sparkles");
  const titleSize = isStory ? 80 : isLandscape ? 64 : 64;
  const iconBoxSize = isStory ? 220 : isLandscape ? 200 : 220;
  const iconSize = iconBoxSize * 0.55;

  return `
<div class="poster grid-layout" style="width:${width}px;height:${height}px;background:#fff;position:relative;overflow:hidden;font-family:'Tajawal','Cairo',sans-serif;direction:rtl;">
  <div style="position:absolute;top:0;left:0;right:0;height:${isStory ? 280 : isLandscape ? 200 : 240}px;background:linear-gradient(135deg,${colors.primary} 0%,${colors.secondary} 100%);"></div>
  ${input.department ? `<div style="position:absolute;top:${isStory ? 100 : 70}px;left:48px;background:${colors.accent};color:${colors.secondary};padding:10px 22px;border-radius:8px;font-weight:800;font-size:20px;z-index:6;">${input.department}</div>` : ""}
  ${input.logo_url ? `<img src="${input.logo_url}" style="position:absolute;top:48px;right:48px;height:${isStory ? 80 : 70}px;z-index:5;" crossorigin="anonymous" />` : ""}
  <div style="position:absolute;top:${isStory ? "100px" : "70px"};right:${input.logo_url ? "200px" : "60px"};left:60px;color:#fff;text-align:right;">
    <h1 style="font-size:${titleSize}px;font-weight:900;margin:0;line-height:1.15;letter-spacing:-1px;">${input.headline}</h1>
    ${input.subtitle ? `<p style="font-size:${isStory ? 34 : 28}px;margin:18px 0 0;opacity:0.95;font-weight:500;">${input.subtitle}</p>` : ""}
  </div>
  <div style="position:absolute;top:${isStory ? "44%" : isLandscape ? "42%" : "40%"};left:50%;transform:translateX(-50%);display:grid;grid-template-columns:repeat(2,${iconBoxSize}px);gap:${isStory ? 40 : 32}px;">
    ${gridIcons
      .map(
        (icn, idx) => `
      <div style="width:${iconBoxSize}px;height:${iconBoxSize}px;background:${idx % 2 === 0 ? colors.primary : colors.accent};border-radius:28px;display:flex;align-items:center;justify-content:center;box-shadow:0 12px 30px rgba(0,0,0,0.12);">
        <div style="color:#fff;">${renderIcon(icn, iconSize, "#fff")}</div>
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
        `<div style="display:flex;align-items:center;gap:12px;color:${colors.secondary};font-weight:700;font-size:${isStory ? 30 : 24}px;">
        ${renderIcon(m.icon, isStory ? 36 : 30, colors.primary)}
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
    return `
<div class="poster split-layout" style="width:${width}px;height:${height}px;background:#fff;position:relative;overflow:hidden;font-family:'Tajawal','Cairo',sans-serif;direction:rtl;display:flex;">
  <div style="width:50%;height:100%;background:linear-gradient(160deg,${colors.primary} 0%,${colors.secondary} 100%);position:relative;display:flex;align-items:center;justify-content:center;">
    <div style="position:absolute;inset:0;background-image:url('${backgroundPattern(input.main_icon, "rgba(255,255,255,0.08)")}');background-size:140px 140px;"></div>
    <div style="position:relative;width:${mainIconSize + 100}px;height:${mainIconSize + 100}px;background:rgba(255,255,255,0.15);border:4px solid rgba(255,255,255,0.3);border-radius:50%;display:flex;align-items:center;justify-content:center;">
      <div style="color:#fff;">${renderIcon(input.main_icon, mainIconSize, "#fff")}</div>
    </div>
    ${supportingRow.length > 0 ? `<div style="position:absolute;bottom:80px;left:0;right:0;display:flex;justify-content:center;gap:30px;">${supportingRow.map((s) => `<div style="width:80px;height:80px;background:rgba(255,255,255,0.2);border-radius:20px;display:flex;align-items:center;justify-content:center;"><div style="color:#fff;">${renderIcon(s, 48, "#fff")}</div></div>`).join("")}</div>` : ""}
  </div>
  <div style="width:50%;height:100%;padding:80px 60px;display:flex;flex-direction:column;justify-content:center;">
    ${input.logo_url ? `<img src="${input.logo_url}" style="height:${isLandscape ? 70 : 80}px;margin-bottom:40px;align-self:flex-start;" crossorigin="anonymous" />` : ""}
    ${input.department ? `<div style="background:${colors.primary};color:#fff;padding:8px 18px;border-radius:6px;font-weight:700;font-size:18px;align-self:flex-start;margin-bottom:24px;">${input.department}</div>` : ""}
    <div style="width:80px;height:6px;background:${colors.primary};border-radius:3px;margin-bottom:30px;"></div>
    <h1 style="font-size:${titleSize}px;font-weight:900;color:${colors.secondary};margin:0 0 24px;line-height:1.15;letter-spacing:-1px;">${input.headline}</h1>
    ${input.subtitle ? `<p style="font-size:${isLandscape ? 30 : 26}px;color:${GAC.primary.coolGray};margin:0 0 50px;line-height:1.5;">${input.subtitle}</p>` : ""}
    <div style="display:flex;flex-direction:column;gap:20px;">
      ${metaItems.map((m) => `<div style="display:flex;align-items:center;gap:16px;color:${colors.secondary};font-weight:700;font-size:${isLandscape ? 26 : 24}px;"><div style="width:50px;height:50px;background:${colors.primary};border-radius:14px;display:flex;align-items:center;justify-content:center;color:#fff;">${renderIcon(m.icon, 28, "#fff")}</div><span>${m.text}</span></div>`).join("")}
    </div>
  </div>
  <div style="position:absolute;bottom:0;left:0;right:0;height:12px;background:linear-gradient(90deg,${colors.accent} 0%,${colors.secondary} 50%,${colors.primary} 100%);"></div>
</div>`.trim();
  } else {
    return `
<div class="poster split-layout" style="width:${width}px;height:${height}px;background:#fff;position:relative;overflow:hidden;font-family:'Tajawal','Cairo',sans-serif;direction:rtl;display:flex;flex-direction:column;">
  <div style="width:100%;height:55%;background:linear-gradient(160deg,${colors.primary} 0%,${colors.secondary} 100%);position:relative;display:flex;align-items:center;justify-content:center;">
    <div style="position:absolute;inset:0;background-image:url('${backgroundPattern(input.main_icon, "rgba(255,255,255,0.08)")}');background-size:140px 140px;"></div>
    ${input.logo_url ? `<img src="${input.logo_url}" style="position:absolute;top:60px;left:60px;height:80px;z-index:5;" crossorigin="anonymous" />` : ""}
    ${input.department ? `<div style="position:absolute;top:60px;right:60px;background:${colors.accent};color:${colors.secondary};padding:10px 22px;border-radius:8px;font-weight:800;font-size:22px;">${input.department}</div>` : ""}
    <div style="position:relative;width:${mainIconSize + 100}px;height:${mainIconSize + 100}px;background:rgba(255,255,255,0.15);border:4px solid rgba(255,255,255,0.3);border-radius:50%;display:flex;align-items:center;justify-content:center;">
      <div style="color:#fff;">${renderIcon(input.main_icon, mainIconSize, "#fff")}</div>
    </div>
  </div>
  <div style="width:100%;height:45%;padding:60px;display:flex;flex-direction:column;justify-content:center;">
    <div style="width:80px;height:6px;background:${colors.primary};border-radius:3px;margin-bottom:30px;align-self:center;"></div>
    <h1 style="font-size:${titleSize}px;font-weight:900;color:${colors.secondary};margin:0 0 20px;line-height:1.15;text-align:center;">${input.headline}</h1>
    ${input.subtitle ? `<p style="font-size:30px;color:${GAC.primary.coolGray};margin:0 0 36px;line-height:1.5;text-align:center;">${input.subtitle}</p>` : ""}
    <div style="display:flex;justify-content:center;gap:40px;flex-wrap:wrap;">
      ${metaItems.map((m) => `<div style="display:flex;align-items:center;gap:12px;color:${colors.secondary};font-weight:700;font-size:26px;">${renderIcon(m.icon, 30, colors.primary)}<span>${m.text}</span></div>`).join("")}
    </div>
  </div>
  <div style="position:absolute;bottom:0;left:0;right:0;height:12px;background:linear-gradient(90deg,${colors.accent} 0%,${colors.secondary} 50%,${colors.primary} 100%);"></div>
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
