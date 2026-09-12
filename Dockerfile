# syntax=docker/dockerfile:1
ARG VERSION=0.0.1

# Stage 1: Build Frontend
FROM --platform=$BUILDPLATFORM node:22-alpine AS frontend-build
ARG VITE_SENTRY_DSN
WORKDIR /src

# Enable pnpm via corepack
ENV COREPACK_ENABLE_DOWNLOAD_PROMPT=0
RUN corepack enable

# Copy frontend package files
COPY boardgametracker.client/package.json boardgametracker.client/pnpm-lock.yaml ./
RUN --mount=type=cache,id=pnpm-store,target=/root/.local/share/pnpm/store \
    pnpm config set store-dir /root/.local/share/pnpm/store && \
    pnpm install --frozen-lockfile --ignore-scripts

# Copy frontend source
COPY boardgametracker.client/ ./

# Build frontend (the sentry_auth_token secret enables sourcemap upload via @sentry/vite-plugin)
ENV VITE_SENTRY_DSN=${VITE_SENTRY_DSN}
RUN --mount=type=secret,id=sentry_auth_token,env=SENTRY_AUTH_TOKEN pnpm build

# Stage 2: Build Backend (runs natively on the build host; the framework-dependent output is architecture-neutral)
FROM --platform=$BUILDPLATFORM mcr.microsoft.com/dotnet/sdk:10.0-alpine AS backend-build
ARG VERSION
WORKDIR /src

# Copy Directory.Build.props (affects MSBuild behavior)
COPY Directory.Build.props ./

# Copy project files and lock files for restore
COPY BoardGameTracker.Common/BoardGameTracker.Common.csproj BoardGameTracker.Common/packages.lock.json BoardGameTracker.Common/
COPY BoardGameTracker.Core/BoardGameTracker.Core.csproj BoardGameTracker.Core/packages.lock.json BoardGameTracker.Core/
COPY BoardGameTracker.Api/BoardGameTracker.Api.csproj BoardGameTracker.Api/packages.lock.json BoardGameTracker.Api/
COPY BoardGameTracker.Host/BoardGameTracker.Host.csproj BoardGameTracker.Host/packages.lock.json BoardGameTracker.Host/

# Restore dependencies (exact versions from the lock files)
RUN --mount=type=cache,id=nuget,target=/root/.nuget/packages \
    dotnet restore BoardGameTracker.Host/BoardGameTracker.Host.csproj --locked-mode

# Copy source code
COPY BoardGameTracker.Common/ BoardGameTracker.Common/
COPY BoardGameTracker.Core/ BoardGameTracker.Core/
COPY BoardGameTracker.Api/ BoardGameTracker.Api/
COPY BoardGameTracker.Host/ BoardGameTracker.Host/

# Copy frontend build output to wwwroot
COPY --from=frontend-build /src/dist BoardGameTracker.Host/wwwroot

# Build and publish backend
WORKDIR /src/BoardGameTracker.Host
RUN --mount=type=cache,id=nuget,target=/root/.nuget/packages \
    ASSEMBLY_VERSION=$(echo "${VERSION}" | cut -d'-' -f1) && \
    dotnet publish \
    -c Release \
    -o /app/publish \
    --no-restore \
    /p:Version=${ASSEMBLY_VERSION} \
    /p:InformationalVersion=${VERSION} \
    /p:UseAppHost=false \
    /p:BuildWithoutEsproj=true

# Stage 3: Runtime
FROM mcr.microsoft.com/dotnet/aspnet:10.0-alpine AS runtime

# Build arguments for runtime configuration
ARG ASPNETCORE_ENVIRONMENT=production
ARG ASPNETCORE_HTTP_PORTS=5444

WORKDIR /app

RUN apk upgrade --no-cache && apk add --no-cache curl su-exec poppler-utils tzdata krb5-libs

RUN mkdir -p /app/images /app/logs /app/manuals

# Copy published files from backend build stage
COPY --from=backend-build /app/publish .

# Copy entrypoint script
COPY entrypoint.sh /entrypoint.sh
RUN chmod +x /entrypoint.sh

# Set environment variables
ENV ASPNETCORE_ENVIRONMENT=${ASPNETCORE_ENVIRONMENT}
ENV DOTNET_EnableDiagnostics=0
ENV ASPNETCORE_HTTP_PORTS=${ASPNETCORE_HTTP_PORTS}

# Default PUID/PGID (can be overridden at runtime)
ENV PUID=1654
ENV PGID=1654

# Expose port
EXPOSE 5444

# Health check (follows the configured port)
HEALTHCHECK --interval=30s --timeout=5s --start-period=10s --retries=3 \
    CMD curl --fail "http://localhost:${ASPNETCORE_HTTP_PORTS%%;*}/api/health" || exit 1

ENTRYPOINT ["/entrypoint.sh"]
