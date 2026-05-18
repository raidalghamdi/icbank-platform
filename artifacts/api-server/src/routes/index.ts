import { Router, type IRouter } from "express";
import healthRouter from "./health";
import dailyReportRouter from "./daily-report";
import weekStartRouter from "./week-start";
import intlDaysRouter from "./international-days";
import aiYearRouter from "./ai-year";
import storageRouter from "./storage";
import authRouter from "./auth";

const router: IRouter = Router();

router.use(authRouter);
router.use(healthRouter);
router.use(dailyReportRouter);
router.use(weekStartRouter);
router.use(intlDaysRouter);
router.use(aiYearRouter);
router.use(storageRouter);

export default router;
