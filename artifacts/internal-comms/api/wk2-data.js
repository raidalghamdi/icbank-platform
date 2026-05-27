/**
 * Vercel serverless function — /wk2-data
 *
 * Generates weekend content (places, deals, podcasts, AI tools, matches, movies)
 * using Gemini + the SPL fixtures API. Mirrors the logic in server.mjs so the
 * frontend's fetch('/wk2-data') keeps working unchanged after migration.
 *
 * Caches per calendar date in module scope (warm-invocation cache).
 */

const GEMINI_API_KEY =
  process.env.GEMINI_API_KEY ||
  process.env.GOOGLE_AI_API_KEY ||
  process.env.AI_INTEGRATIONS_GEMINI_API_KEY;
const GEMINI_MODEL = process.env.GEMINI_TEXT_MODEL || 'gemini-2.5-flash';

async function aiCall(prompt) {
  if (!GEMINI_API_KEY) throw new Error('GEMINI_API_KEY is not configured');
  const url = `https://generativelanguage.googleapis.com/v1beta/models/${GEMINI_MODEL}:generateContent?key=${GEMINI_API_KEY}`;
  const response = await fetch(url, {
    method: 'POST',
    headers: { 'Content-Type': 'application/json' },
    body: JSON.stringify({
      systemInstruction: { parts: [{ text: 'أنت مساعد يجيب بـ JSON فقط بدون أي نص إضافي أو markdown.' }] },
      contents: [{ role: 'user', parts: [{ text: prompt }] }],
      generationConfig: { temperature: 0.7, maxOutputTokens: 3000 },
    }),
  });
  if (!response.ok) {
    const errText = await response.text();
    throw new Error(`Gemini error: ${response.status} — ${errText.slice(0, 200)}`);
  }
  const data = await response.json();
  const content = data?.candidates?.[0]?.content?.parts?.[0]?.text?.trim();
  if (!content) throw new Error('Empty response from Gemini');
  const stripped = content.replace(/^```(?:json)?\s*/i, '').replace(/\s*```$/i, '').trim();
  const startIdx = stripped.search(/[\[{]/);
  if (startIdx === -1) throw new Error('No JSON in response');
  return JSON.parse(stripped.slice(startIdx));
}

const generatePlaces = (d) => aiCall(`أنت مرشد سياحي متخصص في مدينة الرياض. اليوم هو ${d}.
اقترح 6 أماكن رائعة ومتنوعة في الرياض تستحق الزيارة في نهاية هذا الأسبوع.
تنوّع: حدائق، ثقافية، ترفيه، أسواق، طبيعية. راعِ الموسم والطقس.
أجب فقط بـ JSON: [{ "name":"","description":"","category":"","tag":"","color":"#hex","emoji":"" }]`);

const generateDeals = (d) => aiCall(`أنت خبير عروض ومحلات في السعودية. اليوم ${d}.
اقترح 4 عروض حقيقية ومتنوعة هذا الأسبوع في المملكة من ماركات معروفة.
أجب فقط بـ JSON: [{ "brand":"","offer":"","category":"","valid_until":"","color":"#hex","emoji":"" }]`);

const generatePodcasts = (d) => aiCall(`أنت خبير في عالم البودكاست. اليوم ${d}.
اقترح 3 حلقات بودكاست حقيقية وحديثة باللغة العربية أو الإنجليزية مفيدة لموظفي بيئة العمل.
أجب فقط بـ JSON: [{ "title":"","body":"","channel":"","youtube_query":"","tagline":"","video_id":"","color":"#hex" }]`);

const generateAITools = (d) => aiCall(`أنت خبير تقنية وذكاء اصطناعي. اليوم ${d}.
اقترح 3 أدوات ذكاء اصطناعي مفيدة لموظفي بيئة العمل السعودية في هذا الأسبوع.
أجب فقط بـ JSON: [{ "title":"","tagline":"","uses":["","",""],"url":"","color":"#hex","emoji":"" }]`);

const generateMovies = (d) => aiCall(`أنت ناقد سينمائي. اليوم ${d}.
اقترح أفلاماً مناسبة لعائلات سعودية في دور السينما السعودية هذا الأسبوع.
أجب فقط بـ JSON: [{ "title":"","movies":[{"title":"","genre":"","rating":"","cinema":"","note":"","duration":""}] }]
فئتان: العائلة/الأطفال (3)، الأكشن/الكوميديا (3).`);

const SPL_TEAM_AR = {
  HIL: 'الهلال', NSR: 'النصر', ITH: 'الاتحاد', AHL: 'الأهلي',
  SHA: 'الشباب', FAT: 'الفتح', ITF: 'الاتفاق', QAD: 'القادسية',
  KHA: 'الخليج', TAA: 'التعاون', FEI: 'الفيحاء', DAM: 'ضمك',
  HAZ: 'الحزم', AKH: 'الأخدود', RIY: 'الرياض', NAJ: 'النجمة',
  NEO: 'نيوم', KHL: 'الخلود',
};
const SPL_TEAM_COLOR = {
  HIL: '#1E3A8A', NSR: '#CA8A04', ITH: '#1C1C1C', AHL: '#15803D',
  SHA: '#1F2937', FAT: '#DC2626', ITF: '#1D4ED8', QAD: '#7C3AED',
  KHA: '#0E7490', TAA: '#0D9488', FEI: '#166534', DAM: '#9F1239',
  HAZ: '#6D28D9', AKH: '#B91C1C', RIY: '#2563EB', NAJ: '#B45309',
  NEO: '#0F766E', KHL: '#047857',
};
const AR_DAYS = ['الأحد','الاثنين','الثلاثاء','الأربعاء','الخميس','الجمعة','السبت'];

async function fetchSPLMatches() {
  const url = 'https://api.saudi-pro-league.pulselive.com/football/fixtures?competitions=215&compSeasons=859&pageSize=40&sort=asc&statuses=U';
  const res = await fetch(url, {
    headers: { 'Origin': 'https://www.spl.com.sa', 'Referer': 'https://www.spl.com.sa/' },
  });
  if (!res.ok) throw new Error(`SPL API ${res.status}`);
  const data = await res.json();
  const fixtures = data.content || [];
  if (!fixtures.length) throw new Error('No fixtures returned');

  fixtures.sort((a, b) => {
    const tsA = a.kickoff?.completeness >= 3 ? a.kickoff.millis : (a.provisionalKickoff?.millis || 0);
    const tsB = b.kickoff?.completeness >= 3 ? b.kickoff.millis : (b.provisionalKickoff?.millis || 0);
    return tsA - tsB;
  });

  const mapped = fixtures.slice(0, 10).map((f) => {
    const homeAbbr = f.teams[0]?.team?.club?.abbr || '';
    const awayAbbr = f.teams[1]?.team?.club?.abbr || '';
    const homeAr = SPL_TEAM_AR[homeAbbr] || f.teams[0]?.team?.shortName || homeAbbr;
    const awayAr = SPL_TEAM_AR[awayAbbr] || f.teams[1]?.team?.shortName || awayAbbr;
    const confirmed = f.kickoff?.completeness >= 3;
    const millis = confirmed ? f.kickoff.millis : (f.provisionalKickoff?.millis || 0);
    let timeStr = 'يُحدد لاحقاً';
    let dayStr = '';
    if (millis) {
      const saudiMs = millis + 3 * 3600 * 1000;
      const d = new Date(saudiMs);
      const h = d.getUTCHours(), m = d.getUTCMinutes();
      if (confirmed || h !== 0 || m !== 0) {
        const ampm = h >= 12 ? 'م' : 'ص';
        const h12 = h % 12 || 12;
        timeStr = `${h12}:${String(m).padStart(2, '0')} ${ampm}`;
      }
      dayStr = AR_DAYS[d.getUTCDay()];
    }
    const bigTeams = new Set(['HIL','NSR','ITH','AHL']);
    const isBig = bigTeams.has(homeAbbr) && bigTeams.has(awayAbbr);
    const isFeatured = !isBig && (bigTeams.has(homeAbbr) || bigTeams.has(awayAbbr));
    const importance = isBig ? '🔥 قمة كبرى' : isFeatured ? '⚡ مباراة مهمة' : '⚽ دوري روشن';
    return {
      home: homeAr, away: awayAr, time: timeStr, day: dayStr,
      competition: 'دوري روشن',
      home_color: SPL_TEAM_COLOR[homeAbbr] || '#1A56DB',
      away_color: SPL_TEAM_COLOR[awayAbbr] || '#EF4444',
      importance,
    };
  });
  const highlights = mapped.filter((m) => m.importance !== '⚽ دوري روشن').slice(0, 4);
  const groups = [];
  if (highlights.length > 0) groups.push({ title: '🔥 أبرز مباريات الأسبوع', matches: highlights });
  groups.push({ title: '📅 الجدول الكامل', matches: mapped });
  return groups;
}

const cache = {};

async function generateWeekendData(dateStr, cacheKey) {
  const generators = [
    { key: 'places',   fn: () => generatePlaces(dateStr) },
    { key: 'deals',    fn: () => generateDeals(dateStr) },
    { key: 'podcasts', fn: () => generatePodcasts(dateStr) },
    { key: 'aiTools',  fn: () => generateAITools(dateStr) },
    { key: 'matches',  fn: () => fetchSPLMatches() },
    { key: 'movies',   fn: () => generateMovies(dateStr) },
  ];
  const results = await Promise.allSettled(generators.map((g) => g.fn()));
  const result = {};
  generators.forEach((g, idx) => {
    const r = results[idx];
    result[g.key] =
      r.status === 'fulfilled' && Array.isArray(r.value) && r.value.length > 0
        ? r.value
        : null;
  });
  const successCount = Object.values(result).filter((v) => v !== null).length;
  if (successCount >= 4) {
    cache[cacheKey] = result;
    for (const k of Object.keys(cache)) if (k !== cacheKey) delete cache[k];
  }
  return result;
}

export default async function handler(req, res) {
  res.setHeader('Access-Control-Allow-Origin', '*');
  if (req.method !== 'GET') {
    res.status(405).json({ error: 'Method not allowed' });
    return;
  }
  try {
    const cacheKey = new Date().toISOString().slice(0, 10);
    const dateStr = new Date().toLocaleDateString('ar-SA-u-nu-latn', {
      weekday: 'long', day: 'numeric', month: 'long', year: 'numeric',
    });
    if (!cache[cacheKey]) await generateWeekendData(dateStr, cacheKey);
    res.status(200).json(cache[cacheKey] ?? {});
  } catch (err) {
    res.status(500).json({ error: err.message });
  }
}
