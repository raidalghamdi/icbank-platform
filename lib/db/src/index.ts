import { drizzle } from "drizzle-orm/node-postgres";
import pg from "pg";
import * as schema from "./schema";

const { Pool } = pg;

if (!process.env.DATABASE_URL) {
  throw new Error(
    "DATABASE_URL must be set. Did you forget to provision a database?",
  );
}

// Supabase pooler requires SSL but provides a self-signed cert chain.
// rejectUnauthorized=false allows connection while still encrypting traffic.
export const pool = new Pool({
  connectionString: process.env.DATABASE_URL,
  ssl: { rejectUnauthorized: false },
});

pool.on("error", (err) => {
  // Surface idle-client errors instead of crashing the process
  console.error("[db pool] unexpected error on idle client", err);
});
export const db = drizzle(pool, { schema });

export * from "./schema";
