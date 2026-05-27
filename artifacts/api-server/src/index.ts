import app from "./app";
import { logger } from "./lib/logger";
import { runSeedIfNeeded } from "./lib/seed";

// Catch any unhandled errors so the process never exits silently on Railway
process.on("unhandledRejection", (reason) => {
  logger.error({ reason }, "Unhandled promise rejection");
});
process.on("uncaughtException", (err) => {
  logger.error({ err }, "Uncaught exception");
});

const rawPort = process.env["PORT"];

if (!rawPort) {
  throw new Error(
    "PORT environment variable is required but was not provided.",
  );
}

const port = Number(rawPort);

if (Number.isNaN(port) || port <= 0) {
  throw new Error(`Invalid PORT value: "${rawPort}"`);
}

app.listen(port, "0.0.0.0", (err) => {
  if (err) {
    logger.error({ err }, "Error listening on port");
    process.exit(1);
  }

  logger.info({ port, host: "0.0.0.0" }, "Server listening");

  // Skip seed if disabled (data is already seeded via Supabase MCP)
  if (process.env["SKIP_SEED"] === "true") {
    logger.info("SKIP_SEED=true, skipping seed");
    return;
  }

  runSeedIfNeeded().catch((err) => logger.error({ err }, "Seed error"));
});
