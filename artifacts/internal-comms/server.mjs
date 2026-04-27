import http from 'http';
import fs from 'fs';
import path from 'path';
import { fileURLToPath } from 'url';

const __dirname = path.dirname(fileURLToPath(import.meta.url));
const PORT = process.env.PORT || 3000;
const HTML_FILE = path.join(__dirname, 'index.html');

const OPENAI_BASE_URL = process.env.AI_INTEGRATIONS_OPENAI_BASE_URL;
const OPENAI_API_KEY = process.env.AI_INTEGRATIONS_OPENAI_API_KEY;

async function generatePlaces(dateStr) {
  const prompt = `أنت مرشد سياحي متخصص في مدينة الرياض بالمملكة العربية السعودية.
اليوم هو ${dateStr}.
اقترح 6 أماكن رائعة ومتنوعة في الرياض تستحق الزيارة في نهاية هذا الأسبوع.
تنوّع في الأماكن: حدائق، وجهات ثقافية، مراكز ترفيه، مطاعم مميزة، أسواق شعبية، متاحف، إلخ.
راعِ الموسم والطقس الحالي.

أجب فقط بـ JSON صحيح بهذا الشكل بدون أي نص إضافي:
[
  {
    "title": "اسم المكان",
    "body": "وصف جذاب للمكان في جملتين أو ثلاث — ما يميزه ولماذا يستحق الزيارة هذا الأسبوع",
    "maps_query": "search query in English for Google Maps"
  }
]`;

  const response = await fetch(`${OPENAI_BASE_URL}/chat/completions`, {
    method: 'POST',
    headers: {
      'Content-Type': 'application/json',
      'Authorization': `Bearer ${OPENAI_API_KEY}`,
    },
    body: JSON.stringify({
      model: 'gpt-5-mini',
      max_completion_tokens: 2048,
      messages: [{ role: 'user', content: prompt }],
    }),
  });

  if (!response.ok) {
    throw new Error(`OpenAI error: ${response.status}`);
  }

  const data = await response.json();
  const content = data.choices?.[0]?.message?.content?.trim();
  // handle markdown code blocks and raw JSON
  const jsonMatch = content.match(/```(?:json)?\s*([\s\S]*?)```/) || content.match(/(\[[\s\S]*\])/);
  if (!jsonMatch) throw new Error('No JSON array in response');
  const rawJson = (jsonMatch[1] || jsonMatch[0]).trim();
  return JSON.parse(rawJson.startsWith('[') ? rawJson : rawJson.slice(rawJson.indexOf('[')));
}

// Cache: store places per calendar date so one AI call per day
const placesCache = {};

const server = http.createServer(async (req, res) => {
  // CORS headers
  res.setHeader('Access-Control-Allow-Origin', '*');

  if (req.url === '/api/places' && req.method === 'GET') {
    try {
      const today = new Date().toLocaleDateString('ar-SA-u-nu-latn', {
        weekday: 'long', day: 'numeric', month: 'long', year: 'numeric',
      });
      const cacheKey = new Date().toISOString().slice(0, 10);

      if (!placesCache[cacheKey]) {
        console.log(`[AI] توليد أماكن جديدة ليوم ${today}`);
        placesCache[cacheKey] = await generatePlaces(today);
        // إزالة التواريخ القديمة من الكاش
        for (const k of Object.keys(placesCache)) {
          if (k !== cacheKey) delete placesCache[k];
        }
      } else {
        console.log(`[AI] استخدام الكاش ليوم ${cacheKey}`);
      }

      res.writeHead(200, { 'Content-Type': 'application/json; charset=utf-8' });
      res.end(JSON.stringify({ places: placesCache[cacheKey] }));
    } catch (err) {
      console.error('[AI] خطأ:', err.message);
      res.writeHead(500, { 'Content-Type': 'application/json' });
      res.end(JSON.stringify({ error: err.message }));
    }
    return;
  }

  // Static HTML
  fs.readFile(HTML_FILE, (err, data) => {
    if (err) {
      res.writeHead(500);
      res.end('Error loading page');
      return;
    }
    res.writeHead(200, {
      'Content-Type': 'text/html; charset=utf-8',
      'Cache-Control': 'no-cache',
    });
    res.end(data);
  });
});

server.listen(PORT, '0.0.0.0', () => {
  console.log(`Server running at http://0.0.0.0:${PORT}`);
});
