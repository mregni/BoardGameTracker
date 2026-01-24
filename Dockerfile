# Build arguments for multi-arch support
ARG BUILDPLATFORM
ARG TARGETPLATFORM
ARG TARGETOS
ARG VERSION=0.0.1

# Stage 1: Build Frontend
FROM node:22-alpine AS frontend-build
WORKDIR /src

# Copy frontend package files
COPY boardgametracker.client/package*.json ./
RUN npm ci

# Copy frontend source
COPY boardgametracker.client/ ./

# Build frontend
RUN npm run build

# Stage 2: Build Backend
FROM mcr.microsoft.com/dotnet/sdk:8.0-alpine AS backend-build
ARG VERSION
WORKDIR /src

# Copy Directory.Build.props (affects MSBuild behavior)
COPY Directory.Build.props ./

# Copy project files for restore
COPY BoardGameTracker.Common/BoardGameTracker.Common.csproj BoardGameTracker.Common/
COPY BoardGameTracker.Core/BoardGameTracker.Core.csproj BoardGameTracker.Core/
COPY BoardGameTracker.Api/BoardGameTracker.Api.csproj BoardGameTracker.Api/
COPY BoardGameTracker.Host/BoardGameTracker.Host.csproj BoardGameTracker.Host/

# Restore dependencies
RUN dotnet restore BoardGameTracker.Host/BoardGameTracker.Host.csproj

# Copy source code
COPY BoardGameTracker.Common/ BoardGameTracker.Common/
COPY BoardGameTracker.Core/ BoardGameTracker.Core/
COPY BoardGameTracker.Api/ BoardGameTracker.Api/
COPY BoardGameTracker.Host/ BoardGameTracker.Host/

# Copy frontend build output to wwwroot
COPY --from=frontend-build /src/dist BoardGameTracker.Host/wwwroot

# Build and publish backend
WORKDIR /src/BoardGameTracker.Host
RUN ASSEMBLY_VERSION=$(echo "${VERSION}" | cut -d'-' -f1) && \
    dotnet publish \
    -c Release \
    -o /app/publish \
    --no-restore \
    /p:Version=${ASSEMBLY_VERSION} \
    /p:BuildWithoutEsproj=true

# Stage 3: Runtime
FROM mcr.microsoft.com/dotnet/aspnet:8.0-alpine AS runtime

# Build arguments for runtime configuration
ARG ASPNETCORE_ENVIRONMENT=production
ARG ASPNETCORE_URLS=http://*:5444

WORKDIR /app

RUN apk add --no-cache curl su-exec

RUN mkdir -p /app/data /app/images /app/logs /app/config

# Copy published files from backend build stage
COPY --from=backend-build /app/publish .

# Copy entrypoint script
COPY entrypoint.sh /entrypoint.sh
RUN chmod +x /entrypoint.sh

# Set environment variables
ENV ASPNETCORE_ENVIRONMENT=${ASPNETCORE_ENVIRONMENT}
ENV DOTNET_EnableDiagnostics=0
ENV ASPNETCORE_URLS=${ASPNETCORE_URLS}

# Default PUID/PGID (can be overridden at runtime)
ENV PUID=1654
ENV PGID=1654

# Expose port
EXPOSE 5444

# Health check
HEALTHCHECK --interval=30s --timeout=5s --start-period=10s --retries=3 \
    CMD curl --fail http://localhost:5444/api/health || exit 1

ENTRYPOINT ["/entrypoint.sh"]