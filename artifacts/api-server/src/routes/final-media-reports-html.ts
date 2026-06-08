/**
 * HTML renderer for Final Media Reports — matches official GAC template
 * Used by puppeteer to produce PDF + by Resend for email body.
 */

const TEAL = "#1a6e7a";
const NAVY = "#0e3b4a";
const MINT = "#cce4e6";
const MUSTARD = "#b8924a";
const BG = "#f5f8f9";

function esc(s: any): string {
  if (s === null || s === undefined) return "";
  return String(s)
    .replace(/&/g, "&amp;")
    .replace(/</g, "&lt;")
    .replace(/>/g, "&gt;")
    .replace(/"/g, "&quot;")
    .replace(/'/g, "&#39;");
}

function fmtDate(d: any): string {
  if (!d) return "";
  try {
    const dt = new Date(d);
    return dt.toLocaleDateString("ar-SA-u-nu-latn", { day: "2-digit", month: "long", year: "numeric" });
  } catch {
    return String(d);
  }
}

function sectionNumber(n: number, title: string): string {
  return `
<div class="sec-head">
  <div class="sec-num">${n}</div>
  <h2 class="sec-title">${esc(title)}</h2>
</div>`;
}

function renderCover(r: any): string {
  return `
<section class="page cover">
  <div class="cover-bg"></div>
  <div class="cover-content">
    <div class="cover-brand">
      <div class="cover-brand-ar">الهيئة العامة للمنافسة</div>
      <div class="cover-brand-en">General Authority for Competition</div>
    </div>
    <div class="cover-title-wrap">
      <div class="cover-tag">${esc(r.reportNumber)}</div>
      <h1 class="cover-title">${esc(r.title)}</h1>
      <div class="cover-period">${esc(r.periodLabel)}</div>
    </div>
    <div class="cover-meta">
      <div class="cover-meta-row"><span>تاريخ الإصدار</span><strong>${esc(fmtDate(r.issueDate))}</strong></div>
      <div class="cover-meta-row"><span>الجهة المعدّة</span><strong>${esc(r.preparedBy || "الإدارة التنفيذية للتواصل المؤسسي")}</strong></div>
      <div class="cover-meta-row"><span>الجهة المستفيدة</span><strong>${esc(r.beneficiary || "الإدارة التنفيذية")}</strong></div>
      <div class="cover-meta-row"><span>التصنيف</span><strong>${esc(r.classification || "سري — للاستخدام الداخلي")}</strong></div>
    </div>
    <div class="cover-footer">
      <div class="cover-classify">${esc(r.classification || "سري")}</div>
    </div>
  </div>
</section>`;
}

function renderKpis(kpis: any): string {
  const items = [
    { label: "خبر منشور", val: kpis.totalNews ?? "—" },
    { label: "تغطية إيجابية", val: kpis.positivePercent !== undefined ? `${kpis.positivePercent}%` : "—" },
    { label: "وسيلة إعلامية", val: kpis.mediaOutlets ?? "—" },
    { label: "موضوعات رئيسية", val: kpis.keyTopics ?? "—" },
    { label: "وصول جماهيري", val: kpis.reach || "—" },
    { label: "تنبيهات", val: kpis.alertsCount ?? "—" },
  ];
  return `
<div class="kpi-grid">
  ${items.map((it) => `
    <div class="kpi-card">
      <div class="kpi-val">${esc(it.val)}</div>
      <div class="kpi-label">${esc(it.label)}</div>
    </div>`).join("")}
</div>`;
}

function renderExecutiveSummary(r: any): string {
  return `
<section class="page">
  ${sectionNumber(1, "الملخص التنفيذي")}
  <div class="exec-text">${esc(r.executiveSummary || "")}</div>
  ${renderKpis(r.kpis || {})}
</section>`;
}

function renderTopNews(news: any[]): string {
  if (!Array.isArray(news) || news.length === 0) return "";
  const toneClass = (t: string) => t?.includes("إيج") ? "tone-pos" : t?.includes("سل") ? "tone-neg" : "tone-neu";
  return `
<section class="page">
  ${sectionNumber(2, "أبرز الأخبار")}
  <div class="news-list">
    ${news.map((n: any) => `
      <div class="news-card">
        <div class="news-head">
          <span class="news-date">${esc(n.date || "")}</span>
          <span class="tone-chip ${toneClass(n.tone || "")}">${esc(n.tone || "محايد")}</span>
        </div>
        <h3 class="news-headline">${esc(n.headline || "")}</h3>
        ${Array.isArray(n.details) && n.details.length > 0 ? `
          <ul class="news-details">
            ${n.details.map((d: string) => `<li>${esc(d)}</li>`).join("")}
          </ul>` : ""}
        <div class="news-source">المصدر: ${esc(n.source || "")}</div>
      </div>`).join("")}
  </div>
</section>`;
}

function renderTimeline(timeline: any[]): string {
  if (!Array.isArray(timeline) || timeline.length === 0) return "";
  return `
<section class="page">
  ${sectionNumber(3, "الجدول الزمني التفصيلي")}
  <table class="data-table">
    <thead>
      <tr><th>التاريخ</th><th>الحدث</th><th>الوسيلة</th><th>النبرة</th><th>عدد</th></tr>
    </thead>
    <tbody>
      ${timeline.map((t: any) => `
        <tr>
          <td>${esc(t.date || "")}</td>
          <td>${esc(t.event || "")}</td>
          <td>${esc(t.outlet || "")}</td>
          <td>${esc(t.tone || "")}</td>
          <td>${esc(t.count ?? "")}</td>
        </tr>`).join("")}
    </tbody>
  </table>
</section>`;
}

function renderDigitalPresence(dp: any): string {
  if (!dp) return "";
  const platforms = dp.platforms || [];
  const hashtags = dp.hashtags || [];
  return `
<section class="page">
  ${sectionNumber(4, "تحليل الحضور الرقمي")}
  ${platforms.length > 0 ? `
    <h3 class="sub-title">المنصات</h3>
    <table class="data-table">
      <thead><tr><th>المنصة</th><th>إشارات</th><th>إعادة نشر</th><th>تفاعل</th><th>وصول</th></tr></thead>
      <tbody>
        ${platforms.map((p: any) => `
          <tr><td>${esc(p.name)}</td><td>${esc(p.mentions ?? "")}</td><td>${esc(p.reposts ?? "")}</td><td>${esc(p.engagement ?? "")}</td><td>${esc(p.reach || "")}</td></tr>`).join("")}
      </tbody>
    </table>` : ""}
  ${hashtags.length > 0 ? `
    <h3 class="sub-title">الوسوم البارزة</h3>
    <table class="data-table">
      <thead><tr><th>الوسم</th><th>استخدامات</th><th>الاتجاه</th></tr></thead>
      <tbody>
        ${hashtags.map((h: any) => `<tr><td>${esc(h.tag)}</td><td>${esc(h.uses ?? "")}</td><td>${esc(h.trend || "")}</td></tr>`).join("")}
      </tbody>
    </table>` : ""}
</section>`;
}

function renderEditorialTone(et: any): string {
  if (!et) return "";
  const tones = et.distribution || [];
  const cls = et.classification || [];
  const srcs = et.sources || [];
  const tbl = (rows: any[], headers: string[], keys: string[]) => `
    <table class="data-table">
      <thead><tr>${headers.map((h) => `<th>${esc(h)}</th>`).join("")}</tr></thead>
      <tbody>${rows.map((r: any) => `<tr>${keys.map((k) => `<td>${k === "percent" ? `${esc(r[k] ?? "")}%` : esc(r[k] ?? "")}</td>`).join("")}</tr>`).join("")}</tbody>
    </table>`;
  return `
<section class="page">
  ${sectionNumber(5, "تحليل التوجه الإعلامي")}
  ${tones.length > 0 ? `<h3 class="sub-title">توزيع النبرة</h3>${tbl(tones, ["النبرة", "النسبة", "العدد"], ["tone", "percent", "count"])}` : ""}
  ${cls.length > 0 ? `<h3 class="sub-title">التصنيف الموضوعي</h3>${tbl(cls, ["الموضوع", "النسبة", "العدد"], ["topic", "percent", "count"])}` : ""}
  ${srcs.length > 0 ? `<h3 class="sub-title">التوزيع حسب المصدر</h3>${tbl(srcs, ["المصدر", "النسبة", "العدد"], ["source", "percent", "count"])}` : ""}
</section>`;
}

function renderDeepAnalysis(da: any): string {
  if (!da) return "";
  const kws = da.keywords || [];
  return `
<section class="page">
  ${sectionNumber(6, "تحليل عميق ومؤشرات قطاعية")}
  ${kws.length > 0 ? `
    <h3 class="sub-title">الكلمات المفتاحية</h3>
    <table class="data-table">
      <thead><tr><th>الكلمة</th><th>التكرار</th><th>السياق</th></tr></thead>
      <tbody>
        ${kws.map((k: any) => `<tr><td>${esc(k.keyword)}</td><td>${esc(k.frequency ?? "")}</td><td>${esc(k.context || "")}</td></tr>`).join("")}
      </tbody>
    </table>` : ""}
  ${da.quote ? `
    <blockquote class="quote-box">
      <div class="quote-text">«${esc(da.quote.text)}»</div>
      <div class="quote-meta">${esc(da.quote.source)} — ${esc(da.quote.date)}</div>
    </blockquote>` : ""}
  <div class="sw-grid">
    ${Array.isArray(da.strengths) && da.strengths.length > 0 ? `
      <div class="sw-card sw-strong">
        <h4>نقاط القوة</h4>
        <ul>${da.strengths.map((s: string) => `<li>${esc(s)}</li>`).join("")}</ul>
      </div>` : ""}
    ${Array.isArray(da.weaknesses) && da.weaknesses.length > 0 ? `
      <div class="sw-card sw-weak">
        <h4>نقاط الانتباه</h4>
        <ul>${da.weaknesses.map((s: string) => `<li>${esc(s)}</li>`).join("")}</ul>
      </div>` : ""}
  </div>
</section>`;
}

function renderRegional(rc: any[]): string {
  if (!Array.isArray(rc) || rc.length === 0) return "";
  return `
<section class="page">
  ${sectionNumber(7, "مقارنة إقليمية")}
  <table class="data-table">
    <thead><tr><th>الهيئة</th><th>الدولة</th><th>الإشارات</th><th>النبرة</th><th>أبرز ما تم تداوله</th></tr></thead>
    <tbody>
      ${rc.map((r: any) => `<tr><td>${esc(r.authority)}</td><td>${esc(r.country)}</td><td>${esc(r.mentions ?? "")}</td><td>${esc(r.tone || "")}</td><td>${esc(r.highlights || "")}</td></tr>`).join("")}
    </tbody>
  </table>
</section>`;
}

function renderRecommendations(recs: any[], alerts: any[]): string {
  if ((!Array.isArray(recs) || recs.length === 0) && (!Array.isArray(alerts) || alerts.length === 0)) return "";
  return `
<section class="page">
  ${sectionNumber(8, "التوصيات وخطة العمل")}
  ${Array.isArray(recs) && recs.length > 0 ? `
    <h3 class="sub-title">التوصيات</h3>
    <table class="data-table">
      <thead><tr><th>التوصية</th><th>الأولوية</th><th>المسؤول</th><th>المؤشر</th><th>الموعد</th></tr></thead>
      <tbody>
        ${recs.map((r: any) => `<tr>
          <td><strong>${esc(r.title)}</strong><br/><span class="muted">${esc(r.description || "")}</span></td>
          <td><span class="prio prio-${r.priority?.includes("عالية") ? "high" : r.priority?.includes("متوسطة") ? "med" : "low"}">${esc(r.priority || "")}</span></td>
          <td>${esc(r.responsible || "")}</td>
          <td>${esc(r.kpi || "")}</td>
          <td>${esc(r.deadline || "")}</td>
        </tr>`).join("")}
      </tbody>
    </table>` : ""}
  ${Array.isArray(alerts) && alerts.length > 0 ? `
    <h3 class="sub-title">تنبيهات تستوجب المتابعة</h3>
    <table class="data-table">
      <thead><tr><th>التنبيه</th><th>الموقف المقترح</th></tr></thead>
      <tbody>
        ${alerts.map((a: any) => `<tr><td>${esc(a.alert)}</td><td>${esc(a.suggestedPosition)}</td></tr>`).join("")}
      </tbody>
    </table>` : ""}
</section>`;
}

function renderQuotesAppendix(q: any[]): string {
  if (!Array.isArray(q) || q.length === 0) return "";
  return `
<section class="page">
  ${sectionNumber(9, "ملحق: أبرز الاقتباسات والنصوص الصحفية")}
  <div class="quotes-list">
    ${q.map((it: any) => `
      <div class="quote-item">
        <div class="quote-text">«${esc(it.quote)}»</div>
        <div class="quote-meta">${esc(it.source)} — ${esc(it.date)}${it.topic ? ` · ${esc(it.topic)}` : ""}</div>
      </div>`).join("")}
  </div>
</section>`;
}

function renderMethodology(r: any): string {
  const sources = r.sources || [];
  if (!r.methodology && sources.length === 0) return "";
  return `
<section class="page">
  ${sectionNumber(10, "المنهجية والمصادر")}
  ${r.methodology ? `<div class="meth-text">${esc(r.methodology)}</div>` : ""}
  ${sources.length > 0 ? `
    <h3 class="sub-title">المصادر الرئيسية المعتمدة</h3>
    <ol class="src-list">
      ${sources.map((s: any) => `<li><strong>${esc(s.name)}</strong>${s.description ? ` — ${esc(s.description)}` : ""}${s.url ? `<br/><a href="${esc(s.url)}">${esc(s.url)}</a>` : ""}</li>`).join("")}
    </ol>` : ""}
  <div class="immutable-stamp">
    محفوظ نهائياً — غير قابل للتعديل · ${esc(r.contentSha256?.slice(0, 16) || "")}…
  </div>
</section>`;
}

export function buildFinalReportHtml(r: any): string {
  const styles = `
<style>
@import url('https://fonts.googleapis.com/css2?family=Tajawal:wght@400;500;700;900&display=swap');
*, *::before, *::after { box-sizing: border-box; }
html, body { margin: 0; padding: 0; }
body {
  font-family: 'Tajawal', 'Segoe UI', sans-serif;
  direction: rtl;
  background: ${BG};
  color: #1f2937;
  font-size: 12px;
  line-height: 1.6;
}
.page {
  position: relative;
  width: 210mm;
  min-height: 297mm;
  padding: 24mm 18mm;
  background: #fff;
  page-break-after: always;
  break-after: page;
}
.page::before {
  content: "${esc(r.reportNumber)}";
  position: absolute; top: 8mm; left: 18mm;
  font-size: 9px; color: ${TEAL}; font-weight: 700;
}
.page::after {
  content: "${esc(r.classification || "سري — للاستخدام الداخلي")}";
  position: absolute; bottom: 8mm; left: 18mm;
  font-size: 9px; color: #9ca3af;
}
.page .right-tag {
  position: absolute; top: 8mm; right: 18mm;
  font-size: 9px; color: ${NAVY}; font-weight: 700;
}
/* ─── COVER ─── */
.cover { padding: 0; overflow: hidden; }
.cover::before, .cover::after { display: none; }
.cover-bg {
  position: absolute; inset: 0;
  background: linear-gradient(135deg, ${NAVY} 0%, ${TEAL} 60%, #2a8d9b 100%);
}
.cover-bg::before {
  content: ""; position: absolute; inset: 0;
  background: radial-gradient(circle at 80% 20%, rgba(255,255,255,.08) 0%, transparent 50%);
}
.cover-content { position: relative; z-index: 1; padding: 30mm 18mm; color: #fff; height: 100%; display: flex; flex-direction: column; justify-content: space-between; }
.cover-brand { border-bottom: 2px solid rgba(255,255,255,.3); padding-bottom: 12px; }
.cover-brand-ar { font-size: 22px; font-weight: 900; }
.cover-brand-en { font-size: 12px; opacity: .8; margin-top: 4px; letter-spacing: .5px; }
.cover-title-wrap { margin: 40px 0; }
.cover-tag { display: inline-block; background: ${MUSTARD}; color: #fff; padding: 6px 14px; border-radius: 4px; font-size: 13px; font-weight: 700; margin-bottom: 18px; }
.cover-title { font-size: 38px; font-weight: 900; margin: 0 0 10px; line-height: 1.25; }
.cover-period { font-size: 22px; opacity: .9; }
.cover-meta { background: rgba(255,255,255,.08); border-radius: 12px; padding: 20px 24px; backdrop-filter: blur(8px); }
.cover-meta-row { display: flex; justify-content: space-between; padding: 8px 0; border-bottom: 1px solid rgba(255,255,255,.12); font-size: 13px; }
.cover-meta-row:last-child { border-bottom: 0; }
.cover-meta-row span { opacity: .75; }
.cover-meta-row strong { font-weight: 700; }
.cover-footer { margin-top: 30px; }
.cover-classify { display: inline-block; background: rgba(184,146,74,.95); color: #fff; padding: 8px 20px; border-radius: 4px; font-weight: 700; font-size: 12px; }
/* ─── SECTIONS ─── */
.sec-head { display: flex; align-items: center; gap: 14px; margin-bottom: 18px; padding-bottom: 12px; border-bottom: 3px solid ${TEAL}; }
.sec-num { width: 40px; height: 40px; background: ${TEAL}; color: #fff; border-radius: 8px; display: flex; align-items: center; justify-content: center; font-weight: 900; font-size: 18px; flex-shrink: 0; }
.sec-title { margin: 0; font-size: 22px; font-weight: 900; color: ${NAVY}; }
.sub-title { font-size: 14px; font-weight: 700; color: ${NAVY}; margin: 20px 0 8px; padding-right: 10px; border-right: 3px solid ${MUSTARD}; }
.exec-text { font-size: 13px; line-height: 1.85; color: #374151; margin-bottom: 20px; background: #f0f7f8; border-right: 3px solid ${TEAL}; padding: 14px 16px; border-radius: 6px; }
/* ─── KPI ─── */
.kpi-grid { display: grid; grid-template-columns: repeat(3, 1fr); gap: 10px; margin: 14px 0; }
.kpi-card { background: linear-gradient(135deg, ${TEAL} 0%, #2a8d9b 100%); color: #fff; padding: 18px 12px; border-radius: 10px; text-align: center; }
.kpi-val { font-size: 26px; font-weight: 900; line-height: 1; margin-bottom: 6px; }
.kpi-label { font-size: 11px; opacity: .92; font-weight: 500; }
/* ─── NEWS CARDS ─── */
.news-list { display: flex; flex-direction: column; gap: 12px; }
.news-card { background: #fff; border: 1px solid #e5e7eb; border-right: 4px solid ${TEAL}; border-radius: 8px; padding: 14px 16px; }
.news-head { display: flex; gap: 10px; align-items: center; margin-bottom: 8px; }
.news-date { font-size: 11px; color: ${TEAL}; font-weight: 700; }
.tone-chip { font-size: 10px; padding: 3px 10px; border-radius: 12px; font-weight: 700; }
.tone-pos { background: #d1fae5; color: #065f46; }
.tone-neu { background: #e5e7eb; color: #374151; }
.tone-neg { background: #fee2e2; color: #991b1b; }
.news-headline { margin: 0 0 8px; font-size: 14px; font-weight: 700; color: ${NAVY}; }
.news-details { margin: 6px 0 8px; padding-right: 18px; font-size: 12px; color: #4b5563; }
.news-details li { margin-bottom: 3px; }
.news-source { font-size: 11px; color: #6b7280; font-style: italic; }
/* ─── TABLES ─── */
.data-table { width: 100%; border-collapse: collapse; margin: 10px 0 18px; font-size: 11px; }
.data-table th { background: ${MUSTARD}; color: #fff; padding: 8px 10px; text-align: right; font-weight: 700; border: 1px solid ${MUSTARD}; }
.data-table td { padding: 8px 10px; border: 1px solid #e5e7eb; vertical-align: top; }
.data-table tr:nth-child(even) td { background: #f9fafb; }
.muted { color: #6b7280; font-size: 10px; }
/* ─── QUOTE ─── */
.quote-box, .quote-item { background: #f0f7f8; border-right: 4px solid ${TEAL}; padding: 14px 18px; border-radius: 6px; margin: 12px 0; }
.quote-text { font-size: 13px; font-style: italic; color: ${NAVY}; line-height: 1.7; margin-bottom: 6px; }
.quote-meta { font-size: 11px; color: ${TEAL}; font-weight: 700; }
.quotes-list { display: flex; flex-direction: column; gap: 10px; }
/* ─── SW grid ─── */
.sw-grid { display: grid; grid-template-columns: 1fr 1fr; gap: 12px; margin-top: 14px; }
.sw-card { padding: 14px; border-radius: 8px; }
.sw-card h4 { margin: 0 0 8px; font-size: 13px; font-weight: 700; }
.sw-card ul { margin: 0; padding-right: 18px; font-size: 12px; line-height: 1.7; }
.sw-strong { background: #f0fdf4; border-right: 4px solid #16a34a; }
.sw-strong h4 { color: #14532d; }
.sw-weak { background: #fef2f2; border-right: 4px solid #dc2626; }
.sw-weak h4 { color: #7f1d1d; }
/* ─── Priority chips ─── */
.prio { display: inline-block; padding: 3px 10px; border-radius: 10px; font-size: 10px; font-weight: 700; }
.prio-high { background: #fee2e2; color: #991b1b; }
.prio-med { background: #fef3c7; color: #92400e; }
.prio-low { background: #e0e7ff; color: #3730a3; }
/* ─── Methodology ─── */
.meth-text { font-size: 12px; line-height: 1.8; color: #374151; background: #f9fafb; padding: 12px 16px; border-radius: 8px; margin: 10px 0; }
.src-list { font-size: 11px; line-height: 1.8; }
.src-list li { margin-bottom: 8px; }
.src-list a { color: ${TEAL}; word-break: break-all; }
.immutable-stamp { margin-top: 30px; padding: 12px 18px; background: ${MINT}; border: 2px dashed ${TEAL}; border-radius: 8px; text-align: center; font-size: 11px; font-weight: 700; color: ${NAVY}; }
</style>`;

  return `<!DOCTYPE html>
<html dir="rtl" lang="ar">
<head>
<meta charset="UTF-8"/>
<title>${esc(r.reportNumber)} — ${esc(r.title)}</title>
${styles}
</head>
<body>
${renderCover(r)}
${renderExecutiveSummary(r)}
${renderTopNews(r.topNews || [])}
${renderTimeline(r.timeline || [])}
${renderDigitalPresence(r.digitalPresence || {})}
${renderEditorialTone(r.editorialTone || {})}
${renderDeepAnalysis(r.deepAnalysis || {})}
${renderRegional(r.regionalComparison || [])}
${renderRecommendations(r.recommendations || [], r.alerts || [])}
${renderQuotesAppendix(r.quotesAppendix || [])}
${renderMethodology(r)}
</body>
</html>`;
}
