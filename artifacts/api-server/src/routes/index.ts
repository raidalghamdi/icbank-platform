import { Router, type IRouter } from "express";
import healthRouter from "./health";
import dailyReportRouter from "./daily-report";
import weekStartRouter from "./week-start";

const router: IRouter = Router();

router.use(healthRouter);
router.use(dailyReportRouter);
router.use(weekStartRouter);

export default router;
