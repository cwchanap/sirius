# Hoshikawa task runner. Run `just` (no args) to list recipes.

set positional-arguments

# Mono-enabled Godot binary; override with `GODOT_MONO=... just run`
godot := env("GODOT_MONO", "/Applications/Godot_mono.app/Contents/MacOS/Godot")

# Pinned godot-e2e release fetched by `just e2e`; keep in step with GODOT_E2E_VERSION in .github/workflows/e2e.yml
e2e_addon_version := "0.1.2"

# List available recipes (default)
default:
    @just --list

# Build C#, import assets, launch. Extra args go to Godot, e.g. `just run -- --play`
run *args:
    dotnet build Hoshikawa.csproj --nologo
    "{{godot}}" --headless --editor --path . --import
    exec "{{godot}}" --path . "$@"

# gdUnit4Net unit tests (tests/unit); plain .NET, no Godot needed. Extra args go to `dotnet test`
unit *args:
    dotnet test tests/unit/Hoshikawa.UnitTests.csproj --nologo "$@"

# The in-engine self-test (scripts/IntegrationCheck.cs), headless
self-test:
    dotnet build Hoshikawa.csproj --nologo
    "{{godot}}" --headless --editor --path . --import
    "{{godot}}" --headless --path . --fixed-fps 60 -- --self-test

# Fetch the pinned godot-e2e release zip into addons/gdunit_e2e (not versioned)
fetch-e2e:
    #!/usr/bin/env bash
    set -euo pipefail
    if [ "$(cat addons/gdunit_e2e/.e2e-version 2>/dev/null)" = "{{e2e_addon_version}}" ]; then
        exit 0
    fi
    tmp=$(mktemp -d)
    trap 'rm -rf "$tmp"' EXIT
    curl -fsSL "https://github.com/cwchanap/godot-e2e/releases/download/v{{e2e_addon_version}}/godot-e2e-{{e2e_addon_version}}.zip" -o "$tmp/godot-e2e.zip"
    unzip -q "$tmp/godot-e2e.zip" -d "$tmp/unpack"
    rm -rf addons/gdunit_e2e
    mkdir -p addons
    cp -R "$tmp/unpack/addons/gdunit_e2e" addons/gdunit_e2e
    cp "$tmp/unpack/LICENSE" "$tmp/unpack/NOTICE" addons/gdunit_e2e/
    echo "{{e2e_addon_version}}" > addons/gdunit_e2e/.e2e-version

# godot-e2e tests (tests/e2e) against a headless game process; failure artifacts land in test_output/
e2e *args: fetch-e2e
    dotnet build Hoshikawa.csproj --nologo
    "{{godot}}" --headless --editor --path . --import
    rm -rf test_output
    GODOT_BIN="{{godot}}" dotnet test tests/e2e/Hoshikawa.E2E.csproj --nologo "$@"
