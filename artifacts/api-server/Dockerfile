# ─── ICBank API Server — Production Dockerfile ─────────────────────────────
# Multi-stage build: install dev deps in builder, copy only dist + prod deps
# into the runtime image.

# ─── Stage 1: build ────────────────────────────────────────────────────────
FROM node:22-slim AS builder
WORKDIR /app

# pnpm via corepack (matches monorepo lockfile)
RUN corepack enable && corepack prepare pnpm@9 --activate

# Copy workspace manifests first for layer-caching
COPY package.json pnpm-lock.yaml pnpm-workspace.yaml ./
COPY tsconfig.base.json tsconfig.json ./
COPY artifacts/api-server/package.json ./artifacts/api-server/
COPY lib ./lib

RUN pnpm install --frozen-lockfile --filter "@workspace/api-server..."

# Copy source and build
COPY artifacts/api-server ./artifacts/api-server
RUN pnpm --filter @workspace/api-server build

# Prune dev deps for the runtime image
RUN pnpm --filter @workspace/api-server deploy --prod --legacy /prod/api-server

# ─── Stage 2: runtime ──────────────────────────────────────────────────────
FROM node:22-slim AS runtime
WORKDIR /app

ENV NODE_ENV=production
ENV PORT=3000

COPY --from=builder /prod/api-server ./
EXPOSE 3000

CMD ["node", "--enable-source-maps", "./dist/index.mjs"]
