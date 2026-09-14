#!/bin/sh
set -eu
cd "$(dirname "$0")"
GODOT_MONO="${GODOT_MONO:-/Applications/Godot_mono.app/Contents/MacOS/Godot}"
dotnet build Hoshikawa.csproj --nologo
"$GODOT_MONO" --headless --editor --path . --import
exec "$GODOT_MONO" --path . "$@"
