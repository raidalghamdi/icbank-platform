/**
 * Icon Event Composer
 * توليد HTML/CSS لتصاميم الفعاليات باستخدام أيقونات فقط (بدون صور)
 *
 * المعمارية:
 * - 3 تخطيطات: Hero (أيقونة كبيرة مركزية) / Grid (شبكة أيقونات) / Split (تقسيم)
 * - 3 مقاسات: Square (1080×1080) / Story (1080×1920) / Landscape (1920×1080)
 * - متوافق مع هوية GAC بالكامل (الألوان من palette، الشعار من brand-assets)
 */

import { GAC } from "./gac-palette";
import { renderIcon } from "./icon-library";

export type LayoutType = "hero" | "grid" | "split";
export type SizePreset = "square" | "story" | "landscape";
export type ColorScheme = "blue" | "green" | "cyan" | "navy";

export interface IconEventInput {
  /** عنوان الفعالية */
  headline: string;
  /** وصف موجز (سطر واحد) */
  subtitle?: string;
  /** التاريخ بصيغة مقروءة */
  date?: string;
  /** الوقت */
  time?: string;
  /** المكان */
  location?: string;
  /** الأيقونة الرئيسية */
  main_icon: string;
  /** أيقونات داعمة (3 كحد أقصى) */
  supporting_icons?: string[];
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
  blue: {
    primary: GAC.primary.blue, // 0069A7
    secondary: GAC.primary.darkBlue, // 00567D
    accent: GAC.secondary.cyan, // 46BCCD
    bgPattern: GAC.primary.blue,
  },
  green: {
    primary: GAC.primary.green, // 61A60E
    secondary: GAC.secondary.green, // 009845
    accent: GAC.secondary.lightGreen, // 9DC41A
    bgPattern: GAC.primary.green,
  },
  cyan: {
    primary: GAC.secondary.cyan, // 46BCCD
    secondary: GAC.primary.blue, // 0069A7
    accent: GAC.primary.darkBlue, // 00567D
    bgPattern: GAC.secondary.cyan,
  },
  navy: {
    primary: GAC.secondary.navy, // 194F90
    secondary: GAC.primary.darkBlue, // 00567D
    accent: GAC.secondary.cyan, // 46BCCD
    bgPattern: GAC.secondary.navy,
  },
};

/** خلفية مزخرفة بالأيقونات (متكررة بشفافية) */
function backgroundPattern(iconName: string, color: string): string {
  const icon = renderIcon(iconName, 60, color);
  if (!icon) return "";
  const encoded = encodeURIComponent(icon).replace(/'/g, "%27");
  return `data:image/svg+xml;utf8,${encoded}`;
}

/** ============ LAYOUT 1: HERO ============
 *  أيقونة كبيرة مركزية + عنوان + معلومات أسفل
 */
function heroLayout(input: IconEventInput): string {
  const colors = COLOR_MAP[input.color_scheme];
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
  <!-- Background Pattern -->
  <div style="position:absolute;inset:0;background-image:url('${backgroundPattern(input.main_icon, "rgba(255,255,255,0.06)")}');background-size:160px 160px;opacity:0.4;"></div>

  <!-- Logo (top) -->
  ${input.logo_url ? `<img src="${input.logo_url}" style="position:absolute;top:48px;${isStory ? "left:48px" : "left:48px"};height:${isStory ? 90 : 70}px;z-index:5;" />` : ""}

  <!-- Main Icon -->
  <div style="position:absolute;top:${isStory ? "26%" : "22%"};left:50%;transform:translateX(-50%);width:${mainIconSize + 80}px;height:${mainIconSize + 80}px;background:rgba(255,255,255,0.12);border:3px solid rgba(255,255,255,0.25);border-radius:50%;display:flex;align-items:center;justify-content:center;backdrop-filter:blur(10px);">
    <div style="color:#fff;">${renderIcon(input.main_icon, mainIconSize, "#fff")}</div>
  </div>

  <!-- Title block -->
  <div style="position:absolute;top:${isStory ? "55%" : "52%"};left:0;right:0;text-align:center;padding:0 80px;">
    <h1 style="font-size:${titleSize}px;font-weight:900;margin:0 0 24px;line-height:1.2;letter-spacing:-1px;">${input.headline}</h1>
    ${input.subtitle ? `<p style="font-size:${subtitleSize}px;margin:0 0 40px;opacity:0.92;font-weight:500;">${input.subtitle}</p>` : ""}
  </div>

  <!-- Meta info -->
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

  <!-- Bottom accent bar -->
  <div style="position:absolute;bottom:0;left:0;right:0;height:12px;background:linear-gradient(90deg,${colors.accent} 0%,${colors.secondary} 50%,${colors.primary} 100%);"></div>
</div>
  `.trim();
}

/** ============ LAYOUT 2: GRID ============
 *  4 أيقونات في شبكة + عنوان جانبي
 */
function gridLayout(input: IconEventInput): string {
  const colors = COLOR_MAP[input.color_scheme];
  const { width, height } = SIZE_MAP[input.size];
  const isStory = input.size === "story";
  const isLandscape = input.size === "landscape";

  // 4 أيقونات (1 رئيسية + 3 داعمة، أو 4 داعمة)
  const supporting = (input.supporting_icons || []).slice(0, 3);
  const gridIcons = [input.main_icon, ...supporting].slice(0, 4);
  while (gridIcons.length < 4) gridIcons.push("sparkles");

  const titleSize = isStory ? 80 : isLandscape ? 64 : 64;
  const iconBoxSize = isStory ? 220 : isLandscape ? 200 : 220;
  const iconSize = iconBoxSize * 0.55;

  return `
<div class="poster grid-layout" style="width:${width}px;height:${height}px;background:#fff;position:relative;overflow:hidden;font-family:'Tajawal','Cairo',sans-serif;direction:rtl;">

  <!-- Top color band -->
  <div style="position:absolute;top:0;left:0;right:0;height:${isStory ? 280 : isLandscape ? 200 : 240}px;background:linear-gradient(135deg,${colors.primary} 0%,${colors.secondary} 100%);"></div>

  <!-- Logo -->
  ${input.logo_url ? `<img src="${input.logo_url}" style="position:absolute;top:48px;right:48px;height:${isStory ? 80 : 70}px;z-index:5;" />` : ""}

  <!-- Title (on top band) -->
  <div style="position:absolute;top:${isStory ? "100px" : "70px"};left:60px;right:${input.logo_url ? "200px" : "60px"};color:#fff;">
    <h1 style="font-size:${titleSize}px;font-weight:900;margin:0;line-height:1.15;letter-spacing:-1px;">${input.headline}</h1>
    ${input.subtitle ? `<p style="font-size:${isStory ? 34 : 28}px;margin:18px 0 0;opacity:0.95;font-weight:500;">${input.subtitle}</p>` : ""}
  </div>

  <!-- Icons grid (2x2) -->
  <div style="position:absolute;top:${isStory ? "44%" : isLandscape ? "42%" : "40%"};left:50%;transform:translateX(-50%);display:grid;grid-template-columns:repeat(2,${iconBoxSize}px);gap:${isStory ? 40 : 32}px;">
    ${gridIcons
      .map(
        (icn, idx) => `
      <div style="width:${iconBoxSize}px;height:${iconBoxSize}px;background:${idx % 2 === 0 ? colors.primary : colors.accent};border-radius:28px;display:flex;align-items:center;justify-content:center;box-shadow:0 12px 30px rgba(0,0,0,0.12);">
        <div style="color:#fff;">${renderIcon(icn, iconSize, "#fff")}</div>
      </div>
    `
      )
      .join("")}
  </div>

  <!-- Meta strip -->
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

  <!-- Bottom accent -->
  <div style="position:absolute;bottom:0;left:0;right:0;height:12px;background:linear-gradient(90deg,${colors.accent} 0%,${colors.secondary} 50%,${colors.primary} 100%);"></div>
</div>
  `.trim();
}

/** ============ LAYOUT 3: SPLIT ============
 *  تقسيم: نصف للأيقونة الكبيرة على خلفية ملونة + نصف للنص على خلفية بيضاء
 */
function splitLayout(input: IconEventInput): string {
  const colors = COLOR_MAP[input.color_scheme];
  const { width, height } = SIZE_MAP[input.size];
  const isStory = input.size === "story";
  const isLandscape = input.size === "landscape";

  // ستوري = تقسيم أفقي (أعلى/أسفل)، عريض/مربع = تقسيم عمودي
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
    // عمودي: يمين أيقونة، يسار نص (لأن RTL)
    return `
<div class="poster split-layout" style="width:${width}px;height:${height}px;background:#fff;position:relative;overflow:hidden;font-family:'Tajawal','Cairo',sans-serif;direction:rtl;display:flex;">

  <!-- Right (colored) - main icon -->
  <div style="width:50%;height:100%;background:linear-gradient(160deg,${colors.primary} 0%,${colors.secondary} 100%);position:relative;display:flex;align-items:center;justify-content:center;">
    <!-- Pattern -->
    <div style="position:absolute;inset:0;background-image:url('${backgroundPattern(input.main_icon, "rgba(255,255,255,0.08)")}');background-size:140px 140px;"></div>
    <!-- Main icon circle -->
    <div style="position:relative;width:${mainIconSize + 100}px;height:${mainIconSize + 100}px;background:rgba(255,255,255,0.15);border:4px solid rgba(255,255,255,0.3);border-radius:50%;display:flex;align-items:center;justify-content:center;">
      <div style="color:#fff;">${renderIcon(input.main_icon, mainIconSize, "#fff")}</div>
    </div>
    <!-- Supporting icons -->
    ${
      supportingRow.length > 0
        ? `<div style="position:absolute;bottom:80px;left:0;right:0;display:flex;justify-content:center;gap:30px;">
        ${supportingRow.map((s) => `<div style="width:80px;height:80px;background:rgba(255,255,255,0.2);border-radius:20px;display:flex;align-items:center;justify-content:center;"><div style="color:#fff;">${renderIcon(s, 48, "#fff")}</div></div>`).join("")}
      </div>`
        : ""
    }
  </div>

  <!-- Left (white) - text -->
  <div style="width:50%;height:100%;padding:80px 60px;display:flex;flex-direction:column;justify-content:center;">
    <!-- Logo -->
    ${input.logo_url ? `<img src="${input.logo_url}" style="height:${isLandscape ? 70 : 80}px;margin-bottom:40px;align-self:flex-start;" />` : ""}

    <!-- Accent bar -->
    <div style="width:80px;height:6px;background:${colors.primary};border-radius:3px;margin-bottom:30px;"></div>

    <h1 style="font-size:${titleSize}px;font-weight:900;color:${colors.secondary};margin:0 0 24px;line-height:1.15;letter-spacing:-1px;">${input.headline}</h1>
    ${input.subtitle ? `<p style="font-size:${isLandscape ? 30 : 26}px;color:${GAC.primary.coolGray};margin:0 0 50px;line-height:1.5;">${input.subtitle}</p>` : ""}

    <!-- Meta -->
    <div style="display:flex;flex-direction:column;gap:20px;">
      ${metaItems
        .map(
          (m) => `<div style="display:flex;align-items:center;gap:16px;color:${colors.secondary};font-weight:700;font-size:${isLandscape ? 26 : 24}px;">
        <div style="width:50px;height:50px;background:${colors.primary};border-radius:14px;display:flex;align-items:center;justify-content:center;color:#fff;">${renderIcon(m.icon, 28, "#fff")}</div>
        <span>${m.text}</span>
      </div>`
        )
        .join("")}
    </div>
  </div>

  <!-- Bottom accent (across full width) -->
  <div style="position:absolute;bottom:0;left:0;right:0;height:12px;background:linear-gradient(90deg,${colors.accent} 0%,${colors.secondary} 50%,${colors.primary} 100%);"></div>
</div>
    `.trim();
  } else {
    // ستوري: تقسيم أفقي
    return `
<div class="poster split-layout" style="width:${width}px;height:${height}px;background:#fff;position:relative;overflow:hidden;font-family:'Tajawal','Cairo',sans-serif;direction:rtl;display:flex;flex-direction:column;">

  <!-- Top (colored) -->
  <div style="width:100%;height:55%;background:linear-gradient(160deg,${colors.primary} 0%,${colors.secondary} 100%);position:relative;display:flex;align-items:center;justify-content:center;">
    <div style="position:absolute;inset:0;background-image:url('${backgroundPattern(input.main_icon, "rgba(255,255,255,0.08)")}');background-size:140px 140px;"></div>
    ${input.logo_url ? `<img src="${input.logo_url}" style="position:absolute;top:60px;left:60px;height:80px;z-index:5;" />` : ""}
    <div style="position:relative;width:${mainIconSize + 100}px;height:${mainIconSize + 100}px;background:rgba(255,255,255,0.15);border:4px solid rgba(255,255,255,0.3);border-radius:50%;display:flex;align-items:center;justify-content:center;">
      <div style="color:#fff;">${renderIcon(input.main_icon, mainIconSize, "#fff")}</div>
    </div>
  </div>

  <!-- Bottom (white) -->
  <div style="width:100%;height:45%;padding:60px;display:flex;flex-direction:column;justify-content:center;">
    <div style="width:80px;height:6px;background:${colors.primary};border-radius:3px;margin-bottom:30px;"></div>
    <h1 style="font-size:${titleSize}px;font-weight:900;color:${colors.secondary};margin:0 0 20px;line-height:1.15;text-align:center;">${input.headline}</h1>
    ${input.subtitle ? `<p style="font-size:30px;color:${GAC.primary.coolGray};margin:0 0 36px;line-height:1.5;text-align:center;">${input.subtitle}</p>` : ""}
    <div style="display:flex;justify-content:center;gap:40px;flex-wrap:wrap;">
      ${metaItems
        .map(
          (m) => `<div style="display:flex;align-items:center;gap:12px;color:${colors.secondary};font-weight:700;font-size:26px;">
        ${renderIcon(m.icon, 30, colors.primary)}<span>${m.text}</span>
      </div>`
        )
        .join("")}
    </div>
  </div>

  <div style="position:absolute;bottom:0;left:0;right:0;height:12px;background:linear-gradient(90deg,${colors.accent} 0%,${colors.secondary} 50%,${colors.primary} 100%);"></div>
</div>
    `.trim();
  }
}

/** المُرسل الرئيسي */
export function renderIconEventDesign(input: IconEventInput): string {
  const inner = (() => {
    switch (input.layout) {
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
