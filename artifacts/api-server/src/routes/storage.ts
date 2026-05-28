import { Router, type Request, type Response } from "express";
import { timingSafeEqual } from "crypto";
import { ObjectStorageService, ObjectNotFoundError } from "../lib/objectStorage";
import { logger } from "../lib/logger";

const router = Router();
const objectStorage = new ObjectStorageService();

if (!process.env.INTERNAL_STORAGE_TOKEN && process.env.NODE_ENV === "production") {
  logger.warn(
    "INTERNAL_STORAGE_TOKEN is not set — storage objects are publicly readable by anyone with a valid path. " +
    "Set this secret in Replit Secrets to restrict access."
  );
}

/**
 * GET /storage/objects/*path
 *
 * Streams media objects from Object Storage.
 *
 * Access control layers (defence-in-depth):
 *   1. Path prefix allowlist — only ai-year/2026/ is served.
 *   2. Optional bearer token — if INTERNAL_STORAGE_TOKEN env var is set,
 *      every request must carry  Authorization: Bearer <token>.
 *      Set this secret in Replit Secrets for tighter control on deployed envs.
 *      When unset the endpoint relies on the Replit proxy auth boundary alone.
 */
const ALLOWED_PREFIXES = ["ai-year/2026/", "designs/", "gac/", "shorfah/"];

router.get("/storage/objects/*path", async (req: Request, res: Response) => {
  // ── Optional bearer-token guard ────────────────────────────────────────────
  const storageToken = process.env.INTERNAL_STORAGE_TOKEN;
  if (storageToken) {
    const authHeader = req.headers["authorization"] ?? "";
    const bearer = authHeader.startsWith("Bearer ") ? authHeader.slice(7) : "";
    const valid =
      bearer.length === storageToken.length &&
      timingSafeEqual(Buffer.from(bearer), Buffer.from(storageToken));
    if (!valid) {
      res.status(401).json({ error: "Unauthorized" });
      return;
    }
  }

  // ── Path prefix allowlist ─────────────────────────────────────────────────
  // In Express 5 + router v2, named wildcards may come back as string or string[]
  const rawPath = (req.params as Record<string, string | string[]>)["path"];
  const tail = Array.isArray(rawPath) ? rawPath.join("/") : (rawPath ?? "");

  const allowed = ALLOWED_PREFIXES.some((p) => tail.startsWith(p));
  if (!allowed) {
    res.status(403).json({ error: "Forbidden: path not in allowed storage prefixes" });
    return;
  }

  const objectPath = `/objects/${tail}`;

  try {
    const file = await objectStorage.getObjectEntityFile(objectPath);
    const webResponse = await objectStorage.downloadObject(file);

    webResponse.headers.forEach((value, key) => {
      res.setHeader(key, value);
    });

    if (webResponse.body) {
      const { Readable } = await import("stream");
      const nodeStream = Readable.fromWeb(
        webResponse.body as import("stream/web").ReadableStream
      );
      nodeStream.pipe(res);
    } else {
      res.status(204).end();
    }
  } catch (err) {
    if (err instanceof ObjectNotFoundError) {
      res.status(404).json({ error: "Object not found" });
    } else {
      req.log.error({ err }, "Failed to serve storage object");
      res.status(500).json({ error: "Storage read error" });
    }
  }
});

export default router;
