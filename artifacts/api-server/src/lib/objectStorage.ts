/**
 * Object storage layer backed by Supabase Storage.
 *
 * Mirrors the original Replit/GCS-backed API surface used by the routes so the
 * call-sites in routes/ai-year.ts, routes/designs.ts, routes/weekend-places.ts,
 * and routes/storage.ts keep working unchanged.
 *
 * Logical objectPath convention (unchanged from the GCS implementation):
 *   /objects/<relPath>
 * where <relPath> is e.g. ai-year/2026/3/12/uuid.jpg, designs/logos/uuid.png,
 * weekend/places/uuid.jpg, etc.
 *
 * Required env:
 *   SUPABASE_URL                  e.g. https://xyzcompany.supabase.co
 *   SUPABASE_SERVICE_KEY          service-role key (server-side only)
 *   SUPABASE_STORAGE_BUCKET       bucket name (default: "icbank")
 *
 * Backwards-compatible env (optional, used as logical prefixes):
 *   PRIVATE_OBJECT_DIR            kept for path normalisation; default ""
 *   PUBLIC_OBJECT_SEARCH_PATHS    comma-separated logical search paths
 */
import { createClient, SupabaseClient } from "@supabase/supabase-js";
import { randomUUID } from "crypto";
import { Readable } from "stream";
import {
  ObjectAclPolicy,
  ObjectPermission,
  canAccessObject,
  getObjectAclPolicy,
  setObjectAclPolicy,
} from "./objectAcl";

const SUPABASE_URL = process.env.SUPABASE_URL ?? "";
const SUPABASE_SERVICE_KEY = process.env.SUPABASE_SERVICE_KEY ?? "";
export const SUPABASE_STORAGE_BUCKET =
  process.env.SUPABASE_STORAGE_BUCKET ?? "icbank";

let _client: SupabaseClient | null = null;

export function getSupabase(): SupabaseClient {
  if (_client) return _client;
  if (!SUPABASE_URL || !SUPABASE_SERVICE_KEY) {
    throw new Error(
      "SUPABASE_URL and SUPABASE_SERVICE_KEY env vars must be set for object storage"
    );
  }
  _client = createClient(SUPABASE_URL, SUPABASE_SERVICE_KEY, {
    auth: { persistSession: false, autoRefreshToken: false },
  });
  return _client;
}

// ─── Compatibility wrapper so routes that expect a "File" handle keep working ──
//
// The original GCS code passed around `File` objects between methods (e.g.
// getObjectEntityFile returns a File, then downloadObject(file) streams it).
// We replicate that by returning a thin StorageObject handle holding the
// bucket-relative key and exposing the same .name property GCS used.

export interface StorageObject {
  /** Bucket-relative object key (no leading slash). */
  key: string;
  /** Alias mirroring GCS File.name. */
  name: string;
}

// ─── Errors ──────────────────────────────────────────────────────────────────

export class ObjectNotFoundError extends Error {
  constructor() {
    super("Object not found");
    this.name = "ObjectNotFoundError";
    Object.setPrototypeOf(this, ObjectNotFoundError.prototype);
  }
}

// ─── Helpers ─────────────────────────────────────────────────────────────────

function stripLeadingSlash(p: string): string {
  return p.startsWith("/") ? p.slice(1) : p;
}

function ensureTrailingSlash(p: string): string {
  if (!p) return "";
  return p.endsWith("/") ? p : `${p}/`;
}

/**
 * Convert a logical objectPath "/objects/<rel>" into the bucket-relative key.
 *
 * PRIVATE_OBJECT_DIR is honoured as a logical prefix inside the bucket, e.g.
 * if PRIVATE_OBJECT_DIR="prod" then "/objects/foo/bar.png" maps to
 * "prod/foo/bar.png" in the bucket. When unset, "/objects/foo/bar.png" maps
 * straight to "foo/bar.png".
 */
function objectPathToKey(objectPath: string): string {
  if (!objectPath.startsWith("/objects/")) {
    throw new ObjectNotFoundError();
  }
  const rel = objectPath.slice("/objects/".length);
  const privateDir = stripLeadingSlash(process.env.PRIVATE_OBJECT_DIR ?? "");
  if (privateDir) {
    return `${ensureTrailingSlash(privateDir)}${rel}`;
  }
  return rel;
}

/**
 * Build a bucket-relative key from a relPath, applying PRIVATE_OBJECT_DIR.
 */
function relPathToKey(relPath: string): string {
  const privateDir = stripLeadingSlash(process.env.PRIVATE_OBJECT_DIR ?? "");
  if (privateDir) {
    return `${ensureTrailingSlash(privateDir)}${relPath}`;
  }
  return relPath;
}

// ─── Service ────────────────────────────────────────────────────────────────

export class ObjectStorageService {
  constructor() {}

  /** Logical search paths inside the bucket where public-readable objects live. */
  getPublicObjectSearchPaths(): Array<string> {
    const pathsStr = process.env.PUBLIC_OBJECT_SEARCH_PATHS || "";
    const paths = Array.from(
      new Set(
        pathsStr
          .split(",")
          .map((path) => path.trim())
          .filter((path) => path.length > 0)
      )
    );
    if (paths.length === 0) {
      throw new Error(
        "PUBLIC_OBJECT_SEARCH_PATHS not set. Set this env var to a comma-separated " +
          "list of logical paths inside the Supabase Storage bucket."
      );
    }
    return paths;
  }

  /** Logical directory (inside the bucket) where private uploads live. */
  getPrivateObjectDir(): string {
    // Backwards-compatible: original code threw when unset. Here we allow empty
    // (= bucket root) but keep an explicit getter for callers that depend on it.
    return process.env.PRIVATE_OBJECT_DIR ?? "";
  }

  /**
   * Search public search paths for a file. Returns a StorageObject handle if found.
   */
  async searchPublicObject(filePath: string): Promise<StorageObject | null> {
    const supabase = getSupabase();
    const rel = stripLeadingSlash(filePath);
    for (const searchPath of this.getPublicObjectSearchPaths()) {
      const candidateKey = `${ensureTrailingSlash(stripLeadingSlash(searchPath))}${rel}`;
      // HEAD-style existence check via list on the parent folder.
      const lastSlash = candidateKey.lastIndexOf("/");
      const folder = lastSlash >= 0 ? candidateKey.slice(0, lastSlash) : "";
      const base = lastSlash >= 0 ? candidateKey.slice(lastSlash + 1) : candidateKey;
      const { data, error } = await supabase.storage
        .from(SUPABASE_STORAGE_BUCKET)
        .list(folder, { search: base, limit: 1 });
      if (!error && data && data.some((f) => f.name === base)) {
        return { key: candidateKey, name: candidateKey };
      }
    }
    return null;
  }

  /**
   * Stream an object back to the caller as a Fetch Response.
   * Headers include Content-Type, Content-Length (if known) and a Cache-Control
   * derived from the ACL policy attached to the object's metadata.
   */
  async downloadObject(
    object: StorageObject,
    cacheTtlSec: number = 3600
  ): Promise<Response> {
    const supabase = getSupabase();
    const { data, error } = await supabase.storage
      .from(SUPABASE_STORAGE_BUCKET)
      .download(object.key);

    if (error || !data) {
      throw new ObjectNotFoundError();
    }

    const aclPolicy = await getObjectAclPolicy(object);
    const isPublic = aclPolicy?.visibility === "public";

    const contentType = data.type || "application/octet-stream";
    const arrayBuffer = await data.arrayBuffer();
    const buffer = Buffer.from(arrayBuffer);

    const headers: Record<string, string> = {
      "Content-Type": contentType,
      "Content-Length": String(buffer.byteLength),
      "Cache-Control": `${isPublic ? "public" : "private"}, max-age=${cacheTtlSec}`,
    };

    return new Response(buffer, { headers });
  }

  /** Generic uploads/<uuid> presigned PUT URL. */
  async getObjectEntityUploadURL(): Promise<string> {
    const objectId = randomUUID();
    const relPath = `uploads/${objectId}`;
    const key = relPathToKey(relPath);
    return signedUploadUrl(key, 900);
  }

  /**
   * AI Year media: /objects/ai-year/2026/{month}/{activationId}/{uuid}.{ext}
   */
  async getAiYearUploadURL(opts: {
    month: number;
    activationId: number;
    fileName: string;
    contentType?: string;
  }): Promise<{ uploadURL: string; objectPath: string }> {
    const ext =
      opts.fileName.includes(".") ? opts.fileName.split(".").pop() : "bin";
    const uuid = randomUUID();
    const relPath = `ai-year/2026/${opts.month}/${opts.activationId}/${uuid}.${ext}`;
    const key = relPathToKey(relPath);
    const uploadURL = await signedUploadUrl(key, 900);
    return { uploadURL, objectPath: `/objects/${relPath}` };
  }

  // ─── Designs storage helpers ──────────────────────────────────────────────

  async getDesignsUploadURL(opts: {
    folder: "logos" | "fonts" | "backgrounds" | "final";
    fileName: string;
    contentType?: string;
  }): Promise<{ uploadURL: string; objectPath: string }> {
    const ext = opts.fileName.includes(".")
      ? opts.fileName.split(".").pop()!
      : "bin";
    const uuid = randomUUID();
    const relPath = `designs/${opts.folder}/${uuid}.${ext}`;
    const key = relPathToKey(relPath);
    const uploadURL = await signedUploadUrl(key, 900);
    return { uploadURL, objectPath: `/objects/${relPath}` };
  }

  /**
   * Save a generated image Buffer directly to storage (used by Gemini image gen).
   * Returns the logical objectPath: /objects/designs/backgrounds/uuid.ext
   */
  async saveGeneratedBackground(
    buffer: Buffer,
    contentType: string
  ): Promise<string> {
    const supabase = getSupabase();
    const ext = contentType.includes("png") ? "png" : "jpg";
    const uuid = randomUUID();
    const relPath = `designs/backgrounds/${uuid}.${ext}`;
    const key = relPathToKey(relPath);
    const { error } = await supabase.storage
      .from(SUPABASE_STORAGE_BUCKET)
      .upload(key, buffer, { contentType, upsert: false });
    if (error) {
      throw new Error(`Supabase upload failed: ${error.message}`);
    }
    return `/objects/${relPath}`;
  }

  /**
   * Save a fully composed PNG (from the server composer) to the designs/renders
   * folder and return its objectPath.
   */
  async saveComposedDesign(buffer: Buffer): Promise<string> {
    const supabase = getSupabase();
    const uuid = randomUUID();
    const relPath = `designs/renders/${uuid}.png`;
    const key = relPathToKey(relPath);
    const { error } = await supabase.storage
      .from(SUPABASE_STORAGE_BUCKET)
      .upload(key, buffer, { contentType: "image/png", upsert: false });
    if (error) {
      throw new Error(`Supabase upload failed: ${error.message}`);
    }
    return `/objects/${relPath}`;
  }

  /**
   * Upload a logo buffer directly to designs/logos/ and return its objectPath.
   * Used by the GAC logo seed endpoint to push the official brand-manual logos
   * straight from the bundled base64 assets into Supabase Storage.
   */
  async saveLogoBuffer(
    buffer: Buffer,
    contentType: string = "image/png"
  ): Promise<string> {
    const supabase = getSupabase();
    const ext = contentType.includes("png") ? "png" : contentType.includes("jpeg") ? "jpg" : "png";
    const uuid = randomUUID();
    const relPath = `designs/logos/${uuid}.${ext}`;
    const key = relPathToKey(relPath);
    const { error } = await supabase.storage
      .from(SUPABASE_STORAGE_BUCKET)
      .upload(key, buffer, { contentType, upsert: false });
    if (error) {
      throw new Error(`Supabase upload failed: ${error.message}`);
    }
    return `/objects/${relPath}`;
  }

  /**
   * Upload a GAC publication PDF directly to gac/publications/ and return its
   * objectPath. Mirrors saveLogoBuffer but for PDFs in the GAC library.
   */
  async saveGacPublication(
    buffer: Buffer,
    contentType: string = "application/pdf"
  ): Promise<string> {
    const supabase = getSupabase();
    const ext = contentType.includes("pdf") ? "pdf" : "bin";
    const uuid = randomUUID();
    const relPath = `gac/publications/${uuid}.${ext}`;
    const key = relPathToKey(relPath);
    const { error } = await supabase.storage
      .from(SUPABASE_STORAGE_BUCKET)
      .upload(key, buffer, { contentType, upsert: false });
    if (error) {
      throw new Error(`Supabase upload failed: ${error.message}`);
    }
    return `/objects/${relPath}`;
  }

  /**
   * Fetch raw bytes for an objectPath (e.g. /objects/designs/...) — useful so
   * the server composer can read both backgrounds and logos by URL.
   */
  async downloadByObjectPath(objectPath: string): Promise<Buffer | null> {
    const supabase = getSupabase();
    const rel = objectPath.replace(/^\/?objects\//, "");
    const key = relPathToKey(rel);
    const { data, error } = await supabase.storage
      .from(SUPABASE_STORAGE_BUCKET)
      .download(key);
    if (error || !data) return null;
    const ab = await data.arrayBuffer();
    return Buffer.from(ab);
  }

  // ─── Weekend Places storage helpers ──────────────────────────────────────

  async getWeekendPlacesUploadURL(opts: {
    fileName: string;
    contentType?: string;
  }): Promise<{ uploadURL: string; objectPath: string }> {
    const ext = opts.fileName.includes(".")
      ? opts.fileName.split(".").pop()!
      : "jpg";
    const uuid = randomUUID();
    const relPath = `weekend/places/${uuid}.${ext}`;
    const key = relPathToKey(relPath);
    const uploadURL = await signedUploadUrl(key, 900);
    return { uploadURL, objectPath: `/objects/${relPath}` };
  }

  async deleteWeekendPlaceObject(objectPath: string): Promise<void> {
    if (!objectPath.startsWith("/objects/weekend/")) {
      throw new Error(
        "deleteWeekendPlaceObject: path must start with /objects/weekend/"
      );
    }
    const key = objectPathToKey(objectPath);
    const supabase = getSupabase();
    await supabase.storage.from(SUPABASE_STORAGE_BUCKET).remove([key]);
  }

  async deleteDesignObject(objectPath: string): Promise<void> {
    if (!objectPath.startsWith("/objects/designs/")) {
      throw new Error(
        "deleteDesignObject: path must start with /objects/designs/"
      );
    }
    const key = objectPathToKey(objectPath);
    const supabase = getSupabase();
    await supabase.storage.from(SUPABASE_STORAGE_BUCKET).remove([key]);
  }

  /**
   * Resolve a logical /objects/... path to a StorageObject handle, or throw
   * ObjectNotFoundError if the underlying file is missing.
   */
  async getObjectEntityFile(objectPath: string): Promise<StorageObject> {
    if (!objectPath.startsWith("/objects/")) {
      throw new ObjectNotFoundError();
    }
    const key = objectPathToKey(objectPath);
    const supabase = getSupabase();
    const lastSlash = key.lastIndexOf("/");
    const folder = lastSlash >= 0 ? key.slice(0, lastSlash) : "";
    const base = lastSlash >= 0 ? key.slice(lastSlash + 1) : key;
    const { data, error } = await supabase.storage
      .from(SUPABASE_STORAGE_BUCKET)
      .list(folder, { search: base, limit: 1 });
    if (error || !data || !data.some((f) => f.name === base)) {
      throw new ObjectNotFoundError();
    }
    return { key, name: key };
  }

  /**
   * Convert legacy GCS URLs ("https://storage.googleapis.com/...") and Supabase
   * public/signed URLs into the canonical /objects/<rel> form.
   */
  normalizeObjectEntityPath(rawPath: string): string {
    // Already canonical
    if (rawPath.startsWith("/objects/")) return rawPath;

    // Legacy GCS URL — kept for in-flight clients that still hand us one
    if (rawPath.startsWith("https://storage.googleapis.com/")) {
      const url = new URL(rawPath);
      const rawObjectPath = url.pathname;
      const privateDir = ensureTrailingSlash(
        stripLeadingSlash(this.getPrivateObjectDir())
      );
      const trimmed = stripLeadingSlash(rawObjectPath);
      if (privateDir && trimmed.startsWith(privateDir)) {
        return `/objects/${trimmed.slice(privateDir.length)}`;
      }
      // Drop the bucket segment
      const parts = trimmed.split("/");
      return `/objects/${parts.slice(1).join("/")}`;
    }

    // Supabase Storage URL — public or signed
    if (SUPABASE_URL && rawPath.startsWith(SUPABASE_URL)) {
      const url = new URL(rawPath);
      // Path shape: /storage/v1/object/(public|sign)/<bucket>/<key>...
      const segs = url.pathname.split("/").filter(Boolean);
      const bucketIdx = segs.findIndex((s) => s === SUPABASE_STORAGE_BUCKET);
      if (bucketIdx >= 0 && bucketIdx + 1 < segs.length) {
        const key = segs.slice(bucketIdx + 1).join("/");
        const privateDir = ensureTrailingSlash(
          stripLeadingSlash(this.getPrivateObjectDir())
        );
        const rel = privateDir && key.startsWith(privateDir)
          ? key.slice(privateDir.length)
          : key;
        return `/objects/${rel}`;
      }
    }

    return rawPath;
  }

  async trySetObjectEntityAclPolicy(
    rawPath: string,
    aclPolicy: ObjectAclPolicy
  ): Promise<string> {
    const normalizedPath = this.normalizeObjectEntityPath(rawPath);
    if (!normalizedPath.startsWith("/")) {
      return normalizedPath;
    }
    const objectFile = await this.getObjectEntityFile(normalizedPath);
    await setObjectAclPolicy(objectFile, aclPolicy);
    return normalizedPath;
  }

  /**
   * Stream a stored object as a Node Readable. Used by archiver in ai-year ZIP
   * download. Returns the stream plus inferred metadata (contentType, size).
   */
  async createReadStream(
    object: StorageObject
  ): Promise<{ stream: Readable; contentType: string; size: number | null }> {
    const supabase = getSupabase();
    const { data, error } = await supabase.storage
      .from(SUPABASE_STORAGE_BUCKET)
      .download(object.key);
    if (error || !data) {
      throw new ObjectNotFoundError();
    }
    const contentType = data.type || "application/octet-stream";
    const arrayBuffer = await data.arrayBuffer();
    const buf = Buffer.from(arrayBuffer);
    return {
      stream: Readable.from(buf),
      contentType,
      size: buf.byteLength,
    };
  }

  async canAccessObjectEntity({
    userId,
    objectFile,
    requestedPermission,
  }: {
    userId?: string;
    objectFile: StorageObject;
    requestedPermission?: ObjectPermission;
  }): Promise<boolean> {
    return canAccessObject({
      userId,
      objectFile,
      requestedPermission: requestedPermission ?? ObjectPermission.READ,
    });
  }
}

// ─── Signed-URL helpers ─────────────────────────────────────────────────────

/**
 * Generate a presigned PUT URL the browser can upload to directly.
 * Supabase returns { signedUrl, token, path } — we hand back the signedUrl.
 */
async function signedUploadUrl(key: string, _ttlSec: number): Promise<string> {
  const supabase = getSupabase();
  const { data, error } = await supabase.storage
    .from(SUPABASE_STORAGE_BUCKET)
    .createSignedUploadUrl(key);
  if (error || !data) {
    throw new Error(`Failed to create signed upload URL: ${error?.message}`);
  }
  return data.signedUrl;
}
