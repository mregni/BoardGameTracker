#!/bin/sh
set -e

echo "Starting as UID: $(id -u), GID: $(id -g)"

unwritable=""
for dir in /app/images /app/logs /app/manuals; do
    [ -w "$dir" ] || unwritable="$unwritable $dir"
done
if [ -n "$unwritable" ]; then
    echo "ERROR: not writable by UID $(id -u):$unwritable" >&2
    echo "Run the container as the owner of those folders (compose user: or docker run --user), or give the folders to this user: chown -R $(id -u):$(id -g) images logs manuals" >&2
    exit 1
fi

exec dotnet BoardGameTracker.Host.dll
