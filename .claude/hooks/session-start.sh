#!/bin/bash
# Cloud-session setup: .NET 8, Godot 4.6.2 Mono, Blender, just, software Vulkan and xvfb for captures.
set -euo pipefail
[ "${CLAUDE_CODE_REMOTE:-}" = "true" ] || exit 0

GODOT_VERSION=4.6.2
GODOT_DIR=/opt/godot-mono-$GODOT_VERSION
GODOT_BIN=$GODOT_DIR/Godot_v${GODOT_VERSION}-stable_mono_linux.x86_64
PKGS="dotnet-sdk-8.0 blender just mesa-vulkan-drivers xvfb fonts-noto-cjk python3-numpy unzip"

# dot.net and blender.org are blocked by the proxy; Ubuntu's archive carries .NET 8 and Blender 4.0.
missing=$(for p in $PKGS; do dpkg -s "$p" >/dev/null 2>&1 || echo "$p"; done)
if [ -n "$missing" ]; then
  export DEBIAN_FRONTEND=noninteractive
  apt-get update -qq
  apt-get install -y -qq --no-install-recommends $missing >/dev/null
fi

if [ ! -x "$GODOT_BIN" ]; then
  tmp=$(mktemp -d)
  curl -fsSL "https://github.com/godotengine/godot/releases/download/${GODOT_VERSION}-stable/Godot_v${GODOT_VERSION}-stable_mono_linux_x86_64.zip" -o "$tmp/godot.zip"
  unzip -q "$tmp/godot.zip" -d "$tmp"
  rm -rf "$GODOT_DIR"
  mv "$tmp/Godot_v${GODOT_VERSION}-stable_mono_linux_x86_64" "$GODOT_DIR"
  rm -rf "$tmp"
fi
ln -sf "$GODOT_BIN" /usr/local/bin/godot-mono

if [ -n "${CLAUDE_ENV_FILE:-}" ]; then
  {
    echo "export GODOT_MONO=$GODOT_BIN"
    echo "export GODOT_BIN=$GODOT_BIN"
    echo "export DOTNET_CLI_TELEMETRY_OPTOUT=1"
    echo "export DOTNET_NOLOGO=1"
  } >> "$CLAUDE_ENV_FILE"
fi

cd "$CLAUDE_PROJECT_DIR"
dotnet restore Hoshikawa.csproj --nologo -v q
dotnet restore tests/unit/Hoshikawa.UnitTests.csproj --nologo -v q
