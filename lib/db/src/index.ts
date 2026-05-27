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
 * Supabase Direct Connection (db.<ref>.supabase.co:5432) is IPv6-only.
 * Some platforms (Railway, Vercel, Render) don't have IPv6 egress, which
 * causes ENETUNREACH on every query. We auto-rewrite a direct connection
 * URL to the Shared Pooler (transaction mode, port 6543) which has both
 * IPv4 and IPv6 addresses.
 *
 * Direct:  postgresql://postgres:[pw]@db.<ref>.supabase.co:5432/postgres
 * Pooler:  postgresql://postgres.<ref>:[pw]@aws-0-<region>.pooler.supabase.com:6543/postgres
 *
 * SUPABASE_POOLER_REGION env var (e.g. "eu-west-2") controls the region.
 * Defaults to "eu-west-2" matching this project.
 */
function rewriteToPooler(rawUrl: string): string {
  try {
    const u = new URL(rawUrl);
    const directMatch = u.hostname.match(/^db\.([a-z0-9]+)\.supabase\.(co|com)$/i);
    if (!directMatch) return rawUrl; // already a pooler URL or custom host
    const ref = directMatch[1];
    const region = process.env.SUPABASE_POOLER_REGION || "eu-west-2";
    const newHost = `aws-0-${region}.pooler.supabase.com`;
    const newUser = `postgres.${ref}`;
    const password = u.password;
    const newUrl = `postgresql://${newUser}:${password}@${newHost}:6543${u.pathname || "/postgres"}`;
    console.log(`[db] rewrote direct URL → pooler (host=${newHost})`);
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
