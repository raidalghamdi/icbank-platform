import { Router, type IRouter } from "express";
import healthRouter from "./health";
import dailyReportRouter from "./daily-report";
import weekStartRouter from "./week-start";
import intlDaysRouter from "./international-days";
import aiYearRouter from "./ai-year";
import storageRouter from "./storage";
import authRouter from "./auth";
import authSsoRouter from "./auth-sso";
import adminRouter from "./admin";
import { authenticate, requireAuth, requirePageAccess } from "../middleware/auth";

const router: IRouter = Router();

// ─── Public routes (no auth required) ──────────────────────────────────────
router.use(authRouter);
router.use(authSsoRouter);
router.use(healthRouter);

// ─── Auth gate: populates req.user and enforces login ──────────────────────
// authenticate() sets req.user from JWT (Bearer header or access_token cookie).
// requireAuth() returns HTTP 401 if req.user is not set.
router.use(authenticate);
router.use(requireAuth);

// ─── Page-level permission enforcement (method-aware) ──────────────────────
// requirePageAccess maps HTTP method → required permission automatically:
//   GET/HEAD → "view" | POST → "create" | PUT/PATCH → "edit" | DELETE → "delete"
// super_admin and admin bypass all checks; Viewer cannot mutate.
// Routes mapped to RBAC-controlled pages (spec: 8 pages)
router.use(["/daily-report", "/report"], requirePageAccess("dashboard"));
// week-start maps to the "weekend" RBAC page (same content cycle per spec)
router.use("/week-start", requirePageAccess("weekend"));
router.use("/intl-days", requirePageAccess("international_days"));
router.use("/ai-year", requirePageAccess("ai_year"));
// Storage exclusively serves ai-year/2026/* assets
router.use("/storage", requirePageAccess("ai_year"));

// ─── Feature routers ────────────────────────────────────────────────────────
router.use(dailyReportRouter);
router.use(weekStartRouter);
router.use(intlDaysRouter);
router.use(aiYearRouter);
router.use(storageRouter);
router.use(adminRouter);

export default router;
