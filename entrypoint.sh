#!/bin/sh

PUID=${PUID:-1654}
PGID=${PGID:-1654}

echo "Starting with UID: $PUID, GID: $PGID"

if ! getent group appgroup > /dev/null 2>&1; then
    addgroup -g "$PGID" appgroup
fi

if ! getent passwd appuser > /dev/null 2>&1; then
    adduser -u "$PUID" -G appgroup -D -H appuser
fi

chown -R "$PUID:$PGID" /app/data /app/images /app/logs /app/config
exec su-exec "$PUID:$PGID" dotnet BoardGameTracker.Host.dll