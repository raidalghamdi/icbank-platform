/**
 * Shorfah PDF Template — visually identical to the printed sample
 * Sample reference: nshr-shrf-mrs.pdf (March 2026)
 *
 * Design language:
 * - Teal/Cyan palette: #1a6e7a (teal), #0e3b4a (navy), #cce4e6 (mint), #3ec0d0 (cyan), #f0f7f8 (offwhite)
 * - GAC official: #00567D (dark blue), #46BCCD (cyan)
 * - Display font: Tajawal 900 for huge titles, Cairo/Tajawal 700 for body
 * - Each section page has: top nav strip (current highlighted), large title with 3D icon, content
 * - Page themes vary: light/mint cover, light section pages, dark navy pages, full teal pages
 *
 * IMPORTANT: This template targets print via @page A4 portrait. Images are served from
 * /shorfah/*.png (Vercel static).
 */

const TEAL = "#1a6e7a";
const NAVY = "#0e3b4a";
const MINT = "#cce4e6";
const CYAN = "#3ec0d0";
const OFFWHITE = "#f0f7f8";
const TEAL_DARK = "#155a64";
const DEEPNAVY = "#0a2c38";

// Map section type -> 3D icon image file (served from /shorfah/)
export const SECTION_ICON: Record<string, string> = {
  news: "/shorfah/cover-newspaper.png",
  office_interview: "/shorfah/icon-mic.png",
  competition_culture: "/shorfah/icon-monitor.png",
  outside_box: "/shorfah/icon-box-arrow.png",
  events: "/shorfah/icon-bunting.png",
  employee_qa: "/shorfah/icon-speech.png",
};

// Theme per section type (matches sample exactly)
export const SECTION_THEME: Record<string, "light" | "navy" | "teal"> = {
  news: "light",              // light/mint background
  office_interview: "light",  // light with dark navy title strip
  competition_culture: "navy", // dark navy bg
  outside_box: "light",       // light bg with dark navy hero box
  events: "teal",             // full teal bg
  employee_qa: "light",       // light bg
};

export const SECTION_LABEL: Record<string, string> = {
  news: "أخبارنا",
  office_interview: "في مكتبهم",
  competition_culture: "ثقافة المنافسة",
  outside_box: "خارج الصندوق",
  events: "فعالياتنا",
  employee_qa: "عطنا علومك",
};

// Canonical ordered nav (matches printed magazine, right→left in RTL)
export const NAV_ORDER: string[] = [
  "news",
  "office_interview",
  "competition_culture",
  "outside_box",
  "events",
  "employee_qa",
];

export const ARABIC_MONTHS = [
  "يناير","فبراير","مارس","أبريل","مايو","يونيو",
  "يوليو","أغسطس","سبتمبر","أكتوبر","نوفمبر","ديسمبر"
];

/**
 * Lightweight markdown → HTML for content blocks.
 * Supports: headers (## ###), bold, italic, lists, paragraphs, blockquotes.
 */
export function mdToHtml(md: string): string {
  if (!md) return "";
  let h = md
    .replace(/&/g, "&amp;")
    .replace(/</g, "&lt;")
    .replace(/>/g, "&gt;");
  // headers
  h = h
    .replace(/^### (.+)$/gm, "<h4>$1</h4>")
    .replace(/^## (.+)$/gm, "<h3>$1</h3>")
    .replace(/^# (.+)$/gm, "<h2>$1</h2>");
  // bold / italic
  h = h
    .replace(/\*\*(.+?)\*\*/g, "<strong>$1</strong>")
    .replace(/\*(.+?)\*/g, "<em>$1</em>");
  // lists
  h = h.replace(/^- (.+)$/gm, "<li>$1</li>");
  h = h.replace(/(<li>.*?<\/li>(?:\s*<li>.*?<\/li>)*)/gs, "<ul>$1</ul>");
  // blockquotes
  h = h.replace(/^> (.+)$/gm, '<blockquote>$1</blockquote>');
  // paragraphs (split on double newlines)
  const blocks = h.split(/\n{2,}/);
  return blocks
    .map((b) => {
      const t = b.trim();
      if (!t) return "";
      if (/^<(h[2-4]|ul|ol|blockquote|div|figure)/.test(t)) return t;
      return `<p>${t.replace(/\n/g, "<br/>")}</p>`;
    })
    .join("\n");
}

/** Top navigation strip showing all 6 section tabs; active one underlined. */
function navStrip(activeType: string, theme: "light" | "navy" | "teal" = "light"): string {
  const tabs = NAV_ORDER.map((type) => {
    const label = SECTION_LABEL[type];
    const isActive = type === activeType;
    return `<span class="nav-tab${isActive ? " is-active" : ""}">${label}</span>`;
  }).join("");
  return `<nav class="nav-strip nav-${theme}">${tabs}</nav>`;
}

/** Cover page — matches the sample's teal gradient + ribbon date + newspaper art. */
function coverPageHtml(opts: {
  arabicMonth: string;
  year: number;
  subtitle: string;
  motto: string;
}): string {
  const tabs = NAV_ORDER.map(
    (t) => `<span class="cv-tab">${SECTION_LABEL[t]}</span>`
  ).join("");
  return `
  <section class="cover">
    <div class="cover-bg-pattern"></div>
    <!-- Ribbon date (top-right in RTL = right side) -->
    <div class="cv-ribbon">
      <div class="cv-ribbon-year">${opts.year}</div>
      <div class="cv-ribbon-month">${opts.arabicMonth}</div>
    </div>
    <!-- Brand top-left -->
    <div class="cv-brand">
      <div class="cv-brand-ar">الهيئة العامة للمنافسة</div>
      <div class="cv-brand-en">General Authority for Competition</div>
      <div class="cv-brand-mark">
        <svg viewBox="0 0 60 60" xmlns="http://www.w3.org/2000/svg">
          <g fill="#fff">
            <path d="M30 4 L48 18 L48 38 L30 52 L12 38 L12 18 Z" fill="none" stroke="#fff" stroke-width="2.5"/>
            <path d="M22 22 L30 16 L38 22 L38 32 L30 38 L22 32 Z" fill="#fff" opacity="0.95"/>
          </g>
        </svg>
      </div>
    </div>

    <!-- Hero: large title + newspaper artwork -->
    <div class="cv-hero">
      <img class="cv-newspaper" src="/shorfah/cover-newspaper.png" alt=""/>
      <h1 class="cv-title">
        <span class="cv-title-prefix">شرفـــــ</span><span class="cv-title-suffix">ـــــة</span>
      </h1>
    </div>

    <div class="cv-subtitle">${opts.subtitle}</div>

    <!-- Tabs strip with "في هذا العدد" badge on right -->
    <div class="cv-tabs-row">
      <div class="cv-tabs-strip">${tabs}</div>
      <div class="cv-tabs-badge">
        <div>في هذا</div>
        <div>العدد</div>
      </div>
    </div>

    <!-- Motto plate at bottom -->
    <div class="cv-motto">${opts.motto}</div>
  </section>`;
}

/** Default News section (light theme with grid of cards). */
function sectionNewsHtml(opts: {
  titleAr: string;
  contentHtml: string;
  type: string;
}): string {
  return `
  <section class="section section-news theme-light">
    ${navStrip(opts.type, "light")}
    <div class="sec-hero">
      <img class="sec-icon sec-icon-news" src="${SECTION_ICON[opts.type] || "/shorfah/cover-newspaper.png"}" alt=""/>
      <h2 class="sec-title sec-title-news">${opts.titleAr}</h2>
    </div>
    <div class="sec-body sec-body-news">${opts.contentHtml}</div>
  </section>`;
}

/** Office interview — navy title strip + portrait area + 2-column body. */
function sectionOfficeInterviewHtml(opts: {
  titleAr: string;
  descriptionAr?: string | null;
  contentHtml: string;
  type: string;
}): string {
  return `
  <section class="section section-office theme-light">
    ${navStrip(opts.type, "light")}
    <div class="oi-titlebar">${opts.titleAr}</div>
    <div class="oi-grid">
      <div class="oi-left">
        <div class="oi-portrait-placeholder">
          <svg viewBox="0 0 120 160" xmlns="http://www.w3.org/2000/svg">
            <rect width="120" height="160" fill="${MINT}"/>
            <circle cx="60" cy="60" r="28" fill="${TEAL}"/>
            <path d="M20 160 Q60 100 100 160 Z" fill="${TEAL}"/>
          </svg>
        </div>
        ${opts.descriptionAr ? `<div class="oi-caption">${opts.descriptionAr}</div>` : ""}
      </div>
      <div class="oi-right">${opts.contentHtml}</div>
    </div>
  </section>`;
}

/** Competition culture — full dark navy page with big title + infographic-like body. */
function sectionCompetitionCultureHtml(opts: {
  titleAr: string;
  contentHtml: string;
  type: string;
  arabicMonth: string;
  year: number;
}): string {
  return `
  <section class="section section-comp theme-navy">
    ${navStrip(opts.type, "navy")}
    <div class="cc-hero">
      <div class="cc-hero-title-block">
        <h2 class="cc-title">${opts.titleAr}</h2>
        <div class="cc-date">${opts.arabicMonth} ${opts.year}</div>
      </div>
      <div class="cc-hero-callout">
        <div class="cc-hero-callout-text">من منطلق حرص الهيئة على نشر ثقافة المنافسة</div>
      </div>
    </div>
    <div class="cc-body">${opts.contentHtml}</div>
  </section>`;
}

/** Outside-the-box — light bg, navy box for title, portrait + content. */
function sectionOutsideBoxHtml(opts: {
  titleAr: string;
  descriptionAr?: string | null;
  contentHtml: string;
  type: string;
}): string {
  return `
  <section class="section section-outside theme-light">
    ${navStrip(opts.type, "light")}
    <div class="ob-titlebar">
      <div class="ob-title-text">${opts.titleAr}</div>
    </div>
    <div class="ob-grid">
      <div class="ob-portrait">
        <svg viewBox="0 0 120 160" xmlns="http://www.w3.org/2000/svg">
          <rect width="120" height="160" fill="${NAVY}"/>
          <circle cx="60" cy="60" r="28" fill="#fff" opacity="0.95"/>
          <path d="M20 160 Q60 100 100 160 Z" fill="#fff" opacity="0.9"/>
        </svg>
        ${opts.descriptionAr ? `<div class="ob-portrait-caption">${opts.descriptionAr}</div>` : ""}
      </div>
      <div class="ob-icon">
        <img src="${SECTION_ICON.outside_box}" alt=""/>
      </div>
      <div class="ob-body">${opts.contentHtml}</div>
    </div>
  </section>`;
}

/** Events — full teal page with bunting + photo collage placeholder. */
function sectionEventsHtml(opts: {
  titleAr: string;
  contentHtml: string;
  type: string;
}): string {
  return `
  <section class="section section-events theme-teal">
    ${navStrip(opts.type, "teal")}
    <div class="ev-banner">
      <img class="ev-bunting" src="${SECTION_ICON.events}" alt=""/>
      <h2 class="ev-title">${opts.titleAr}</h2>
    </div>
    <div class="ev-collage">${opts.contentHtml || `<div class="ev-empty">سيتم إضافة صور الفعاليات</div>`}</div>
  </section>`;
}

/** Employee Q&A — light bg, teal title plate + speech bubble + Q/A bubbles. */
function sectionEmployeeQAHtml(opts: {
  titleAr: string;
  descriptionAr?: string | null;
  contentHtml: string;
  type: string;
}): string {
  return `
  <section class="section section-qa theme-light">
    ${navStrip(opts.type, "light")}
    <div class="qa-titlebar">
      <h2 class="qa-title">${opts.titleAr}</h2>
      <img class="qa-icon" src="${SECTION_ICON.employee_qa}" alt=""/>
    </div>
    <div class="qa-grid">
      <div class="qa-portrait-col">
        <div class="qa-portrait">
          <svg viewBox="0 0 120 160" xmlns="http://www.w3.org/2000/svg">
            <rect width="120" height="160" fill="${MINT}"/>
            <circle cx="60" cy="56" r="26" fill="${TEAL}"/>
            <path d="M20 160 Q60 100 100 160 Z" fill="${TEAL}"/>
          </svg>
        </div>
        ${opts.descriptionAr ? `<div class="qa-name">${opts.descriptionAr}</div>` : ""}
        <div class="qa-qr">
          <div class="qa-qr-box">
            <div class="qa-qr-grid"></div>
          </div>
          <div class="qa-qr-caption">للمشاركة في شرفة يسعدنا تواصلك عبر مسح رمز QR</div>
        </div>
      </div>
      <div class="qa-body">${opts.contentHtml}</div>
    </div>
  </section>`;
}

/** Generic fallback for unknown section types. */
function sectionGenericHtml(opts: {
  titleAr: string;
  contentHtml: string;
  type: string;
  iconUrl?: string;
}): string {
  return `
  <section class="section section-generic theme-light">
    ${navStrip(opts.type, "light")}
    <div class="sec-hero">
      ${opts.iconUrl ? `<img class="sec-icon" src="${opts.iconUrl}" alt=""/>` : ""}
      <h2 class="sec-title">${opts.titleAr}</h2>
    </div>
    <div class="sec-body">${opts.contentHtml}</div>
  </section>`;
}

/** Renders the right section template based on type. */
export function renderSectionHtml(s: {
  sectionType: string;
  titleAr: string;
  descriptionAr?: string | null;
  contentMd?: string | null;
}, ctx: { arabicMonth: string; year: number }): string {
  const contentHtml = mdToHtml(s.contentMd || "");
  const common = {
    type: s.sectionType,
    titleAr: s.titleAr,
    descriptionAr: s.descriptionAr,
    contentHtml,
  };
  switch (s.sectionType) {
    case "news":
    case "local_news":
    case "regional_news":
    case "global_news":
      return sectionNewsHtml(common);
    case "office_interview":
      return sectionOfficeInterviewHtml(common);
    case "competition_culture":
      return sectionCompetitionCultureHtml({ ...common, arabicMonth: ctx.arabicMonth, year: ctx.year });
    case "outside_box":
      return sectionOutsideBoxHtml(common);
    case "events":
      return sectionEventsHtml(common);
    case "employee_qa":
      return sectionEmployeeQAHtml(common);
    default:
      return sectionGenericHtml({ ...common, iconUrl: SECTION_ICON[s.sectionType] });
  }
}

/** The big global stylesheet — mirrors the printed magazine. */
export const SHORFAH_PDF_CSS = `
  @page { size: A4; margin: 0; }
  * { box-sizing: border-box; -webkit-print-color-adjust: exact; print-color-adjust: exact; }
  html, body { margin: 0; padding: 0; }
  body {
    font-family: "Tajawal", "Noto Sans Arabic", "Cairo", system-ui, sans-serif;
    color: ${NAVY};
    line-height: 1.7;
    background: #fff;
    direction: rtl;
  }

  /* ─── Cover ─────────────────────────────────────────────── */
  .cover {
    position: relative;
    width: 210mm;
    height: 297mm;
    background: linear-gradient(135deg, ${TEAL} 0%, #2a8d9b 55%, ${CYAN} 100%);
    color: #fff;
    overflow: hidden;
    page-break-after: always;
  }
  .cover-bg-pattern {
    position: absolute; inset: 0;
    background-image:
      repeating-linear-gradient(45deg, transparent 0 14px, rgba(255,255,255,0.04) 14px 16px),
      repeating-linear-gradient(-45deg, transparent 0 14px, rgba(255,255,255,0.03) 14px 16px);
    opacity: 0.65;
    pointer-events: none;
  }
  /* Ribbon date - right side */
  .cv-ribbon {
    position: absolute; top: 0; right: 32mm;
    background: ${NAVY}; color: #fff;
    padding: 18mm 7mm 10mm; min-width: 28mm;
    border-radius: 0 0 4mm 4mm;
    text-align: center;
    box-shadow: 0 4mm 8mm rgba(0,0,0,0.2);
  }
  .cv-ribbon::after {
    content:""; position: absolute; bottom: -5mm; left: 0; right: 0;
    border-style: solid; border-width: 0 14mm 5mm 14mm;
    border-color: transparent transparent ${NAVY} transparent;
    transform: rotate(180deg);
  }
  .cv-ribbon-year { font-size: 28pt; font-weight: 900; line-height: 1; }
  .cv-ribbon-month { font-size: 20pt; font-weight: 700; margin-top: 4mm; }

  /* Brand top-left */
  .cv-brand { position: absolute; top: 18mm; left: 14mm; display: flex; align-items: center; gap: 4mm; }
  .cv-brand-ar { font-size: 11pt; font-weight: 700; text-align: left; }
  .cv-brand-en { font-size: 7.5pt; font-weight: 400; text-align: left; opacity: 0.9; margin-top: 1mm; }
  .cv-brand-mark { width: 16mm; height: 16mm; }
  .cv-brand-mark svg { width: 100%; height: 100%; }

  /* Hero with title overlaid on newspaper */
  .cv-hero {
    position: absolute; top: 60mm; left: 0; right: 0;
    display: flex; align-items: center; justify-content: center;
    height: 90mm;
  }
  .cv-newspaper {
    position: absolute;
    width: 130mm; height: auto;
    top: 50%; left: 50%; transform: translate(-50%, -50%) rotate(-3deg);
    z-index: 1;
    opacity: 0.95;
  }
  .cv-title {
    position: relative; z-index: 2;
    font-family: "Tajawal", "Noto Sans Arabic", sans-serif;
    font-size: 110pt; font-weight: 900; line-height: 1;
    margin: 0; color: #fff;
    text-shadow: 0 6mm 12mm rgba(0,0,0,0.18);
    letter-spacing: -2pt;
    text-align: center;
    display: flex; justify-content: center;
  }
  .cv-title-prefix, .cv-title-suffix { display:inline-block; }

  .cv-subtitle {
    position: absolute; top: 155mm; left: 0; right: 0;
    text-align: center; font-size: 12pt; font-weight: 600;
    color: #fff; opacity: 0.97;
    padding: 0 30mm; line-height: 1.5;
  }

  /* Tabs strip with badge */
  .cv-tabs-row {
    position: absolute; bottom: 60mm; left: 14mm; right: 14mm;
    display: flex; align-items: center; gap: 6mm;
  }
  .cv-tabs-strip {
    flex: 1;
    background: rgba(255,255,255,0.55);
    padding: 4mm 6mm;
    border-radius: 2mm;
    display: flex; gap: 4mm; justify-content: space-around;
    backdrop-filter: blur(2px);
  }
  .cv-tab {
    color: ${NAVY}; font-size: 9pt; font-weight: 700;
    background: rgba(255,255,255,0.85);
    padding: 1.5mm 4mm; border-radius: 1.2mm;
    white-space: nowrap;
  }
  .cv-tabs-badge {
    background: ${MINT}; color: ${NAVY};
    padding: 4mm 5mm; border-radius: 2mm 2mm 8mm 2mm;
    text-align: center; font-weight: 800; font-size: 12pt;
    line-height: 1.2; min-width: 22mm;
    box-shadow: 0 2mm 5mm rgba(0,0,0,0.1);
  }

  /* Motto plate */
  .cv-motto {
    position: absolute; bottom: 22mm; left: 14mm; right: 14mm;
    background: rgba(240,247,248,0.95);
    color: ${TEAL};
    padding: 6mm 10mm;
    text-align: center; font-size: 14pt; font-weight: 800;
    border-radius: 2mm;
    box-shadow: 0 3mm 8mm rgba(0,0,0,0.1);
  }

  /* Decorative pattern strip bottom */
  .cover::after {
    content: "";
    position: absolute; bottom: 0; left: 0; right: 0; height: 12mm;
    background-image: repeating-linear-gradient(60deg,
      transparent 0 4mm,
      rgba(255,255,255,0.1) 4mm 4.4mm,
      transparent 4.4mm 8mm);
  }

  /* ─── Nav strip (top of each section page) ─────────────────── */
  .nav-strip {
    display: flex; justify-content: flex-end; gap: 12mm;
    padding: 6mm 14mm; font-size: 10pt; font-weight: 700;
    direction: rtl;
  }
  .nav-light { background: ${OFFWHITE}; color: #8aa9b0; border-bottom: 0.3mm solid #d6e9eb; }
  .nav-navy { background: ${DEEPNAVY}; color: rgba(255,255,255,0.45); border-bottom: 0.3mm solid #1a4f60; }
  .nav-teal { background: ${TEAL_DARK}; color: rgba(255,255,255,0.55); border-bottom: 0.3mm solid ${TEAL}; }
  .nav-tab { padding: 1mm 0; }
  .nav-light .nav-tab.is-active { color: ${NAVY}; border-bottom: 1mm solid ${TEAL}; padding-bottom: 1mm; }
  .nav-navy .nav-tab.is-active  { color: #fff; border-bottom: 1mm solid ${CYAN}; padding-bottom: 1mm; }
  .nav-teal .nav-tab.is-active  { color: #fff; border-bottom: 1mm solid #fff; padding-bottom: 1mm; }

  /* ─── Generic section page ─────────────────────────────────── */
  .section {
    position: relative;
    width: 210mm; min-height: 297mm;
    page-break-after: always;
    overflow: hidden;
  }
  .theme-light { background: ${OFFWHITE}; color: ${NAVY}; }
  .theme-navy  { background: ${NAVY}; color: ${MINT}; }
  .theme-teal  { background: ${TEAL}; color: #fff; }

  .sec-hero { display: flex; align-items: center; gap: 8mm; padding: 14mm 14mm 6mm; }
  .sec-icon { width: 50mm; height: 50mm; object-fit: contain; flex-shrink: 0; }
  .sec-title { font-size: 48pt; font-weight: 900; margin: 0; color: ${TEAL}; line-height: 1; }
  .sec-body { padding: 0 14mm 18mm; font-size: 11pt; line-height: 1.85; }
  .sec-body h3 { color: ${TEAL}; font-size: 14pt; margin-top: 8mm; font-weight: 800; }
  .sec-body h4 { color: ${TEAL}; font-size: 12pt; margin-top: 6mm; font-weight: 800; }
  .sec-body p { margin: 4mm 0; text-align: justify; }
  .sec-body ul { padding-right: 6mm; margin: 4mm 0; }
  .sec-body li { margin: 2mm 0; }
  .sec-body strong { color: ${TEAL}; font-weight: 800; }

  /* ─── News section (theme-light + 2-col cards) ─────────────── */
  .section-news .sec-hero { flex-direction: row-reverse; justify-content: center; padding: 18mm 14mm 8mm; }
  .section-news .sec-title-news { font-size: 64pt; text-align: center; }
  .section-news .sec-icon-news { width: 60mm; height: 60mm; transform: rotate(-3deg); }
  .section-news .sec-body-news { padding: 4mm 14mm 18mm; }
  .section-news .sec-body-news p {
    background: ${MINT}; padding: 8mm; border-radius: 2mm;
    margin: 3mm 0; box-shadow: 0 1mm 3mm rgba(0,0,0,0.04);
  }
  .section-news .sec-body-news h3 {
    color: ${NAVY}; background: none; padding: 0;
    border-right: 2mm solid ${TEAL}; padding-right: 4mm; margin-top: 8mm;
  }

  /* ─── Office interview (light, navy title bar) ─────────────── */
  .section-office .oi-titlebar {
    background: ${NAVY}; color: #fff;
    padding: 6mm 10mm; margin: 6mm 14mm 8mm;
    font-size: 22pt; font-weight: 900;
    text-align: center; border-radius: 1mm;
  }
  .section-office .oi-grid {
    display: grid; grid-template-columns: 60mm 1fr;
    gap: 8mm; padding: 0 14mm 18mm;
  }
  .section-office .oi-portrait-placeholder {
    width: 60mm; height: 80mm;
    border-radius: 2mm; overflow: hidden;
    background: ${MINT};
  }
  .section-office .oi-portrait-placeholder svg { width: 100%; height: 100%; display: block; }
  .section-office .oi-caption {
    font-weight: 800; font-size: 11pt; color: ${NAVY};
    margin-top: 4mm; padding-right: 2mm;
    border-right: 1mm solid ${TEAL};
  }
  .section-office .oi-right {
    font-size: 10pt; line-height: 1.8;
    column-count: 2; column-gap: 6mm;
    text-align: justify;
  }
  .section-office .oi-right h3 {
    color: ${NAVY}; font-size: 11pt; font-weight: 900;
    margin: 4mm 0 2mm; break-after: avoid;
  }
  .section-office .oi-right p { margin: 2mm 0; }
  .section-office .oi-right strong { color: ${TEAL}; }

  /* ─── Competition culture (full navy) ──────────────────────── */
  .section-comp { background: ${NAVY}; }
  .section-comp .cc-hero {
    padding: 14mm 14mm 6mm;
    display: grid; grid-template-columns: 1fr 60mm; gap: 8mm;
    align-items: center;
  }
  .section-comp .cc-title {
    font-size: 56pt; color: #fff; font-weight: 900;
    text-align: center; line-height: 1;
    margin: 0;
    writing-mode: horizontal-tb;
  }
  .section-comp .cc-date {
    background: ${CYAN}; color: ${NAVY};
    padding: 3mm 8mm; display: inline-block;
    font-size: 18pt; font-weight: 900;
    margin-top: 4mm;
  }
  .section-comp .cc-hero-callout {
    background: ${MINT}; color: ${NAVY};
    padding: 8mm 6mm; border-radius: 2mm;
    font-size: 11pt; line-height: 1.6; font-weight: 600;
  }
  .section-comp .cc-body {
    padding: 6mm 14mm 18mm;
    color: ${MINT};
  }
  .section-comp .cc-body h3, .section-comp .cc-body strong {
    color: ${CYAN}; font-weight: 800;
  }
  .section-comp .cc-body p {
    background: rgba(255,255,255,0.06);
    padding: 6mm; border-radius: 2mm; margin: 3mm 0;
  }

  /* ─── Outside the box (light, navy title bar) ──────────────── */
  .section-outside .ob-titlebar {
    background: ${NAVY}; color: #fff;
    padding: 6mm 10mm; margin: 6mm 14mm 8mm;
    font-size: 20pt; font-weight: 900;
    border-radius: 1mm;
  }
  .section-outside .ob-grid {
    display: grid;
    grid-template-columns: 60mm 50mm 1fr;
    gap: 6mm;
    padding: 0 14mm 18mm;
  }
  .section-outside .ob-portrait { width: 60mm; height: 80mm; border-radius: 2mm; overflow: hidden; background: ${NAVY}; }
  .section-outside .ob-portrait svg { width: 100%; height: 100%; display: block; }
  .section-outside .ob-portrait-caption {
    font-weight: 800; font-size: 11pt; color: ${NAVY};
    margin-top: 4mm; padding-right: 2mm;
    border-right: 1mm solid ${TEAL};
  }
  .section-outside .ob-icon { display: flex; align-items: center; justify-content: center; }
  .section-outside .ob-icon img { max-width: 100%; max-height: 80mm; }
  .section-outside .ob-body {
    font-size: 10pt; line-height: 1.8;
    column-count: 1;
    text-align: justify;
  }
  .section-outside .ob-body strong { color: ${TEAL}; }

  /* ─── Events (full teal) ───────────────────────────────────── */
  .section-events { background: ${TEAL}; }
  .section-events .ev-banner {
    margin: 12mm 14mm 8mm;
    background: ${MINT};
    padding: 8mm 10mm;
    border-radius: 2mm;
    display: flex; align-items: center; justify-content: center;
    gap: 8mm; position: relative;
    min-height: 30mm;
  }
  .section-events .ev-bunting {
    position: absolute; top: -4mm; left: 4mm; right: 4mm;
    width: calc(100% - 8mm); height: 18mm;
    object-fit: contain;
    z-index: 1;
  }
  .section-events .ev-title {
    font-size: 48pt; font-weight: 900;
    color: #fff;
    margin: 0;
    text-shadow: 0 1mm 3mm rgba(0,0,0,0.15);
    position: relative; z-index: 2;
    padding-top: 6mm;
  }
  .section-events .ev-collage {
    margin: 8mm 14mm;
    background: rgba(255,255,255,0.06);
    border-radius: 2mm;
    padding: 10mm;
    color: #fff; font-size: 11pt; line-height: 1.8;
  }
  .section-events .ev-collage h3 { color: #fff; }
  .section-events .ev-empty {
    text-align: center; padding: 20mm; opacity: 0.7;
    border: 0.5mm dashed rgba(255,255,255,0.4);
    border-radius: 2mm;
  }

  /* ─── Employee QA (light, teal title plate) ────────────────── */
  .section-qa .qa-titlebar {
    background: ${TEAL}; color: #fff;
    padding: 6mm 10mm; margin: 6mm 14mm 8mm;
    font-size: 30pt; font-weight: 900;
    border-radius: 1mm; position: relative;
    display: flex; align-items: center; justify-content: space-between;
  }
  .section-qa .qa-title { margin: 0; color: #fff; }
  .section-qa .qa-icon { width: 22mm; height: 22mm; object-fit: contain; }
  .section-qa .qa-grid {
    display: grid; grid-template-columns: 55mm 1fr;
    gap: 8mm; padding: 0 14mm 18mm;
  }
  .section-qa .qa-portrait { width: 55mm; height: 75mm; border-radius: 2mm; overflow: hidden; background: ${MINT}; }
  .section-qa .qa-portrait svg { width:100%; height:100%; display:block; }
  .section-qa .qa-name {
    font-weight: 800; font-size: 11pt; color: ${NAVY};
    margin-top: 4mm; text-align: center;
    background: ${TEAL}; color: #fff;
    padding: 3mm; border-radius: 1mm;
  }
  .section-qa .qa-qr { margin-top: 6mm; background: ${MINT}; padding: 4mm; border-radius: 1.5mm; text-align: center; }
  .section-qa .qa-qr-box { width: 30mm; height: 30mm; margin: 0 auto 3mm; background: #fff; padding: 2mm; }
  .section-qa .qa-qr-grid {
    width: 100%; height: 100%;
    background-image:
      repeating-linear-gradient(0deg, ${NAVY} 0 1.5mm, transparent 1.5mm 3mm),
      repeating-linear-gradient(90deg, ${NAVY} 0 1.5mm, transparent 1.5mm 3mm);
    background-blend-mode: multiply;
  }
  .section-qa .qa-qr-caption { font-size: 8pt; color: ${NAVY}; line-height: 1.4; }
  .section-qa .qa-body { display: flex; flex-direction: column; gap: 4mm; }
  .section-qa .qa-body p {
    background: ${TEAL}; color: #fff;
    padding: 4mm 6mm; border-radius: 1.5mm;
    margin: 0; font-size: 10pt; font-weight: 600;
    position: relative;
  }
  .section-qa .qa-body p:nth-child(odd) {
    background: ${MINT}; color: ${NAVY};
  }
  .section-qa .qa-body p:nth-child(odd)::before {
    content: ""; position: absolute; top: 50%;
    right: -3mm; transform: translateY(-50%);
    border-style: solid; border-width: 3mm 0 3mm 3mm;
    border-color: transparent transparent transparent ${MINT};
  }
  .section-qa .qa-body p:nth-child(even)::before {
    content: ""; position: absolute; top: 50%;
    right: -3mm; transform: translateY(-50%);
    border-style: solid; border-width: 3mm 0 3mm 3mm;
    border-color: transparent transparent transparent ${TEAL};
  }
`;

/** Build full PDF HTML document. */
export function buildShorfahPdfHtml(opts: {
  issue: {
    titleAr: string;
    subtitleAr?: string | null;
    editorLetter?: string | null;
    month: number;
    year: number;
    issueNo: number;
    publishedAt?: Date | null;
  };
  sections: Array<{
    sectionType: string;
    titleAr: string;
    descriptionAr?: string | null;
    contentMd?: string | null;
  }>;
  baseUrl?: string;
}): string {
  const arabicMonth = ARABIC_MONTHS[opts.issue.month - 1] || "—";
  const subtitle = opts.issue.subtitleAr || "نشرة داخلية شهرية تصدر من الإدارة التنفيذية للتواصل المؤسسي";
  const motto = opts.issue.editorLetter || "بجهودكم تتعزز بيئة المنافسة... وبعملكم يترسخ مبدأ العدالة.";

  // Re-order sections to match NAV_ORDER, then keep unknowns at end in their given order
  const ordered = [
    ...NAV_ORDER.flatMap((t) => opts.sections.filter((s) => s.sectionType === t)),
    ...opts.sections.filter((s) => !NAV_ORDER.includes(s.sectionType)),
  ];

  const cover = coverPageHtml({ arabicMonth, year: opts.issue.year, subtitle, motto });

  const sectionsHtml = ordered
    .map((s) => renderSectionHtml(s, { arabicMonth, year: opts.issue.year }))
    .join("\n");

  return `<!DOCTYPE html>
<html dir="rtl" lang="ar">
<head>
  <meta charset="UTF-8"/>
  <title>${opts.issue.titleAr} — ${arabicMonth} ${opts.issue.year}</title>
  <link rel="preconnect" href="https://fonts.googleapis.com"/>
  <link rel="preconnect" href="https://fonts.gstatic.com" crossorigin/>
  <link href="https://fonts.googleapis.com/css2?family=Tajawal:wght@400;500;700;900&family=Noto+Sans+Arabic:wght@400;700;900&display=swap" rel="stylesheet"/>
  <style>${SHORFAH_PDF_CSS}</style>
</head>
<body>
  ${cover}
  ${sectionsHtml}
  <script>
    // Auto-print if ?autoprint=1
    if (location.search.includes('autoprint')) setTimeout(() => window.print(), 800);
  </script>
</body>
</html>`;
}
