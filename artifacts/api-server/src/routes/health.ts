import { Router, type IRouter } from "express";
import { HealthCheckResponse } from "@workspace/api-zod";

const router: IRouter = Router();

router.get("/healthz", (_req, res) => {
  const data = HealthCheckResponse.parse({ status: "ok" });
  res.json(data);
});

// SEC-03 / DATA-03 / C-3: /debug/db and /debug/env were removed outright.
// They were mounted before the auth gate (routes/index.ts) and returned, to any
// unauthenticated caller, raw system_settings rows (including Azure AD secrets)
// and the database host/port parsed from DATABASE_URL. No functionality in this
// app depends on them (confirmed via repo-wide grep for "debug/db"/"debug/env").

export default router;
