import { Router, type IRouter } from "express";
import { HealthCheckResponse } from "@workspace/api-zod";
import { pool } from "@workspace/db";

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

export default router;
