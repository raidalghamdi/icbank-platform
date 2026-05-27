import { Router, type IRouter } from "express";
import { HealthCheckResponse } from "@workspace/api-zod";
import { pool } from "@workspace/db";

const router: IRouter = Router();

router.get("/healthz", (_req, res) => {
  const data = HealthCheckResponse.parse({ status: "ok" });
  res.json(data);
});

// TEMP debug endpoint to surface real DB error messages
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
      where: err?.where,
      stack: err?.stack?.split("\n").slice(0, 6),
    });
  }
});

router.get("/debug/env", (_req, res) => {
  const dbUrl = process.env.DATABASE_URL || "";
  res.json({
    hasDbUrl: !!dbUrl,
    dbUrlPrefix: dbUrl.slice(0, 40),
    dbUrlContainsSslmode: dbUrl.includes("sslmode"),
    nodeEnv: process.env.NODE_ENV,
    skipSeed: process.env.SKIP_SEED,
  });
});

export default router;
