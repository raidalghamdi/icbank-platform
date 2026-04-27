import http from 'http';
import fs from 'fs';
import path from 'path';
import { fileURLToPath } from 'url';

const __dirname = path.dirname(fileURLToPath(import.meta.url));
const PORT = process.env.PORT || 3000;
const HTML_FILE = path.join(__dirname, 'index.html');

const OPENAI_BASE_URL = process.env.AI_INTEGRATIONS_OPENAI_BASE_URL;
const OPENAI_API_KEY = process.env.AI_INTEGRATIONS_OPENAI_API_KEY;

async function aiCall(prompt) {
  const response = await fetch(`${OPENAI_BASE_URL}/chat/completions`, {
    method: 'POST',
    headers: {
      'Content-Type': 'application/json',
      'Authorization': `Bearer ${OPENAI_API_KEY}`,
    },
    body: JSON.stringify({
      model: 'gpt-5-mini',
      max_completion_tokens: 3000,
      messages: [
        { role: 'system', content: 'أنت مساعد يجيب بـ JSON فقط بدون أي نص إضافي أو markdown.' },
        { role: 'user', content: prompt },
      ],
    }),
  });
  if (!response.ok) throw new Error(`OpenAI error: ${response.status}`);
  const data = await response.json();
  const content = data.choices?.[0]?.message?.content?.trim();
  if (!content) throw new Error('Empty response');
  // strip markdown code fences if present
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

async function generateMatches(dateStr) {
  return aiCall(`أنت محلل رياضي متخصص في كرة القدم السعودية. اليوم ${dateStr}.
اقترح جدول مباريات دوري روشن للمحترفين لنهاية هذا الأسبوع (الخميس والجمعة والسبت).
استخدم أسماء الأندية السعودية الحقيقية: الهلال، النصر، الاتحاد، الأهلي، الشباب، الفتح، الاتفاق، ضمك، الخليج، التعاون، الفيصلي، القادسية، الحزم، الأخدود، الرائد، النجمة.
أجب فقط بـ JSON:
[{
  "title": "عنوان المجموعة مثل: أبرز مباريات الأسبوع",
  "matches": [
    {"home": "الفريق المضيف","away": "الفريق الضيف","time": "4:30 م","day": "الخميس","competition": "دوري روشن","home_color": "#1A56DB","away_color": "#EF4444","importance": "🔥 قمة كبرى أو ⚡ مباراة مهمة أو ⚽ دوري روشن"}
  ]
}]
أضف مجموعتين: الأولى أبرز المباريات (3-4 مباريات)، الثانية الجدول الكامل (6-8 مباريات).`);
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
    { key: 'matches',  fn: () => generateMatches(dateStr) },
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

const server = http.createServer(async (req, res) => {
  res.setHeader('Access-Control-Allow-Origin', '*');

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


  // Static HTML
  fs.readFile(HTML_FILE, (err, data) => {
    if (err) { res.writeHead(500); res.end('Error loading page'); return; }
    res.writeHead(200, { 'Content-Type': 'text/html; charset=utf-8', 'Cache-Control': 'no-cache' });
    res.end(data);
  });
});

server.listen(PORT, '0.0.0.0', () => {
  console.log(`Server running at http://0.0.0.0:${PORT}`);
});
