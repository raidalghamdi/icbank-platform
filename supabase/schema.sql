-- ╔══════════════════════════════════════════════════════════════════════════╗
-- ║  ICBank Platform — Postgres Schema                                       ║
-- ║  Target: Supabase Postgres                                               ║
-- ║                                                                          ║
-- ║  Generated from Drizzle schemas in lib/db/src/schema/*.ts                ║
-- ║  Apply once on a fresh database:                                          ║
-- ║    psql "$DATABASE_URL" -f supabase/schema.sql                           ║
-- ║  Or via Supabase SQL editor (Project → SQL → New query → paste → Run).   ║
-- ╚══════════════════════════════════════════════════════════════════════════╝

BEGIN;

-- ─── RBAC ──────────────────────────────────────────────────────────────────
CREATE TABLE IF NOT EXISTS users (
  id                  SERIAL PRIMARY KEY,
  email               TEXT NOT NULL UNIQUE,
  name                TEXT NOT NULL,
  title               TEXT,
  department          TEXT,
  password_hash       TEXT,
  azure_oid           TEXT UNIQUE,
  is_active           BOOLEAN NOT NULL DEFAULT TRUE,
  is_locked           BOOLEAN NOT NULL DEFAULT FALSE,
  failed_attempts     INTEGER NOT NULL DEFAULT 0,
  last_login          TIMESTAMP,
  password_changed_at TIMESTAMP,
  created_at          TIMESTAMP NOT NULL DEFAULT NOW(),
  updated_at          TIMESTAMP NOT NULL DEFAULT NOW()
);

CREATE TABLE IF NOT EXISTS roles (
  id          SERIAL PRIMARY KEY,
  name        TEXT NOT NULL UNIQUE,
  name_ar     TEXT NOT NULL,
  description TEXT,
  is_system   BOOLEAN NOT NULL DEFAULT FALSE,
  created_at  TIMESTAMP NOT NULL DEFAULT NOW()
);

CREATE TABLE IF NOT EXISTS pages (
  id         SERIAL PRIMARY KEY,
  slug       TEXT NOT NULL UNIQUE,
  name_ar    TEXT NOT NULL,
  icon       TEXT,
  sort_order INTEGER NOT NULL DEFAULT 0,
  is_active  BOOLEAN NOT NULL DEFAULT TRUE
);

CREATE TABLE IF NOT EXISTS permissions (
  id      SERIAL PRIMARY KEY,
  name    TEXT NOT NULL UNIQUE,
  name_ar TEXT NOT NULL
);

CREATE TABLE IF NOT EXISTS role_permissions (
  id            SERIAL PRIMARY KEY,
  role_id       INTEGER NOT NULL REFERENCES roles(id)       ON DELETE CASCADE,
  page_id       INTEGER NOT NULL REFERENCES pages(id)       ON DELETE CASCADE,
  permission_id INTEGER NOT NULL REFERENCES permissions(id) ON DELETE CASCADE
);
CREATE UNIQUE INDEX IF NOT EXISTS role_page_perm_idx
  ON role_permissions (role_id, page_id, permission_id);

CREATE TABLE IF NOT EXISTS user_roles (
  id          SERIAL PRIMARY KEY,
  user_id     INTEGER NOT NULL REFERENCES users(id) ON DELETE CASCADE,
  role_id     INTEGER NOT NULL REFERENCES roles(id) ON DELETE CASCADE,
  assigned_by INTEGER REFERENCES users(id),
  assigned_at TIMESTAMP NOT NULL DEFAULT NOW()
);
CREATE UNIQUE INDEX IF NOT EXISTS user_role_idx
  ON user_roles (user_id, role_id);

CREATE TABLE IF NOT EXISTS user_page_overrides (
  id            SERIAL PRIMARY KEY,
  user_id       INTEGER NOT NULL REFERENCES users(id)       ON DELETE CASCADE,
  page_id       INTEGER NOT NULL REFERENCES pages(id)       ON DELETE CASCADE,
  permission_id INTEGER NOT NULL REFERENCES permissions(id) ON DELETE CASCADE,
  grant_type    TEXT NOT NULL,
  created_by    INTEGER REFERENCES users(id),
  created_at    TIMESTAMP NOT NULL DEFAULT NOW()
);

CREATE TABLE IF NOT EXISTS activity_logs (
  id          SERIAL PRIMARY KEY,
  user_id     INTEGER REFERENCES users(id) ON DELETE SET NULL,
  action      TEXT NOT NULL,
  entity_type TEXT,
  entity_id   TEXT,
  details     JSONB,
  ip_address  TEXT,
  user_agent  TEXT,
  created_at  TIMESTAMP NOT NULL DEFAULT NOW()
);

CREATE TABLE IF NOT EXISTS system_settings (
  id         SERIAL PRIMARY KEY,
  key        TEXT NOT NULL UNIQUE,
  value      TEXT NOT NULL,
  updated_at TIMESTAMP NOT NULL DEFAULT NOW()
);

-- ─── AI Year (عام الذكاء الاصطناعي) ──────────────────────────────────────
CREATE TABLE IF NOT EXISTS ai_year_activations (
  id              SERIAL PRIMARY KEY,
  title           TEXT NOT NULL,
  month           INTEGER NOT NULL,
  year            INTEGER NOT NULL DEFAULT 2026,
  activation_date TEXT,
  type            TEXT NOT NULL,
  channels        TEXT[] NOT NULL DEFAULT '{}',
  description     TEXT,
  tags            JSONB DEFAULT '[]'::jsonb,
  status          TEXT NOT NULL DEFAULT 'published',
  reach           INTEGER,
  engagement      INTEGER,
  notes           TEXT,
  created_at      TIMESTAMPTZ NOT NULL DEFAULT NOW(),
  updated_at      TIMESTAMPTZ NOT NULL DEFAULT NOW()
);

CREATE TABLE IF NOT EXISTS ai_year_media (
  id            SERIAL PRIMARY KEY,
  activation_id INTEGER NOT NULL REFERENCES ai_year_activations(id) ON DELETE CASCADE,
  object_path   TEXT NOT NULL,
  file_name     TEXT,
  content_type  TEXT,
  sort_order    INTEGER NOT NULL DEFAULT 0,
  created_at    TIMESTAMPTZ NOT NULL DEFAULT NOW()
);

CREATE TABLE IF NOT EXISTS ai_year_metrics (
  id            SERIAL PRIMARY KEY,
  activation_id INTEGER NOT NULL REFERENCES ai_year_activations(id) ON DELETE CASCADE,
  metric_key    TEXT NOT NULL,
  metric_value  TEXT,
  created_at    TIMESTAMPTZ NOT NULL DEFAULT NOW()
);

-- ─── Daily Reports ────────────────────────────────────────────────────────
CREATE TABLE IF NOT EXISTS daily_reports (
  id          SERIAL PRIMARY KEY,
  report_date DATE NOT NULL,
  report_data JSONB NOT NULL,
  created_at  TIMESTAMPTZ NOT NULL DEFAULT NOW()
);

-- ─── Week Start (بداية الأسبوع) ──────────────────────────────────────────
CREATE TABLE IF NOT EXISTS archive_entries (
  id          SERIAL PRIMARY KEY,
  title       TEXT NOT NULL,
  body_text   TEXT NOT NULL,
  date        TIMESTAMPTZ,
  occasion    TEXT,
  tone        TEXT,
  source_file TEXT,
  embedding   JSONB,
  created_at  TIMESTAMPTZ NOT NULL DEFAULT NOW()
);

CREATE TABLE IF NOT EXISTS style_profile (
  id                   SERIAL PRIMARY KEY,
  tone_summary         TEXT,
  avg_paragraph_length REAL,
  opener_patterns      JSONB,
  closer_patterns      JSONB,
  recurring_keywords   JSONB,
  quote_usage          TEXT,
  updated_at           TIMESTAMPTZ NOT NULL DEFAULT NOW()
);

CREATE TABLE IF NOT EXISTS generated_outputs (
  id           SERIAL PRIMARY KEY,
  topic        TEXT NOT NULL,
  model_name   TEXT NOT NULL,
  output_text  TEXT NOT NULL DEFAULT '',
  archive_refs JSONB DEFAULT '[]'::jsonb,
  selected     BOOLEAN NOT NULL DEFAULT FALSE,
  created_at   TIMESTAMPTZ NOT NULL DEFAULT NOW()
);

-- ─── International Days ──────────────────────────────────────────────────
CREATE TABLE IF NOT EXISTS international_days (
  id                         SERIAL PRIMARY KEY,
  day_name_ar                TEXT NOT NULL,
  day_name_en                TEXT,
  annual_date                TEXT,
  category                   TEXT,
  official_organizer         TEXT,
  official_organizer_source  TEXT,
  history_summary            TEXT,
  history_source             TEXT,
  suggestions                JSONB,
  last_searched_at           TIMESTAMPTZ,
  created_at                 TIMESTAMPTZ NOT NULL DEFAULT NOW(),
  updated_at                 TIMESTAMPTZ NOT NULL DEFAULT NOW()
);

CREATE TABLE IF NOT EXISTS day_yearly_themes (
  id               SERIAL PRIMARY KEY,
  day_id           INTEGER NOT NULL REFERENCES international_days(id) ON DELETE CASCADE,
  year             INTEGER NOT NULL,
  theme_ar         TEXT,
  theme_en         TEXT,
  theme_source_url TEXT
);

CREATE TABLE IF NOT EXISTS day_activations (
  id              SERIAL PRIMARY KEY,
  day_id          INTEGER NOT NULL REFERENCES international_days(id) ON DELETE CASCADE,
  year            INTEGER,
  entity_name     TEXT,
  entity_type     TEXT,
  activation_type TEXT,
  platform        TEXT,
  description     TEXT,
  media_url       TEXT,
  source_url      TEXT,
  country         TEXT,
  verified        BOOLEAN NOT NULL DEFAULT FALSE
);

CREATE TABLE IF NOT EXISTS intl_day_sources (
  id               SERIAL PRIMARY KEY,
  related_table    TEXT NOT NULL,
  related_id       INTEGER NOT NULL,
  source_url       TEXT,
  source_title     TEXT,
  source_publisher TEXT,
  accessed_at      TIMESTAMPTZ NOT NULL DEFAULT NOW()
);

CREATE TABLE IF NOT EXISTS intl_search_history (
  id          SERIAL PRIMARY KEY,
  query       TEXT NOT NULL,
  day_id      INTEGER,
  ip_address  TEXT,
  searched_at TIMESTAMPTZ NOT NULL DEFAULT NOW()
);

-- ─── Designs ─────────────────────────────────────────────────────────────
CREATE TABLE IF NOT EXISTS design_templates (
  id                       SERIAL PRIMARY KEY,
  template_name_ar         TEXT NOT NULL,
  category                 TEXT NOT NULL,
  canvas_width             INTEGER NOT NULL DEFAULT 1920,
  canvas_height            INTEGER NOT NULL DEFAULT 1080,
  background_panel_config  JSONB,
  text_slots               JSONB NOT NULL DEFAULT '[]'::jsonb,
  logo_slots               JSONB NOT NULL DEFAULT '[]'::jsonb,
  thumbnail_url            TEXT,
  created_at               TIMESTAMPTZ NOT NULL DEFAULT NOW()
);

CREATE TABLE IF NOT EXISTS brand_logos (
  id            SERIAL PRIMARY KEY,
  logo_name     TEXT NOT NULL,
  file_url      TEXT NOT NULL,
  transparent   BOOLEAN NOT NULL DEFAULT FALSE,
  default_width INTEGER,
  uploaded_at   TIMESTAMPTZ NOT NULL DEFAULT NOW()
);

CREATE TABLE IF NOT EXISTS brand_fonts (
  id            SERIAL PRIMARY KEY,
  font_name     TEXT NOT NULL,
  font_file_url TEXT NOT NULL,
  is_default    BOOLEAN NOT NULL DEFAULT FALSE,
  uploaded_at   TIMESTAMPTZ NOT NULL DEFAULT NOW()
);

CREATE TABLE IF NOT EXISTS generated_designs (
  id                    SERIAL PRIMARY KEY,
  template_id           INTEGER REFERENCES design_templates(id) ON DELETE SET NULL,
  title_text            TEXT,
  body_text             TEXT,
  background_image_url  TEXT,
  selected_logos        JSONB NOT NULL DEFAULT '[]'::jsonb,
  final_image_url       TEXT,
  created_by            INTEGER,
  created_at            TIMESTAMPTZ NOT NULL DEFAULT NOW()
);

-- ─── Weekend Places ──────────────────────────────────────────────────────
CREATE TABLE IF NOT EXISTS weekend_places (
  id          SERIAL PRIMARY KEY,
  name        TEXT NOT NULL,
  description TEXT NOT NULL,
  image_url   TEXT,
  city        TEXT NOT NULL DEFAULT 'الرياض',
  maps_query  TEXT,
  is_active   BOOLEAN NOT NULL DEFAULT TRUE,
  sort_order  INTEGER NOT NULL DEFAULT 0,
  created_at  TIMESTAMPTZ NOT NULL DEFAULT NOW()
);

-- ─── Object ACL (replaces GCS custom metadata) ───────────────────────────
CREATE TABLE IF NOT EXISTS object_acl (
  key    TEXT PRIMARY KEY,
  policy JSONB NOT NULL
);

COMMIT;

-- ─── Seed minimal RBAC data ──────────────────────────────────────────────
INSERT INTO permissions (name, name_ar) VALUES
  ('view',   'عرض'),
  ('create', 'إنشاء'),
  ('edit',   'تعديل'),
  ('delete', 'حذف'),
  ('admin',  'إدارة')
ON CONFLICT (name) DO NOTHING;

INSERT INTO roles (name, name_ar, description, is_system) VALUES
  ('super_admin', 'مسؤول أعلى',  'وصول كامل لكل الصفحات والإجراءات', TRUE),
  ('admin',       'مسؤول',       'إدارة المحتوى والصلاحيات',          TRUE),
  ('editor',      'محرر',         'إنشاء وتعديل المحتوى',              TRUE),
  ('viewer',      'مشاهد',        'عرض فقط',                            TRUE)
ON CONFLICT (name) DO NOTHING;

INSERT INTO pages (slug, name_ar, icon, sort_order) VALUES
  ('dashboard',           'لوحة المعلومات',              'home',   1),
  ('ai-year',             'عام الذكاء الاصطناعي',         'cpu',    2),
  ('week-start',          'بداية الأسبوع',                'sun',    3),
  ('international-days',  'الأيام العالمية',              'globe',  4),
  ('designs',             'مصمم القوالب',                 'image',  5),
  ('weekend-places',      'وجهات نهاية الأسبوع',           'map',    6),
  ('daily-reports',       'التقارير اليومية',             'file',   7),
  ('admin',               'الإدارة',                      'shield', 99)
ON CONFLICT (slug) DO NOTHING;
