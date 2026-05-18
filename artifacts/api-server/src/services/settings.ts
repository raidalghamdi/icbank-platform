import { db } from "@workspace/db";
import { systemSettingsTable } from "@workspace/db";

const DEFAULTS: Record<string, string> = {
  password_min_length: "8",
  password_require_uppercase: "true",
  password_require_symbols: "true",
  password_expiry_days: "90",
  session_duration_minutes: "60",
  auto_logout_minutes: "30",
};

let _cache: Record<string, string> | null = null;
let _cacheAt = 0;
const TTL_MS = 30_000;

export async function getSettings(): Promise<Record<string, string>> {
  const now = Date.now();
  if (_cache && now - _cacheAt < TTL_MS) return _cache;
  const rows = await db.select().from(systemSettingsTable);
  const s: Record<string, string> = { ...DEFAULTS };
  for (const row of rows) s[row.key] = row.value;
  _cache = s;
  _cacheAt = now;
  return s;
}

export function invalidateSettingsCache() {
  _cache = null;
}

export function validatePassword(password: string, s: Record<string, string>): string | null {
  const minLen = parseInt(s["password_min_length"] || "8");
  if (password.length < minLen) return `كلمة المرور يجب أن تكون ${minLen} أحرف على الأقل`;
  if (s["password_require_uppercase"] === "true" && !/[A-Z]/.test(password))
    return "كلمة المرور يجب أن تحتوي على حرف كبير واحد على الأقل";
  if (s["password_require_symbols"] === "true" && !/[!@#$%^&*()\-_+=\[\]{};':"\\|,.<>/?]/.test(password))
    return "كلمة المرور يجب أن تحتوي على رمز خاص واحد على الأقل";
  return null;
}
