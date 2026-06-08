/**
 * Unified AI provider wrapper.
 *
 * Strategy after Replit migration:
 *   • Gemini  → primary engine for all text/JSON/streaming generation
 *   • Perplexity → web-grounded search (international-days)
 *   • OpenAI / Anthropic → optional, gracefully fall back to Gemini when keys absent
 *
 * All historic call signatures (anthropic.messages.create / openai.chat.completions.create)
 * are mimicked by the adapters below so existing route code keeps working with minimal edits.
 */

import { GoogleGenAI } from "@google/genai";

// ─── Env keys ─────────────────────────────────────────────────────────────────
const GEMINI_API_KEY =
  process.env.GEMINI_API_KEY ??
  process.env.GOOGLE_AI_API_KEY ??
  process.env.AI_INTEGRATIONS_GEMINI_API_KEY ??
  "";

const PERPLEXITY_API_KEY = process.env.PERPLEXITY_API_KEY ?? "";

if (!GEMINI_API_KEY) {
  // eslint-disable-next-line no-console
  console.warn(
    "[ai] No Gemini API key found. Set GEMINI_API_KEY (or GOOGLE_AI_API_KEY) — AI features will return errors until configured.",
  );
}

// ─── Gemini client ────────────────────────────────────────────────────────────
export const gemini = new GoogleGenAI({ apiKey: GEMINI_API_KEY || "dummy" });

// Model selection — current latest Gemini text/multimodal models
export const GEMINI_TEXT_MODEL = process.env.GEMINI_TEXT_MODEL ?? "gemini-2.5-flash";
export const GEMINI_PRO_MODEL = process.env.GEMINI_PRO_MODEL ?? "gemini-2.5-pro";
export const GEMINI_IMAGE_MODEL =
  process.env.GEMINI_IMAGE_MODEL ?? "gemini-2.5-flash-image";

/**
 * Sleep helper for retry backoff.
 */
function sleep(ms: number): Promise<void> {
  return new Promise((r) => setTimeout(r, ms));
}

/**
 * Detect transient Gemini errors that should be retried (503 UNAVAILABLE,
 * 429 quota, network blips). Returns true if we should retry.
 */
function isTransientGeminiError(err: unknown): boolean {
  const msg = (err instanceof Error ? err.message : String(err || "")).toLowerCase();
  return (
    msg.includes("503") ||
    msg.includes("unavailable") ||
    msg.includes("overloaded") ||
    msg.includes("high demand") ||
    msg.includes("429") ||
    msg.includes("rate limit") ||
    msg.includes("deadline") ||
    msg.includes("timeout") ||
    msg.includes("econnreset") ||
    msg.includes("socket hang up") ||
    msg.includes("fetch failed")
  );
}

/**
 * Errors that should trigger — not retry on the same model — immediate fallback
 * to the next model in the chain (e.g. model deprecated, 404, permission denied).
 */
function isModelLevelError(err: unknown): boolean {
  const msg = (err instanceof Error ? err.message : String(err || "")).toLowerCase();
  return (
    msg.includes("404") ||
    msg.includes("not_found") ||
    msg.includes("no longer available") ||
    msg.includes("is not found") ||
    msg.includes("permission_denied") ||
    msg.includes("unsupported")
  );
}

/**
 * Fallback chain of models tried in order when the primary returns transient errors
 * (503/UNAVAILABLE/overloaded). Each tier has its own internal retry loop.
 */
function buildModelChain(primary: string): string[] {
  const chain: string[] = [primary];
  // Fall back to faster/lighter flash variants when primary is overloaded
  if (!chain.includes("gemini-2.5-flash")) chain.push("gemini-2.5-flash");
  if (!chain.includes("gemini-2.5-flash-lite")) chain.push("gemini-2.5-flash-lite");
  if (!chain.includes("gemini-flash-latest")) chain.push("gemini-flash-latest");
  return chain;
}

/**
 * Generate plain text from a single user prompt. Auto-retries transient errors
 * (503/UNAVAILABLE, 429, network) with exponential backoff per model, then
 * falls back to alternative Gemini models if all retries fail.
 */
export async function geminiText(
  prompt: string,
  opts: { model?: string; maxTokens?: number; system?: string } = {},
): Promise<string> {
  const primary = opts.model ?? GEMINI_TEXT_MODEL;
  const chain = buildModelChain(primary);
  const attemptsPerModel = 2; // 2 attempts per model, then fall back
  let lastErr: unknown = null;
  for (let mi = 0; mi < chain.length; mi++) {
    const model = chain[mi];
    for (let attempt = 1; attempt <= attemptsPerModel; attempt++) {
      try {
        const result = await gemini.models.generateContent({
          model,
          contents: [
            ...(opts.system
              ? [{ role: "user", parts: [{ text: opts.system }] }]
              : []),
            { role: "user", parts: [{ text: prompt }] },
          ],
          config: {
            maxOutputTokens: opts.maxTokens ?? 2048,
            temperature: 0.7,
          },
        });
        if (mi > 0 || attempt > 1) {
          // eslint-disable-next-line no-console
          console.log(`[ai] geminiText recovered with model=${model} attempt=${attempt}`);
        }
        return result.text ?? "";
      } catch (err) {
        lastErr = err;
        const transient = isTransientGeminiError(err);
        const modelErr = isModelLevelError(err);
        if (!transient && !modelErr) throw err;
        // Model-level error → jump immediately to next model
        if (modelErr) {
          // eslint-disable-next-line no-console
          console.warn(
            `[ai] geminiText: model=${model} unavailable (${err instanceof Error ? err.message.slice(0, 120) : String(err)}), falling back`,
          );
          break; // breaks inner attempt loop → goes to next model
        }
        if (attempt < attemptsPerModel) {
          const delay = 1200 * attempt + Math.floor(Math.random() * 400);
          // eslint-disable-next-line no-console
          console.warn(
            `[ai] geminiText transient on model=${model} (attempt ${attempt}/${attemptsPerModel}), retry in ${delay}ms`,
          );
          await sleep(delay);
        } else if (mi < chain.length - 1) {
          // eslint-disable-next-line no-console
          console.warn(
            `[ai] geminiText: model=${model} exhausted, falling back to ${chain[mi + 1]}`,
          );
          await sleep(800);
        }
      }
    }
  }
  throw lastErr ?? new Error("geminiText: all models exhausted");
}

/**
 * Generate strict JSON from a prompt. Strips markdown fences automatically.
 */
export async function geminiJSON<T = unknown>(
  prompt: string,
  opts: { model?: string; maxTokens?: number; system?: string } = {},
): Promise<T> {
  const sysPrefix =
    "أجب فقط بـ JSON صحيح بدون أي نص إضافي أو markdown أو شروحات. ";
  const maxParseAttempts = 3;
  let lastErr: unknown = null;
  for (let attempt = 1; attempt <= maxParseAttempts; attempt++) {
    let text = "";
    try {
      text = await geminiText(prompt, {
        ...opts,
        system: (opts.system ? opts.system + "\n" : "") + sysPrefix,
      });
      const clean = text
        .replace(/^```(?:json)?\s*/i, "")
        .replace(/\s*```$/i, "")
        .trim();
      const start = clean.search(/[\[{]/);
      if (start === -1) throw new Error("Gemini returned no JSON");
      // Find matching closing bracket — Gemini sometimes appends commentary
      const body = clean.slice(start);
      try {
        return JSON.parse(body) as T;
      } catch {
        // Try trimming to last } or ]
        const lastBrace = Math.max(body.lastIndexOf("}"), body.lastIndexOf("]"));
        if (lastBrace > 0) {
          return JSON.parse(body.slice(0, lastBrace + 1)) as T;
        }
        throw new Error("AI response was not valid JSON");
      }
    } catch (err) {
      lastErr = err;
      // Retry on transient errors OR on JSON parse failures (Gemini truncation/noise)
      const msg = err instanceof Error ? err.message : String(err);
      const isParseError = msg.includes("JSON") || msg.includes("valid JSON");
      if (attempt < maxParseAttempts && (isTransientGeminiError(err) || isParseError)) {
        const delay = 1200 * attempt;
        // eslint-disable-next-line no-console
        console.warn(
          `[ai] geminiJSON ${isParseError ? "parse" : "transient"} error (attempt ${attempt}/${maxParseAttempts}), retrying in ${delay}ms`,
        );
        await sleep(delay);
        continue;
      }
      throw err;
    }
  }
  throw lastErr ?? new Error("geminiJSON: unknown error");
}

// ─── Anthropic-compatible adapter ─────────────────────────────────────────────
// Mimics the shape of `await anthropic.messages.create({...})` returning `.content[0].text`
export const anthropicAdapter = {
  messages: {
    create: async (params: {
      model?: string;
      max_tokens?: number;
      messages: { role: string; content: string }[];
    }) => {
      const userMsg = params.messages.find((m) => m.role === "user");
      const prompt = userMsg?.content ?? "";
      const text = await geminiText(prompt, {
        maxTokens: params.max_tokens,
        model: GEMINI_TEXT_MODEL,
      });
      return {
        content: [{ type: "text", text }],
      };
    },
    stream: (params: {
      model?: string;
      max_tokens?: number;
      messages: { role: string; content: string }[];
    }) => {
      const userMsg = params.messages.find((m) => m.role === "user");
      const prompt = userMsg?.content ?? "";
      // Build an async iterator that emits text_delta events compatible with old code
      async function* iter() {
        const stream = await gemini.models.generateContentStream({
          model: GEMINI_TEXT_MODEL,
          contents: [{ role: "user", parts: [{ text: prompt }] }],
          config: { maxOutputTokens: params.max_tokens ?? 2048 },
        });
        for await (const chunk of stream) {
          const t = chunk.text;
          if (t)
            yield {
              type: "content_block_delta",
              delta: { type: "text_delta", text: t },
            };
        }
      }
      return iter();
    },
  },
};

// ─── OpenAI-compatible adapter ────────────────────────────────────────────────
export const openaiAdapter = {
  chat: {
    completions: {
      create: async (params: {
        model?: string;
        max_tokens?: number;
        max_completion_tokens?: number;
        messages: { role: string; content: string }[];
        stream?: boolean;
      }) => {
        const userMsg = [...params.messages].reverse().find((m) => m.role === "user");
        const sysMsg = params.messages.find((m) => m.role === "system");
        const prompt = userMsg?.content ?? "";
        const maxTokens = params.max_completion_tokens ?? params.max_tokens ?? 2048;

        if (params.stream) {
          async function* iter() {
            const stream = await gemini.models.generateContentStream({
              model: GEMINI_TEXT_MODEL,
              contents: [{ role: "user", parts: [{ text: prompt }] }],
              config: {
                maxOutputTokens: maxTokens,
                ...(sysMsg ? { systemInstruction: sysMsg.content } : {}),
              },
            });
            for await (const chunk of stream) {
              const t = chunk.text;
              if (t) yield { choices: [{ delta: { content: t } }] };
            }
          }
          return iter();
        }

        const text = await geminiText(prompt, {
          maxTokens,
          system: sysMsg?.content,
          model: GEMINI_TEXT_MODEL,
        });
        return { choices: [{ message: { content: text } }] };
      },
    },
  },
  embeddings: {
    create: async (_params: { model?: string; input: string | string[] }) => {
      // Gemini has its own embedding model; we expose a minimal compatible shape.
      const inputs = Array.isArray(_params.input) ? _params.input : [_params.input];
      const data: { embedding: number[] }[] = [];
      for (const t of inputs) {
        // Use Gemini text-embedding model
        const resp = await gemini.models.embedContent({
          model: "gemini-embedding-001",
          contents: [{ role: "user", parts: [{ text: t }] }],
        });
        const emb = resp.embeddings?.[0]?.values ?? [];
        data.push({ embedding: emb });
      }
      return { data };
    },
  },
};

// ─── Perplexity (web search) ──────────────────────────────────────────────────
export async function perplexitySearch(
  prompt: string,
  opts: { model?: string; maxTokens?: number } = {},
): Promise<string> {
  if (!PERPLEXITY_API_KEY) {
    throw new Error("PERPLEXITY_API_KEY is not configured");
  }
  const response = await fetch("https://api.perplexity.ai/chat/completions", {
    method: "POST",
    headers: {
      Authorization: `Bearer ${PERPLEXITY_API_KEY}`,
      "Content-Type": "application/json",
    },
    body: JSON.stringify({
      model: opts.model ?? "sonar-pro",
      messages: [
        {
          role: "system",
          content:
            "أنت محلل بيانات متخصص. أرجع دائماً JSON منظم صالح فقط بدون أي نص إضافي أو markdown.",
        },
        { role: "user", content: prompt },
      ],
      max_tokens: opts.maxTokens ?? 4000,
    }),
    signal: AbortSignal.timeout(40_000),
  });
  if (!response.ok) {
    const err = await response.text();
    throw new Error(`Perplexity error ${response.status}: ${err.slice(0, 200)}`);
  }
  const data = (await response.json()) as {
    choices: { message: { content: string } }[];
  };
  return data.choices[0]?.message?.content ?? "";
}

export const hasGemini = !!GEMINI_API_KEY;
export const hasPerplexity = !!PERPLEXITY_API_KEY;
