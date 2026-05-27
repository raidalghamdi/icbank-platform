/**
 * GAC publications seed (Library — مكتبة الهيئة).
 *
 * Source of truth for the initial "GAC Library" content. These five PDFs were
 * downloaded ahead of time from gacbep.gac.gov.sa (via the Wayback Machine,
 * because the live origin returns HTTP 503 from non-Saudi IPs) and one from
 * the UNESCWA mirror of the Saudi Competition Law.
 *
 * The reseed endpoint loads each PDF from disk (PUBLICATIONS_DIR), uploads it
 * to Supabase Storage at gac/publications/{uuid}.pdf, and inserts a metadata
 * row into gac_publications.
 *
 * To refresh content: replace the file at `localPath`, then re-run
 *   POST /api/gac/publications/reseed
 * Idempotency is keyed on `titleAr`.
 */
import type { TextSlot } from "@workspace/db";
// (TextSlot import kept consistent with sibling seeds even if unused.)
void ([] as TextSlot[]);

export type SeedPublication = {
  /** Filename inside PUBLICATIONS_DIR (e.g. "b9376edc-...pdf"). */
  localFile: string;
  titleAr: string;
  titleEn?: string;
  category: "guidelines" | "regulations" | "statistics" | "research" | "policy" | "brand";
  language: "ar" | "en" | "bilingual";
  descriptionAr?: string;
  descriptionEn?: string;
  version?: string;
  /** ISO date "YYYY-MM-DD". */
  publishedAt?: string;
  /** Public URL where the document originally came from. */
  originalUrl?: string;
  pageCount?: number;
  tags?: string[];
  sourceDomain: "gacbep" | "acnbe" | "unescwa" | "direct" | "manual";
  displayOrder?: number;
};

export const SEED_PUBLICATIONS: SeedPublication[] = [
  // 1) Economic Concentration Review Guidelines — English v5
  {
    localFile: "b9376edc-79a1-4573-a36d-4f3effaba838.pdf",
    titleAr: "الدليل الإرشادي لفحص التركز الاقتصادي (الإصدار الخامس - إنجليزي)",
    titleEn: "Economic Concentration Review Guidelines — Version 5",
    category: "guidelines",
    language: "en",
    descriptionAr:
      "النسخة الإنجليزية المحدّثة من الدليل الإرشادي لفحص طلبات التركز الاقتصادي الصادر عن الهيئة العامة للمنافسة، يوضح المنهجية والمعايير المعتمدة في فحص طلبات الاندماج والاستحواذ.",
    descriptionEn:
      "Updated English version of GAC's Economic Concentration Review Guidelines, outlining the methodology and criteria adopted in reviewing merger and acquisition applications.",
    version: "v5",
    publishedAt: "2025-04-01",
    originalUrl:
      "https://gacbep.gac.gov.sa/cms/b9376edc-79a1-4573-a36d-4f3effaba838.pdf",
    tags: ["تركز اقتصادي", "اندماج", "استحواذ", "Guidelines"],
    sourceDomain: "gacbep",
    displayOrder: 10,
  },

  // 2) الدليل الإرشادي لفحص التركز الاقتصادي — Arabic
  {
    localFile: "912a9673-01a9-4737-a480-f8f65783205f.pdf",
    titleAr: "الدليل الإرشادي لفحص التركز الاقتصادي (النسخة العربية)",
    titleEn: "Economic Concentration Review Guidelines (Arabic)",
    category: "guidelines",
    language: "ar",
    descriptionAr:
      "الدليل الإرشادي الصادر عن الهيئة العامة للمنافسة لفحص طلبات التركز الاقتصادي، يوضح آلية تقديم الطلبات والمعايير المعتمدة في تقييم تأثير العمليات على المنافسة في الأسواق السعودية.",
    version: "محدث",
    publishedAt: "2025-04-01",
    originalUrl:
      "https://gacbep.gac.gov.sa/cms/912a9673-01a9-4737-a480-f8f65783205f.pdf",
    tags: ["تركز اقتصادي", "اندماج", "استحواذ", "دليل إرشادي"],
    sourceDomain: "gacbep",
    displayOrder: 20,
  },

  // 3) دليل تقدير إساءة استغلال الوضع المهيمن
  {
    localFile: "11505903-4d11-4c95-93b3-460fed2bf166.pdf",
    titleAr: "دليل تقدير إساءة استغلال الوضع المهيمن",
    titleEn: "Guidelines on Abuse of Dominant Position",
    category: "guidelines",
    language: "ar",
    descriptionAr:
      "الدليل الإرشادي الصادر عن الهيئة العامة للمنافسة الذي يوضح المعايير والممارسات التي تُعدّ إساءة استغلال للوضع المهيمن في الأسواق السعودية، بما في ذلك تسعير الإفتراس والتمييز السعري والصفقات الحصرية.",
    publishedAt: "2024-01-01",
    originalUrl:
      "https://gacbep.gac.gov.sa/cms/11505903-4d11-4c95-93b3-460fed2bf166.pdf",
    tags: ["وضع مهيمن", "ممارسات احتكارية", "دليل إرشادي"],
    sourceDomain: "gacbep",
    displayOrder: 30,
  },

  // 4) إحصاءات طلبات التركز الواردة - الربع الثاني
  {
    localFile: "5c2d0a08-dff9-43dd-9893-cdb581ea5479.pdf",
    titleAr: "إحصاءات طلبات التركز الاقتصادي الواردة — الربع الثاني",
    titleEn: "Economic Concentration Applications Statistics — Q2",
    category: "statistics",
    language: "ar",
    descriptionAr:
      "تقرير إحصائي ربعي يستعرض أعداد طلبات التركز الاقتصادي الواردة إلى الهيئة العامة للمنافسة وتصنيفها حسب القطاع وحجم العمليات والقرارات الصادرة بشأنها.",
    publishedAt: "2024-06-30",
    originalUrl:
      "https://gacbep.gac.gov.sa/cms/5c2d0a08-dff9-43dd-9893-cdb581ea5479.pdf",
    tags: ["إحصاءات", "تقرير ربعي", "تركز اقتصادي"],
    sourceDomain: "gacbep",
    displayOrder: 40,
  },

  // 5) نظام المنافسة السعودي (UNESCWA mirror)
  {
    localFile: "competition-law-unescwa.pdf",
    titleAr: "نظام المنافسة في المملكة العربية السعودية",
    titleEn: "Saudi Arabia Competition Law",
    category: "regulations",
    language: "ar",
    descriptionAr:
      "النص الكامل لنظام المنافسة الصادر بالمرسوم الملكي رقم (م/75) وتاريخ 29/6/1440هـ، الذي يهدف إلى حماية المنافسة العادلة في الأسواق السعودية ومكافحة الممارسات الاحتكارية والتركزات الضارة.",
    publishedAt: "2019-03-06",
    originalUrl:
      "https://www.unescwa.org/sites/default/files/inline-files/ABLF-2023-competition-CP-Saudi-Arabia-arabic.pdf",
    tags: ["نظام المنافسة", "تشريع", "مرسوم ملكي", "م/75"],
    sourceDomain: "unescwa",
    displayOrder: 50,
  },
];
