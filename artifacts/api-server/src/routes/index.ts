import { Router, type IRouter } from "express";
import healthRouter from "./health";
import dailyReportRouter from "./daily-report";
import weekStartRouter from "./week-start";
import intlDaysRouter from "./international-days";
import aiYearRouter from "./ai-year";
import storageRouter from "./storage";
import authRouter from "./auth";
import { authenticate, requireAuth } from "../middleware/auth";

const router: IRouter = Router();

// Public routes — no auth required
router.use(authRouter);
router.use(healthRouter);

// All routes below this line require a valid authenticated session.
// `authenticate` populates req.user from JWT (Bearer header or access_token cookie).
// `requireAuth` returns 401 if req.user is not set.
router.use(authenticate);
router.use(requireAuth);

router.use(dailyReportRouter);
router.use(weekStartRouter);
router.use(intlDaysRouter);
router.use(aiYearRouter);
router.use(storageRouter);

export default router;
