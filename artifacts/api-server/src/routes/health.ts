import { Router, type IRouter } from "express";
import { HealthCheckResponse } from "@workspace/api-zod";
import { pool } from "@workspace/db";
import pg from "pg";

const { Pool } = pg;

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

// Probe each pooler region to find the correct one
router.get("/debug/probe", async (_req, res) => {
  const dbUrl = process.env.DATABASE_URL || "";
  let password = "";
  try {
    const u = new URL(dbUrl);
    password = u.password;
  } catch {
    return res.status(500).json({ error: "could not parse DATABASE_URL" });
  }

  const ref = "ejcxwicwduyvqdxdkbyo";
  const regions = ["eu-west-1", "eu-west-2", "eu-west-3", "eu-central-1", "eu-north-1", "eu-central-2", "eu-south-1"];
  const usernames = ["postgres." + ref, "postgres"];

  const results: any[] = [];

  for (const region of regions) {
    for (const username of usernames) {
      const host = "aws-0-" + region + ".pooler.supabase.com";
      const url = "postgresql://" + username + ":" + password + "@" + host + ":6543/postgres";
      const p = new Pool({
        connectionString: url,
        ssl: { rejectUnauthorized: false },
        max: 1,
        connectionTimeoutMillis: 6000,
      });
      try {
        const r = await p.query("select 1 as ok");
        await p.end();
        results.push({ region, username, ok: true, result: r.rows[0] });
        return res.json({ success: true, working: { region, username, host }, all: results });
      } catch (e: any) {
        results.push({ region, username, ok: false, message: e?.message?.slice(0, 100), code: e?.code });
        try { await p.end(); } catch {}
      }
    }
  }
  res.status(500).json({ success: false, results });
});

export default router;
