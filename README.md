# ICBank Platform — Internal Communications & AI Initiatives

> Bilingual (AR/EN) internal communications platform for the General Authority
> for Competition. Includes AI Year 2026 campaign management, international-day
> tracker, design generator, weekend content, daily reports, and RBAC.

[![Built with](https://img.shields.io/badge/built%20with-Express%205%20%2B%20Vite%20%2B%20Drizzle-blue)]()
[![Database](https://img.shields.io/badge/database-Supabase%20Postgres-3ecf8e)]()
[![AI](https://img.shields.io/badge/AI-Gemini%20%2B%20Perplexity-orange)]()

---

## Architecture

```
┌─────────────────┐        ┌─────────────────┐        ┌──────────────────┐
│  Vercel         │  HTTPS │  Railway        │  SQL   │  Supabase        │
│  (frontend +    │ ─────▶ │  (Express 5     │ ─────▶ │  Postgres +      │
│   /wk2-data fn) │        │   API server)   │        │  Storage         │
└─────────────────┘        └─────────────────┘        └──────────────────┘
        │                          │                          │
        └────── Gemini API ────────┴──── Perplexity API ──────┘
```

| Layer        | Service                          | Location                            |
|--------------|----------------------------------|-------------------------------------|
| Frontend     | Vercel                           | `artifacts/internal-comms/`         |
| Backend API  | Railway (Docker)                 | `artifacts/api-server/`             |
| Database     | Supabase Postgres                | `lib/db/src/schema/`                |
| File storage | Supabase Storage                 | bucket: `icbank`                    |
| AI text/image| Google Gemini (free tier)        | `gemini-2.5-flash` / `flash-image`  |
| AI search    | Perplexity API ($5/mo Pro)       | international-days route only       |

---

## Repository layout (pnpm monorepo)

```
.
├── artifacts/
│   ├── api-server/          # Express 5 API (auth, ai-year, designs, etc.)
│   ├── internal-comms/      # Frontend (HTML + Vercel serverless function)
│   └── mockup-sandbox/      # Visual playground
├── lib/
│   ├── db/                  # Drizzle schemas + connection
│   ├── api-zod/             # Shared zod validators
│   ├── api-spec/            # OpenAPI spec
│   └── api-client-react/    # Typed react-query client
├── scripts/                 # Seed scripts
├── supabase/
│   └── schema.sql           # Postgres schema for fresh deploy
├── railway.json             # Railway build config
└── .env.example             # All environment variables
```

---

## Quick-start deployment (≈ 30 min)

### 0. Prerequisites

- Node 22, pnpm 9 (`corepack enable && corepack prepare pnpm@9 --activate`)
- Accounts on **Supabase**, **Railway** (or Render), **Vercel**, **GitHub**
- API keys:
  - **Gemini**: <https://aistudio.google.com/apikey> (free)
  - **Perplexity**: <https://www.perplexity.ai/settings/api> (Pro plan = $5/mo credit)

### 1. Supabase — database + storage

1. Create a new project at <https://supabase.com>.
2. **Database** → copy the **transaction pooler** connection string into
   `DATABASE_URL`.
3. **Storage** → New bucket → name `icbank` → **Private**.
4. **SQL Editor** → New query → paste `supabase/schema.sql` → **Run**.
5. **Project Settings → API** → copy:
   - `SUPABASE_URL` (Project URL)
   - `SUPABASE_SERVICE_KEY` (service_role secret — server-side only)

### 2. Railway — API server

1. Sign in with GitHub at <https://railway.app>.
2. **New project → Deploy from GitHub repo** → pick this repo.
3. Railway auto-detects `railway.json` → uses `artifacts/api-server/Dockerfile`.
4. **Variables** → add everything from `.env.example` except the `VITE_*` and
   `BASE_PATH` keys.
5. Wait for the build → copy the generated public URL
   (e.g. `https://icbank-api.up.railway.app`) — this becomes
   `VITE_API_BASE_URL` for the frontend.
6. Verify: `curl https://<your-railway>.up.railway.app/health` → `{"ok":true}`.

### 3. Vercel — frontend + /wk2-data function

1. Sign in at <https://vercel.com> and import the same GitHub repo.
2. **Root directory** → `artifacts/internal-comms`.
3. **Framework preset** → *Other*.
4. **Build command** → leave empty (static HTML + serverless function).
5. **Environment variables** → set:
   - `GEMINI_API_KEY` (for /wk2-data)
   - `VITE_API_BASE_URL` (your Railway URL from step 2)
6. Deploy → site is live at `https://<project>.vercel.app`.

### 4. First-time setup

1. Create your admin user — easiest via Supabase SQL editor:

   ```sql
   INSERT INTO users (email, name, title, password_hash, is_active)
   VALUES (
     'you@example.com',
     'اسمك',
     'مسؤول النظام',
     -- bcrypt hash of "TempPass123!" — change after first login
     '$2b$10$N9qo8uLOickgx2ZMRZoMyeI8q1H4cE3p4r4l8WfP1RA0eFAjqEQk2',
     TRUE
   );

   INSERT INTO user_roles (user_id, role_id)
   SELECT u.id, r.id FROM users u, roles r
   WHERE u.email = 'you@example.com' AND r.name = 'super_admin';
   ```

2. (Optional) Seed AI Year demo data:
   ```bash
   pnpm --filter @workspace/scripts run seed:aiyear
   ```

---

## Environment variables reference

See `.env.example` for the full list. Key vars per service:

| Variable                  | Railway (API) | Vercel (frontend) | Notes                              |
|---------------------------|:-------------:|:-----------------:|------------------------------------|
| `DATABASE_URL`            | ✅            | —                 | Supabase pooler connection string  |
| `SUPABASE_URL`            | ✅            | —                 | Project URL                        |
| `SUPABASE_SERVICE_KEY`    | ✅            | —                 | service_role secret                |
| `SUPABASE_STORAGE_BUCKET` | ✅            | —                 | Default `icbank`                   |
| `GEMINI_API_KEY`          | ✅            | ✅                | Used by API + /wk2-data            |
| `PERPLEXITY_API_KEY`      | ✅            | —                 | international-days route only      |
| `JWT_SECRET`              | ✅            | —                 | `openssl rand -hex 32`             |
| `REPORT_API_KEY`          | ✅            | —                 | Optional bearer for /report        |
| `INTERNAL_STORAGE_TOKEN`  | ✅            | —                 | Optional bearer for /storage/objects |
| `VITE_API_BASE_URL`       | —             | ✅                | Railway public URL                 |

---

## Local development

```bash
# Install everything
pnpm install

# Run the API (needs DATABASE_URL + SUPABASE_* in .env)
pnpm --filter @workspace/api-server dev

# Run the frontend (needs GEMINI_API_KEY for /wk2-data)
cd artifacts/internal-comms && PORT=3000 node server.mjs
```

The legacy `server.mjs` is still functional for local dev — Vercel uses the
serverless function under `api/wk2-data.js` instead.

---

## Migration notes (from Replit)

This repo was migrated off Replit. Key changes:

- **Storage**: Google Cloud Storage (Replit sidecar) → **Supabase Storage**
- **AI providers**: Claude + OpenAI + Gemini → **Gemini only** (Perplexity
  preserved for international-days web search)
- **Build/host**: Replit Deployments → **Vercel (FE) + Railway (API)**
- **Database**: Replit Postgres → **Supabase Postgres**

The original `objectStorage.ts` API surface was preserved; route code did not
need refactoring beyond two lines in `ai-year.ts` (replaced `file.getMetadata()`
+ `file.createReadStream()` with the new `createReadStream(file)` helper).

---

## License

Internal — General Authority for Competition (GAC).
