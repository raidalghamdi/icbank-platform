import { Router, type IRouter } from "express";
import { HealthCheckResponse } from "@workspace/api-zod";
import { pool } from "@workspace/db";
import { Pool } from "pg";

const router: IRouter = Router();

router.get("/healthz", (_req, res) => {
  const data = HealthCheckResponse.parse({ status: "ok" });
  res.json(data);
});

router.get("/debug/db", async (_req, res) => {
  try {
    const result = await pool.query(
      'select "id", "key", "value", "updated_at" from "system_settings" limit 1',
    );
    res.json({ ok: true, rowCount: result.rowCount, rows: result.rows });
  } catch (err: any) {
    res.status(500).json({
      ok: false,
      message: err?.message,
      code: err?.code,
      detail: err?.detail,
      hint: err?.hint,
    });
  }
});

router.get("/debug/env", (_req, res) => {
  const dbUrl = process.env.DATABASE_URL || "";
  let host = "";
  let port = "";
  try {
    const u = new URL(dbUrl);
    host = u.hostname;
    port = u.port;
  } catch {}
  res.json({
    hasDbUrl: !!dbUrl,
    host,
    port,
    nodeEnv: process.env.NODE_ENV,
  });
});

// Try connecting via Pooler URL derived from current DATABASE_URL
router.get("/debug/try-pooler", async (_req, res) => {
  const dbUrl = process.env.DATABASE_URL || "";
  try {
    const u = new URL(dbUrl);
    // Extract password from current URL, build pooler URL
    const password = decodeURIComponent(u.password);
    // Project ref = "ejcxwicwduyvqdxdkbyo" — username for pooler is `postgres.<ref>`
    const ref = "ejcxwicwduyvqdxdkbyo";
    const poolerHost = "aws-0-eu-west-2.pooler.supabase.com";
    const poolerUrl = `postgresql://postgres.${ref}:${encodeURIComponent(password)}@${poolerHost}:6543/postgres`;
    const tmpPool = new Pool({
      connectionString: poolerUrl,
      ssl: { rejectUnauthorized: false },
      max: 1,
      connectionTimeoutMillis: 8000,
    });
    const r = await tmpPool.query('select 1 as ok');
    await tmpPool.end();
    res.json({
      ok: true,
      poolerHostUsed: poolerHost,
      result: r.rows[0],
      message: "Pooler connection works. Update DATABASE_URL to use this host.",
    });
  } catch (err: any) {
    res.status(500).json({
      ok: false,
      message: err?.message,
      code: err?.code,
    });
  }
});

export default router;
