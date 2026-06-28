/**
 * Icon Library — Lucide SVG (inline)
 * مكتبة أيقونات لتوليد تصاميم الفعاليات
 * المصدر: lucide.dev — MIT License
 *
 * كل أيقونة محفوظة كسلسلة SVG قابلة للحقن في HTML
 * مع دعم لتغيير اللون والحجم عبر currentColor و width/height
 */

export type IconCategory = "workshop" | "meeting" | "launch" | "social" | "common";

export interface IconDef {
  name: string;
  label_ar: string;
  category: IconCategory;
  keywords: string[];
  svg: string; // inline SVG (24x24 viewBox)
}

const SVG_WRAP = (path: string) =>
  `<svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round">${path}</svg>`;

export const ICON_LIBRARY: IconDef[] = [
  // ============ WORKSHOP / TRAINING ============
  {
    name: "graduation-cap",
    label_ar: "تخرج / تعليم",
    category: "workshop",
    keywords: ["ورشة", "تعليم", "تدريب", "تعلم", "أكاديمي"],
    svg: SVG_WRAP(`<path d="M22 10v6M2 10l10-5 10 5-10 5z"/><path d="M6 12v5c3 3 9 3 12 0v-5"/>`),
  },
  {
    name: "book-open",
    label_ar: "كتاب",
    category: "workshop",
    keywords: ["كتاب", "قراءة", "محتوى", "دورة"],
    svg: SVG_WRAP(`<path d="M2 3h6a4 4 0 0 1 4 4v14a3 3 0 0 0-3-3H2z"/><path d="M22 3h-6a4 4 0 0 0-4 4v14a3 3 0 0 1 3-3h7z"/>`),
  },
  {
    name: "lightbulb",
    label_ar: "فكرة",
    category: "workshop",
    keywords: ["فكرة", "ابتكار", "إلهام", "ابداع"],
    svg: SVG_WRAP(`<path d="M15 14c.2-1 .7-1.7 1.5-2.5 1-.9 1.5-2.2 1.5-3.5A6 6 0 0 0 6 8c0 1 .2 2.2 1.5 3.5.7.7 1.3 1.5 1.5 2.5"/><path d="M9 18h6"/><path d="M10 22h4"/>`),
  },
  {
    name: "presentation",
    label_ar: "عرض تقديمي",
    category: "workshop",
    keywords: ["عرض", "بريزنتيشن", "محاضرة", "شرح"],
    // v5: رفع الحافة العلوية بصرياً لتتحاذى مع أيقونات building و users
    svg: SVG_WRAP(`<path d="M1 2h22"/><path d="M21 2v11a2 2 0 0 1-2 2H5a2 2 0 0 1-2-2V2"/><path d="m7 22 5-5 5 5"/>`),
  },
  {
    name: "code",
    label_ar: "كود برمجي",
    category: "workshop",
    keywords: ["برمجة", "كود", "تطوير", "تقنية"],
    svg: SVG_WRAP(`<polyline points="16 18 22 12 16 6"/><polyline points="8 6 2 12 8 18"/>`),
  },
  {
    name: "brain",
    label_ar: "ذكاء اصطناعي",
    category: "workshop",
    keywords: ["ذكاء", "AI", "تفكير", "عقل", "اصطناعي"],
    svg: SVG_WRAP(`<path d="M12 5a3 3 0 1 0-5.997.125 4 4 0 0 0-2.526 5.77 4 4 0 0 0 .556 6.588A4 4 0 1 0 12 18Z"/><path d="M12 5a3 3 0 1 1 5.997.125 4 4 0 0 1 2.526 5.77 4 4 0 0 1-.556 6.588A4 4 0 1 1 12 18Z"/>`),
  },
  {
    name: "target",
    label_ar: "هدف",
    category: "workshop",
    keywords: ["هدف", "تركيز", "أهداف", "غاية"],
    svg: SVG_WRAP(`<circle cx="12" cy="12" r="10"/><circle cx="12" cy="12" r="6"/><circle cx="12" cy="12" r="2"/>`),
  },
  {
    name: "trending-up",
    label_ar: "تطور / نمو",
    category: "workshop",
    keywords: ["نمو", "تطور", "تقدم", "صعود"],
    svg: SVG_WRAP(`<polyline points="22 7 13.5 15.5 8.5 10.5 2 17"/><polyline points="16 7 22 7 22 13"/>`),
  },
  {
    name: "award",
    label_ar: "شهادة / إنجاز",
    category: "workshop",
    keywords: ["شهادة", "جائزة", "إنجاز", "تكريم"],
    svg: SVG_WRAP(`<circle cx="12" cy="8" r="6"/><polyline points="15.477 12.89 17 22 12 19 7 22 8.523 12.89"/>`),
  },

  // ============ MEETING / CONFERENCE ============
  {
    name: "users",
    label_ar: "مجموعة",
    category: "meeting",
    keywords: ["مجموعة", "فريق", "حضور", "أشخاص"],
    svg: SVG_WRAP(`<path d="M16 21v-2a4 4 0 0 0-4-4H6a4 4 0 0 0-4 4v2"/><circle cx="9" cy="7" r="4"/><path d="M22 21v-2a4 4 0 0 0-3-3.87"/><path d="M16 3.13a4 4 0 0 1 0 7.75"/>`),
  },
  {
    name: "mic",
    label_ar: "ميكروفون",
    category: "meeting",
    keywords: ["ميكروفون", "خطاب", "صوت", "متحدث"],
    svg: SVG_WRAP(`<path d="M12 2a3 3 0 0 0-3 3v7a3 3 0 0 0 6 0V5a3 3 0 0 0-3-3Z"/><path d="M19 10v2a7 7 0 0 1-14 0v-2"/><line x1="12" x2="12" y1="19" y2="22"/>`),
  },
  {
    name: "podium",
    label_ar: "منصة",
    category: "meeting",
    keywords: ["منصة", "محاضرة", "خطابة"],
    svg: SVG_WRAP(`<path d="M12 2v8"/><path d="M9 7h6"/><path d="M5 22h14"/><path d="M7 22V10"/><path d="M17 22V10"/>`),
  },
  {
    name: "building",
    label_ar: "مبنى / قاعة",
    category: "meeting",
    keywords: ["مبنى", "قاعة", "مكان", "موقع"],
    svg: SVG_WRAP(`<rect width="16" height="20" x="4" y="2" rx="2"/><path d="M9 22v-4h6v4"/><path d="M8 6h.01"/><path d="M16 6h.01"/><path d="M12 6h.01"/><path d="M12 10h.01"/><path d="M12 14h.01"/><path d="M16 10h.01"/><path d="M16 14h.01"/><path d="M8 10h.01"/><path d="M8 14h.01"/>`),
  },
  {
    name: "handshake",
    label_ar: "مصافحة / اتفاق",
    category: "meeting",
    keywords: ["مصافحة", "شراكة", "اتفاق", "تعاون"],
    svg: SVG_WRAP(`<path d="m11 17 2 2a1 1 0 1 0 3-3"/><path d="m14 14 2.5 2.5a1 1 0 1 0 3-3l-3.88-3.88a3 3 0 0 0-4.24 0l-.88.88a1 1 0 1 1-3-3l2.81-2.81a5.79 5.79 0 0 1 7.06-.87l.47.28a2 2 0 0 0 1.42.25L21 4"/><path d="m21 3 1 11h-2"/><path d="M3 3 2 14l6.5 6.5a1 1 0 1 0 3-3"/><path d="M3 4h8"/>`),
  },
  {
    name: "video",
    label_ar: "اجتماع مرئي",
    category: "meeting",
    keywords: ["فيديو", "زوم", "اجتماع", "افتراضي"],
    svg: SVG_WRAP(`<path d="m22 8-6 4 6 4V8Z"/><rect width="14" height="12" x="2" y="6" rx="2" ry="2"/>`),
  },
  {
    name: "message-circle",
    label_ar: "محادثة",
    category: "meeting",
    keywords: ["محادثة", "نقاش", "حوار", "كلام"],
    svg: SVG_WRAP(`<path d="M21 15a2 2 0 0 1-2 2H7l-4 4V5a2 2 0 0 1 2-2h14a2 2 0 0 1 2 2z"/>`),
  },
  {
    name: "globe",
    label_ar: "عالمي",
    category: "meeting",
    keywords: ["عالمي", "دولي", "عالم", "مؤتمر"],
    svg: SVG_WRAP(`<circle cx="12" cy="12" r="10"/><path d="M12 2a14.5 14.5 0 0 0 0 20 14.5 14.5 0 0 0 0-20"/><path d="M2 12h20"/>`),
  },

  // ============ LAUNCH / ANNOUNCEMENT ============
  {
    name: "rocket",
    label_ar: "صاروخ / إطلاق",
    category: "launch",
    keywords: ["إطلاق", "بداية", "انطلاقة", "صاروخ"],
    svg: SVG_WRAP(`<path d="M4.5 16.5c-1.5 1.26-2 5-2 5s3.74-.5 5-2c.71-.84.7-2.13-.09-2.91a2.18 2.18 0 0 0-2.91-.09z"/><path d="m12 15-3-3a22 22 0 0 1 2-3.95A12.88 12.88 0 0 1 22 2c0 2.72-.78 7.5-6 11a22.35 22.35 0 0 1-4 2z"/><path d="M9 12H4s.55-3.03 2-4c1.62-1.08 5 0 5 0"/><path d="M12 15v5s3.03-.55 4-2c1.08-1.62 0-5 0-5"/>`),
  },
  {
    name: "megaphone",
    label_ar: "إعلان",
    category: "launch",
    keywords: ["إعلان", "نشر", "تنبيه", "بث"],
    svg: SVG_WRAP(`<path d="m3 11 18-5v12L3 14v-3z"/><path d="M11.6 16.8a3 3 0 1 1-5.8-1.6"/>`),
  },
  {
    name: "sparkles",
    label_ar: "تميز / جديد",
    category: "launch",
    keywords: ["جديد", "مميز", "نجوم", "إطلاق"],
    svg: SVG_WRAP(`<path d="M9.937 15.5A2 2 0 0 0 8.5 14.063l-6.135-1.582a.5.5 0 0 1 0-.962L8.5 9.936A2 2 0 0 0 9.937 8.5l1.582-6.135a.5.5 0 0 1 .963 0L14.063 8.5A2 2 0 0 0 15.5 9.937l6.135 1.581a.5.5 0 0 1 0 .964L15.5 14.063a2 2 0 0 0-1.437 1.437l-1.582 6.135a.5.5 0 0 1-.963 0z"/>`),
  },
  {
    name: "star",
    label_ar: "نجمة",
    category: "launch",
    keywords: ["نجمة", "تميز", "أفضل", "مفضل"],
    svg: SVG_WRAP(`<polygon points="12 2 15.09 8.26 22 9.27 17 14.14 18.18 21.02 12 17.77 5.82 21.02 7 14.14 2 9.27 8.91 8.26 12 2"/>`),
  },
  {
    name: "zap",
    label_ar: "طاقة / سرعة",
    category: "launch",
    keywords: ["طاقة", "سرعة", "برق", "قوة"],
    svg: SVG_WRAP(`<path d="M4 14a1 1 0 0 1-.78-1.63l9.9-10.2a.5.5 0 0 1 .86.46l-1.92 6.02A1 1 0 0 0 13 10h7a1 1 0 0 1 .78 1.63l-9.9 10.2a.5.5 0 0 1-.86-.46l1.92-6.02A1 1 0 0 0 11 14z"/>`),
  },
  {
    name: "trophy",
    label_ar: "كأس / نجاح",
    category: "launch",
    keywords: ["كأس", "نجاح", "فوز", "تتويج"],
    svg: SVG_WRAP(`<path d="M6 9H4.5a2.5 2.5 0 0 1 0-5H6"/><path d="M18 9h1.5a2.5 2.5 0 0 0 0-5H18"/><path d="M4 22h16"/><path d="M10 14.66V17c0 .55-.47.98-.97 1.21C7.85 18.75 7 20.24 7 22"/><path d="M14 14.66V17c0 .55.47.98.97 1.21C16.15 18.75 17 20.24 17 22"/><path d="M18 2H6v7a6 6 0 0 0 12 0V2Z"/>`),
  },
  {
    name: "flag",
    label_ar: "علم / مرحلة",
    category: "launch",
    keywords: ["علم", "مرحلة", "إنجاز", "هدف"],
    svg: SVG_WRAP(`<path d="M4 15s1-1 4-1 5 2 8 2 4-1 4-1V3s-1 1-4 1-5-2-8-2-4 1-4 1z"/><line x1="4" x2="4" y1="22" y2="15"/>`),
  },
  {
    name: "gift",
    label_ar: "هدية",
    category: "launch",
    keywords: ["هدية", "مفاجأة", "عرض", "جائزة"],
    svg: SVG_WRAP(`<rect x="3" y="8" width="18" height="4" rx="1"/><path d="M12 8v13"/><path d="M19 12v7a2 2 0 0 1-2 2H7a2 2 0 0 1-2-2v-7"/><path d="M7.5 8a2.5 2.5 0 0 1 0-5A4.8 8 0 0 1 12 8a4.8 8 0 0 1 4.5-5 2.5 2.5 0 0 1 0 5"/>`),
  },

  // ============ SOCIAL EVENTS ============
  {
    name: "party-popper",
    label_ar: "احتفال",
    category: "social",
    keywords: ["احتفال", "حفلة", "اجتماعي", "فرح"],
    svg: SVG_WRAP(`<path d="M5.8 11.3 2 22l10.7-3.79"/><path d="M4 3h.01"/><path d="M22 8h.01"/><path d="M15 2h.01"/><path d="M22 20h.01"/><path d="m22 2-2.24.75a2.9 2.9 0 0 0-1.96 3.12c.1.86-.57 1.63-1.45 1.63h-.38c-.86 0-1.6.6-1.76 1.44L14 10"/><path d="m22 13-.82-.33c-.86-.34-1.82.2-1.98 1.11c-.11.7-.72 1.22-1.43 1.22H17"/><path d="m11 2 .33.82c.34.86-.2 1.82-1.11 1.98C9.52 4.9 9 5.52 9 6.23V7"/><path d="M11 13c1.93 1.93 2.83 4.17 2 5-.83.83-3.07-.07-5-2-1.93-1.93-2.83-4.17-2-5 .83-.83 3.07.07 5 2Z"/>`),
  },
  {
    name: "heart",
    label_ar: "حب / اهتمام",
    category: "social",
    keywords: ["حب", "قلب", "اهتمام", "عاطفة"],
    svg: SVG_WRAP(`<path d="M19 14c1.49-1.46 3-3.21 3-5.5A5.5 5.5 0 0 0 16.5 3c-1.76 0-3 .5-4.5 2-1.5-1.5-2.74-2-4.5-2A5.5 5.5 0 0 0 2 8.5c0 2.3 1.5 4.05 3 5.5l7 7Z"/>`),
  },
  {
    name: "coffee",
    label_ar: "قهوة / استراحة",
    category: "social",
    keywords: ["قهوة", "استراحة", "لقاء", "كوفي"],
    svg: SVG_WRAP(`<path d="M10 2v2"/><path d="M14 2v2"/><path d="M16 8a1 1 0 0 1 1 1v8a4 4 0 0 1-4 4H7a4 4 0 0 1-4-4V9a1 1 0 0 1 1-1h14a4 4 0 1 1 0 8h-1"/><path d="M6 2v2"/>`),
  },
  {
    name: "music",
    label_ar: "موسيقى",
    category: "social",
    keywords: ["موسيقى", "غناء", "ترفيه", "نغمة"],
    svg: SVG_WRAP(`<path d="M9 18V5l12-2v13"/><circle cx="6" cy="18" r="3"/><circle cx="18" cy="16" r="3"/>`),
  },
  {
    name: "cake",
    label_ar: "كيك / مناسبة",
    category: "social",
    keywords: ["كيك", "حفلة", "مناسبة", "ميلاد"],
    svg: SVG_WRAP(`<path d="M20 21v-8a2 2 0 0 0-2-2H6a2 2 0 0 0-2 2v8"/><path d="M4 16s.5-1 2-1 2.5 2 4 2 2.5-2 4-2 2.5 2 4 2 2-1 2-1"/><path d="M2 21h20"/><path d="M7 8v3"/><path d="M12 8v3"/><path d="M17 8v3"/><path d="M7 4h.01"/><path d="M12 4h.01"/><path d="M17 4h.01"/>`),
  },
  {
    name: "smile",
    label_ar: "ابتسامة",
    category: "social",
    keywords: ["ابتسامة", "سعادة", "فرح", "إيجابي"],
    svg: SVG_WRAP(`<circle cx="12" cy="12" r="10"/><path d="M8 14s1.5 2 4 2 4-2 4-2"/><line x1="9" x2="9.01" y1="9" y2="9"/><line x1="15" x2="15.01" y1="9" y2="9"/>`),
  },
  {
    name: "users-round",
    label_ar: "تجمع",
    category: "social",
    keywords: ["تجمع", "أصدقاء", "اجتماع", "مجتمع"],
    svg: SVG_WRAP(`<path d="M18 21a8 8 0 0 0-16 0"/><circle cx="10" cy="8" r="5"/><path d="M22 20c0-3.37-2-6.5-4-8a5 5 0 0 0-.45-8.3"/>`),
  },
  {
    name: "hand-heart",
    label_ar: "تطوع / عطاء",
    category: "social",
    keywords: ["تطوع", "عطاء", "خير", "مساعدة"],
    svg: SVG_WRAP(`<path d="M11 14h2a2 2 0 1 0 0-4h-3c-.6 0-1.1.2-1.4.6L3 16"/><path d="m7 20 1.6-1.4c.3-.4.8-.6 1.4-.6h4c1.1 0 2.1-.4 2.8-1.2l4.6-4.4a2 2 0 0 0-2.75-2.91l-4.2 3.9"/><path d="m2 15 6 6"/><path d="M19.5 8.5c.7-.7 1.5-1.6 1.5-2.7A2.73 2.73 0 0 0 16 4a2.78 2.78 0 0 0-5 1.8c0 1.2.8 2 1.5 2.8L16 12Z"/>`),
  },

  // ============ COMMON / UTILITY ============
  {
    name: "calendar",
    label_ar: "تقويم / تاريخ",
    category: "common",
    keywords: ["تاريخ", "تقويم", "موعد", "يوم"],
    svg: SVG_WRAP(`<rect width="18" height="18" x="3" y="4" rx="2" ry="2"/><line x1="16" x2="16" y1="2" y2="6"/><line x1="8" x2="8" y1="2" y2="6"/><line x1="3" x2="21" y1="10" y2="10"/>`),
  },
  {
    name: "clock",
    label_ar: "ساعة / وقت",
    category: "common",
    keywords: ["وقت", "ساعة", "زمن", "موعد"],
    svg: SVG_WRAP(`<circle cx="12" cy="12" r="10"/><polyline points="12 6 12 12 16 14"/>`),
  },
  {
    name: "map-pin",
    label_ar: "موقع / مكان",
    category: "common",
    keywords: ["موقع", "مكان", "خريطة", "عنوان"],
    svg: SVG_WRAP(`<path d="M20 10c0 6-8 12-8 12s-8-6-8-12a8 8 0 0 1 16 0Z"/><circle cx="12" cy="10" r="3"/>`),
  },
  {
    name: "user",
    label_ar: "شخص",
    category: "common",
    keywords: ["شخص", "متحدث", "مستخدم", "فرد"],
    svg: SVG_WRAP(`<path d="M19 21v-2a4 4 0 0 0-4-4H9a4 4 0 0 0-4 4v2"/><circle cx="12" cy="7" r="4"/>`),
  },
  {
    name: "ticket",
    label_ar: "تذكرة",
    category: "common",
    keywords: ["تذكرة", "حجز", "دخول", "تسجيل"],
    svg: SVG_WRAP(`<path d="M2 9a3 3 0 0 1 0 6v2a2 2 0 0 0 2 2h16a2 2 0 0 0 2-2v-2a3 3 0 0 1 0-6V7a2 2 0 0 0-2-2H4a2 2 0 0 0-2 2Z"/><path d="M13 5v2"/><path d="M13 17v2"/><path d="M13 11v2"/>`),
  },
  {
    name: "qr-code",
    label_ar: "QR / مسح",
    category: "common",
    keywords: ["QR", "مسح", "كود", "تسجيل"],
    svg: SVG_WRAP(`<rect width="5" height="5" x="3" y="3" rx="1"/><rect width="5" height="5" x="16" y="3" rx="1"/><rect width="5" height="5" x="3" y="16" rx="1"/><path d="M21 16h-3a2 2 0 0 0-2 2v3"/><path d="M21 21v.01"/><path d="M12 7v3a2 2 0 0 1-2 2H7"/><path d="M3 12h.01"/><path d="M12 3h.01"/><path d="M12 16v.01"/><path d="M16 12h1"/><path d="M21 12v.01"/><path d="M12 21v-1"/>`),
  },
  {
    name: "check-circle",
    label_ar: "تأكيد / صح",
    category: "common",
    keywords: ["تأكيد", "صح", "إنجاز", "نجاح"],
    svg: SVG_WRAP(`<path d="M22 11.08V12a10 10 0 1 1-5.93-9.14"/><polyline points="22 4 12 14.01 9 11.01"/>`),
  },
  {
    name: "phone",
    label_ar: "اتصال",
    category: "common",
    keywords: ["اتصال", "هاتف", "تواصل", "رقم"],
    svg: SVG_WRAP(`<path d="M22 16.92v3a2 2 0 0 1-2.18 2 19.79 19.79 0 0 1-8.63-3.07 19.5 19.5 0 0 1-6-6 19.79 19.79 0 0 1-3.07-8.67A2 2 0 0 1 4.11 2h3a2 2 0 0 1 2 1.72 12.84 12.84 0 0 0 .7 2.81 2 2 0 0 1-.45 2.11L8.09 9.91a16 16 0 0 0 6 6l1.27-1.27a2 2 0 0 1 2.11-.45 12.84 12.84 0 0 0 2.81.7A2 2 0 0 1 22 16.92z"/>`),
  },
  {
    name: "mail",
    label_ar: "بريد",
    category: "common",
    keywords: ["بريد", "إيميل", "رسالة", "تواصل"],
    svg: SVG_WRAP(`<rect width="20" height="16" x="2" y="4" rx="2"/><path d="m22 7-8.97 5.7a1.94 1.94 0 0 1-2.06 0L2 7"/>`),
  },
  {
    name: "link",
    label_ar: "رابط",
    category: "common",
    keywords: ["رابط", "اتصال", "وصلة"],
    svg: SVG_WRAP(`<path d="M10 13a5 5 0 0 0 7.54.54l3-3a5 5 0 0 0-7.07-7.07l-1.72 1.71"/><path d="M14 11a5 5 0 0 0-7.54-.54l-3 3a5 5 0 0 0 7.07 7.07l1.71-1.71"/>`),
  },
  {
    name: "shield",
    label_ar: "حماية",
    category: "common",
    keywords: ["حماية", "أمان", "سرية", "ضمان"],
    svg: SVG_WRAP(`<path d="M20 13c0 5-3.5 7.5-7.66 8.95a1 1 0 0 1-.67-.01C7.5 20.5 4 18 4 13V6a1 1 0 0 1 1-1c2 0 4.5-1.2 6.24-2.72a1.17 1.17 0 0 1 1.52 0C14.51 3.81 17 5 19 5a1 1 0 0 1 1 1z"/>`),
  },
  {
    name: "chart-bar",
    label_ar: "إحصائيات",
    category: "common",
    keywords: ["إحصائيات", "بيانات", "رسم", "نتائج"],
    svg: SVG_WRAP(`<line x1="12" x2="12" y1="20" y2="10"/><line x1="18" x2="18" y1="20" y2="4"/><line x1="6" x2="6" y1="20" y2="16"/>`),
  },
  {
    name: "globe-arabic",
    label_ar: "السعودية",
    category: "common",
    keywords: ["سعودية", "وطن", "بلد", "محلي"],
    svg: SVG_WRAP(`<circle cx="12" cy="12" r="10"/><path d="M12 2v20M2 12h20"/>`),
  },
];

/** البحث عن أيقونة بالاسم */
export function getIcon(name: string): IconDef | undefined {
  return ICON_LIBRARY.find((i) => i.name === name);
}

/** أيقونات حسب الفئة */
export function iconsByCategory(category: IconCategory): IconDef[] {
  return ICON_LIBRARY.filter((i) => i.category === category);
}

/** قائمة مختصرة للـ AI (الاسم + التسمية العربية + الكلمات المفتاحية) */
export function iconListForAI(): string {
  return ICON_LIBRARY.map(
    (i) => `- "${i.name}" (${i.label_ar}): ${i.keywords.join(", ")}`
  ).join("\n");
}

/** توليد SVG inline بحجم ولون مخصص */
export function renderIcon(name: string, size: number = 48, color: string = "currentColor"): string {
  const icon = getIcon(name);
  if (!icon) return "";
  return icon.svg
    .replace("<svg ", `<svg width="${size}" height="${size}" style="color:${color}" `);
}
