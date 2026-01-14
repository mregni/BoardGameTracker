# Multi-architecture Dockerfile for BoardGameTracker
# Supports: linux/amd64, linux/arm64, linux/arm/v7
#
# Platforms supported:
# - Debian/Ubuntu x64 (linux/amd64)
# - macOS Intel (linux/amd64)
# - macOS Apple Silicon M1/M2/M3 (linux/arm64)
# - Raspberry Pi 4 (linux/arm/v7 or linux/arm64)
# - Raspberry Pi 5 (linux/arm64)
# - TrueNAS (linux/amd64)
# - Synology NAS (linux/amd64 or linux/arm64)
# - Unraid (linux/amd64)
#
# Build with Docker Compose:
# docker-compose -f docker-compose.build.yml build
#
# Or build for multiple platforms with Docker Buildx:
# docker buildx create --name multibuilder --use
# docker buildx build --platform linux/amd64,linux/arm64,linux/arm/v7 \
#   --build-arg VERSION=1.0.0 \
#   -t uping/boardgametracker:latest \
#   --push .
#
# Run example:
# docker run -e DB_HOST=db -e DB_USER=dev -e DB_PASSWORD=dev -e DB_NAME=boardgametracker \
#   -p 5444:5444 \
#   -v ./data:/app/data \
#   -v ./images:/app/images \
#   -v ./logs:/app/logs \
#   boardgametracker:latest

# Build arguments for multi-arch support
ARG BUILDPLATFORM
ARG TARGETPLATFORM
ARG TARGETOS
ARG VERSION=0.0.1

# Stage 1: Build
FROM --platform=$BUILDPLATFORM mcr.microsoft.com/dotnet/sdk:8.0 AS build
ARG VERSION
WORKDIR /src

# Install Node.js (required for frontend build via .esproj)
RUN apt-get update && \
    apt-get install -y curl && \
    curl -fsSL https://deb.nodesource.com/setup_20.x | bash - && \
    apt-get install -y nodejs && \
    rm -rf /var/lib/apt/lists/*

# Copy Directory.Build.props (affects MSBuild behavior)
COPY Directory.Build.props Directory.Build.props

# Copy project files
COPY BoardGameTracker.Common/BoardGameTracker.Common.csproj BoardGameTracker.Common/
COPY BoardGameTracker.Core/BoardGameTracker.Core.csproj BoardGameTracker.Core/
COPY BoardGameTracker.Api/BoardGameTracker.Api.csproj BoardGameTracker.Api/
COPY BoardGameTracker.Host/BoardGameTracker.Host.csproj BoardGameTracker.Host/
COPY boardgametracker.client/boardgametracker.client.esproj boardgametracker.client/
COPY boardgametracker.client/package*.json boardgametracker.client/

# Restore dependencies
RUN dotnet restore BoardGameTracker.Host/BoardGameTracker.Host.csproj

# Copy source code (only what's needed for build)
COPY BoardGameTracker.Common/ BoardGameTracker.Common/
COPY BoardGameTracker.Core/ BoardGameTracker.Core/
COPY BoardGameTracker.Api/ BoardGameTracker.Api/
COPY BoardGameTracker.Host/ BoardGameTracker.Host/
COPY boardgametracker.client/ boardgametracker.client/

# Build and publish
WORKDIR /src/BoardGameTracker.Host
RUN dotnet publish -c Release -o /app/publish \
    --no-restore \
    /p:Version=${VERSION}

# Stage 2: Runtime
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS runtime

# Build arguments for runtime configuration
ARG ASPNETCORE_ENVIRONMENT=production
ARG ASPNETCORE_URLS=http://*:5444
ARG TZ=UTC

WORKDIR /app

# Install curl for healthcheck
RUN apt-get update && \
    apt-get install -y curl && \
    rm -rf /var/lib/apt/lists/*

# Copy published files from build stage
COPY --from=build /app/publish .

# Create directories for data and images
RUN mkdir -p /app/data /app/images /app/logs

# Set environment variables with defaults from build args
ENV ASPNETCORE_ENVIRONMENT=${ASPNETCORE_ENVIRONMENT}
ENV DOTNET_EnableDiagnostics=0
ENV ASPNETCORE_URLS=${ASPNETCORE_URLS}
ENV TZ=${TZ}

# Expose port
EXPOSE 5444

# Health check
HEALTHCHECK --interval=30s --timeout=5s --start-period=10s --retries=3 \
    CMD curl --fail http://localhost:5444/api/health || exit 1

CMD ["dotnet", "BoardGameTracker.Host.dll"]
