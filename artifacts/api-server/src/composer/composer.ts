/**
 * Server-side design composer.
 *
 * Renders a design template (background panel + text slots + logos + optional
 * presentation extras) into a final PNG using sharp + Pango.
 *
 * Ported from design-studio/server/composer.ts and adapted to the icbank
 * schema (which uses { key, label_ar, default_font_size, alignment, max_words }
 * for text slots and either { width, height } or { maxWidth, maxHeight, align,
 * tintColor } for logo slots).
 */
import sharp from "sharp";
import fs from "fs";
import path from "path";
import type {
  DesignTemplate,
  BrandLogo,
  TextSlot,
  LogoSlot,
  GradientHeader,
  ImagePlaceholder,
  DepartmentBadge,
  ContentPanel,
  VerticalSeparator,
} from "@workspace/db";
import { normalizeDashes, isArabicText } from "./gac-palette";

export interface ComposeInput {
  template: DesignTemplate;
  backgroundBuffer: Buffer;
  titleText: string;
  bodyText: string;
  titleFontSize?: number;
  bodyFontSize?: number;
  fontFamily?: string;
  selectedLogoBuffers?: { buffer: Buffer; logo: BrandLogo }[];
  department?: string | null;
}

/* ───── helpers ───── */
function pct(v: number, total: number): number {
  return Math.round((v / 100) * total);
}

function parseColor(input: string): { r: number; g: number; b: number; a: number } {
  const s = (input || "").trim();
  let m = s.match(/^#?([0-9a-f]{3}|[0-9a-f]{6}|[0-9a-f]{8})$/i);
  if (m) {
    let hex = m[1];
    if (hex.length === 3) hex = hex.split("").map((c) => c + c).join("");
    const r = parseInt(hex.slice(0, 2), 16);
    const g = parseInt(hex.slice(2, 4), 16);
    const b = parseInt(hex.slice(4, 6), 16);
    const a = hex.length === 8 ? parseInt(hex.slice(6, 8), 16) / 255 : 1;
    return { r, g, b, a };
  }
  m = s.match(/^rgba?\(\s*(\d+)\s*,\s*(\d+)\s*,\s*(\d+)\s*(?:,\s*([\d.]+)\s*)?\)$/i);
  if (m) {
    return {
      r: parseInt(m[1]),
      g: parseInt(m[2]),
      b: parseInt(m[3]),
      a: m[4] != null ? parseFloat(m[4]) : 1,
    };
  }
  return { r: 0, g: 0, b: 0, a: 1 };
}

async function makePanel(
  width: number,
  height: number,
  colorStr: string,
  opacity: number,
  borderRadius: number,
): Promise<Buffer> {
  const c = parseColor(colorStr);
  const a = Math.max(0, Math.min(1, c.a * opacity));
  const svg = `<svg xmlns="http://www.w3.org/2000/svg" width="${width}" height="${height}">
    <rect x="0" y="0" width="${width}" height="${height}"
      rx="${borderRadius}" ry="${borderRadius}"
      fill="rgba(${c.r},${c.g},${c.b},${a})"/>
  </svg>`;
  return sharp(Buffer.from(svg)).png().toBuffer();
}

async function makeDepartmentBadgeFloating(
  text: string,
  fontFamily: string,
): Promise<Buffer> {
  const padX = 28;
  const padY = 12;
  const fontSize = 28;
  const SS = 2;
  const escaped = text.replace(/&/g, "&amp;").replace(/</g, "&lt;").replace(/>/g, "&gt;");
  const textImgHi = await sharp({
    text: {
      text: `<span font_family="${fontFamily}" font_weight="600" foreground="#FFFFFF" size="${fontSize * SS * 1024}">${escaped}</span>`,
      rgba: true,
      dpi: 72,
    },
  }).png().toBuffer();
  const hiMeta = await sharp(textImgHi).metadata();
  const tw = Math.round((hiMeta.width || 200) / SS);
  const th = Math.round((hiMeta.height || fontSize) / SS);
  const textImg = await sharp(textImgHi)
    .resize(tw, th, { kernel: "lanczos3", fit: "fill" })
    .png().toBuffer();
  const w = tw + padX * 2;
  const h = th + padY * 2;
  const bgSvg = `<svg xmlns="http://www.w3.org/2000/svg" width="${w}" height="${h}">
    <rect x="0" y="0" width="${w}" height="${h}" rx="8" ry="8"
      fill="rgba(0,0,0,0.55)" stroke="rgba(255,255,255,0.18)" stroke-width="1"/>
  </svg>`;
  const bg = await sharp(Buffer.from(bgSvg)).png().toBuffer();
  return sharp(bg).composite([{ input: textImg, top: padY, left: padX }]).png().toBuffer();
}

async function renderText(opts: {
  text: string;
  width: number;
  height: number;
  fontSize: number;
  minFontSize: number;
  maxFontSize: number;
  color: string;
  fontFamily: string;
  fontWeight: string | number;
  textAlign: "right" | "center" | "left";
  lineHeight: number;
}): Promise<Buffer> {
  const { text, width, height, color, fontFamily, lineHeight } = opts;
  if (!text.trim() || width <= 0 || height <= 0) {
    return sharp({
      create: {
        width: Math.max(1, width),
        height: Math.max(1, height),
        channels: 4,
        background: { r: 0, g: 0, b: 0, alpha: 0 },
      },
    }).png().toBuffer();
  }

  const weight =
    typeof opts.fontWeight === "number"
      ? opts.fontWeight >= 700
        ? "bold"
        : opts.fontWeight >= 500
          ? "500"
          : "normal"
      : ["bold", "700", "800", "900"].includes(String(opts.fontWeight))
        ? "bold"
        : "normal";

  const pangoAlign =
    opts.textAlign === "center" ? "centre" : opts.textAlign === "left" ? "left" : "right";

  const escaped = text.replace(/&/g, "&amp;").replace(/</g, "&lt;").replace(/>/g, "&gt;");

  const SS = 2;
  const ssWidth = width * SS;
  const ssHeight = height * SS;

  let size = Math.min(opts.maxFontSize, Math.max(opts.minFontSize, Math.round(opts.fontSize)));
  let attempts = 0;
  let buf: Buffer = Buffer.from([]);
  let tw = 0;
  let th = 0;

  const renderOnce = async (s: number) => {
    const markup = `<span font_family="${fontFamily}" font_weight="${weight}" foreground="${color}" size="${s * SS * 1024}" line_height="${lineHeight}">${escaped}</span>`;
    const out = await sharp({
      text: {
        text: markup,
        rgba: true,
        width: ssWidth,
        align: pangoAlign as any,
        dpi: 72,
        wrap: "word-char" as any,
        spacing: Math.round((lineHeight - 1) * s * SS),
      },
    }).png().toBuffer();
    const m = await sharp(out).metadata();
    return { out, w: m.width || 0, h: m.height || 0 };
  };

  while (attempts < 80) {
    try {
      const r = await renderOnce(size);
      buf = r.out;
      tw = r.w;
      th = r.h;
    } catch {
      size -= 4;
      if (size < opts.minFontSize) {
        size = opts.minFontSize;
        break;
      }
      attempts++;
      continue;
    }
    if (th <= ssHeight && tw <= ssWidth) break;
    size -= 4;
    if (size <= opts.minFontSize) {
      size = opts.minFontSize;
      break;
    }
    attempts++;
  }

  const finalR = await renderOnce(size);
  buf = finalR.out;
  tw = finalR.w;
  th = finalR.h;

  const scaledW = Math.max(1, Math.round(tw / SS));
  const scaledH = Math.max(1, Math.round(th / SS));
  const scaled = await sharp(buf)
    .resize(scaledW, scaledH, { kernel: "lanczos3", fit: "fill" })
    .png().toBuffer();

  const top = Math.max(0, Math.floor((height - scaledH) / 2));
  let left = 0;
  if (opts.textAlign === "right") left = Math.max(0, width - scaledW);
  else if (opts.textAlign === "center") left = Math.max(0, Math.floor((width - scaledW) / 2));

  return sharp({
    create: { width, height, channels: 4, background: { r: 0, g: 0, b: 0, alpha: 0 } },
  }).composite([{ input: scaled, top, left }]).png().toBuffer();
}

/* ───── Lucide icons ───── */
function resolveLucideIconsDir(): string | null {
  const candidates = [
    path.join(process.cwd(), "node_modules", "lucide-static", "icons"),
    "/var/task/node_modules/lucide-static/icons",
  ];
  try {
    const req = (globalThis as any).require;
    if (typeof req === "function") {
      const pkgPath = req.resolve("lucide-static/package.json");
      candidates.unshift(path.join(path.dirname(pkgPath), "icons"));
    }
  } catch {}
  for (const p of candidates) {
    try {
      if (fs.existsSync(p)) return p;
    } catch {}
  }
  return null;
}
const LUCIDE_DIR = resolveLucideIconsDir();

async function renderLucideIcon(
  name: string,
  size: number,
  color: string,
  strokeWidth: number,
): Promise<Buffer | null> {
  if (!LUCIDE_DIR) return null;
  const fileName = name.toLowerCase().trim().replace(/[^a-z0-9-]/g, "") + ".svg";
  const filePath = path.join(LUCIDE_DIR, fileName);
  if (!fs.existsSync(filePath)) return null;
  try {
    let svg = fs.readFileSync(filePath, "utf-8");
    svg = svg.replace(/stroke="currentColor"/g, `stroke="${color}"`);
    svg = svg.replace(/stroke-width="[^"]*"/g, `stroke-width="${strokeWidth}"`);
    return sharp(Buffer.from(svg), { density: 300 })
      .resize(size, size, { fit: "contain", background: { r: 0, g: 0, b: 0, alpha: 0 } })
      .png().toBuffer();
  } catch {
    return null;
  }
}

/* ───── extras renderers ───── */
async function makeGradientHeader(width: number, height: number, cfg: GradientHeader): Promise<Buffer> {
  const direction = cfg.direction || "horizontal";
  let x1 = 0, y1 = 0, x2 = width, y2 = 0;
  if (direction === "vertical") { x2 = 0; y2 = height; }
  else if (direction === "diagonal") { x2 = width; y2 = height; }
  const svg = `<svg xmlns="http://www.w3.org/2000/svg" width="${width}" height="${height}">
    <defs>
      <linearGradient id="g" x1="${x1}" y1="${y1}" x2="${x2}" y2="${y2}" gradientUnits="userSpaceOnUse">
        <stop offset="0%" stop-color="${cfg.colorStart}"/>
        <stop offset="100%" stop-color="${cfg.colorEnd}"/>
      </linearGradient>
    </defs>
    <rect x="0" y="0" width="${width}" height="${height}" fill="url(#g)"/>
  </svg>`;
  return sharp(Buffer.from(svg)).png().toBuffer();
}

async function makeImagePlaceholder(
  width: number, height: number, cfg: ImagePlaceholder, fontFamily: string,
): Promise<Buffer> {
  const bg = await sharp(Buffer.from(
    `<svg xmlns="http://www.w3.org/2000/svg" width="${width}" height="${height}">
      <rect x="0" y="0" width="${width}" height="${height}"
        rx="${cfg.borderRadius || 0}" ry="${cfg.borderRadius || 0}"
        fill="${cfg.bgColor || "#E5E5E5"}"/>
    </svg>`,
  )).png().toBuffer();
  if (!cfg.label) return bg;
  const label = await renderText({
    text: cfg.label,
    width, height,
    fontSize: cfg.labelFontSize || 20,
    minFontSize: 14, maxFontSize: 60,
    color: cfg.labelColor || "#0E5F8B",
    fontFamily, fontWeight: 700,
    textAlign: "center",
    lineHeight: 1.2,
  });
  return sharp(bg).composite([{ input: label, top: 0, left: 0 }]).png().toBuffer();
}

async function makeSeparator(width: number, height: number, color: string): Promise<Buffer> {
  return sharp(Buffer.from(
    `<svg xmlns="http://www.w3.org/2000/svg" width="${width}" height="${height}">
      <rect x="0" y="0" width="${width}" height="${height}" fill="${color}"/>
    </svg>`,
  )).png().toBuffer();
}

async function makeContentPanel(width: number, height: number, cfg: ContentPanel): Promise<Buffer> {
  const c = parseColor(cfg.color);
  const a = Math.max(0, Math.min(1, c.a * (cfg.opacity ?? 1)));
  return sharp(Buffer.from(
    `<svg xmlns="http://www.w3.org/2000/svg" width="${width}" height="${height}">
      <rect x="0" y="0" width="${width}" height="${height}"
        rx="${cfg.borderRadius || 0}" ry="${cfg.borderRadius || 0}"
        fill="rgba(${c.r},${c.g},${c.b},${a})"/>
    </svg>`,
  )).png().toBuffer();
}

async function makeFixedDepartmentBadge(
  width: number, height: number, cfg: DepartmentBadge, text: string, fontFamily: string,
): Promise<Buffer> {
  const bg = await sharp(Buffer.from(
    `<svg xmlns="http://www.w3.org/2000/svg" width="${width}" height="${height}">
      <rect x="0" y="0" width="${width}" height="${height}"
        rx="${cfg.borderRadius || 0}" ry="${cfg.borderRadius || 0}"
        fill="${cfg.bgColor}"/>
    </svg>`,
  )).png().toBuffer();
  if (!text) return bg;
  const textImg = await renderText({
    text, width, height,
    fontSize: cfg.fontSize,
    minFontSize: 12, maxFontSize: 80,
    color: cfg.textColor,
    fontFamily, fontWeight: 700,
    textAlign: cfg.textAlign || "right",
    lineHeight: 1.2,
  });
  return sharp(bg).composite([{ input: textImg, top: 0, left: 0 }]).png().toBuffer();
}

async function fitImage(buffer: Buffer, maxW: number, maxH: number): Promise<{ buf: Buffer; w: number; h: number }> {
  const out = await sharp(buffer)
    .resize(maxW, maxH, { fit: "inside", withoutEnlargement: false })
    .png().toBuffer();
  const m = await sharp(out).metadata();
  return { buf: out, w: m.width || maxW, h: m.height || maxH };
}

/**
 * Re-tint opaque pixels of a PNG to `tintHex` while preserving the original
 * alpha channel.  Used to render the GAC logo as white on dark headers.
 */
async function tintImage(buffer: Buffer, tintHex: string): Promise<Buffer> {
  const c = parseColor(tintHex);
  const meta = await sharp(buffer).metadata();
  const w = meta.width || 1;
  const h = meta.height || 1;
  const alphaRaw = await sharp(buffer).ensureAlpha().extractChannel("alpha").raw().toBuffer();
  const solidRgb = await sharp({
    create: { width: w, height: h, channels: 3, background: { r: c.r, g: c.g, b: c.b } },
  }).raw().toBuffer();
  return sharp(solidRgb, { raw: { width: w, height: h, channels: 3 } })
    .joinChannel(alphaRaw, { raw: { width: w, height: h, channels: 1 } })
    .png().toBuffer();
}

/* ─────────────────────────────────────────────────────────────
 * Main: composeDesign
 * ───────────────────────────────────────────────────────────── */
export async function composeDesign(input: ComposeInput): Promise<Buffer> {
  const { template, department } = input;
  const fontFamily = input.fontFamily || "Tajawal";
  const extras = (template.extras || null) as DesignTemplate["extras"] | null;
  const isPresentationLayout =
    !!extras &&
    (extras.layoutKind === "presentation-paragraphs" ||
      extras.layoutKind === "presentation-icons-2x2");

  const CW = template.canvasWidth;
  const CH = template.canvasHeight;

  const titleText = normalizeDashes(input.titleText || "");
  const bodyText = normalizeDashes(input.bodyText || "");

  // 1) background
  let bgPng: Buffer;
  if (isPresentationLayout) {
    bgPng = await sharp({
      create: { width: CW, height: CH, channels: 4, background: { r: 255, g: 255, b: 255, alpha: 1 } },
    }).png().toBuffer();
  } else if (input.backgroundBuffer && input.backgroundBuffer.length > 0) {
    let bgInput: Buffer = input.backgroundBuffer;
    try {
      bgInput = await sharp(input.backgroundBuffer)
        .trim({ background: "#ffffff", threshold: 10 })
        .png().toBuffer();
    } catch {}
    bgPng = await sharp(bgInput)
      .resize(CW, CH, { fit: "cover", position: "center", kernel: "lanczos3" })
      .sharpen({ sigma: 0.6, m1: 0.5, m2: 2 })
      .png().toBuffer();
  } else {
    bgPng = await sharp({
      create: { width: CW, height: CH, channels: 4, background: { r: 245, g: 245, b: 245, alpha: 1 } },
    }).png().toBuffer();
  }

  const composites: sharp.OverlayOptions[] = [];

  // 2) extras (gradient header, image placeholder, separator, content panel, fixed badge)
  if (extras) {
    if (extras.gradientHeader) {
      const gh = extras.gradientHeader;
      const hh = pct(gh.heightPct, CH);
      composites.push({ input: await makeGradientHeader(CW, hh, gh), top: 0, left: 0 });
    }
    if (extras.imagePlaceholder) {
      const ip = extras.imagePlaceholder;
      const ipw = pct(ip.width, CW);
      const iph = pct(ip.height, CH);
      const ipx = pct(ip.x, CW);
      const ipy = pct(ip.y, CH);
      if (isPresentationLayout && input.backgroundBuffer && input.backgroundBuffer.length > 100) {
        try {
          const fitted = await sharp(input.backgroundBuffer)
            .resize(ipw, iph, { fit: "cover", position: "center", kernel: "lanczos3" })
            .png().toBuffer();
          composites.push({ input: fitted, top: ipy, left: ipx });
        } catch {
          const ph = await makeImagePlaceholder(ipw, iph, ip, fontFamily);
          composites.push({ input: ph, top: ipy, left: ipx });
        }
      } else {
        const ph = await makeImagePlaceholder(ipw, iph, ip, fontFamily);
        composites.push({ input: ph, top: ipy, left: ipx });
      }
    }
    if (extras.verticalSeparator) {
      const vs = extras.verticalSeparator;
      const vw = Math.max(1, pct(vs.width, CW));
      const vh = pct(vs.height, CH);
      composites.push({
        input: await makeSeparator(vw, vh, vs.color || "#CCCCCC"),
        top: pct(vs.y, CH),
        left: pct(vs.x, CW),
      });
    }
    if (extras.contentPanel) {
      const cp = extras.contentPanel;
      composites.push({
        input: await makeContentPanel(pct(cp.width, CW), pct(cp.height, CH), cp),
        top: pct(cp.y, CH),
        left: pct(cp.x, CW),
      });
    }
    if (extras.departmentBadge) {
      const db = extras.departmentBadge;
      const badgeText = department && department.trim() ? department.trim() : "";
      composites.push({
        input: await makeFixedDepartmentBadge(
          pct(db.width, CW), pct(db.height, CH),
          db, badgeText, fontFamily,
        ),
        top: pct(db.y, CH),
        left: pct(db.x, CW),
      });
    }
  }

  // 3) background panel (legacy social templates)
  const cfg = template.backgroundPanelConfig;
  if (cfg && cfg.width > 0 && cfg.height > 0) {
    composites.push({
      input: await makePanel(
        pct(cfg.width, CW), pct(cfg.height, CH),
        cfg.color, cfg.opacity, cfg.borderRadius || 0,
      ),
      top: pct(cfg.y, CH),
      left: pct(cfg.x, CW),
    });
  }

  // helpers to get slot fields from either schema shape
  const slotFontSize = (s: TextSlot) => s.default_font_size;
  const slotAlign = (s: TextSlot) => (s.alignment || "right") as "right" | "center" | "left";
  const slotMin = (s: TextSlot) => s.minFontSize ?? 14;
  const slotMax = (s: TextSlot) => s.maxFontSize ?? 140;
  const slotWeight = (s: TextSlot) => s.fontWeight ?? 700;
  const slotLineHeight = (s: TextSlot) => s.lineHeight ?? 1.3;

  const titleSlot =
    template.textSlots.find((s: TextSlot) => s.role === "title" || s.key === "title");
  const bodySlot =
    template.textSlots.find((s: TextSlot) => s.role === "body" || s.key === "body");

  const enforceLatinAlign = (text: string, align: "right" | "center" | "left") =>
    !isArabicText(text) && align === "center" ? "right" : align;

  if (titleSlot && titleText.trim()) {
    const w = pct(titleSlot.width, CW);
    const h = pct(titleSlot.height, CH);
    const fs = input.titleFontSize
      ? Math.min(slotMax(titleSlot), Math.max(slotMin(titleSlot), input.titleFontSize))
      : slotFontSize(titleSlot);
    composites.push({
      input: await renderText({
        text: titleText,
        width: w, height: h,
        fontSize: fs,
        minFontSize: slotMin(titleSlot),
        maxFontSize: slotMax(titleSlot),
        color: titleSlot.color || "#FFFFFF",
        fontFamily,
        fontWeight: slotWeight(titleSlot) as any,
        textAlign: enforceLatinAlign(titleText, slotAlign(titleSlot)),
        lineHeight: slotLineHeight(titleSlot),
      }),
      top: pct(titleSlot.y, CH),
      left: pct(titleSlot.x, CW),
    });
  }

  if (bodySlot && bodyText.trim()) {
    const w = pct(bodySlot.width, CW);
    const h = pct(bodySlot.height, CH);
    const fs = input.bodyFontSize
      ? Math.min(slotMax(bodySlot), Math.max(slotMin(bodySlot), input.bodyFontSize))
      : slotFontSize(bodySlot);
    composites.push({
      input: await renderText({
        text: bodyText,
        width: w, height: h,
        fontSize: fs,
        minFontSize: slotMin(bodySlot),
        maxFontSize: slotMax(bodySlot),
        color: bodySlot.color || "#FFFFFF",
        fontFamily,
        fontWeight: slotWeight(bodySlot) as any,
        textAlign: enforceLatinAlign(bodyText, slotAlign(bodySlot)),
        lineHeight: slotLineHeight(bodySlot),
      }),
      top: pct(bodySlot.y, CH),
      left: pct(bodySlot.x, CW),
    });
  }

  // floating department badge — only if no fixed badge in extras
  const hasFixedBadge = !!(extras && extras.departmentBadge);
  if (department && department.trim() && !hasFixedBadge) {
    try {
      const badge = await makeDepartmentBadgeFloating(department, fontFamily);
      composites.push({ input: badge, top: 40, left: 40 });
    } catch {}
  }

  // sub-heading + icon slots
  if (extras) {
    if (extras.subHeading && (extras.subHeading.text || "").trim()) {
      const sh = extras.subHeading;
      composites.push({
        input: await renderText({
          text: sh.text!,
          width: pct(sh.width, CW),
          height: pct(sh.height, CH),
          fontSize: sh.fontSize ?? 28,
          minFontSize: 14, maxFontSize: 80,
          color: sh.color ?? "#9DC41A",
          fontFamily,
          fontWeight: (sh.fontWeight ?? 700) as any,
          textAlign: (sh.textAlign || "right") as any,
          lineHeight: 1.2,
        }),
        top: pct(sh.y, CH),
        left: pct(sh.x, CW),
      });
    }

    if (extras.iconSlots && extras.iconSlots.length > 0) {
      for (const slot of extras.iconSlots) {
        const ix = pct(slot.x, CW);
        const iy = pct(slot.y, CH);
        const iconPng = await renderLucideIcon(
          slot.lucideName,
          slot.size,
          slot.color || "#0E5F8B",
          slot.strokeWidth || 1.5,
        );
        if (iconPng) composites.push({ input: iconPng, top: iy, left: ix });

        const textBoxW = pct(slot.textWidth ?? 22, CW);
        const titleFs = slot.titleFontSize ?? 22;
        const bodyFs = slot.bodyFontSize ?? 16;
        const titleHeight = Math.round(titleFs * 1.6);
        const titleX = ix + Math.floor(slot.size / 2) - Math.floor(textBoxW / 2);
        const titleY = iy + slot.size + 12;

        if (slot.titleText && slot.titleText.trim()) {
          composites.push({
            input: await renderText({
              text: slot.titleText,
              width: textBoxW, height: titleHeight,
              fontSize: titleFs,
              minFontSize: 12, maxFontSize: 80,
              color: slot.titleColor || "#0E5F8B",
              fontFamily, fontWeight: 800,
              textAlign: (slot.textAlign || "center") as any,
              lineHeight: 1.2,
            }),
            top: Math.max(0, Math.min(CH - titleHeight, titleY)),
            left: Math.max(0, Math.min(CW - textBoxW, titleX)),
          });
        }

        if (slot.bodyText && slot.bodyText.trim()) {
          const bodyHeight = Math.round(bodyFs * 1.5 * 4);
          const bodyY = titleY + titleHeight + 8;
          composites.push({
            input: await renderText({
              text: slot.bodyText,
              width: textBoxW, height: bodyHeight,
              fontSize: bodyFs,
              minFontSize: 12, maxFontSize: 60,
              color: slot.bodyColor || "#333333",
              fontFamily, fontWeight: 400,
              textAlign: (slot.textAlign || "center") as any,
              lineHeight: 1.4,
            }),
            top: Math.max(0, Math.min(CH - bodyHeight, bodyY)),
            left: Math.max(0, Math.min(CW - textBoxW, titleX)),
          });
        }
      }
    }
  }

  // 4) logos
  const logoBuffers = input.selectedLogoBuffers || [];
  if (logoBuffers.length > 0) {
    const first = logoBuffers[0];
    const primarySlot = template.logoSlots && template.logoSlots[0];
    if (primarySlot) {
      const maxW = primarySlot.maxWidth ?? primarySlot.width ?? 280;
      const maxH = primarySlot.maxHeight ?? primarySlot.height ?? 180;
      let { buf, w, h } = await fitImage(first.buffer, maxW, maxH);
      if (primarySlot.tintColor) buf = await tintImage(buf, primarySlot.tintColor);
      const xPx = pct(primarySlot.x, CW);
      const yPx = pct(primarySlot.y, CH);
      const align = primarySlot.align;
      let left = xPx;
      if (align === "right") left = xPx - w;
      else if (align === "center") left = xPx - Math.floor(w / 2);
      // Legacy templates without `align` interpret x/y as the top-left of the
      // logo bounding box in absolute px (template stored x/y as pixel values
      // when width/height were used). Use percent positioning anyway since
      // x/y are pct in the new schema.
      left = Math.max(0, Math.min(CW - w, left));
      const top = Math.max(0, Math.min(CH - h, yPx));
      composites.push({ input: buf, top, left });
    }

    const extraLogos = logoBuffers.slice(1);
    for (let i = 0; i < extraLogos.length; i++) {
      const slot = template.logoSlots[i + 1];
      if (!slot) break;
      const maxW = slot.maxWidth ?? slot.width ?? 200;
      const maxH = slot.maxHeight ?? slot.height ?? 130;
      let { buf, w, h } = await fitImage(extraLogos[i].buffer, maxW, maxH);
      if (slot.tintColor) buf = await tintImage(buf, slot.tintColor);
      const xPx = pct(slot.x, CW);
      const yPx = pct(slot.y, CH);
      let left = xPx;
      if (slot.align === "right") left = xPx - w;
      else if (slot.align === "center") left = xPx - Math.floor(w / 2);
      left = Math.max(0, Math.min(CW - w, left));
      const top = Math.max(0, Math.min(CH - h, yPx));
      composites.push({ input: buf, top, left });
    }
  }

  return sharp(bgPng)
    .composite(composites)
    .png({ compressionLevel: 9, adaptiveFiltering: true, palette: false })
    .toBuffer();
}
