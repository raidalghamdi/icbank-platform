import { drizzle } from "drizzle-orm/node-postgres";
import pg from "pg";
import * as schema from "./schema";

const { Pool } = pg;

if (!process.env.DATABASE_URL) {
  throw new Error(
    "DATABASE_URL must be set. Did you forget to provision a database?",
  );
}

/**
 * Supabase Direct Connection (db.<ref>.supabase.co) is IPv6-only.
 * Some platforms (Railway, Vercel, Render) don't have IPv6 egress, which
 * causes ENETUNREACH on every query.
 *
 * This auto-rewrites the URL to the Shared Pooler (IPv4-compatible):
 *   Direct:  postgresql://postgres:[pw]@db.<ref>.supabase.co:6543/postgres
 *   Pooler:  postgresql://postgres.<ref>:[pw]@aws-1-eu-west-2.pooler.supabase.com:6543/postgres
 *
 * Region is hard-coded to aws-1-eu-west-2 for project ejcxwicwduyvqdxdkbyo.
 * Override with SUPABASE_POOLER_HOST env var if needed.
 */
function rewriteToPooler(rawUrl: string): string {
  try {
    const u = new URL(rawUrl);
    const directMatch = u.hostname.match(/^db\.([a-z0-9]+)\.supabase\.(co|com)$/i);
    if (!directMatch) return rawUrl; // already a pooler URL or custom host
    const ref = directMatch[1];
    const poolerHost = process.env.SUPABASE_POOLER_HOST || "aws-1-eu-west-2.pooler.supabase.com";
    const newUser = `postgres.${ref}`;
    const password = u.password;
    const newUrl = `postgresql://${newUser}:${password}@${poolerHost}:6543${u.pathname || "/postgres"}`;
    console.log(`[db] rewrote direct URL to pooler (host=${poolerHost}, user=${newUser})`);
    return newUrl;
  } catch (err) {
    console.error("[db] failed to parse DATABASE_URL, using as-is", err);
    return rawUrl;
  }
}

const connectionString = rewriteToPooler(process.env.DATABASE_URL);

// Supabase pooler requires SSL but provides a self-signed cert chain.
// rejectUnauthorized=false allows connection while still encrypting traffic.
export const pool = new Pool({
  connectionString,
  ssl: { rejectUnauthorized: false },
});

pool.on("error", (err) => {
  // Surface idle-client errors instead of crashing the process
  console.error("[db pool] unexpected error on idle client", err);
});
export const db = drizzle(pool, { schema });

export * from "./schema";
