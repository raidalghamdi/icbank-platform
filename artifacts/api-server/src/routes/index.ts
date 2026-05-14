import { Router, type IRouter } from "express";
import healthRouter from "./health";
import dailyReportRouter from "./daily-report";

const router: IRouter = Router();

router.use(healthRouter);
router.use(dailyReportRouter);

export default router;
