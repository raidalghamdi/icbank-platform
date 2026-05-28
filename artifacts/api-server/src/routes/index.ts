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
import dashboardRouter from "./dashboard";
import designsRouter from "./designs";
import weekendPlacesRouter from "./weekend-places";
import weekendDraftsRouter from "./weekend-drafts";
import shorfahRouter from "./shorfah";
import shorfahCronRouter from "./shorfah-cron";
import gacRouter from "./gac";
import { authenticate, requireAuth, requirePageAccess } from "../middleware/auth";

const router: IRouter = Router();

// ─── Public routes (no auth required) ──────────────────────────────────────
router.use(authRouter);
router.use(authSsoRouter);
router.use(healthRouter);

// ─── N8N report ingestion routes (API-key auth, no JWT required) ────────────
// These routes use their own x-api-key header check (REPORT_API_KEY secret).
// They must be registered BEFORE the JWT requireAuth gate.
router.use(dailyReportRouter);

// ─── Shorfah cron routes (x-cron-secret auth, no JWT required) ────────────
router.use(shorfahCronRouter);

// ─── Storage (public read, path-allowlisted) ────────────────────────────────
// Must be before requireAuth so <img> tags and direct browser requests can load
// media objects without needing to send a Bearer token.
// Access is restricted inside the handler via ALLOWED_PREFIXES (path allowlist)
// and the optional INTERNAL_STORAGE_TOKEN secret for tighter production control.
router.use(storageRouter);

// ─── Auth gate: populates req.user and enforces login ──────────────────────
// authenticate() sets req.user from JWT (Bearer header or access_token cookie).
// requireAuth() returns HTTP 401 if req.user is not set.
router.use(authenticate);

// ─── GAC content (public reads + admin-protected reseed) ───────────────────
// Mounted after authenticate so requireAdmin can see req.user, but before
// requireAuth so the public GETs (publications, social-feed, news) work
// without a logged-in session.
router.use(gacRouter);

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

// ─── Feature routers ────────────────────────────────────────────────────────
router.use(dashboardRouter);
router.use(weekStartRouter);
router.use(intlDaysRouter);
router.use(aiYearRouter);
router.use(storageRouter);
router.use(adminRouter);
router.use(designsRouter);
router.use(weekendPlacesRouter);
router.use(weekendDraftsRouter);
router.use(shorfahRouter);

export default router;
