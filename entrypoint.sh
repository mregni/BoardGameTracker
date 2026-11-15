#!/bin/sh
set -e

PUID=${PUID:-1654}
PGID=${PGID:-1654}

case "$PUID" in
    ''|*[!0-9]*) echo "ERROR: PUID must be a number, got '$PUID'" >&2; exit 1 ;;
esac
case "$PGID" in
    ''|*[!0-9]*) echo "ERROR: PGID must be a number, got '$PGID'" >&2; exit 1 ;;
esac

if [ "$(id -u)" != "0" ]; then
    echo "Starting as UID: $(id -u), GID: $(id -g); PUID and PGID are ignored when the container does not run as root."
    unwritable=""
    for dir in /app/images /app/logs /app/manuals; do
        [ -w "$dir" ] || unwritable="$unwritable $dir"
    done
    if [ -n "$unwritable" ]; then
        echo "ERROR: not writable by UID $(id -u):$unwritable" >&2
        echo "Give the mounted host folders to this user, for example: chown -R $(id -u):$(id -g) images logs manuals" >&2
        exit 1
    fi
    exec dotnet BoardGameTracker.Host.dll
fi

echo "Starting with UID: $PUID, GID: $PGID"

group_name=$(getent group "$PGID" | cut -d: -f1)
if [ -z "$group_name" ]; then
    group_name="appgroup$PGID"
    addgroup -g "$PGID" "$group_name"
fi

user_name=$(getent passwd "$PUID" | cut -d: -f1)
if [ -z "$user_name" ]; then
    user_name="appuser$PUID"
    adduser -u "$PUID" -G "$group_name" -D -H "$user_name"
fi

# Only touch files that are not owned by the configured user yet; a failure (read-only or
# network mount) is reported but does not stop the application.
for dir in /app/images /app/logs /app/manuals; do
    if ! find "$dir" \( ! -user "$PUID" -o ! -group "$PGID" \) -exec chown "$PUID:$PGID" {} + 2>/dev/null; then
        echo "WARN: could not change the owner of $dir to $PUID:$PGID; make sure the mounted folder is writable by that user" >&2
    fi
done

exec su-exec "$PUID:$PGID" dotnet BoardGameTracker.Host.dll
