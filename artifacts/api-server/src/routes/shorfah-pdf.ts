/**
 * Shorfah PDF Template — visually identical to the printed sample
 * Sample reference: nshr-shrf-mrs.pdf (March 2026)
 *
 * Design language:
 * - Teal/Cyan palette: #1a6e7a (teal), #0e3b4a (navy), #cce4e6 (mint), #3ec0d0 (cyan), #f0f7f8 (offwhite)
 * - GAC official: #00567D (dark blue), #46BCCD (cyan)
 * - Display font: Tajawal 900 for huge titles, Cairo/Tajawal 700 for body
 * - Each section page has: top nav strip (current highlighted), large title with icon, content
 * - Page themes vary: light/mint cover, light section pages, dark navy pages, full teal pages
 *
 * IMPORTANT: All icons are inline SVG — no external image dependencies.
 */

const TEAL = "#1a6e7a";
const NAVY = "#0e3b4a";
const MINT = "#cce4e6";
const CYAN = "#3ec0d0";
const OFFWHITE = "#f0f7f8";
const TEAL_DARK = "#155a64";
const DEEPNAVY = "#0a2c38";

// Phase 7 — Map section type -> inline SVG icon key (11-section canonical order)
export const SECTION_ICON: Record<string, string> = {
  global_news: "newspaper",
  news: "newspaper",
  intl_participation: "bunting",
  our_comms: "speech",
  economic_observatory: "monitor",
  system_index: "monitor",
  legal_window: "box",
  office_interview: "microphone",
  competition_culture: "monitor",
  outside_box: "box",
  events: "bunting",
  employee_qa: "speech",
};

/** Returns inline SVG markup for each section icon type. */
function sectionIconSvg(type: string): string {
  switch (type) {
    case "newspaper":
      return `<svg viewBox="0 0 80 80" xmlns="http://www.w3.org/2000/svg" style="width:100%;height:100%">
        <rect x="8" y="14" width="52" height="60" rx="4" fill="#e8f4f6" stroke="${TEAL}" stroke-width="2.5"/>
        <rect x="18" y="24" width="32" height="4" rx="2" fill="${TEAL}"/>
        <rect x="18" y="32" width="24" height="3" rx="1.5" fill="${MINT}"/>
        <rect x="18" y="39" width="28" height="3" rx="1.5" fill="${MINT}"/>
        <rect x="18" y="46" width="20" height="3" rx="1.5" fill="${MINT}"/>
        <rect x="18" y="53" width="26" height="3" rx="1.5" fill="${MINT}"/>
        <rect x="18" y="60" width="18" height="3" rx="1.5" fill="${MINT}"/>
        <rect x="56" y="20" width="18" height="48" rx="3" fill="${CYAN}" opacity="0.85"/>
        <rect x="60" y="28" width="10" height="2.5" rx="1.2" fill="white"/>
        <rect x="60" y="34" width="10" height="2.5" rx="1.2" fill="white"/>
        <rect x="60" y="40" width="10" height="2.5" rx="1.2" fill="white"/>
        <rect x="60" y="46" width="10" height="2.5" rx="1.2" fill="white"/>
      </svg>`;
    case "microphone":
      return `<svg viewBox="0 0 80 80" xmlns="http://www.w3.org/2000/svg" style="width:100%;height:100%">
        <rect x="28" y="8" width="24" height="36" rx="12" fill="${TEAL}"/>
        <rect x="34" y="8" width="12" height="36" rx="6" fill="${TEAL_DARK}" opacity="0.5"/>
        <path d="M16 38 Q16 62 40 62 Q64 62 64 38" stroke="${CYAN}" stroke-width="4" fill="none" stroke-linecap="round"/>
        <line x1="40" y1="62" x2="40" y2="72" stroke="${TEAL}" stroke-width="4" stroke-linecap="round"/>
        <line x1="28" y1="72" x2="52" y2="72" stroke="${TEAL}" stroke-width="4" stroke-linecap="round"/>
        <rect x="34" y="18" width="4" height="3" rx="1.5" fill="white" opacity="0.7"/>
        <rect x="34" y="25" width="4" height="3" rx="1.5" fill="white" opacity="0.7"/>
      </svg>`;
    case "monitor":
      return `<svg viewBox="0 0 80 80" xmlns="http://www.w3.org/2000/svg" style="width:100%;height:100%">
        <rect x="8" y="12" width="64" height="44" rx="5" fill="${NAVY}"/>
        <rect x="14" y="18" width="52" height="32" rx="2" fill="${CYAN}" opacity="0.15"/>
        <rect x="14" y="18" width="52" height="32" rx="2" fill="none" stroke="${CYAN}" stroke-width="1.5"/>
        <circle cx="40" cy="34" r="8" fill="${CYAN}" opacity="0.7"/>
        <path d="M36 34 L44 30 L44 38 Z" fill="white"/>
        <rect x="32" y="56" width="16" height="6" rx="2" fill="${TEAL}" opacity="0.7"/>
        <rect x="20" y="62" width="40" height="4" rx="2" fill="${TEAL}"/>
      </svg>`;
    case "box":
      return `<svg viewBox="0 0 80 80" xmlns="http://www.w3.org/2000/svg" style="width:100%;height:100%">
        <rect x="10" y="30" width="50" height="38" rx="4" fill="${MINT}" stroke="${TEAL}" stroke-width="2.5"/>
        <rect x="8" y="22" width="54" height="12" rx="3" fill="${TEAL}"/>
        <line x1="35" y1="22" x2="35" y2="34" stroke="white" stroke-width="2"/>
        <path d="M50 14 L60 6 M60 6 L54 6 M60 6 L60 12" stroke="${CYAN}" stroke-width="3" stroke-linecap="round" stroke-linejoin="round"/>
        <rect x="18" y="40" width="34" height="3" rx="1.5" fill="${TEAL}" opacity="0.5"/>
        <rect x="18" y="47" width="28" height="3" rx="1.5" fill="${TEAL}" opacity="0.4"/>
        <rect x="18" y="54" width="22" height="3" rx="1.5" fill="${TEAL}" opacity="0.3"/>
      </svg>`;
    case "bunting":
      return `<svg viewBox="0 0 120 40" xmlns="http://www.w3.org/2000/svg" style="width:100%;height:100%">
        <path d="M4 4 Q15 4 20 4 Q30 4 35 4 Q50 4 55 4 Q65 4 70 4 Q80 4 85 4 Q100 4 105 4 Q115 4 116 4" stroke="${TEAL}" stroke-width="2" fill="none"/>
        <polygon points="12,6 18,28 6,28" fill="${TEAL}"/>
        <polygon points="30,6 36,28 24,28" fill="${CYAN}"/>
        <polygon points="48,6 54,28 42,28" fill="${MINT}" stroke="${TEAL}" stroke-width="1"/>
        <polygon points="66,6 72,28 60,28" fill="${TEAL}"/>
        <polygon points="84,6 90,28 78,28" fill="${CYAN}"/>
        <polygon points="102,6 108,28 96,28" fill="${MINT}" stroke="${TEAL}" stroke-width="1"/>
      </svg>`;
    case "speech":
      return `<svg viewBox="0 0 80 80" xmlns="http://www.w3.org/2000/svg" style="width:100%;height:100%">
        <rect x="6" y="10" width="58" height="40" rx="10" fill="${TEAL}"/>
        <path d="M20 50 L12 64 L32 54" fill="${TEAL}"/>
        <rect x="16" y="22" width="38" height="4" rx="2" fill="white" opacity="0.9"/>
        <rect x="16" y="31" width="30" height="4" rx="2" fill="white" opacity="0.7"/>
        <rect x="16" y="40" width="22" height="4" rx="2" fill="white" opacity="0.5"/>
      </svg>`;
    default:
      return `<svg viewBox="0 0 80 80" xmlns="http://www.w3.org/2000/svg" style="width:100%;height:100%">
        <circle cx="40" cy="40" r="32" fill="${TEAL}" opacity="0.3"/>
        <circle cx="40" cy="40" r="20" fill="${TEAL}"/>
      </svg>`;
  }
}

/** GAC official logo SVG — inline, no external dependency. */
function gacLogoSvg(): string {
  return `<svg viewBox="0 0 80 80" xmlns="http://www.w3.org/2000/svg" style="width:100%;height:100%">
    <!-- Interlocking diamond / weave pattern inspired by GAC hexagonal logo -->
    <g fill="none" stroke="white" stroke-width="2.2" stroke-linecap="round" stroke-linejoin="round">
      <!-- Outer hex -->
      <polygon points="40,4 68,20 68,56 40,72 12,56 12,20" stroke-width="2.5"/>
      <!-- Inner woven cross -->
      <line x1="40" y1="14" x2="40" y2="62"/>
      <line x1="18" y1="26" x2="62" y2="50"/>
      <line x1="62" y1="26" x2="18" y2="50"/>
      <!-- Accent dots at intersections -->
    </g>
    <circle cx="40" cy="38" r="6" fill="white" opacity="0.9"/>
    <circle cx="40" cy="20" r="3" fill="white" opacity="0.7"/>
    <circle cx="55" cy="29" r="3" fill="white" opacity="0.7"/>
    <circle cx="55" cy="47" r="3" fill="white" opacity="0.7"/>
    <circle cx="40" cy="56" r="3" fill="white" opacity="0.7"/>
    <circle cx="25" cy="47" r="3" fill="white" opacity="0.7"/>
    <circle cx="25" cy="29" r="3" fill="white" opacity="0.7"/>
  </svg>`;
}

/** Newspaper illustration SVG for cover hero — no external dependency. */
function coverNewspaperSvg(): string {
  return `<svg viewBox="0 0 260 200" xmlns="http://www.w3.org/2000/svg" style="width:100%;height:auto;filter:drop-shadow(0 8px 20px rgba(0,0,0,0.22))">
    <!-- Main newspaper body — slightly rotated, rendered inline via transform -->
    <g transform="rotate(-6 130 100)">
      <!-- Shadow -->
      <rect x="20" y="22" width="190" height="156" rx="10" fill="rgba(10,44,56,0.25)" transform="translate(6,8)"/>
      <!-- Paper body -->
      <rect x="20" y="22" width="190" height="156" rx="10" fill="#e8f4f6"/>
      <!-- Header strip -->
      <rect x="20" y="22" width="190" height="34" rx="10" fill="${TEAL}"/>
      <rect x="20" y="44" width="190" height="12" fill="${TEAL}"/>
      <!-- Header lines -->
      <rect x="34" y="30" width="100" height="6" rx="3" fill="white" opacity="0.9"/>
      <rect x="34" y="40" width="70" height="4" rx="2" fill="white" opacity="0.6"/>
      <!-- Photo placeholder left col -->
      <rect x="30" y="66" width="80" height="54" rx="5" fill="${CYAN}" opacity="0.4"/>
      <rect x="38" y="74" width="64" height="38" rx="3" fill="${CYAN}" opacity="0.5"/>
      <!-- Lines right col -->
      <rect x="122" y="66" width="78" height="5" rx="2.5" fill="${NAVY}" opacity="0.4"/>
      <rect x="122" y="76" width="72" height="5" rx="2.5" fill="${NAVY}" opacity="0.3"/>
      <rect x="122" y="86" width="68" height="5" rx="2.5" fill="${NAVY}" opacity="0.3"/>
      <rect x="122" y="96" width="74" height="5" rx="2.5" fill="${NAVY}" opacity="0.3"/>
      <rect x="122" y="106" width="60" height="5" rx="2.5" fill="${NAVY}" opacity="0.2"/>
      <!-- Bottom lines full width -->
      <rect x="30" y="132" width="180" height="5" rx="2.5" fill="${NAVY}" opacity="0.25"/>
      <rect x="30" y="142" width="160" height="5" rx="2.5" fill="${NAVY}" opacity="0.2"/>
      <rect x="30" y="152" width="140" height="5" rx="2.5" fill="${NAVY}" opacity="0.15"/>
      <rect x="30" y="162" width="100" height="5" rx="2.5" fill="${NAVY}" opacity="0.12"/>
    </g>
    <!-- Folded corner highlight -->
    <path d="M192 28 L216 4 L216 28 Z" fill="${MINT}" opacity="0.7" transform="rotate(-6 130 100)"/>
  </svg>`;
}

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

/**
 * Parse markdown into news card objects: { title, body }[].
 * Looks for ## heading followed by paragraphs as cards.
 * Falls back to a single card with all content if no headings found.
 */
function parseNewsCards(md: string): Array<{ title: string; body: string }> {
  if (!md || !md.trim()) return [];

  const lines = md.split("\n");
  const cards: Array<{ title: string; body: string[] }> = [];
  let current: { title: string; body: string[] } | null = null;

  for (const line of lines) {
    // Match ## OR ### OR #### headings as card titles
    const headingMatch = line.match(/^#{2,4}\s+(.+)$/);
    if (headingMatch) {
      if (current) cards.push(current);
      current = { title: headingMatch[1].trim(), body: [] };
    } else if (current) {
      if (line.trim()) current.body.push(line.trim());
    } else {
      // Content before any heading — make a headerless card
      if (line.trim()) {
        if (!current) current = { title: "", body: [] };
        current.body.push(line.trim());
      }
    }
  }
  if (current) cards.push(current);

  return cards
    .filter((c) => c.title || c.body.length)
    .map((c) => ({ title: c.title, body: c.body.join(" ") }));
}

/**
 * Parse markdown Q&A pairs — FIXED line-by-line parser.
 *
 * Shorfah seed format:
 *   **س: question text?**
 *   ج: answer text
 *
 * The original bug: regex `[^\n]+` for the answer failed when there
 * were blank lines between the question marker and ج:, because the
 * global regex consumed the whole string and the lookahead didn't
 * cross newline boundaries properly.
 *
 * Fix: split into lines, walk them explicitly, skip blank lines
 * between **س:...** and ج:...
 */
function parseQAPairs(md: string): Array<{ q: string; a: string }> {
  if (!md || !md.trim()) return [];

  const pairs: Array<{ q: string; a: string }> = [];

  // ── Pattern 1 (primary): line-by-line walk for Shorfah seed format ──
  const lines = md.split("\n");
  let i = 0;
  while (i < lines.length) {
    const line = lines[i].trim();
    // Match: **س: text** (with or without trailing ؟/?)
    const qMatch = line.match(/^\*\*\s*س\s*[:：]\s*(.+?)\s*\*\*\s*$/);
    if (qMatch) {
      const question = qMatch[1].trim().replace(/[?？\u061F]+$/, "") + "؟";
      // Advance past blank lines to find ج:
      let j = i + 1;
      while (j < lines.length && lines[j].trim() === "") j++;
      let answer = "";
      if (j < lines.length) {
        const aMatch = lines[j].trim().match(/^ج\s*[:：]\s*(.+)$/);
        if (aMatch) {
          answer = aMatch[1].trim();
          i = j + 1;
        } else {
          i++;
        }
      } else {
        i++;
      }
      pairs.push({ q: question, a: answer });
      continue;
    }
    i++;
  }
  if (pairs.length > 0) return pairs;

  // ── Pattern 2: blockquote > Q then paragraph A ──
  const bqPattern = /^> (.+)$/gm;
  const bqMatches = [...md.matchAll(bqPattern)];
  if (bqMatches.length > 0) {
    const parts = md.split(/^> .+$/m);
    for (let k = 0; k < bqMatches.length; k++) {
      const q = bqMatches[k][1].trim();
      const a = (parts[k + 1] || "").trim().replace(/\n+/g, " ").replace(/^[>-]\s*/gm, "");
      if (q || a) pairs.push({ q, a });
    }
    if (pairs.length > 0) return pairs;
  }

  // ── Pattern 3: alternating paragraphs (odd=Q, even=A) ──
  const paras = md.split(/\n{2,}/).map((p) => p.trim()).filter(Boolean);
  for (let k = 0; k < paras.length; k += 2) {
    const q = paras[k].replace(/^[#>*\-\s]+/, "").replace(/\*\*/g, "").trim();
    const a = (paras[k + 1] || "").replace(/^[#>*\-\s]+/, "").replace(/\*\*/g, "").trim();
    pairs.push({ q, a });
  }
  return pairs;
}

/**
 * Parse competition culture stats from markdown.
 * Looks for patterns like:
 *   - Caption: Number
 *   - Caption — Number
 *   Or ## heading with following bullet list
 */
function parseStatCards(md: string): Array<{ label: string; value: string; caption?: string }> {
  const stats: Array<{ label: string; value: string; caption?: string }> = [];

  // Pattern A: "- **NUMBER** caption"  (Shorfah seed format)
  const boldFirstPattern = /^-\s+\*\*([0-9٠-٩][0-9٠-٩,]*%?)\*\*\s+(.+?)$/gm;
  let m: RegExpExecArray | null;
  while ((m = boldFirstPattern.exec(md)) !== null) {
    stats.push({ label: m[2].trim(), value: m[1].trim() });
  }
  if (stats.length > 0) return stats;

  // Pattern B: "- label: number" or "- label — number"
  const linePattern = /^-\s+(.+?)[:—–]\s*([0-9٠-٩][0-9٠-٩,]*%?)\s*$/gm;
  while ((m = linePattern.exec(md)) !== null) {
    stats.push({ label: m[1].trim(), value: m[2].trim() });
  }

  return stats;
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
    <!-- Ribbon date — top-LEFT visually (start in RTL), matching sample-1 -->
    <div class="cv-ribbon">
      <div class="cv-ribbon-year">${opts.year}</div>
      <div class="cv-ribbon-month">${opts.arabicMonth}</div>
    </div>
    <!-- Brand top-RIGHT visually (end in RTL), matching sample-1 -->
    <div class="cv-brand">
      <div class="cv-brand-text">
        <div class="cv-brand-ar">الهيئة العامة للمنافسة</div>
        <div class="cv-brand-en">General Authority for Competition</div>
      </div>
      <div class="cv-brand-mark">${gacLogoSvg()}</div>
    </div>

    <!-- Hero: newspaper SVG illustration + شرفة wordmark -->
    <div class="cv-hero">
      <div class="cv-newspaper-wrap">${coverNewspaperSvg()}</div>
      <h1 class="cv-title">شرفـــــة</h1>
    </div>

    <div class="cv-subtitle">${opts.subtitle}</div>

    <!-- Tabs strip with "في هذا العدد" badge on left (RTL start) -->
    <div class="cv-tabs-row">
      <div class="cv-tabs-badge">
        <div>في هذا</div>
        <div>العدد</div>
      </div>
      <div class="cv-tabs-strip">${tabs}</div>
    </div>

    <!-- Motto plate at bottom -->
    <div class="cv-motto">${opts.motto}</div>
  </section>`;
}

/** News section — 2-column card grid with gradient photo areas + teal title plates. */
function sectionNewsHtml(opts: {
  titleAr: string;
  contentMd: string;
  type: string;
  media?: Array<{ url: string; caption: string | null }>;
}): string {
  const cards = parseNewsCards(opts.contentMd);
  const media = opts.media ?? [];

  let cardsHtml: string;
  if (cards.length === 0) {
    // Fallback: render parsed HTML as-is
    cardsHtml = `<div class="news-single">${mdToHtml(opts.contentMd)}</div>`;
  } else {
    cardsHtml = cards
      .map((card, idx) => {
        // Alternate gradient hues for photo placeholder
        const gradients = [
          `linear-gradient(135deg, ${TEAL} 0%, ${CYAN} 100%)`,
          `linear-gradient(135deg, ${NAVY} 0%, ${TEAL} 100%)`,
          `linear-gradient(135deg, ${TEAL_DARK} 0%, ${CYAN} 100%)`,
          `linear-gradient(135deg, ${DEEPNAVY} 0%, ${TEAL} 100%)`,
        ];
        const grad = gradients[idx % gradients.length];
        const photo = media[idx];
        const photoStyle = photo
          ? `background:url('${photo.url}') center/cover no-repeat, ${grad}`
          : `background:${grad}`;
        const titleHtml = card.title
          ? `<div class="news-card-title">${card.title}</div>`
          : "";
        const bodyHtml = card.body
          ? `<div class="news-card-body">${card.body}</div>`
          : "";
        return `<div class="news-card">
          <div class="news-card-photo" style="${photoStyle}">
            ${photo ? "" : '<div class="news-card-photo-pattern"></div>'}
          </div>
          ${titleHtml}
          ${bodyHtml}
        </div>`;
      })
      .join("");
  }

  return `
  <section class="section section-news theme-light">
    ${navStrip(opts.type, "light")}
    <div class="sec-hero">
      <div class="sec-icon-wrap">${sectionIconSvg("newspaper")}</div>
      <h2 class="sec-title sec-title-news">${opts.titleAr}</h2>
    </div>
    <div class="sec-body sec-body-news">
      <div class="news-grid">${cardsHtml}</div>
    </div>
  </section>`;
}

/** Office interview — navy title strip + portrait area + 2-column body. */
function sectionOfficeInterviewHtml(opts: {
  titleAr: string;
  descriptionAr?: string | null;
  contentHtml: string;
  type: string;
  media?: Array<{ url: string; caption: string | null }>;
}): string {
  const portrait = (opts.media ?? [])[0];
  const portraitBlock = portrait
    ? `<div class="oi-portrait-placeholder" style="background:url('${portrait.url}') center/cover no-repeat;"></div>`
    : `<div class="oi-portrait-placeholder">
          <svg viewBox="0 0 120 160" xmlns="http://www.w3.org/2000/svg">
            <rect width="120" height="160" fill="${MINT}"/>
            <rect x="20" y="100" width="80" height="60" rx="8" fill="${TEAL}" opacity="0.6"/>
            <rect x="48" y="86" width="24" height="20" rx="4" fill="#d4a87a"/>
            <ellipse cx="60" cy="72" rx="26" ry="28" fill="#d4a87a"/>
            <ellipse cx="60" cy="50" rx="28" ry="14" fill="white" opacity="0.9"/>
            <rect x="32" y="50" width="56" height="8" rx="2" fill="white" opacity="0.85"/>
            <ellipse cx="60" cy="50" rx="20" ry="6" fill="${NAVY}" opacity="0.75"/>
            <ellipse cx="50" cy="74" rx="4" ry="3" fill="${NAVY}" opacity="0.3"/>
            <ellipse cx="70" cy="74" rx="4" ry="3" fill="${NAVY}" opacity="0.3"/>
            <path d="M50 84 Q60 90 70 84" stroke="${NAVY}" stroke-width="2" fill="none" opacity="0.4" stroke-linecap="round"/>
          </svg>
        </div>`;
  const captionText = portrait?.caption || opts.descriptionAr || "حوار شهري مع أحد القياديين";
  return `
  <section class="section section-office theme-light">
    ${navStrip(opts.type, "light")}
    <div class="oi-titlebar">${opts.titleAr}</div>
    <div class="oi-grid">
      <div class="oi-left">
        ${portraitBlock}
        <div class="oi-caption">${captionText}</div>
      </div>
      <div class="oi-right">${opts.contentHtml}</div>
    </div>
  </section>`;
}

/** Competition culture — full dark navy page with big vertical title + stat cards. */
function sectionCompetitionCultureHtml(opts: {
  titleAr: string;
  contentMd: string;
  type: string;
  arabicMonth: string;
  year: number;
}): string {
  const stats = parseStatCards(opts.contentMd);
  const contentHtml = mdToHtml(opts.contentMd);

  // Build stat cards or fallback
  let statsHtml: string;
  if (stats.length > 0) {
    statsHtml = stats
      .map(
        (s) => `<div class="cc-stat-card">
          <div class="cc-stat-value">${s.value}</div>
          <div class="cc-stat-label">${s.label}</div>
        </div>`
      )
      .join("");
  } else {
    // Render any markdown content as-is
    statsHtml = contentHtml
      ? `<div class="cc-content-fallback">${contentHtml}</div>`
      : `<div class="cc-stat-card cc-stat-placeholder"><div class="cc-stat-value">—</div><div class="cc-stat-label">أضف الإحصائيات</div></div>
         <div class="cc-stat-card cc-stat-placeholder"><div class="cc-stat-value">—</div><div class="cc-stat-label">أضف الإحصائيات</div></div>`;
  }

  return `
  <section class="section section-comp theme-navy">
    ${navStrip(opts.type, "navy")}
    <div class="cc-layout">
      <!-- Big vertical title on the RIGHT (RTL start) -->
      <div class="cc-title-col">
        <div class="cc-big-title">${opts.titleAr}</div>
        <div class="cc-date-badge">${opts.arabicMonth} ${opts.year}</div>
      </div>
      <!-- Stat content on the LEFT (RTL end) -->
      <div class="cc-content-col">
        <div class="cc-callout">من منطلق حرص الهيئة على نشر ثقافة المنافسة</div>
        <div class="cc-stats-grid">${statsHtml}</div>
      </div>
    </div>
  </section>`;
}

/** Outside-the-box — light bg, navy titlebar spanning full width, portrait + box icon + body. */
function sectionOutsideBoxHtml(opts: {
  titleAr: string;
  descriptionAr?: string | null;
  contentHtml: string;
  type: string;
  media?: Array<{ url: string; caption: string | null }>;
}): string {
  const portrait = (opts.media ?? [])[0];
  const portraitBlock = portrait
    ? `<div class="ob-portrait" style="background:url('${portrait.url}') center/cover no-repeat;"></div>`
    : `<div class="ob-portrait">
          <svg viewBox="0 0 120 160" xmlns="http://www.w3.org/2000/svg">
            <rect width="120" height="160" fill="${NAVY}"/>
            <rect x="18" y="105" width="84" height="55" rx="8" fill="${TEAL_DARK}" opacity="0.8"/>
            <rect x="46" y="90" width="28" height="22" rx="4" fill="#c8956a"/>
            <ellipse cx="60" cy="76" rx="28" ry="30" fill="#c8956a"/>
            <ellipse cx="60" cy="52" rx="30" ry="15" fill="white" opacity="0.92"/>
            <rect x="30" y="52" width="60" height="10" rx="3" fill="white" opacity="0.88"/>
            <ellipse cx="60" cy="52" rx="22" ry="7" fill="${NAVY}" opacity="0.8"/>
            <path d="M40 120 L60 108 L80 120" stroke="white" stroke-width="3" fill="none" opacity="0.5"/>
          </svg>
        </div>`;
  const obCaption = portrait?.caption || opts.descriptionAr || "مقال شهري من موظف";
  return `
  <section class="section section-outside theme-light">
    ${navStrip(opts.type, "light")}
    <div class="ob-titlebar">
      <div class="ob-title-text">${opts.titleAr}</div>
    </div>
    <div class="ob-grid">
      <div class="ob-portrait-col">
        ${portraitBlock}
        <div class="ob-portrait-caption">${obCaption}</div>
      </div>
      <div class="ob-center-col">
        <div class="ob-box-icon">${sectionIconSvg("box")}</div>
        <div class="ob-box-wordmark">خارج<br/>الصندوق</div>
      </div>
      <div class="ob-body">${opts.contentHtml}</div>
    </div>
  </section>`;
}

/** Events — full teal page with CSS bunting banner + photo collage placeholders. */
function sectionEventsHtml(opts: {
  titleAr: string;
  contentHtml: string;
  type: string;
  media?: Array<{ url: string; caption: string | null }>;
}): string {
  // Generate a collage of photo tiles with tilts. Use real media if provided.
  const placeholderPhotos = [
    { label: "إفطار رمضان", rotate: "-3deg", grad: `${TEAL_DARK}` },
    { label: "الاحتفال بعيد الفطر", rotate: "2deg", grad: `${NAVY}` },
    { label: "فعالية الهيئة", rotate: "-1.5deg", grad: `${TEAL}` },
    { label: "لقاء المنافسة", rotate: "3deg", grad: `${TEAL_DARK}` },
    { label: "فعالية داخلية", rotate: "-2deg", grad: `${DEEPNAVY}` },
    { label: "نشاط مؤسسي", rotate: "1.5deg", grad: `${NAVY}` },
  ];
  const media = opts.media ?? [];

  const collageTiles = placeholderPhotos
    .map((ph, idx) => {
      const photo = media[idx];
      const tileBg = photo
        ? `background: url('${photo.url}') center/cover no-repeat`
        : `background: linear-gradient(145deg, ${ph.grad} 0%, ${TEAL_DARK} 100%)`;
      const label = photo?.caption || ph.label;
      return `<div class="ev-photo-tile" style="
        transform: rotate(${ph.rotate});
        grid-area: auto;
        ${tileBg};
      ">
        ${photo ? "" : '<div class="ev-photo-inner"><div class="ev-photo-pattern"></div></div>'}
        <div class="ev-photo-label">${label}</div>
      </div>`;
    })
    .join("");

  return `
  <section class="section section-events theme-teal">
    ${navStrip(opts.type, "teal")}
    <!-- Bunting banner -->
    <div class="ev-banner">
      <div class="ev-bunting-svg">${sectionIconSvg("bunting")}</div>
      <h2 class="ev-title">${opts.titleAr}</h2>
    </div>
    <!-- Photo collage grid -->
    <div class="ev-collage">
      ${collageTiles}
    </div>
    ${opts.contentHtml ? `<div class="ev-extra">${opts.contentHtml}</div>` : ""}
  </section>`;
}

/** Employee Q&A — light bg, teal title plate + speech bubble icon + alternating Q/A bubbles. */
function sectionEmployeeQAHtml(opts: {
  titleAr: string;
  descriptionAr?: string | null;
  contentMd: string;
  type: string;
  media?: Array<{ url: string; caption: string | null }>;
}): string {
  const pairs = parseQAPairs(opts.contentMd);

  let bubblesHtml: string;
  if (pairs.length === 0) {
    // Fallback: render parsed markdown
    bubblesHtml = mdToHtml(opts.contentMd);
  } else {
    bubblesHtml = pairs
      .map(
        (pair) => `
      ${pair.q ? `<div class="qa-bubble qa-bubble-q"><span class="qa-label-q">س</span><span class="qa-text">${pair.q}</span></div>` : ""}
      ${pair.a ? `<div class="qa-bubble qa-bubble-a"><span class="qa-label-a">ج</span><span class="qa-text">${pair.a}</span></div>` : ""}
      `
      )
      .join("");
  }

  // Caption/name label below portrait
  const portrait = (opts.media ?? [])[0];
  const captionText = portrait?.caption || opts.descriptionAr || "ست أسئلة سريعة مع أحد الزملاء";
  const portraitBlock = portrait
    ? `<div class="qa-portrait" style="background:url('${portrait.url}') center/cover no-repeat;"></div>`
    : `<div class="qa-portrait">
          <svg viewBox="0 0 120 160" xmlns="http://www.w3.org/2000/svg">
            <rect width="120" height="160" fill="${MINT}"/>
            <rect x="20" y="104" width="80" height="56" rx="8" fill="${TEAL}" opacity="0.65"/>
            <rect x="46" y="88" width="28" height="22" rx="4" fill="#d4a87a"/>
            <ellipse cx="60" cy="74" rx="27" ry="29" fill="#d4a87a"/>
            <ellipse cx="60" cy="50" rx="29" ry="14" fill="white" opacity="0.9"/>
            <rect x="31" y="50" width="58" height="9" rx="2" fill="white" opacity="0.88"/>
            <ellipse cx="60" cy="50" rx="20" ry="6" fill="${NAVY}" opacity="0.75"/>
          </svg>
        </div>`;

  return `
  <section class="section section-qa theme-light">
    ${navStrip(opts.type, "light")}
    <div class="qa-titlebar">
      <h2 class="qa-title">${opts.titleAr}</h2>
      <div class="qa-speech-icon">${sectionIconSvg("speech")}</div>
    </div>
    <div class="qa-grid">
      <div class="qa-portrait-col">
        ${portraitBlock}
        <div class="qa-name">${captionText}</div>
        <div class="qa-qr">
          <div class="qa-qr-box">
            <div class="qa-qr-grid"></div>
          </div>
          <div class="qa-qr-caption">للمشاركة في شرفة يسعدنا تواصلك عبر مسح رمز QR</div>
        </div>
      </div>
      <div class="qa-body">
      ${bubblesHtml}
      </div>
    </div>
  </section>`;
}

/** Generic fallback for unknown section types. */
function sectionGenericHtml(opts: {
  titleAr: string;
  contentHtml: string;
  type: string;
  iconSvgType?: string;
}): string {
  return `
  <section class="section section-generic theme-light">
    ${navStrip(opts.type, "light")}
    <div class="sec-hero">
      ${opts.iconSvgType ? `<div class="sec-icon-wrap">${sectionIconSvg(opts.iconSvgType)}</div>` : ""}
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
  media?: Array<{ url: string; caption: string | null }>;
}, ctx: { arabicMonth: string; year: number }): string {
  const contentHtml = mdToHtml(s.contentMd || "");
  const contentMd = s.contentMd || "";
  const media = s.media ?? [];
  const common = {
    type: s.sectionType,
    titleAr: s.titleAr,
    descriptionAr: s.descriptionAr,
    contentHtml,
    contentMd,
  };
  switch (s.sectionType) {
    case "news":
    case "local_news":
    case "regional_news":
    case "global_news":
      return sectionNewsHtml({ type: s.sectionType, titleAr: s.titleAr, contentMd, media });
    case "office_interview":
      return sectionOfficeInterviewHtml({ ...common, media });
    case "competition_culture":
      return sectionCompetitionCultureHtml({ type: s.sectionType, titleAr: s.titleAr, contentMd, arabicMonth: ctx.arabicMonth, year: ctx.year });
    case "outside_box":
      return sectionOutsideBoxHtml({ ...common, media });
    case "events":
      return sectionEventsHtml({ type: s.sectionType, titleAr: s.titleAr, contentHtml, media });
    case "employee_qa":
      return sectionEmployeeQAHtml({ type: s.sectionType, titleAr: s.titleAr, descriptionAr: s.descriptionAr, contentMd, media });
    default:
      return sectionGenericHtml({ type: s.sectionType, titleAr: s.titleAr, contentHtml, iconSvgType: SECTION_ICON[s.sectionType] });
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

  /* Ribbon date — top-LEFT visually (matches sample-1: ribbon on left, logo on right) */
  .cv-ribbon {
    position: absolute; top: 0; left: 28mm;
    background: ${NAVY}; color: #fff;
    padding: 14mm 7mm 10mm; min-width: 28mm;
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

  /* Brand — top-RIGHT visually (matches sample-1) */
  .cv-brand {
    position: absolute; top: 14mm; right: 14mm;
    display: flex; align-items: center; gap: 4mm;
    direction: ltr;
  }
  .cv-brand-text { text-align: right; direction: rtl; }
  .cv-brand-ar { font-size: 10pt; font-weight: 700; }
  .cv-brand-en { font-size: 7pt; font-weight: 400; opacity: 0.85; margin-top: 1mm; }
  .cv-brand-mark { width: 16mm; height: 16mm; flex-shrink: 0; }
  .cv-brand-mark svg { width: 100%; height: 100%; }

  /* Hero — newspaper SVG + wordmark, vertically stacked */
  .cv-hero {
    position: absolute; top: 48mm; left: 0; right: 0;
    display: flex; flex-direction: column; align-items: center; justify-content: flex-start;
    gap: 0mm;
    height: 110mm;
  }
  .cv-newspaper-wrap {
    width: 110mm; height: 70mm;
    display: flex; align-items: center; justify-content: center;
    margin-bottom: -8mm;
  }
  .cv-newspaper-wrap svg { width: 100%; height: 100%; }
  .cv-title {
    position: relative; z-index: 2;
    font-family: "Tajawal", "Noto Sans Arabic", sans-serif;
    font-size: 78pt; font-weight: 900; line-height: 1;
    margin: 0; color: #fff;
    text-shadow: 0 4mm 10mm rgba(0,0,0,0.2);
    letter-spacing: -1pt;
    text-align: center;
  }

  .cv-subtitle {
    position: absolute; top: 175mm; left: 0; right: 0;
    text-align: center; font-size: 11pt; font-weight: 600;
    color: #fff; opacity: 0.97;
    padding: 0 30mm; line-height: 1.5;
  }

  /* Tabs strip with badge */
  .cv-tabs-row {
    position: absolute; bottom: 60mm; left: 14mm; right: 14mm;
    display: flex; align-items: center; gap: 6mm;
    direction: rtl;
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
    flex-shrink: 0;
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
    display: flex; justify-content: flex-end; gap: 10mm;
    padding: 5mm 14mm; font-size: 9.5pt; font-weight: 700;
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
    width: 210mm; height: 297mm;
    page-break-after: always;
    page-break-inside: avoid;
    overflow: hidden;
  }
  .theme-light { background: ${OFFWHITE}; color: ${NAVY}; }
  .theme-navy  { background: ${NAVY}; color: ${MINT}; }
  .theme-teal  { background: ${TEAL}; color: #fff; }

  .sec-hero {
    display: flex; align-items: center; gap: 8mm;
    padding: 10mm 14mm 6mm;
    direction: rtl;
  }
  .sec-icon-wrap { width: 40mm; height: 40mm; flex-shrink: 0; }
  .sec-title {
    font-size: 52pt; font-weight: 900; margin: 0;
    color: ${TEAL}; line-height: 1;
  }
  .sec-body {
    padding: 0 14mm 18mm;
    font-size: 11pt; line-height: 1.85;
    direction: rtl;
    text-align: right;
  }
  .sec-body h3 { color: ${TEAL}; font-size: 14pt; margin-top: 8mm; font-weight: 800; }
  .sec-body h4 { color: ${TEAL}; font-size: 12pt; margin-top: 6mm; font-weight: 800; }
  .sec-body p { margin: 4mm 0; text-align: justify; }
  .sec-body ul { padding-right: 6mm; margin: 4mm 0; }
  .sec-body li { margin: 2mm 0; }
  .sec-body strong { color: ${TEAL}; font-weight: 800; }

  /* ─── News section — 2-column card grid ──────────────────── */
  .section-news .sec-hero { justify-content: center; padding: 12mm 14mm 6mm; }
  .section-news .sec-title-news { font-size: 64pt; text-align: center; }
  .section-news .sec-icon-wrap { width: 55mm; height: 55mm; transform: rotate(-3deg); }
  .section-news .sec-body-news { padding: 4mm 14mm 14mm; }

  .news-grid {
    display: grid;
    grid-template-columns: 1fr 1fr;
    gap: 6mm;
    direction: rtl;
  }
  .news-card {
    background: #fff;
    border-radius: 3mm;
    overflow: hidden;
    box-shadow: 0 2mm 8mm rgba(14,59,74,0.10);
    display: flex; flex-direction: column;
  }
  .news-card-photo {
    width: 100%; height: 45mm;
    position: relative; overflow: hidden;
    flex-shrink: 0;
  }
  .news-card-photo-pattern {
    position: absolute; inset: 0;
    background-image:
      repeating-linear-gradient(45deg, transparent 0 8px, rgba(255,255,255,0.05) 8px 9px);
  }
  .news-card-title {
    background: ${MINT};
    color: ${NAVY};
    padding: 3mm 5mm;
    font-size: 11pt; font-weight: 800;
    text-align: right;
    line-height: 1.4;
    direction: rtl;
  }
  .news-card-body {
    padding: 4mm 5mm;
    font-size: 9pt; line-height: 1.75;
    color: ${NAVY};
    text-align: justify;
    direction: rtl;
    flex: 1;
  }
  .news-single {
    direction: rtl;
  }
  .news-single p {
    background: ${MINT}; padding: 5mm; border-radius: 2mm;
    margin: 3mm 0; text-align: justify;
  }
  .news-single h3 {
    color: ${NAVY}; border-right: 2mm solid ${TEAL}; padding-right: 4mm; margin-top: 6mm;
  }

  /* ─── Office interview (light, navy title bar) ─────────────── */
  .section-office .oi-titlebar {
    background: ${NAVY}; color: #fff;
    padding: 6mm 14mm; margin: 0 0 6mm;
    font-size: 20pt; font-weight: 900;
    text-align: right;
    direction: rtl;
    line-height: 1.4;
  }
  .section-office .oi-grid {
    display: grid; grid-template-columns: 58mm 1fr;
    gap: 8mm; padding: 0 14mm 18mm;
    direction: rtl;
  }
  .section-office .oi-left { display: flex; flex-direction: column; }
  .section-office .oi-portrait-placeholder {
    width: 58mm; height: 78mm;
    border-radius: 3mm; overflow: hidden;
    background: ${MINT}; flex-shrink: 0;
  }
  .section-office .oi-portrait-placeholder svg { width: 100%; height: 100%; display: block; }
  .section-office .oi-caption {
    font-weight: 800; font-size: 11pt; color: ${NAVY};
    margin-top: 4mm; padding-right: 3mm;
    border-right: 1.5mm solid ${TEAL};
    text-align: right; direction: rtl;
  }
  .section-office .oi-right {
    font-size: 10pt; line-height: 1.85;
    column-count: 2; column-gap: 6mm;
    text-align: justify;
    direction: rtl;
  }
  .section-office .oi-right h3 {
    color: ${NAVY}; font-size: 11pt; font-weight: 900;
    margin: 4mm 0 2mm; break-after: avoid;
    column-span: all;
  }
  .section-office .oi-right p { margin: 2mm 0; }
  .section-office .oi-right strong { color: ${TEAL}; }

  /* ─── Competition culture (full navy, big vertical title) ─── */
  .section-comp { background: ${NAVY}; }
  .cc-layout {
    display: grid;
    grid-template-columns: 1fr 70mm;
    gap: 8mm;
    padding: 10mm 14mm 14mm;
    min-height: 260mm;
    direction: rtl;
  }
  /* Right col = RTL start = BIG title */
  .cc-title-col {
    display: flex; flex-direction: column;
    align-items: flex-start; justify-content: center;
    grid-column: 2;
    grid-row: 1;
  }
  .cc-big-title {
    font-size: 52pt; font-weight: 900;
    color: #fff; line-height: 1.05;
    text-align: right;
    direction: rtl;
    word-break: keep-all;
  }
  .cc-date-badge {
    background: ${CYAN}; color: ${NAVY};
    padding: 2mm 6mm; display: inline-block;
    font-size: 20pt; font-weight: 900;
    margin-top: 6mm; border-radius: 1mm;
    direction: ltr;
  }
  /* Left col = RTL end = stats */
  .cc-content-col {
    grid-column: 1;
    grid-row: 1;
    display: flex; flex-direction: column; gap: 6mm;
  }
  .cc-callout {
    background: ${MINT}; color: ${NAVY};
    padding: 6mm; border-radius: 2mm;
    font-size: 11pt; line-height: 1.6; font-weight: 600;
    text-align: right; direction: rtl;
  }
  .cc-stats-grid {
    display: grid; grid-template-columns: 1fr 1fr;
    gap: 5mm;
  }
  .cc-stat-card {
    background: rgba(255,255,255,0.08);
    border: 0.5mm solid rgba(62,192,208,0.35);
    border-radius: 2mm;
    padding: 6mm; text-align: center;
  }
  .cc-stat-placeholder { opacity: 0.5; }
  .cc-stat-value {
    font-size: 48pt; font-weight: 900;
    color: ${CYAN}; line-height: 1;
    direction: ltr;
  }
  .cc-stat-label {
    font-size: 10pt; color: ${MINT};
    margin-top: 2mm; line-height: 1.4;
  }
  .cc-content-fallback {
    color: ${MINT}; font-size: 10pt; line-height: 1.75;
    text-align: right; direction: rtl;
  }
  .cc-content-fallback h3 { color: ${CYAN}; }
  .cc-content-fallback p { margin: 3mm 0; }

  /* ─── Outside the box ──────────────────────────────────────── */
  .section-outside .ob-titlebar {
    background: ${NAVY}; color: #fff;
    padding: 7mm 14mm; margin: 0 0 6mm;
    font-size: 19pt; font-weight: 900;
    text-align: right; direction: rtl;
    line-height: 1.4;
  }
  .section-outside .ob-grid {
    display: grid;
    grid-template-columns: 58mm 46mm 1fr;
    gap: 6mm;
    padding: 0 14mm 18mm;
    direction: rtl;
  }
  .section-outside .ob-portrait-col { display: flex; flex-direction: column; }
  .section-outside .ob-portrait {
    width: 58mm; height: 80mm;
    border-radius: 3mm; overflow: hidden;
    background: ${NAVY};
  }
  .section-outside .ob-portrait svg { width: 100%; height: 100%; display: block; }
  .section-outside .ob-portrait-caption {
    font-weight: 800; font-size: 11pt; color: ${NAVY};
    margin-top: 4mm; text-align: center;
    background: ${MINT}; padding: 3mm; border-radius: 1.5mm;
  }
  .section-outside .ob-center-col {
    display: flex; flex-direction: column;
    align-items: center; justify-content: center; gap: 4mm;
  }
  .section-outside .ob-box-icon { width: 38mm; height: 38mm; }
  .section-outside .ob-box-wordmark {
    font-size: 22pt; font-weight: 900; color: ${NAVY};
    text-align: center; line-height: 1.2;
    background: ${MINT}; padding: 3mm 5mm; border-radius: 2mm;
  }
  .section-outside .ob-body {
    font-size: 10pt; line-height: 1.85;
    text-align: justify; direction: rtl;
  }
  .section-outside .ob-body p { margin: 2mm 0; }
  .section-outside .ob-body strong { color: ${TEAL}; }

  /* ─── Events (full teal, bunting, collage) ─────────────────── */
  .section-events { background: ${TEAL}; }
  .section-events .ev-banner {
    margin: 8mm 14mm 8mm;
    background: ${MINT};
    padding: 8mm 10mm 6mm;
    border-radius: 2mm;
    text-align: center;
    position: relative;
    overflow: hidden;
  }
  .section-events .ev-bunting-svg {
    position: absolute; top: 0; left: 0; right: 0;
    height: 16mm; overflow: hidden;
  }
  .section-events .ev-bunting-svg svg { width: 100%; height: 100%; }
  .section-events .ev-title {
    font-size: 54pt; font-weight: 900;
    color: ${TEAL}; margin: 10mm 0 0;
    text-shadow: none;
    line-height: 1;
    position: relative; z-index: 2;
  }

  .section-events .ev-collage {
    margin: 6mm 14mm;
    display: grid;
    grid-template-columns: 1fr 1fr 1fr;
    grid-template-rows: 52mm 52mm;
    gap: 4mm;
  }
  .ev-photo-tile {
    border-radius: 2mm;
    overflow: hidden;
    position: relative;
    display: flex; flex-direction: column;
    box-shadow: 0 3mm 10mm rgba(0,0,0,0.25);
  }
  .ev-photo-inner {
    flex: 1;
    position: relative;
    overflow: hidden;
    min-height: 36mm;
  }
  .ev-photo-pattern {
    position: absolute; inset: 0;
    background-image: repeating-linear-gradient(45deg,
      transparent 0 10px,
      rgba(255,255,255,0.06) 10px 11px);
  }
  .ev-photo-label {
    background: rgba(255,255,255,0.9);
    color: ${NAVY};
    padding: 2mm 4mm;
    font-size: 8.5pt; font-weight: 700;
    text-align: center;
    direction: rtl;
  }
  .section-events .ev-extra {
    margin: 4mm 14mm 8mm;
    color: #fff; font-size: 10pt; line-height: 1.75;
    text-align: right; direction: rtl;
  }

  /* ─── Employee QA (light, teal title plate, speech bubbles) ── */
  .section-qa .qa-titlebar {
    background: ${TEAL}; color: #fff;
    padding: 4mm 14mm; margin: 0 0 5mm;
    border-radius: 0;
    display: flex; align-items: center; justify-content: space-between;
    direction: rtl;
    gap: 6mm;
  }
  .section-qa .qa-title { margin: 0; color: #fff; font-size: 28pt; font-weight: 900; flex: 1; }
  .section-qa .qa-speech-icon { width: 20mm; height: 20mm; flex-shrink: 0; }

  .section-qa .qa-grid {
    display: grid; grid-template-columns: 50mm 1fr;
    gap: 6mm; padding: 0 14mm 8mm;
    direction: rtl;
  }
  .section-qa .qa-portrait-col { display: flex; flex-direction: column; }
  .section-qa .qa-portrait {
    width: 50mm; height: 60mm;
    border-radius: 3mm; overflow: hidden;
    background: ${MINT};
  }
  .section-qa .qa-portrait svg { width:100%; height:100%; display:block; }
  .section-qa .qa-name {
    font-weight: 800; font-size: 11pt;
    margin-top: 4mm; text-align: center;
    background: ${TEAL}; color: #fff;
    padding: 3mm; border-radius: 1mm;
  }
  .section-qa .qa-qr { margin-top: 5mm; background: ${MINT}; padding: 4mm; border-radius: 1.5mm; text-align: center; }
  .section-qa .qa-qr-box { width: 28mm; height: 28mm; margin: 0 auto 3mm; background: #fff; padding: 2mm; }
  .section-qa .qa-qr-grid {
    width: 100%; height: 100%;
    background-image:
      repeating-linear-gradient(0deg, ${NAVY} 0 1.5mm, transparent 1.5mm 3mm),
      repeating-linear-gradient(90deg, ${NAVY} 0 1.5mm, transparent 1.5mm 3mm);
    background-blend-mode: multiply;
  }
  .section-qa .qa-qr-caption { font-size: 7.5pt; color: ${NAVY}; line-height: 1.4; margin-top: 2mm; }

  /* Q&A speech bubbles */
  .section-qa .qa-body {
    display: flex; flex-direction: column; gap: 2mm;
    direction: rtl;
  }
  .qa-bubble {
    padding: 2.5mm 4mm;
    border-radius: 2mm;
    font-size: 9.5pt; line-height: 1.5;
    display: flex; align-items: flex-start; gap: 3mm;
    position: relative;
  }
  .qa-bubble-q {
    background: ${MINT};
    color: ${NAVY};
    margin-right: 0;
    margin-left: 8mm;
    border-radius: 2mm 0 2mm 2mm;
  }
  .qa-bubble-q::after {
    content: "";
    position: absolute; top: 0; left: -6mm;
    border-style: solid; border-width: 5mm 6mm 0 0;
    border-color: ${MINT} transparent transparent transparent;
  }
  .qa-bubble-a {
    background: ${TEAL};
    color: #fff;
    margin-left: 0;
    margin-right: 8mm;
    border-radius: 0 2mm 2mm 2mm;
  }
  .qa-bubble-a::after {
    content: "";
    position: absolute; top: 0; right: -6mm;
    border-style: solid; border-width: 5mm 0 0 6mm;
    border-color: ${TEAL} transparent transparent transparent;
  }
  .qa-label-q {
    background: ${TEAL}; color: #fff;
    font-size: 9pt; font-weight: 900;
    padding: 1mm 2.5mm; border-radius: 1mm;
    flex-shrink: 0; align-self: flex-start;
    margin-top: 0.5mm;
  }
  .qa-label-a {
    background: rgba(255,255,255,0.25); color: #fff;
    font-size: 9pt; font-weight: 900;
    padding: 1mm 2.5mm; border-radius: 1mm;
    flex-shrink: 0; align-self: flex-start;
    margin-top: 0.5mm;
  }
  .qa-text { flex: 1; text-align: right; }
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
    media?: Array<{ url: string; caption: string | null }>;
  }>;
  /** Base URL for static assets (e.g. /shorfah/*.png). Defaults to empty (relative). */
  baseUrl?: string;
}): string {
  // baseUrl is accepted but no longer needed (all icons are inline SVG)
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
