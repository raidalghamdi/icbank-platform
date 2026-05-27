import http from 'http';
import fs from 'fs';
import path from 'path';
import { fileURLToPath } from 'url';

const __dirname = path.dirname(fileURLToPath(import.meta.url));
const PORT = process.env.PORT || 3000;
const HTML_FILE = path.join(__dirname, 'index.html');
const LOGIN_FILE = path.join(__dirname, 'login.html');

// AI calls — Gemini (free) is used as the primary engine after Replit migration.
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
  if (startIdx === -1) {
    console.error('[AI] unexpected content:', stripped.slice(0, 200));
    throw new Error('No JSON in response');
  }
  return JSON.parse(stripped.slice(startIdx));
}

async function generatePlaces(dateStr) {
  return aiCall(`أنت مرشد سياحي متخصص في مدينة الرياض. اليوم هو ${dateStr}.
اقترح 6 أماكن رائعة ومتنوعة في الرياض تستحق الزيارة في نهاية هذا الأسبوع.
تنوّع: حدائق، ثقافية، ترفيه، أسواق، طبيعية. راعِ الموسم والطقس.
أجب فقط بـ JSON بهذا الشكل:
[{"title":"اسم المكان","body":"وصف جذاب في 2-3 جمل","maps_query":"English search query for Google Maps"}]`);
}

async function generateDeals(dateStr) {
  return aiCall(`اليوم ${dateStr}. أعطني 3 فئات عروض وخصومات نهاية الأسبوع في الرياض. كل فئة تحتوي 3 عروض.
أجب بـ JSON فقط:
[{"title":"فئة عربية","items":[{"place":"اسم المكان","discount":"نسبة أو نوع الخصم","detail":"تفاصيل العرض","emoji":"إيموجي","url":"رابط"}]}]
الفئات المطلوبة: مطاعم وكافيهات، تسوق وأزياء، ترفيه ورياضة.`);
}

async function generatePodcasts(dateStr) {
  return aiCall(`أنت محرر محتوى متخصص في البودكاست العربي. اليوم ${dateStr}.
اقترح 3 بودكاستات عربية مميزة مناسبة لموظفي بيئة العمل الحكومي والخاص في السعودية.
تنوّع بين: التطوير المهني، الثقافة، الصحة النفسية، ريادة الأعمال، رؤية 2030.
أجب فقط بـ JSON:
[{
  "title": "اسم البودكاست",
  "field": "مجال البودكاست (قصير)",
  "episode": "عنوان حلقة مقترحة مناسبة",
  "body": "وصف البودكاست وفائدته لبيئة العمل في 2 جمل",
  "channel": "اسم القناة أو المقدم",
  "youtube_query": "search query للبحث في يوتيوب",
  "tagline": "جملة ملهمة قصيرة عن البودكاست",
  "video_id": "",
  "color": "لون hex مناسب مثل #1A56DB"
}]`);
}

async function generateAITools(dateStr) {
  return aiCall(`أنت خبير تقنية وذكاء اصطناعي. اليوم ${dateStr}.
اقترح 3 أدوات ذكاء اصطناعي مفيدة لموظفي بيئة العمل السعودية في هذا الأسبوع.
تنوّع بين: كتابة، تصميم، إنتاجية، تحليل، تواصل.
أجب فقط بـ JSON:
[{
  "title": "اسم الأداة بالإنجليزي",
  "tagline": "وصف جذاب قصير بالعربية",
  "uses": ["استخدام 1","استخدام 2","استخدام 3"],
  "url": "https://رابط الأداة",
  "color": "لون hex مناسب",
  "emoji": "إيموجي مناسب"
}]`);
}

const SPL_TEAM_AR = {
  HIL: 'الهلال',   NSR: 'النصر',    ITH: 'الاتحاد',  AHL: 'الأهلي',
  SHA: 'الشباب',   FAT: 'الفتح',    ITF: 'الاتفاق',  QAD: 'القادسية',
  KHA: 'الخليج',   TAA: 'التعاون',  FEI: 'الفيحاء',  DAM: 'ضمك',
  HAZ: 'الحزم',    AKH: 'الأخدود',  RIY: 'الرياض',   NAJ: 'النجمة',
  NEO: 'نيوم',     KHL: 'الخلود',
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

  // sort: confirmed kickoff first, then provisional
  fixtures.sort((a, b) => {
    const tsA = a.kickoff?.completeness >= 3 ? a.kickoff.millis : (a.provisionalKickoff?.millis || 0);
    const tsB = b.kickoff?.completeness >= 3 ? b.kickoff.millis : (b.provisionalKickoff?.millis || 0);
    return tsA - tsB;
  });

  const mapped = fixtures.slice(0, 10).map(f => {
    const homeAbbr = f.teams[0]?.team?.club?.abbr || '';
    const awayAbbr = f.teams[1]?.team?.club?.abbr || '';
    const homeAr = SPL_TEAM_AR[homeAbbr] || f.teams[0]?.team?.shortName || homeAbbr;
    const awayAr = SPL_TEAM_AR[awayAbbr] || f.teams[1]?.team?.shortName || awayAbbr;

    const confirmed = f.kickoff?.completeness >= 3;
    const millis = confirmed ? f.kickoff.millis : (f.provisionalKickoff?.millis || 0);
    let timeStr = 'يُحدد لاحقاً';
    let dayStr = '';
    if (millis) {
      // convert UTC millis → Saudi time (UTC+3)
      const saudiMs = millis + 3 * 3600 * 1000;
      const d = new Date(saudiMs);
      const h = d.getUTCHours(), m = d.getUTCMinutes();
      if (confirmed || h !== 0 || m !== 0) {
        const ampm = h >= 12 ? 'م' : 'ص';
        const h12 = h % 12 || 12;
        timeStr = `${h12}:${String(m).padStart(2,'0')} ${ampm}`;
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

  const highlights = mapped.filter(m => m.importance !== '⚽ دوري روشن').slice(0, 4);
  const groups = [];
  if (highlights.length > 0) {
    groups.push({ title: '🔥 أبرز مباريات الأسبوع', matches: highlights });
  }
  groups.push({ title: '📅 الجدول الكامل', matches: mapped });
  return groups;
}

async function generateMovies(dateStr) {
  return aiCall(`أنت ناقد سينمائي ومرشد ترفيهي. اليوم ${dateStr}.
اقترح أفلاماً متنوعة مناسبة لعائلات سعودية في نهاية هذا الأسبوع في دور السينما السعودية (VOX، Muvi، AMC).
أجب فقط بـ JSON:
[
  {
    "title": "فئة الأفلام مثل: أفلام العائلة",
    "movies": [
      {"title": "اسم الفيلم بالإنجليزي","genre": "النوع بالعربية","rating": "⭐⭐⭐⭐ أو 5 نجوم","cinema": "Muvi أو VOX أو AMC","note": "وصف قصير جذاب","duration": "مدة الفيلم بالدقائق"}
    ]
  }
]
أضف فئتين: أفلام العائلة/الأطفال (3 أفلام)، وأفلام الأكشن/الكوميديا (3 أفلام). اذكر أفلاماً حقيقية معروفة.`);
}

// Cache per calendar date
const cache = {};

async function generateWeekendData(dateStr, cacheKey) {
  console.log(`[AI] توليد محتوى نهاية الأسبوع ليوم ${dateStr} ...`);

  // run all in parallel, with per-section retry on failure
  const generators = [
    { key: 'places',   fn: () => generatePlaces(dateStr) },
    { key: 'deals',    fn: () => generateDeals(dateStr) },
    { key: 'podcasts', fn: () => generatePodcasts(dateStr) },
    { key: 'aiTools',  fn: () => generateAITools(dateStr) },
    { key: 'matches',  fn: () => fetchSPLMatches() },
    { key: 'movies',   fn: () => generateMovies(dateStr) },
  ];

  const results = await Promise.allSettled(generators.map(g => g.fn()));
  const result = {};
  generators.forEach((g, idx) => {
    const r = results[idx];
    if (r.status === 'fulfilled' && Array.isArray(r.value) && r.value.length > 0) {
      result[g.key] = r.value;
    } else {
      console.error(`[AI] فشل توليد ${g.key}:`, r.reason?.message || 'empty result');
      result[g.key] = null;
    }
  });

  const successCount = Object.values(result).filter(v => v !== null).length;
  console.log(`[AI] اكتمل: ${successCount}/6 أقسام تم توليدها`);

  // only cache if at least 4 sections succeeded
  if (successCount >= 4) {
    cache[cacheKey] = result;
    for (const k of Object.keys(cache)) {
      if (k !== cacheKey) delete cache[k];
    }
  }
  return result;
}

/**
 * Parse cookies from the Cookie request header.
 */
function parseCookies(cookieHeader) {
  const cookies = {};
  if (!cookieHeader) return cookies;
  cookieHeader.split(';').forEach(part => {
    const eqIdx = part.indexOf('=');
    if (eqIdx === -1) return;
    const name = part.slice(0, eqIdx).trim();
    const value = part.slice(eqIdx + 1).trim();
    if (name) cookies[name] = value;
  });
  return cookies;
}

const server = http.createServer(async (req, res) => {
  res.setHeader('Access-Control-Allow-Origin', '*');

  const urlPath = (req.url || '/').split('?')[0];

  // Public paths that don't require auth
  const isPublicPath =
    urlPath === '/login' ||
    urlPath === '/login.html' ||
    urlPath === '/wk2-data';

  // Server-side auth gate — redirect to /login only when the browser has no
  // session indicator at all.  Two JS-accessible cookies signal presence:
  //   • access_token  — short-lived (15 min); set client-side after login/refresh
  //   • has_session   — long-lived (7 days); set client-side at login to mirror
  //                     the httpOnly refresh_token lifetime.  Cleared on logout.
  // Note: refresh_token is httpOnly + path:/api/auth so it is NOT visible here.
  // When only has_session is present (access token expired), we still serve the
  // HTML shell; the auth guard silently calls /api/auth/refresh and continues.
  if (!isPublicPath) {
    const cookies = parseCookies(req.headers['cookie']);
    const hasSession = cookies['access_token'] || cookies['has_session'];
    if (!hasSession) {
      res.writeHead(302, { 'Location': '/login', 'Cache-Control': 'no-store' });
      res.end();
      return;
    }
  }

  if (req.url === '/wk2-data' && req.method === 'GET') {
    try {
      const cacheKey = new Date().toISOString().slice(0, 10);
      const dateStr = new Date().toLocaleDateString('ar-SA-u-nu-latn', {
        weekday: 'long', day: 'numeric', month: 'long', year: 'numeric',
      });

      if (!cache[cacheKey]) {
        await generateWeekendData(dateStr, cacheKey);
      } else {
        console.log(`[AI] استخدام الكاش ليوم ${cacheKey}`);
      }

      res.writeHead(200, { 'Content-Type': 'application/json; charset=utf-8' });
      res.end(JSON.stringify(cache[cacheKey]));
    } catch (err) {
      console.error('[AI] خطأ عام:', err.message);
      res.writeHead(500, { 'Content-Type': 'application/json' });
      res.end(JSON.stringify({ error: err.message }));
    }
    return;
  }


  // Login page
  if (req.url === '/login' || req.url === '/login.html') {
    fs.readFile(LOGIN_FILE, (err, data) => {
      if (err) { res.writeHead(500); res.end('Error loading login page'); return; }
      res.writeHead(200, { 'Content-Type': 'text/html; charset=utf-8', 'Cache-Control': 'no-cache' });
      res.end(data);
    });
    return;
  }

  // Static HTML — auth guard runs client-side via /api/auth/me
  fs.readFile(HTML_FILE, (err, data) => {
    if (err) { res.writeHead(500); res.end('Error loading page'); return; }
    res.writeHead(200, { 'Content-Type': 'text/html; charset=utf-8', 'Cache-Control': 'no-cache' });
    res.end(data);
  });
});

server.listen(PORT, '0.0.0.0', () => {
  console.log(`Server running at http://0.0.0.0:${PORT}`);
});
