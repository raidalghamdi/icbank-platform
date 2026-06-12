# ─── ICBank API Server — Production Dockerfile ─────────────────────────────
# Multi-stage build: install dev deps in builder, copy only dist + prod deps
# into the runtime image.

# ─── Stage 1: build ────────────────────────────────────────────────────────
FROM node:22-slim AS builder
WORKDIR /app

# pnpm via corepack (matches monorepo lockfile)
RUN corepack enable && corepack prepare pnpm@9 --activate

# Use system chromium installed in runtime stage (avoids large download in builder)
ENV PUPPETEER_SKIP_DOWNLOAD=true
ENV PUPPETEER_SKIP_CHROMIUM_DOWNLOAD=true

# Copy workspace manifests first for layer-caching
COPY package.json pnpm-lock.yaml pnpm-workspace.yaml .npmrc ./
COPY tsconfig.base.json tsconfig.json ./
COPY artifacts/api-server/package.json ./artifacts/api-server/
COPY lib ./lib

RUN pnpm install --frozen-lockfile --filter "@workspace/api-server..."

# Copy source and build
COPY artifacts/api-server ./artifacts/api-server
RUN pnpm --filter @workspace/api-server build

# Prune dev deps for the runtime image
RUN pnpm --filter @workspace/api-server deploy --prod /prod/api-server

# ─── Stage 2: runtime ──────────────────────────────────────────────────────
FROM node:22-slim AS runtime
WORKDIR /app

ENV NODE_ENV=production
ENV PORT=3000

# Arabic + Latin fonts for the sharp/Pango composer (server-side design render).
# PLUS: Chromium + shared libs required by Puppeteer for PDF export.
# Without libnss3/libxss1/libasound2 etc, /api/media-reports/:id/pdf returns code 127.
RUN apt-get update \
 && apt-get install -y --no-install-recommends \
      fontconfig \
      fonts-noto-core \
      fonts-noto-color-emoji \
      fonts-dejavu-core \
      chromium \
      libnss3 \
      libxss1 \
      libasound2 \
      libatk1.0-0 \
      libatk-bridge2.0-0 \
      libcups2 \
      libgbm1 \
      libgtk-3-0 \
      libxshmfence1 \
      libdrm2 \
      libxcomposite1 \
      libxdamage1 \
      libxfixes3 \
      libxrandr2 \
      libpango-1.0-0 \
      libpangocairo-1.0-0 \
      ca-certificates \
      dumb-init \
 && fc-cache -f \
 && rm -rf /var/lib/apt/lists/*

# Tell Puppeteer to use the system Chromium binary
ENV PUPPETEER_EXECUTABLE_PATH=/usr/bin/chromium
ENV PUPPETEER_SKIP_DOWNLOAD=true

COPY --from=builder /prod/api-server ./

# Copy bundled assets (GAC publication PDFs, etc.) — these sit beside
# package.json in the source tree and are loaded at runtime from
# process.cwd()/assets/... by the gac reseed endpoints.
COPY --from=builder /app/artifacts/api-server/assets ./assets

EXPOSE 3000

CMD ["node", "--enable-source-maps", "./dist/index.mjs"]
