#!/bin/sh
set -e

PUID=${PUID:-1654}
PGID=${PGID:-1654}

echo "Starting with UID: $PUID, GID: $PGID"

if ! getent group appgroup > /dev/null 2>&1; then
    addgroup -g "$PGID" appgroup
fi
if [ "$(id -u)" != "0" ]; then
    echo "Not running as root (UID $(id -u)); skipping user/group setup and privilege drop."
    exec dotnet BoardGameTracker.Host.dll
fi

group_name=$(getent group "$PGID" | cut -d: -f1)
if [ -z "$group_name" ]; then
    group_name=appgroup
    addgroup -g "$PGID" "$group_name"
fi

user_name=$(getent passwd "$PUID" | cut -d: -f1)
if [ -z "$user_name" ]; then
    user_name=appuser
    adduser -u "$PUID" -G "$group_name" -D -H "$user_name"
fi

chown -R "$PUID:$PGID" /app/images /app/logs /app/manuals
exec su-exec "$PUID:$PGID" dotnet BoardGameTracker.Host.dll