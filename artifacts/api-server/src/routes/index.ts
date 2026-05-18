import { Router, type IRouter } from "express";
import healthRouter from "./health";
import dailyReportRouter from "./daily-report";
import weekStartRouter from "./week-start";
import intlDaysRouter from "./international-days";
import aiYearRouter from "./ai-year";
import storageRouter from "./storage";
import authRouter from "./auth";
import { authenticate, requireAuth, requirePermission } from "../middleware/auth";

const router: IRouter = Router();

// ─── Public routes (no auth required) ──────────────────────────────────────
router.use(authRouter);
router.use(healthRouter);

// ─── Auth gate: populates req.user and enforces login ──────────────────────
// authenticate() sets req.user from JWT (Bearer header or access_token cookie).
// requireAuth() returns HTTP 401 if req.user is not set.
router.use(authenticate);
router.use(requireAuth);

// ─── Page-level permission enforcement ─────────────────────────────────────
// requirePermission checks that the authenticated user has at least "view"
// access to the relevant page. super_admin / admin bypass all page checks.
// These middleware run before the matching feature router handles the request.
router.use(["/daily-report", "/report"], requirePermission("dashboard", "view"));
router.use("/week-start", requirePermission("week_start", "view"));
router.use("/intl-days", requirePermission("international_days", "view"));
router.use("/ai-year", requirePermission("ai_year", "view"));

// ─── Feature routers ────────────────────────────────────────────────────────
router.use(dailyReportRouter);
router.use(weekStartRouter);
router.use(intlDaysRouter);
router.use(aiYearRouter);
router.use(storageRouter);

export default router;
